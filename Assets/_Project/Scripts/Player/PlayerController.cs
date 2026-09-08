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
        [SerializeField] private int _baseDelay = 100;
        [SerializeField] private float _delayPerDistanceUnit = 10.0f;
        [SerializeField] private LayerMask _collisionMask = ~0;

        [Header("Power Bar & Shot Charging Settings")]
        [SerializeField] private Slider _powerBar;
        [SerializeField] private float _minPower = 0.0f;
        [SerializeField] private float _maxPower = 100.0f;
        [SerializeField] private float _chargeSpeed = 50.0f;

        [Header("Physics & Wind Data")]
        [SerializeField] private Vector3 _gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField] private Vector3 _windVector = Vector3.zero;

        // Current internal state
        private int _accumulatedDelay;
        private bool _isMoving;
        private int _facingDirection = 1; // 1 for right (+X), -1 for left (-X)

        private float _currentPower;
        private bool _isCharging;
        private bool _isMobileCharging;

        // Events
        public event Action<bool> OnMoveStateChanged;
        public event Action<float, float> OnShotFired; // (power, angle)
        public event Action<int> OnDelayAccumulated;

        public int FacingDirection => _facingDirection;
        public int AccumulatedDelay => _accumulatedDelay;
        public TurretAim TurretAim => _turretAim;
        public Slider PowerBar => _powerBar;

        private void Awake()
        {
            if (_turretAim == null)
            {
                _turretAim = GetComponentInChildren<TurretAim>();
            }

            SetupPowerBar();
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

        private bool _isMobileMovingThisFrame;

        private void Update()
        {
            HandlePowerCharging();
            HandleMovementInput();
            HandleAimInput();
        }

        private void HandleMovementInput()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            float keyboardInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                keyboardInput -= 1f;
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                keyboardInput += 1f;
            }

            if (!Mathf.Approximately(keyboardInput, 0f))
            {
                Move(keyboardInput);
            }
            else if (!_isMobileMovingThisFrame && _isMoving)
            {
                Move(0f);
            }

            _isMobileMovingThisFrame = false;
        }

        public void OnMoveLeftHold()
        {
            _isMobileMovingThisFrame = true;
            Move(-1f);
        }

        public void OnMoveRightHold()
        {
            _isMobileMovingThisFrame = true;
            Move(1f);
        }

        private void HandleAimInput()
        {
            if (_turretAim == null) return;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return;

            float angleInput = 0f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                angleInput += 1f;
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                angleInput -= 1f;
            }

            if (!Mathf.Approximately(angleInput, 0f))
            {
                _turretAim.AdjustAngle(angleInput * 30.0f * Time.deltaTime);
            }
        }

        private void HandlePowerCharging()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                if (_isCharging)
                {
                    _isCharging = false;
                    _isMobileCharging = false;
                    ResetPowerBar();
                }
                return;
            }

            bool spaceKeyDown = Input.GetKeyDown(KeyCode.Space);
            bool spaceKey = Input.GetKey(KeyCode.Space);
            bool spaceKeyUp = Input.GetKeyUp(KeyCode.Space);

            // Dual input trigger: Space key down or Mobile button press
            if (spaceKeyDown && !_isCharging)
            {
                StartCharging();
            }

            // Continuous charging logic while Space key or Mobile button is held
            bool isHolding = spaceKey || _isMobileCharging;

            if (_isCharging && isHolding)
            {
                _currentPower += _chargeSpeed * Time.deltaTime;
                if (_currentPower > _maxPower)
                {
                    _currentPower = _maxPower;
                }

                UpdatePowerBarUI(_currentPower);
            }

            // Release trigger: Space key up or Mobile button release
            if (_isCharging && !isHolding)
            {
                ReleaseShot();
            }
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
        }

        private void ReleaseShot()
        {
            _isCharging = false;
            float finalPower = _currentPower;
            Fire(finalPower);
            ResetPowerBar();
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
                if (_isMoving)
                {
                    _isMoving = false;
                    OnMoveStateChanged?.Invoke(false);
                }
                return;
            }

            if (Mathf.Approximately(direction, 0f))
            {
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

            // Update facing direction
            if (direction > 0.05f)
            {
                _facingDirection = 1;
            }
            else if (direction < -0.05f)
            {
                _facingDirection = -1;
            }

            float deltaX = direction * _moveSpeed * Time.deltaTime;
            Vector3 currentPos = transform.position;
            float newX = Mathf.Clamp(currentPos.x + deltaX, _minX, _maxX);

            float distanceMoved = Mathf.Abs(newX - currentPos.x);
            int addedDelay = Mathf.RoundToInt(distanceMoved * _delayPerDistanceUnit);
            if (addedDelay > 0)
            {
                AddDelay(addedDelay);
            }

            transform.position = new Vector3(newX, currentPos.y, currentPos.z);
        }

        /// <summary>
        /// Fires a projectile with the specified launch speed (power).
        /// </summary>
        /// <param name="launchSpeed">Muzzle speed / power value.</param>
        public void Fire(float launchSpeed)
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

            if (_projectilePrefab == null)
            {
                Debug.LogError("[PlayerController] Cannot fire: ProjectilePrefab is null!");
                return;
            }

            Vector3 spawnPosition = _turretAim.GetFirePointPosition();
            Vector3 launchVelocity = _turretAim.GetLaunchVelocity(launchSpeed, _facingDirection);

            Vector3 activeWind = TurnManager.Instance != null ? TurnManager.Instance.CurrentWind : _windVector;

            GameObject projObj = Instantiate(_projectilePrefab, spawnPosition, Quaternion.identity);
            if (projObj.TryGetComponent<Gunbound.Combat.Projectile>(out var projectile))
            {
                projectile.Initialize(spawnPosition, launchVelocity, _gravity, activeWind, _collisionMask);
                TurnManager.Instance?.RegisterProjectile(projectile);
            }

            AddDelay(_baseDelay);
            OnShotFired?.Invoke(launchSpeed, _turretAim.CurrentAngle);
        }

        public void SetWindVector(Vector3 wind)
        {
            _windVector = wind;
        }

        private void AddDelay(int amount)
        {
            _accumulatedDelay += amount;
            OnDelayAccumulated?.Invoke(_accumulatedDelay);
        }
    }
}
