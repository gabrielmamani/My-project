using System;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Player;
using Gunbound.Managers;

namespace Gunbound.UI
{
    /// <summary>
    /// Manages the uGUI HUD interface, including shot power slider, aim angle text, and wind/turn displays.
    /// Supports dynamic charging of launch power and input interaction.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Text _angleText;
        [SerializeField] private Text _windText;
        [SerializeField] private Text _turnText;
        [SerializeField] private Text _statusText;

        [Header("Scene References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private TurnManager _turnManager;

        [Header("Power Bar Parameters")]
        [SerializeField] private float _minPower = 0.0f;
        [SerializeField] private float _maxPower = 100.0f;

        private TurretAim _turretAim;

        private void Start()
        {
            FindReferencesIfMissing();
            SetupPowerSlider();
            SubscribeEvents();
            UpdateAllUI();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void FindReferencesIfMissing()
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }

            if (_turnManager == null)
            {
                _turnManager = TurnManager.Instance != null ? TurnManager.Instance : FindAnyObjectByType<TurnManager>();
            }

            if (_playerController != null)
            {
                _turretAim = _playerController.TurretAim != null ? _playerController.TurretAim : _playerController.GetComponentInChildren<TurretAim>();
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

        private void SubscribeEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnWindChanged += UpdateWindDisplay;
                _turnManager.OnTurnChanged += UpdateTurnDisplay;
                _turnManager.OnInputStateChanged += UpdateInputStateDisplay;
            }

            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged += UpdateAngleDisplay;
            }

            if (_playerController != null)
            {
                _playerController.OnShotFired += HandleShotFired;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_turnManager != null)
            {
                _turnManager.OnWindChanged -= UpdateWindDisplay;
                _turnManager.OnTurnChanged -= UpdateTurnDisplay;
                _turnManager.OnInputStateChanged -= UpdateInputStateDisplay;
            }

            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged -= UpdateAngleDisplay;
            }

            if (_playerController != null)
            {
                _playerController.OnShotFired -= HandleShotFired;
            }
        }

        private void Update()
        {
            HandleAimInput();
        }

        private void HandleAimInput()
        {
            if (_turretAim == null) return;
            if (_turnManager != null && _turnManager.IsInputLocked) return;

            // Simple keyboard controls for angle adjustment (Up / Down arrows or W/S)
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                OnAngleUpHold();
            }
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                OnAngleDownHold();
            }
        }

        public void OnAngleUpHold()
        {
            if (_turretAim == null) return;
            if (_turnManager != null && _turnManager.IsInputLocked) return;
            _turretAim.AdjustAngle(30.0f * Time.deltaTime);
        }

        public void OnAngleDownHold()
        {
            if (_turretAim == null) return;
            if (_turnManager != null && _turnManager.IsInputLocked) return;
            _turretAim.AdjustAngle(-30.0f * Time.deltaTime);
        }

        private void UpdateAllUI()
        {
            if (_turnManager != null)
            {
                UpdateWindDisplay(_turnManager.CurrentWind);
                UpdateTurnDisplay(_turnManager.CurrentTurn);
                UpdateInputStateDisplay(_turnManager.IsInputLocked);
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

        public void UpdateTurnDisplay(int turn)
        {
            if (_turnText != null)
            {
                _turnText.text = $"Turno: {turn}";
            }
        }

        public void UpdateAngleDisplay(float angle)
        {
            if (_angleText != null)
            {
                _angleText.text = $"Ángulo: {angle:F1}°";
            }
        }

        public void UpdateInputStateDisplay(bool isLocked)
        {
            if (_statusText != null)
            {
                _statusText.text = isLocked ? "Proyectil en vuelo..." : "Tu Turno";
                _statusText.color = isLocked ? Color.red : Color.green;
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
