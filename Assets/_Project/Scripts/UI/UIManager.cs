using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Player;
using Gunbound.Managers;
using Gunbound.Environment;

namespace Gunbound.UI
{
    /// <summary>
    /// Manages the uGUI HUD interface, including shot power slider, aim angle text, wind/turn displays,
    /// real-time player delays, 20s countdown timer, double turn alerts, and victory notifications.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Text _angleText;
        [SerializeField] private Text _windText;
        [SerializeField] private Text _turnText;
        [SerializeField] private Text _statusText;

        [Header("Phase 3 UI References")]
        [SerializeField] private Text _delayText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _doubleTurnText;
        [SerializeField] private Text _satelliteText;
        [SerializeField] private ShiftListUI _shiftListUI;

        [Header("Weapon Selection UI References")]
        [SerializeField] private Button _btnShot1;
        [SerializeField] private Button _btnShot2;
        [SerializeField] private Button _btnSS;
        [SerializeField] private Image _btnShot1Bg;
        [SerializeField] private Image _btnShot2Bg;
        [SerializeField] private Image _btnSSBg;

        [Header("Tactical Item UI References")]
        [SerializeField] private Button _btnDual;
        [SerializeField] private Button _btnTeleport;
        [SerializeField] private Button _btnHeal;
        [SerializeField] private Button _btnChangeWind;
        [SerializeField] private Button _btnShield;
        [SerializeField] private Button _btnDualPlus;
        [SerializeField] private Image _btnDualBg;
        [SerializeField] private Image _btnTeleportBg;
        [SerializeField] private Image _btnHealBg;
        [SerializeField] private Image _btnChangeWindBg;
        [SerializeField] private Image _btnShieldBg;
        [SerializeField] private Image _btnDualPlusBg;

        [Header("Scene References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private TurnManager _turnManager;

        [Header("Power Bar Parameters")]
        [SerializeField] private float _minPower = 0.0f;
        [SerializeField] private float _maxPower = 100.0f;

        private TurretAim _turretAim;
        private Coroutine _doubleTurnCoroutine;

        private static readonly Color ActiveColor = new Color(1.0f, 0.85f, 0.0f, 1.0f); // Bright Yellow / Gold
        private static readonly Color InactiveColor = new Color(0.25f, 0.28f, 0.35f, 0.8f); // Dark Slate Gray
        private static readonly Color DualColor = new Color(0.85f, 0.65f, 0.1f, 1.0f); // Gold/Orange
        private static readonly Color TeleportColor = new Color(0.2f, 0.65f, 0.85f, 1.0f); // Cyan
        private static readonly Color HealColor = new Color(0.2f, 0.85f, 0.4f, 1.0f); // Emerald Green
        private static readonly Color ChangeWindColor = new Color(0.7f, 0.4f, 0.9f, 1.0f); // Violet
        private static readonly Color ShieldColor = new Color(0.2f, 0.8f, 1.0f, 1.0f); // Bright Cyan
        private static readonly Color DualPlusColor = new Color(1.0f, 0.3f, 0.2f, 1.0f); // Crimson Red

        private void Start()
        {
            FindReferencesIfMissing();
            SetupPowerSlider();
            SetupWeaponButtons();
            SetupItemButtons();
            SubscribeEvents();
            UpdateAllUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void FindReferencesIfMissing()
        {
            if (_turnManager == null)
            {
                _turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
            }

            UpdateActivePlayerReference();
        }

        private void UpdateActivePlayerReference()
        {
            PlayerController oldPlayer = _playerController;

            if (_turnManager != null && _turnManager.ActivePlayer != null)
            {
                _playerController = _turnManager.ActivePlayer;
            }
            else if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }

            if (oldPlayer != _playerController)
            {
                if (oldPlayer != null)
                {
                    oldPlayer.OnShotFired -= HandleShotFired;
                    oldPlayer.OnWeaponChanged -= HandleWeaponChanged;
                    oldPlayer.OnSSCooldownChanged -= HandleSSCooldownChanged;
                }
                if (_playerController != null)
                {
                    _playerController.OnShotFired += HandleShotFired;
                    _playerController.OnWeaponChanged += HandleWeaponChanged;
                    _playerController.OnSSCooldownChanged += HandleSSCooldownChanged;
                }
            }

            if (_playerController != null)
            {
                if (_turretAim != null)
                {
                    _turretAim.OnAngleChanged -= UpdateAngleDisplay;
                }
                _turretAim = _playerController.TurretAim != null ? _playerController.TurretAim : _playerController.GetComponentInChildren<TurretAim>();
                if (_turretAim != null)
                {
                    _turretAim.OnAngleChanged += UpdateAngleDisplay;
                    UpdateAngleDisplay(_turretAim.CurrentAngle);
                }
                UpdateWeaponSelectionUI(_playerController.CurrentWeapon);
            }
        }

        private void SetupPowerSlider()
        {
            if (_powerSlider != null)
            {
                _powerSlider.minValue = _minPower;
                _powerSlider.maxValue = _maxPower;
                _powerSlider.value = _minPower;
            }
        }

        public void SetupWeaponButtons()
        {
            if (_btnShot1 != null)
            {
                _btnShot1.onClick.RemoveAllListeners();
                _btnShot1.onClick.AddListener(OnShot1ButtonClicked);
                if (_btnShot1Bg == null) _btnShot1Bg = _btnShot1.GetComponent<Image>();
            }

            if (_btnShot2 != null)
            {
                _btnShot2.onClick.RemoveAllListeners();
                _btnShot2.onClick.AddListener(OnShot2ButtonClicked);
                if (_btnShot2Bg == null) _btnShot2Bg = _btnShot2.GetComponent<Image>();
            }

            if (_btnSS != null)
            {
                _btnSS.onClick.RemoveAllListeners();
                _btnSS.onClick.AddListener(OnSSButtonClicked);
                if (_btnSSBg == null) _btnSSBg = _btnSS.GetComponent<Image>();
            }

            if (_playerController != null)
            {
                UpdateWeaponSelectionUI(_playerController.CurrentWeapon);
            }
        }

        public void OnShot1ButtonClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.SelectWeapon(WeaponType.Shot1);
            }
        }

