using System;
using UnityEngine;
using Gunbound.Combat;

namespace Gunbound.Managers
{
    /// <summary>
    /// Manages game turn progression, random wind vectors, and player input locking while projectiles are active.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Wind Configuration")]
        [SerializeField] private float _minWindX = -6.0f;
        [SerializeField] private float _maxWindX = 6.0f;
        [SerializeField] private float _minWindY = -1.5f;
        [SerializeField] private float _maxWindY = 1.5f;

        [Header("Turn State")]
        [SerializeField] private int _currentTurn = 1;
        [SerializeField] private Vector3 _currentWind = Vector3.zero;
        [SerializeField] private bool _isInputLocked = false;

        private Projectile _activeProjectile;

        // Events
        public event Action<Vector3> OnWindChanged;
        public event Action<int> OnTurnChanged;
        public event Action<bool> OnInputStateChanged;

        public Vector3 CurrentWind => _currentWind;
        public int CurrentTurn => _currentTurn;
        public bool IsInputLocked => _isInputLocked;

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
            GenerateRandomWind();
            OnTurnChanged?.Invoke(_currentTurn);
            SetInputLocked(false);
        }

        /// <summary>
        /// Generates a new random wind vector within configured limits.
        /// </summary>
        public void GenerateRandomWind()
        {
            float windX = UnityEngine.Random.Range(_minWindX, _maxWindX);
            float windY = UnityEngine.Random.Range(_minWindY, _maxWindY);
            _currentWind = new Vector3(windX, windY, 0f);

            Debug.Log($"[TurnManager] New Wind Generated: {_currentWind}");
            OnWindChanged?.Invoke(_currentWind);
        }

        /// <summary>
        /// Locks input and tracks an active projectile.
        /// </summary>
        public void RegisterProjectile(Projectile projectile)
        {
            if (projectile == null) return;

            _activeProjectile = projectile;
            SetInputLocked(true);
            
            // Subscribe to projectile impact/destruction event
            projectile.OnImpact += HandleProjectileImpact;
            projectile.OnDestroyed += HandleProjectileDestroyed;
        }

        private void HandleProjectileImpact(Vector3 point, Vector3 normal)
        {
            // Can add impact delay or effects here if needed
        }

        private void HandleProjectileDestroyed()
        {
            if (_activeProjectile != null)
            {
                _activeProjectile.OnImpact -= HandleProjectileImpact;
                _activeProjectile.OnDestroyed -= HandleProjectileDestroyed;
                _activeProjectile = null;
            }

            AdvanceTurn();
        }

        /// <summary>
        /// Advances turn counter, generates new wind, and unlocks input.
        /// </summary>
        public void AdvanceTurn()
        {
            _currentTurn++;
            GenerateRandomWind();
            SetInputLocked(false);
            OnTurnChanged?.Invoke(_currentTurn);
            Debug.Log($"[TurnManager] Turn Advanced to {_currentTurn}");
        }

        /// <summary>
        /// Sets input lock state and notifies listeners.
        /// </summary>
        public void SetInputLocked(bool locked)
        {
            _isInputLocked = locked;
            OnInputStateChanged?.Invoke(_isInputLocked);
        }
    }
}
