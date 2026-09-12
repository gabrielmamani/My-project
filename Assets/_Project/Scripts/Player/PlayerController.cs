using System;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Managers;

namespace Gunbound.Player
{
    /// <summary>
    /// Controls horizontal movement in 2.5D, turn delay tracking, power bar charging (0 to 100), and firing logic.
    /// Decoupled via C# Actions to maintain clean architecture.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private float _minX = -12.0f;
        [SerializeField] private float _maxX = 12.0f;

        [Header("Combat & Delay Settings")]
        [SerializeField] private TurretAim _turretAim;
        [SerializeField] private GameObject _projectilePrefab;
        [SerializeField] private GameObject _projectilePrefabShot1;
        [SerializeField] private GameObject _projectilePrefabShot2;
        [SerializeField] private GameObject _projectilePrefabShotSS;
        [SerializeField] private WeaponType _currentWeapon = WeaponType.Shot1;
        [SerializeField] private int _baseDelay = 250;
        [SerializeField] private float _delayPerDistanceUnit = 5.0f;
        [SerializeField] private LayerMask _collisionMask = ~0;

        [Header("Power Bar & Shot Charging Settings")]
        [SerializeField] private Slider _powerBar;
        [SerializeField] private float _minPower = 0.0f;
        [SerializeField] private float _maxPower = 100.0f;
        [SerializeField] private float _chargeSpeed = 50.0f;
        [SerializeField] private float _minLaunchForce = 3.0f;
        [SerializeField] private float _maxLaunchForce = 28.0f;

        [Header("VFX & SFX Settings")]
        [SerializeField] private GameObject _muzzleFlashPrefab;