        public void OnShot2ButtonClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.SelectWeapon(WeaponType.Shot2);
            }
        }

        public void OnSSButtonClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.SelectWeapon(WeaponType.SS);
            }
        }

        public void SetupItemButtons()
        {
            if (_btnDual != null)
            {
                _btnDual.onClick.RemoveAllListeners();
                _btnDual.onClick.AddListener(OnDualButtonClicked);
                if (_btnDualBg == null) _btnDualBg = _btnDual.GetComponent<Image>();
            }

            if (_btnTeleport != null)
            {
                _btnTeleport.onClick.RemoveAllListeners();
                _btnTeleport.onClick.AddListener(OnTeleportButtonClicked);
                if (_btnTeleportBg == null) _btnTeleportBg = _btnTeleport.GetComponent<Image>();
            }

            if (_btnHeal != null)
            {
                _btnHeal.onClick.RemoveAllListeners();
                _btnHeal.onClick.AddListener(OnHealButtonClicked);
                if (_btnHealBg == null) _btnHealBg = _btnHeal.GetComponent<Image>();
            }

            if (_btnChangeWind != null)
            {
                _btnChangeWind.onClick.RemoveAllListeners();
                _btnChangeWind.onClick.AddListener(OnChangeWindButtonClicked);
                if (_btnChangeWindBg == null) _btnChangeWindBg = _btnChangeWind.GetComponent<Image>();
            }

            if (_btnShield != null)
            {
                _btnShield.onClick.RemoveAllListeners();
                _btnShield.onClick.AddListener(OnShieldButtonClicked);
                if (_btnShieldBg == null) _btnShieldBg = _btnShield.GetComponent<Image>();
            }

            if (_btnDualPlus != null)
            {
                _btnDualPlus.onClick.RemoveAllListeners();
                _btnDualPlus.onClick.AddListener(OnDualPlusButtonClicked);
                if (_btnDualPlusBg == null) _btnDualPlusBg = _btnDualPlus.GetComponent<Image>();
            }

            UpdateItemButtonsUI();
        }

        public void OnDualButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.Dual);
        }

        public void OnTeleportButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.Teleport);
        }

        public void OnHealButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.Heal);
        }

        public void OnChangeWindButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.ChangeWind);
        }

        public void OnShieldButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.Shield);
        }

        public void OnDualPlusButtonClicked()
        {
            TriggerItemUse(Gunbound.Managers.ItemType.DualPlus);
        }

        private void TriggerItemUse(Gunbound.Managers.ItemType item)
        {
            int playerNum = (_turnManager != null && _turnManager.ActivePlayerNumber != 0) ? _turnManager.ActivePlayerNumber : 1;

            if (Gunbound.Network.NetworkShotManager.Instance != null && Gunbound.Network.NetworkShotManager.Instance.IsNetworkActive)
            {
                Gunbound.Network.NetworkShotManager.Instance.SendItemUseRequest(playerNum, item);
                Gunbound.Core.AudioManager.Instance?.PlayItemSFX();
                UpdateItemButtonsUI();
            }
            else if (ItemManager.Instance != null && ItemManager.Instance.UseItem(item))
            {
                Gunbound.Core.AudioManager.Instance?.PlayItemSFX();
                UpdateItemButtonsUI();
                if (_statusText != null)
                {
                    _statusText.text = $"¡Item {item} Activado!";
                    _statusText.color = DualColor;
                }
            }
        }

        public void UpdateItemButtonsUI()
        {
            PlayerController activePlayer = GetActivePlayer();
            bool inputLocked = _turnManager != null && (_turnManager.IsInputLocked || _turnManager.IsGameOver);

            if (_btnDual != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsDualUsed;
                _btnDual.interactable = !isUsed && !inputLocked;
                if (_btnDualBg == null) _btnDualBg = _btnDual.GetComponent<Image>();
                if (_btnDualBg != null) _btnDualBg.color = isUsed ? InactiveColor : DualColor;
            }

            if (_btnTeleport != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsTeleportUsed;
                _btnTeleport.interactable = !isUsed && !inputLocked;
                if (_btnTeleportBg == null) _btnTeleportBg = _btnTeleport.GetComponent<Image>();
                if (_btnTeleportBg != null) _btnTeleportBg.color = isUsed ? InactiveColor : TeleportColor;
            }

            if (_btnHeal != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsHealUsed;
                _btnHeal.interactable = !isUsed && !inputLocked;
                if (_btnHealBg == null) _btnHealBg = _btnHeal.GetComponent<Image>();
                if (_btnHealBg != null) _btnHealBg.color = isUsed ? InactiveColor : HealColor;
            }

            if (_btnChangeWind != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsChangeWindUsed;
                _btnChangeWind.interactable = !isUsed && !inputLocked;
                if (_btnChangeWindBg == null) _btnChangeWindBg = _btnChangeWind.GetComponent<Image>();
                if (_btnChangeWindBg != null) _btnChangeWindBg.color = isUsed ? InactiveColor : ChangeWindColor;
            }

            if (_btnShield != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsShieldUsed;
                _btnShield.interactable = !isUsed && !inputLocked;
                if (_btnShieldBg == null) _btnShieldBg = _btnShield.GetComponent<Image>();
                if (_btnShieldBg != null) _btnShieldBg.color = isUsed ? InactiveColor : ShieldColor;
            }

            if (_btnDualPlus != null)
            {
                bool isUsed = activePlayer != null && activePlayer.IsDualPlusUsed;
                _btnDualPlus.interactable = !isUsed && !inputLocked;
                if (_btnDualPlusBg == null) _btnDualPlusBg = _btnDualPlus.GetComponent<Image>();
                if (_btnDualPlusBg != null) _btnDualPlusBg.color = isUsed ? InactiveColor : DualPlusColor;
            }
        }

        private PlayerController GetActivePlayer()
        {
            if (_turnManager != null && _turnManager.ActivePlayer != null)
            {
                return _turnManager.ActivePlayer;
            }
            return _playerController != null ? _playerController : FindAnyObjectByType<PlayerController>();
        }

        private void HandleWeaponChanged(WeaponType weapon)
        {
            UpdateWeaponSelectionUI(weapon);
        }

        private void HandleSSCooldownChanged(int cooldown)
        {
            PlayerController activePlayer = GetActivePlayer();
            WeaponType activeWeapon = activePlayer != null ? activePlayer.CurrentWeapon : WeaponType.Shot1;
            UpdateWeaponSelectionUI(activeWeapon);
        }

        public void UpdateWeaponSelectionUI(WeaponType selectedWeapon)
        {
            PlayerController activePlayer = GetActivePlayer();
            bool isSSReady = activePlayer == null || activePlayer.IsSSReady;
            int ssCooldown = activePlayer != null ? activePlayer.CurrentSSCooldown : 0;

            if (_btnShot1Bg != null)
            {
                _btnShot1Bg.color = (selectedWeapon == WeaponType.Shot1) ? ActiveColor : InactiveColor;
            }

            if (_btnShot2Bg != null)
            {
                _btnShot2Bg.color = (selectedWeapon == WeaponType.Shot2) ? ActiveColor : InactiveColor;
            }

            if (_btnSS != null)
            {
                _btnSS.interactable = isSSReady;

                Text btnSSText = _btnSS.GetComponentInChildren<Text>();
                if (btnSSText != null)
                {
                    btnSSText.text = isSSReady ? "SS" : $"SS ({ssCooldown})";
                }
            }

            if (_btnSSBg != null)
            {
                if (!isSSReady)
                {
                    _btnSSBg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                }
                else
                {
                    _btnSSBg.color = (selectedWeapon == WeaponType.SS) ? ActiveColor : InactiveColor;
                }
            }
        }

        private void SubscribeEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnWindChanged += UpdateWindDisplay;
                _turnManager.OnTurnChanged += UpdateTurnDisplay;
                _turnManager.OnTurnPlayerChanged += HandleTurnPlayerChanged;
                _turnManager.OnInputStateChanged += UpdateInputStateDisplay;
                _turnManager.OnGameOver += HandleGameOver;
                _turnManager.OnDelaysUpdated += UpdateDelayDisplay;
                _turnManager.OnTurnTimerUpdated += UpdateTimerDisplay;
                _turnManager.OnDoubleTurnTriggered += HandleDoubleTurn;
                _turnManager.OnTurnTimeout += HandleTurnTimeout;
            }

            if (_playerController != null)
            {
                _playerController.OnShotFired += HandleShotFired;
                _playerController.OnWeaponChanged += HandleWeaponChanged;
                _playerController.OnSSCooldownChanged += HandleSSCooldownChanged;
            }

            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged += UpdateSatelliteDisplay;
                UpdateSatelliteDisplay(SatelliteManager.Instance.CurrentSatellite);
            }
        }

        private void UnsubscribeEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnWindChanged -= UpdateWindDisplay;
                _turnManager.OnTurnChanged -= UpdateTurnDisplay;
                _turnManager.OnTurnPlayerChanged -= HandleTurnPlayerChanged;
                _turnManager.OnInputStateChanged -= UpdateInputStateDisplay;
                _turnManager.OnGameOver -= HandleGameOver;
                _turnManager.OnDelaysUpdated -= UpdateDelayDisplay;
                _turnManager.OnTurnTimerUpdated -= UpdateTimerDisplay;
                _turnManager.OnDoubleTurnTriggered -= HandleDoubleTurn;
                _turnManager.OnTurnTimeout -= HandleTurnTimeout;
            }

            if (_playerController != null)
            {
                _playerController.OnShotFired -= HandleShotFired;
                _playerController.OnWeaponChanged -= HandleWeaponChanged;
                _playerController.OnSSCooldownChanged -= HandleSSCooldownChanged;
            }

            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged -= UpdateAngleDisplay;
            }

            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged -= UpdateSatelliteDisplay;
            }
        }

        private void HandleTurnPlayerChanged(int turn, int activePlayerNumber)
        {
            UpdateActivePlayerReference();
            UpdateTurnDisplay(turn);
            UpdateItemButtonsUI();
            if (_turretAim != null)
            {
                UpdateAngleDisplay(_turretAim.CurrentAngle);
            }
        }

        private void Update()
        {
            HandleItemInput();
        }

        private void HandleItemInput()
        {
            if (_turnManager != null && (_turnManager.IsInputLocked || _turnManager.IsGameOver)) return;

            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                OnDualButtonClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.T))
            {
                OnTeleportButtonClicked();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.H))
            {
                OnHealButtonClicked();
            }
        }

        public void OnAngleUpHold()
        {
            if (_turretAim == null) return;
            if (_turnManager != null && (_turnManager.IsInputLocked || _turnManager.IsGameOver)) return;
            _turretAim.AdjustAngle(30.0f * Time.deltaTime);
        }

        public void OnAngleDownHold()
        {
            if (_turretAim == null) return;
            if (_turnManager != null && (_turnManager.IsInputLocked || _turnManager.IsGameOver)) return;
            _turretAim.AdjustAngle(-30.0f * Time.deltaTime);
        }

        private void UpdateAllUI()
        {
            if (_turnManager != null)
            {
                UpdateWindDisplay(_turnManager.CurrentWind);
                UpdateTurnDisplay(_turnManager.CurrentTurn);
                UpdateInputStateDisplay(_turnManager.IsInputLocked);
                UpdateDelayDisplay(_turnManager.Player1Delay, _turnManager.Player2Delay);
                UpdateTimerDisplay(_turnManager.RemainingTurnTime);
            }

            if (SatelliteManager.Instance != null)
            {
                UpdateSatelliteDisplay(SatelliteManager.Instance.CurrentSatellite);
            }

            if (_turretAim != null)
            {
                UpdateAngleDisplay(_turretAim.CurrentAngle);
            }
        }

        public void UpdateWindDisplay(Vector3 wind)
        {
            if (_windText == null) return;

            string directionArrow = wind.x > 0.05f ? "➔" : (wind.x < -0.05f ? "⬅" : "●");
            _windText.text = $"Viento: {wind.x:+0.0;-0.0;0.0} m/s {directionArrow}";
        }

        public void UpdateSatelliteDisplay(SatelliteType satellite)
        {
            if (_satelliteText == null) return;

            string label = satellite.ToString().ToUpper();
            _satelliteText.text = $"[ Satélite: {label} ACTIVO ]";

            switch (satellite)
            {
                case SatelliteType.Thor:
                    _satelliteText.color = new Color(0.2f, 0.9f, 1.0f); // Bright Cyan
                    break;
                case SatelliteType.Force:
                    _satelliteText.color = new Color(0.9f, 0.3f, 1.0f); // Purple / Magenta
                    break;
                case SatelliteType.Hurricane:
                    _satelliteText.color = new Color(0.3f, 1.0f, 0.5f); // Green
                    break;
                default:
                    _satelliteText.color = Color.white;
                    break;
            }
        }

        public void UpdateTurnDisplay(int turn)
        {
            if (_turnText != null)
            {
                int playerNum = _turnManager != null ? _turnManager.ActivePlayerNumber : 1;
                _turnText.text = $"Turno de: Jugador {playerNum}";
            }
        }

        public void UpdateAngleDisplay(float angle)
        {
            if (_angleText != null)
            {
                _angleText.text = $"Ángulo: {angle:F1}°";
            }
        }

        public void UpdateDelayDisplay(int p1Delay, int p2Delay)
        {
            if (_delayText != null)
            {
                _delayText.text = $"P1: {p1Delay} | P2: {p2Delay}";
            }

            if (_shiftListUI != null)
            {
                _shiftListUI.UpdateShiftList();
            }
        }

        public void UpdateTimerDisplay(float remainingSeconds)
        {
            if (_timerText != null)
            {
                int seconds = Mathf.CeilToInt(remainingSeconds);
                _timerText.text = $"Tiempo: {seconds}s";
                _timerText.color = remainingSeconds <= 5.0f ? Color.red : Color.white;
            }
        }

        public void HandleDoubleTurn(int activePlayerNumber)
        {
            if (_doubleTurnCoroutine != null)
            {
                StopCoroutine(_doubleTurnCoroutine);
            }
            _doubleTurnCoroutine = StartCoroutine(ShowDoubleTurnAlertRoutine(activePlayerNumber));
        }

        private IEnumerator ShowDoubleTurnAlertRoutine(int activePlayerNumber)
        {
            if (_doubleTurnText != null)
            {
                _doubleTurnText.gameObject.SetActive(true);
                _doubleTurnText.text = $"¡Doble Turno!";
            }

            yield return new WaitForSeconds(2.0f);

            if (_doubleTurnText != null)
            {
                _doubleTurnText.gameObject.SetActive(false);
            }
        }

        public void HandleTurnTimeout(int playerNumber)
        {
            if (_statusText != null)
            {
                _statusText.text = $"¡Tiempo agotado! (Jugador {playerNumber}) +800 Delay";
                _statusText.color = Color.red;
            }
        }

        public void UpdateInputStateDisplay(bool isLocked)
        {
            UpdateItemButtonsUI();
            if (_statusText == null) return;
            if (_turnManager != null && _turnManager.IsGameOver) return;

            int playerNum = _turnManager != null ? _turnManager.ActivePlayerNumber : 1;
            _statusText.text = isLocked ? "Proyectil en vuelo..." : $"Turno de: Jugador {playerNum}";
            _statusText.color = isLocked ? Color.red : Color.green;
        }

        private void HandleGameOver(int winnerNumber)
        {
            if (_statusText != null)
            {
                _statusText.text = $"¡Victoria para el Jugador {winnerNumber}!";
                _statusText.color = new Color(1.0f, 0.85f, 0.0f); // Gold / Yellow
            }
        }

        private void HandleShotFired(float power, float angle)
        {
            if (_powerSlider != null)
            {
                _powerSlider.value = power;
            }
        }
    }
}
