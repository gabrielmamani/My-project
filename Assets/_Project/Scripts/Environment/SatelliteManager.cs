using System;
using UnityEngine;
using Gunbound.Managers;

namespace Gunbound.Environment
{
    public enum SatelliteType
    {
        Normal,
        Thor,
        Force,
        Hurricane
    }

    /// <summary>
    /// Manages global satellite states (environmental modifiers) in a dynamic/cyclic progression.
    /// Advances state on each turn change and notifies HUD and combat systems.
    /// </summary>
    public class SatelliteManager : MonoBehaviour
    {
        public static SatelliteManager Instance { get; private set; }

        [Header("Satellite Settings")]
        [SerializeField] private SatelliteType _currentSatellite = SatelliteType.Normal;
        [SerializeField] private bool _useRandomSelection = false;

        public SatelliteType CurrentSatellite => _currentSatellite;
        public float DamageMultiplier => _currentSatellite == SatelliteType.Force ? 1.25f : 1.0f;
        public float WindMultiplier => _currentSatellite == SatelliteType.Hurricane ? 2.0f : 1.0f;
        public bool IsThorActive => _currentSatellite == SatelliteType.Thor;

        public event Action<SatelliteType> OnSatelliteChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged += HandleTurnChanged;
            }
            
            // Notify initial state
            OnSatelliteChanged?.Invoke(_currentSatellite);
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnChanged -= HandleTurnChanged;
            }
        }

        private void HandleTurnChanged(int currentTurn)
        {
            AdvanceSatellite();
        }

        /// <summary>
        /// Advances to the next satellite state via cyclic roulette or random pick.
        /// </summary>
        public void AdvanceSatellite()
        {
            if (_useRandomSelection)
            {
                Array values = Enum.GetValues(typeof(SatelliteType));
                _currentSatellite = (SatelliteType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
            }
            else
            {
                int nextIndex = ((int)_currentSatellite + 1) % Enum.GetNames(typeof(SatelliteType)).Length;
                _currentSatellite = (SatelliteType)nextIndex;
            }

            Debug.Log($"[SatelliteManager] Turn advanced. Active Satellite: {_currentSatellite}");
            OnSatelliteChanged?.Invoke(_currentSatellite);
        }

        /// <summary>
        /// Manually sets the active satellite state.
        /// </summary>
        public void SetSatellite(SatelliteType newType)
        {
            _currentSatellite = newType;
            Debug.Log($"[SatelliteManager] Satellite manually set to: {_currentSatellite}");
            OnSatelliteChanged?.Invoke(_currentSatellite);
        }
    }
}