        [Header("Physics & Wind Data")]
        [SerializeField] private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField] private Vector3 _windVector = Vector3.zero;

        [Header("Turn Control Settings")]
        [SerializeField] private bool _isMyTurn = true;

        // Current internal state
        private int _currentDelay = 0;
        private float _unprocessedDistance = 0f;
        private bool _isMoving;
        private int _facingDirection = 1; // 1 for right (+X), -1 for left (-X)

        private float _currentPower;
        private bool _isCharging;
        private bool _isMobileCharging;

        // Tactical Items state
        private bool _isDualActive = false;
        private bool _isTeleportActive = false;

        [Header("Tactical Items Single-Use State")]
        [SerializeField] private bool _isDualUsed = false;
        [SerializeField] private bool _isTeleportUsed = false;
        [SerializeField] private bool _isHealUsed = false;

        [Header("SS Weapon Cooldown Settings")]
        [SerializeField] private int _ssCooldownRounds = 2;
        private int _currentSSCooldown = 0;

        // Events
        public event Action<bool> OnMoveStateChanged;
        public event Action<float, float> OnShotFired; // (power, angle)
        public event Action<int> OnDelayAccumulated;
        public event Action<WeaponType> OnWeaponChanged;
        public event Action<int> OnSSCooldownChanged;
        public event Action<string> OnItemActivated; // ("Dual", "Teleport", or "Heal")

        public bool IsMyTurn => _isMyTurn;
        public int FacingDirection => _facingDirection;
        public int CurrentDelay => _currentDelay;
        public int AccumulatedDelay => _currentDelay;
        public WeaponType CurrentWeapon => _currentWeapon;
        public int CurrentSSCooldown => _currentSSCooldown;
        public bool IsSSReady => _currentSSCooldown <= 0;
        public TurretAim TurretAim => _turretAim;
        public bool IsDualActive => _isDualActive;
        public bool IsTeleportActive => _isTeleportActive;
        public bool IsDualPlusActive => _isDualPlusActive;
        public bool IsDualUsed => _isDualUsed;
        public bool IsTeleportUsed => _isTeleportUsed;
        public bool IsHealUsed => _isHealUsed;
        public bool IsShieldUsed => _isShieldUsed;
        public bool IsChangeWindUsed => _isChangeWindUsed;
        public bool IsDualPlusUsed => _isDualPlusUsed;
        public Slider PowerBar => _powerBar;
        public float MinLaunchForce => _minLaunchForce;
        public float MaxLaunchForce => _maxLaunchForce;

        private bool _isShieldUsed = false;
        private bool _isChangeWindUsed = false;
        private bool _isDualPlusUsed = false;
        private bool _isDualPlusActive = false;
        private GameObject _shieldVisualObj;

        private float _baseScaleX = 1f;
        private Rigidbody2D _rb2d;

        // Drag to Power state
        private bool _isDragPowerActive = false;
        private Vector2 _startDragPos;
        private float _dragSensitivity = 300.0f;

        private void Awake()
        {
            _rb2d = GetComponent<Rigidbody2D>();
            _baseScaleX = Mathf.Abs(transform.localScale.x);
            if (_baseScaleX < 0.001f) _baseScaleX = 1f;

            if (transform.localScale.x < 0f || Mathf.Abs(transform.eulerAngles.y - 180f) < 5f)
            {
                _facingDirection = -1;
            }
            else
            {
                _facingDirection = 1;
            }

            if (_turretAim == null)
            {
                _turretAim = GetComponentInChildren<TurretAim>();
            }

            SetupPowerBar();
            ApplyFacingDirection(_facingDirection);
        }

        /// <summary>
        /// Sets horizontal facing direction (1 for Right, -1 for Left) and flips the vehicle and cannon scale.
        /// </summary>
        public void SetFacingDirection(int direction)
        {
            if (direction == 0) return;
            int targetDir = direction > 0 ? 1 : -1;
            if (_facingDirection != targetDir)
            {
                _facingDirection = targetDir;
                ApplyFacingDirection(_facingDirection);
            }
        }

        /// <summary>
        /// Applies horizontal scale inversion (flip) to the vehicle body, turret, and un-mirrors health canvas.
        /// </summary>
        public void ApplyFacingDirection(int direction)
        {
            _facingDirection = direction > 0 ? 1 : -1;
            float absX = Mathf.Abs(transform.localScale.x);
            if (absX < 0.001f) absX = _baseScaleX > 0.001f ? _baseScaleX : 1f;

            transform.localScale = new Vector3(_facingDirection * absX, transform.localScale.y, transform.localScale.z);

            // Maintain floating health canvas orientation un-mirrored
            Transform healthCanvas = transform.Find("HealthCanvas");
            if (healthCanvas != null)
            {
                Vector3 cScale = healthCanvas.localScale;
                float absCX = Mathf.Abs(cScale.x);
                if (absCX < 0.0001f) absCX = 0.01f;
                healthCanvas.localScale = new Vector3(_facingDirection * absCX, cScale.y, cScale.z);
            }

            if (_turretAim != null)
            {
                _turretAim.UpdateTurretRotation(_facingDirection);
            }
        }

        /// <summary>
        /// Activates or deactivates turn state for this player controller.
        /// When activated, decrements SS weapon cooldown if active.
        /// When deactivated, cancels active power charging and movement.
        /// </summary>
        public void SetTurnActive(bool active)
        {
            _isMyTurn = active;
            if (active)
            {
                DecrementSSCooldown();
            }
            else
            {
                _isCharging = false;
                _isMobileCharging = false;
                Gunbound.Core.AudioManager.Instance?.StopChargeSFX();
                ResetPowerBar();
                if (_isMoving)
                {
                    _isMoving = false;
                    OnMoveStateChanged?.Invoke(false);
                }
            }
        }

        public void DecrementSSCooldown()
        {
            if (_currentSSCooldown > 0)
            {
                _currentSSCooldown--;
                Debug.Log($"[PlayerController] SS Cooldown decremented for {gameObject.name}: {_currentSSCooldown} turns remaining.");
                OnSSCooldownChanged?.Invoke(_currentSSCooldown);
            }
        }

        public void ResetSSCooldown()
        {
            _currentSSCooldown = 0;
            OnSSCooldownChanged?.Invoke(_currentSSCooldown);
        }

        public void ResetDelay()
        {
            _currentDelay = 0;
            _unprocessedDistance = 0f;
            OnDelayAccumulated?.Invoke(_currentDelay);
        }


        public void ApplyTimeoutPenalty()
        {
            AddDelay(800);
            Debug.Log($"[PlayerController] Turn timeout penalty (+800 delay) applied to {gameObject.name}. Total Delay: {_currentDelay}");
        }

        public bool ActivateDual()
        {
            if (_isDualUsed)
            {
                Debug.LogWarning($"[PlayerController] Dual item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isDualUsed = true;
            _isDualActive = true;
            AddDelay(250);
            Debug.Log($"[PlayerController] Dual mode activated on {gameObject.name}. +250 delay added. Total Delay: {_currentDelay}");
            OnItemActivated?.Invoke("Dual");
            return true;
        }

        public void ConsumeDual()
        {
            _isDualActive = false;
            Debug.Log($"[PlayerController] Dual mode consumed on {gameObject.name}.");
        }

        public bool ActivateTeleport()
        {
            if (_isTeleportUsed)
            {
                Debug.LogWarning($"[PlayerController] Teleport item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isTeleportUsed = true;
            _isTeleportActive = true;
            Debug.Log($"[PlayerController] Teleport mode activated on {gameObject.name}. Next shot will be a beacon.");
            OnItemActivated?.Invoke("Teleport");
            return true;
        }

        public bool ActivateHeal(float healAmount = 200f)
        {
            if (_isHealUsed)
            {
                Debug.LogWarning($"[PlayerController] Heal item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isHealUsed = true;
            if (TryGetComponent<Gunbound.Gameplay.Health>(out var health))
            {
                health.Heal(healAmount);
            }
            Debug.Log($"[PlayerController] Heal item activated on {gameObject.name} (+{healAmount} HP).");
            OnItemActivated?.Invoke("Heal");
            return true;
        }

        public bool ActivateShield(float shieldAmount = 200f)
        {
            if (_isShieldUsed)
            {
                Debug.LogWarning($"[PlayerController] Shield item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isShieldUsed = true;
            if (TryGetComponent<Gunbound.Gameplay.Health>(out var health))
            {
                health.ActivateShield(shieldAmount);
            }
            CreateShieldVisualEffect();
            Debug.Log($"[PlayerController] Shield item activated on {gameObject.name} (+{shieldAmount} HP Barrier).");
            OnItemActivated?.Invoke("Shield");
            return true;
        }

        public bool ActivateChangeWind()
        {
            if (_isChangeWindUsed)
            {
                Debug.LogWarning($"[PlayerController] Change Wind item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isChangeWindUsed = true;
            AddDelay(150);

            if (Gunbound.Environment.SatelliteManager.Instance != null)
            {
                Gunbound.Environment.SatelliteManager.Instance.AdvanceSatellite();
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.GenerateRandomWind();
            }

            Debug.Log($"[PlayerController] Change Wind item activated on {gameObject.name}. +150 delay added.");
            OnItemActivated?.Invoke("ChangeWind");
            return true;
        }

        public bool ActivateDualPlus()
        {
            if (_isDualPlusUsed)
            {
                Debug.LogWarning($"[PlayerController] Dual+ item has already been used by {gameObject.name} in this match!");
                return false;
            }

            _isDualPlusUsed = true;
            _isDualPlusActive = true;
            _isDualActive = true;
            AddDelay(500);
            Debug.Log($"[PlayerController] Dual+ mode activated on {gameObject.name}. +500 delay added. Special double-shot armed!");
            OnItemActivated?.Invoke("DualPlus");
            return true;
        }

        public void ResetItems()
        {
            _isDualUsed = false;
            _isTeleportUsed = false;
            _isHealUsed = false;
            _isShieldUsed = false;
            _isChangeWindUsed = false;
            _isDualPlusUsed = false;
            _isDualActive = false;
            _isTeleportActive = false;
            _isDualPlusActive = false;
            if (_shieldVisualObj != null)
            {
                Destroy(_shieldVisualObj);
            }
        }

        private void CreateShieldVisualEffect()
        {
            if (_shieldVisualObj != null) return;

            _shieldVisualObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shieldVisualObj.name = "Shield_Barrier_FX";
            _shieldVisualObj.transform.SetParent(transform);
            _shieldVisualObj.transform.localPosition = Vector3.up * 0.4f;
            _shieldVisualObj.transform.localScale = Vector3.one * 2.2f;

            if (_shieldVisualObj.TryGetComponent<Collider>(out var col)) Destroy(col);
            if (_shieldVisualObj.TryGetComponent<Collider2D>(out var col2d)) Destroy(col2d);

            var mr = _shieldVisualObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material shieldMat = new Material(Shader.Find("Sprites/Default"));
                shieldMat.color = new Color(0.2f, 0.8f, 1.0f, 0.35f);
                mr.material = shieldMat;
            }
        }

        private void SetupPowerBar()
        {
            if (_powerBar != null)
            {
                _powerBar.minValue = _minPower;
                _powerBar.maxValue = _maxPower;
                _powerBar.value = _minPower;
            }
            _currentPower = _minPower;
        }

        public bool SelectWeapon(WeaponType weapon)
        {
            if (weapon == WeaponType.SS && !IsSSReady)
            {
                Debug.LogWarning($"[PlayerController] Cannot select SS weapon: Cooldown active! ({_currentSSCooldown} turns remaining)");
                return false;
            }

            if (_currentWeapon == weapon) return true;
            _currentWeapon = weapon;
            Debug.Log($"[PlayerController] Selected weapon: {_currentWeapon} on {gameObject.name}");
            OnWeaponChanged?.Invoke(_currentWeapon);
            return true;
        }

        private bool _isMobileMovingThisFrame;

        private void Update()
        {
            if (!_isMyTurn) return;

            HandleWeaponInput();
            HandlePowerCharging();
            HandleMovementInput();
            HandleAimInput();
        }

        private void HandleWeaponInput()
        {
            if (!_isMyTurn) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                SelectWeapon(WeaponType.Shot1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                SelectWeapon(WeaponType.Shot2);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            {
                SelectWeapon(WeaponType.SS);
            }
        }

        private void HandleMovementInput()
        {
            if (!_isMyTurn) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            float horizontalInput = Input.GetAxisRaw("Horizontal");

            if (!Mathf.Approximately(horizontalInput, 0f))
            {
                Move(horizontalInput);
            }
            else if (!_isMobileMovingThisFrame && _isMoving)
            {
                Move(0f);
            }

            _isMobileMovingThisFrame = false;
        }

        public void OnMoveLeftHold()
        {
            if (!_isMyTurn) return;
            _isMobileMovingThisFrame = true;
            Move(-1f);
        }

        public void OnMoveRightHold()
        {
            if (!_isMyTurn) return;
            _isMobileMovingThisFrame = true;
            Move(1f);
        }

        public void OnAngleUpHold()
        {
            if (!_isMyTurn || _turretAim == null) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;
            _turretAim.AdjustAngle(30.0f * Time.deltaTime);
        }

        public void OnAngleDownHold()
        {
            if (!_isMyTurn || _turretAim == null) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;
            _turretAim.AdjustAngle(-30.0f * Time.deltaTime);
        }

        private void HandleAimInput()
        {
            if (!_isMyTurn) return;
            if (_turretAim == null) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            float verticalInput = Input.GetAxisRaw("Vertical");

            if (!Mathf.Approximately(verticalInput, 0f))
            {
                _turretAim.AdjustAngle(verticalInput * 30.0f * Time.deltaTime);
            }
        }

        private void HandlePowerCharging()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                if (_isCharging || _isDragPowerActive)
                {
                    _isCharging = false;
                    _isMobileCharging = false;
                    _isDragPowerActive = false;
                    Gunbound.Core.AudioManager.Instance?.StopChargeSFX();
                    ResetPowerBar();
                }
                return;
            }

            bool spaceKeyDown = Input.GetKeyDown(KeyCode.Space);
            bool spaceKey = Input.GetKey(KeyCode.Space);

            // Space key trigger
            if (spaceKeyDown && !_isCharging && !_isDragPowerActive)
            {
                StartCharging();
            }

            bool isHoldingSpace = spaceKey || _isMobileCharging;

            if (_isCharging && isHoldingSpace)
            {
                _currentPower += _chargeSpeed * Time.deltaTime;
                if (_currentPower > _maxPower)
                {
                    _currentPower = _maxPower;
                }

                float powerRatio = Mathf.Clamp01((_currentPower - _minPower) / (_maxPower - _minPower));
                Gunbound.Core.AudioManager.Instance?.UpdateChargePitch(powerRatio);
                UpdatePowerBarUI(_currentPower);
            }

            if (_isCharging && !isHoldingSpace)
            {
                ReleaseShot();
            }
        }

        /// <summary>
        /// Starts Drag to Power shot charging gesture.
        /// </summary>
        public void StartDragPower(Vector2 startPos)
        {
            if (!_isMyTurn) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            _isDragPowerActive = true;
            _startDragPos = startPos;
            _currentPower = _minPower;
            UpdatePowerBarUI(_currentPower);
            Gunbound.Core.AudioManager.Instance?.StartChargeSFX();
        }

        /// <summary>
        /// Updates Drag to Power value dynamically based on horizontal drag distance (deltaX = currentPos.x - startDragPos.x).
        /// Dragging right increases power (0% to 100%), dragging left decreases power.
        /// </summary>
        public void UpdateDragPower(Vector2 currentPos)
        {
            if (!_isDragPowerActive || !_isMyTurn) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                CancelDragPower();
                return;
            }

            float deltaX = currentPos.x - _startDragPos.x;
            float maxDragPx = Mathf.Max(250.0f, Screen.width * 0.35f);
            float powerRatio = Mathf.Clamp01(deltaX / maxDragPx);

            _currentPower = Mathf.Lerp(_minPower, _maxPower, powerRatio);
            Gunbound.Core.AudioManager.Instance?.UpdateChargePitch(powerRatio);
            UpdatePowerBarUI(_currentPower);
        }

        /// <summary>
        /// Releases shot with power level accumulated via Drag to Power.
        /// </summary>
        public void ReleaseDragShot()
        {
            if (!_isDragPowerActive) return;
            _isDragPowerActive = false;
            Gunbound.Core.AudioManager.Instance?.StopChargeSFX();
            float finalPower = _currentPower;
            RequestShot(finalPower);
            ResetPowerBar();
        }

        public void CancelDragPower()
        {
            _isDragPowerActive = false;
            Gunbound.Core.AudioManager.Instance?.StopChargeSFX();
            ResetPowerBar();
        }

        public void OnFireButtonDown()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            _isMobileCharging = true;
            if (!_isCharging)
            {
                StartCharging();
            }
        }

        public void OnFireButtonUp()
        {
            _isMobileCharging = false;
            if (_isCharging && !Input.GetKey(KeyCode.Space))
            {
                ReleaseShot();
            }
        }

        private void StartCharging()
        {
            _isCharging = true;
            _currentPower = _minPower;
            UpdatePowerBarUI(_currentPower);
            Gunbound.Core.AudioManager.Instance?.StartChargeSFX();
        }

        private void ReleaseShot()
        {
            _isCharging = false;
            Gunbound.Core.AudioManager.Instance?.StopChargeSFX();
            float finalPower = _currentPower;
            RequestShot(finalPower);
            ResetPowerBar();
        }

        private void RequestShot(float finalPower)
        {
            float powerRatio = Mathf.Clamp01((finalPower - _minPower) / (_maxPower - _minPower));
            int playerNum = (TurnManager.Instance != null && TurnManager.Instance.Player2 == this) ? 2 : 1;
            float currentAngle = _turretAim != null ? _turretAim.CurrentAngle : 45f;

            if (Gunbound.Network.NetworkShotManager.Instance != null && Gunbound.Network.NetworkShotManager.Instance.IsNetworkActive)
            {
                Gunbound.Network.NetworkShotManager.Instance.SendFireRequest(playerNum, currentAngle, powerRatio, _currentWeapon, _isDualActive, _isTeleportActive);
            }
            else
            {
                Fire(finalPower);
            }
        }

        /// <summary>
        /// Authoritatively executes firing sequence triggered by Server RPC (RF-07.3.3).
        /// </summary>
        public void ExecuteFireFromNetworkServer(float powerRatio, float angle, WeaponType weapon, bool isDualActive, bool isTeleportActive)
        {
            if (_turretAim != null)
            {
                _turretAim.SetAngle(angle);
            }
            _currentWeapon = weapon;
            _isDualActive = isDualActive;
            _isTeleportActive = isTeleportActive;

            float chargedPower = Mathf.Lerp(_minPower, _maxPower, Mathf.Clamp01(powerRatio));
            Fire(chargedPower);
        }

        private void ResetPowerBar()
        {
            _currentPower = _minPower;
            UpdatePowerBarUI(_minPower);
        }

        private void UpdatePowerBarUI(float value)
        {
            if (_powerBar != null)
            {
                _powerBar.value = value;
            }
        }

        /// <summary>
        /// Moves the player horizontally along the X axis.
        /// </summary>
        /// <param name="direction">Axis value between -1 and 1.</param>
        public void Move(float direction)
        {
            // Block input if turn manager locks inputs
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                if (_rb2d != null)
                {
                    _rb2d.linearVelocity = new Vector2(0f, _rb2d.linearVelocity.y);
                }
                if (_isMoving)
                {
                    _isMoving = false;
                    OnMoveStateChanged?.Invoke(false);
                }
                return;
            }

            if (Mathf.Approximately(direction, 0f))
            {
                if (_rb2d != null)
                {
                    _rb2d.linearVelocity = new Vector2(0f, _rb2d.linearVelocity.y);
                }
                if (_isMoving)
                {
                    _isMoving = false;
                    OnMoveStateChanged?.Invoke(false);
                }
                return;
            }

            if (!_isMoving)
            {
                _isMoving = true;
                OnMoveStateChanged?.Invoke(true);
            }

            // Update facing direction & horizontal flip
            if (direction > 0.05f)
            {
                SetFacingDirection(1);
            }
            else if (direction < -0.05f)
            {
                SetFacingDirection(-1);
            }

            float deltaX = direction * _moveSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;
            float newX = Mathf.Clamp(currentPos.x + deltaX, _minX, _maxX);

            float distanceMoved = Mathf.Abs(newX - currentPos.x);
            _unprocessedDistance += distanceMoved;

            int delayToAdd = Mathf.FloorToInt(_unprocessedDistance * _delayPerDistanceUnit);
            if (delayToAdd > 0)
            {
                _unprocessedDistance -= delayToAdd / _delayPerDistanceUnit;
                AddDelay(delayToAdd);
            }

            if (_rb2d != null)
            {
                _rb2d.linearVelocity = new Vector2(direction * _moveSpeed, _rb2d.linearVelocity.y);
                transform.position = new Vector3(newX, currentPos.y, currentPos.z);
            }
            else
            {
                transform.position = new Vector3(newX, currentPos.y, currentPos.z);
            }
        }

        /// <summary>
        /// Fires a projectile with launch speed interpolated strictly linearly between minLaunchForce (4f) and maxLaunchForce (28f).
        /// </summary>
        /// <param name="chargedPower">Power value charged (0 to 100).</param>
        public void Fire(float chargedPower)
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                Debug.LogWarning("[PlayerController] Cannot fire: Input is locked!");
                return;
            }

            if (_turretAim == null)
            {
                Debug.LogError("[PlayerController] Cannot fire: TurretAim component reference is missing!");
                return;
            }

            GameObject targetPrefab = GetActiveProjectilePrefab(_currentWeapon);

            if (targetPrefab == null)
            {
                Debug.LogError("[PlayerController] Cannot fire: Target ProjectilePrefab is null!");
                return;
            }

            // Stop turn timer in TurnManager as shot was released
            TurnManager.Instance?.NotifyShotFired();

            // Calculate power ratio (0..1) and interpolate final launch speed strictly linearly (RF-01.4)
            float currentPowerRatio = Mathf.Clamp01((chargedPower - _minPower) / (_maxPower - _minPower));
            float launchSpeed = Mathf.Lerp(_minLaunchForce, _maxLaunchForce, currentPowerRatio);

            Vector3 spawnPosition = _turretAim.GetFirePointPosition();
            Vector3 launchVelocity = _turretAim.GetLaunchVelocity(launchSpeed, _facingDirection);

            // Slightly separate FirePoint from tank along launch direction to prevent spawning inside tank/ground
            Vector3 launchDirection = launchVelocity.sqrMagnitude > 0.001f ? launchVelocity.normalized : Vector3.up;
            spawnPosition += launchDirection * 0.6f;

            // Audio & Muzzle Flash VFX
            Gunbound.Core.AudioManager.Instance?.PlayFireSFX(_currentWeapon, spawnPosition);
            TriggerMuzzleFlash(spawnPosition, launchDirection);

            Vector3 activeWind = TurnManager.Instance != null ? TurnManager.Instance.CurrentWind : _windVector;

            // Retrieve shooter's collider to pass to projectile for collision ignoring
            Collider2D shooterCollider = GetComponent<Collider2D>();
            if (shooterCollider == null)
            {
                shooterCollider = GetComponentInChildren<Collider2D>();
            }

            // Weapon stats configuration
            float shotDamage = (_currentWeapon == WeaponType.SS) ? 600f : ((_currentWeapon == WeaponType.Shot2) ? 400f : 250f);
            float shotRadius = (_currentWeapon == WeaponType.SS) ? 5.0f : ((_currentWeapon == WeaponType.Shot2) ? 4.0f : 2.5f);
            int weaponBaseDelay = (_currentWeapon == WeaponType.SS) ? 800 : ((_currentWeapon == WeaponType.Shot2) ? 500 : 250);

            bool isSSShot = (_currentWeapon == WeaponType.SS);

            // Teleport item flag check
            bool isTeleportShot = _isTeleportActive;
            _isTeleportActive = false; // Reset teleport flag after firing

            GameObject projObj = Instantiate(targetPrefab, spawnPosition, Quaternion.identity);
            if (projObj.TryGetComponent<Gunbound.Combat.Projectile>(out var projectile))
            {
                projectile.SetStats(shotDamage, shotRadius);
                projectile.SetTeleportMode(isTeleportShot, this);
                projectile.Initialize(spawnPosition, launchVelocity, _gravity, activeWind, _collisionMask, shooterCollider);
                TurnManager.Instance?.RegisterProjectile(projectile);
            }

            Debug.Log($"[PlayerController] Fired {_currentWeapon} (Damage: {shotDamage}, Radius: {shotRadius}m, Teleport: {isTeleportShot}) with PowerRatio: {currentPowerRatio:P0} -> Launch Speed: {launchSpeed:F2} u/s");

            // Delay rules: Base shot (+250 for T1, +500 for T2, +800 for SS), Charge penalty (+1 delay per 2% charged power)
            int powerPenalty = Mathf.FloorToInt(chargedPower / 2.0f);
            int totalShotDelay = weaponBaseDelay + powerPenalty;

            AddDelay(totalShotDelay);
            OnShotFired?.Invoke(chargedPower, _turretAim.CurrentAngle);

            // Trigger SS Cooldown and auto-revert to Shot1 after SS fire (RF-02.5)
            if (isSSShot)
            {
                _currentSSCooldown = _ssCooldownRounds;
                Debug.Log($"[PlayerController] SS Fired! Entering cooldown for {_ssCooldownRounds} turns. Reverting to Shot1.");
                OnSSCooldownChanged?.Invoke(_currentSSCooldown);
                SelectWeapon(WeaponType.Shot1);
            }
        }

        private void TriggerMuzzleFlash(Vector3 position, Vector3 direction)
        {
            if (_muzzleFlashPrefab != null)
            {
                Instantiate(_muzzleFlashPrefab, position, Quaternion.LookRotation(direction));
                return;
            }

            GameObject flashObj = new GameObject("MuzzleFlash_FX");
            flashObj.transform.position = position;

            ParticleSystem ps = flashObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.15f;
            main.startLifetime = 0.15f;
            main.startSpeed = 5.0f;
            main.startSize = 0.6f;
            main.startColor = new Color(1.0f, 0.85f, 0.3f, 1.0f);
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;

            ps.Play();
        }

        public void SetWindVector(Vector3 wind)
        {
            _windVector = wind;
        }

        private MobileData _currentMobileData;
        public MobileData CurrentMobileData => _currentMobileData;

        /// <summary>
        /// Applies stats, elevation angle boundaries, base delay, health, and visual sprites from a MobileData asset (RF-5.1.3).
        /// </summary>
        public void ApplyMobileData(MobileData data)
        {
            if (data == null) return;
            _currentMobileData = data;

            _moveSpeed = data.MoveSpeed;
            _baseDelay = data.BaseDelay;

            if (_turretAim != null)
            {
                _turretAim.SetAngleBoundaries(data.MinAngle, data.MaxAngle);
            }

            var health = GetComponent<Gunbound.Gameplay.Health>();
            if (health != null)
            {
                health.SetMaxHealth(data.MaxHealth, true);
                health.ArmorDefense = data.ArmorDefense;
            }

            if (data.ChassisSprite != null)
            {
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = data.ChassisSprite;
            }

            if (data.TurretSprite != null && _turretAim != null)
            {
                var turretSr = _turretAim.GetComponent<SpriteRenderer>();
                if (turretSr != null) turretSr.sprite = data.TurretSprite;
            }

            if (data.Shot1Prefab != null) _projectilePrefabShot1 = data.Shot1Prefab;
            if (data.Shot2Prefab != null) _projectilePrefabShot2 = data.Shot2Prefab;
            if (data.SSPrefab != null) _projectilePrefabShotSS = data.SSPrefab;

            Debug.Log($"[PlayerController] '{gameObject.name}' applied MobileData '{data.MobileName}' (HP={data.MaxHealth}, Armor={data.ArmorDefense * 100:F0}%, Angle={data.MinAngle}°-{data.MaxAngle}°, Speed={data.MoveSpeed}, Delay={data.BaseDelay}).");
        }

        /// <summary>
        /// Retrieves active projectile prefab based on MobileData and WeaponType (RF-5.2.1).
        /// </summary>
        public GameObject GetActiveProjectilePrefab(WeaponType weapon)
        {
            if (_currentMobileData != null)
            {
                if (weapon == WeaponType.Shot1 && _currentMobileData.Shot1Prefab != null) return _currentMobileData.Shot1Prefab;
                if (weapon == WeaponType.Shot2 && _currentMobileData.Shot2Prefab != null) return _currentMobileData.Shot2Prefab;
                if (weapon == WeaponType.SS && _currentMobileData.SSPrefab != null) return _currentMobileData.SSPrefab;
            }

            if (weapon == WeaponType.Shot1 && _projectilePrefabShot1 != null) return _projectilePrefabShot1;
            if (weapon == WeaponType.Shot2 && _projectilePrefabShot2 != null) return _projectilePrefabShot2;
            if (weapon == WeaponType.SS && _projectilePrefabShotSS != null) return _projectilePrefabShotSS;

            return _projectilePrefab;
        }

        private void AddDelay(int amount)
        {
            _currentDelay += amount;
            OnDelayAccumulated?.Invoke(_currentDelay);
            TurnManager.Instance?.NotifyDelayChanged();
        }
    }
}

