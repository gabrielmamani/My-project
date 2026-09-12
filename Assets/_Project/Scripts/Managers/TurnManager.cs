using System;
using System.Collections;
using UnityEngine;
using Gunbound.Combat;
using Gunbound.Gameplay;
using Gunbound.Player;

namespace Gunbound.Managers
{
    /// <summary>
    /// Manages 1v1 dynamic turn progression based on accumulated delay, active player selection,
    /// 20-second turn countdown timer, double turn detection, wind variation, and game over detection.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public static TurnManager Instance { get; private set; }

        [Header("Player References")]
        [SerializeField] private PlayerController _player1;
        [SerializeField] private PlayerController _player2;

        [Header("Wind Configuration")]
        [SerializeField] private float _minWindX = -6.0f;
        [SerializeField] private float _maxWindX = 6.0f;
        [SerializeField] private float _minWindY = -1.5f;
        [SerializeField] private float _maxWindY = 1.5f;

        [Header("Turn & Delay Settings")]
        [SerializeField] private float _turnDuration = 20.0f;
        [SerializeField] private int _currentTurn = 1;
        [SerializeField] private Vector3 _currentWind = Vector3.zero;
        [SerializeField] private bool _isInputLocked = false;
        [SerializeField] private float _postImpactDelay = 1.0f;

        private PlayerController _activePlayer;
        private Projectile _activeProjectile;
        private bool _isGameOver = false;

        private float _currentTurnTimer;
        private bool _isTimerActive = false;

        // Events
        public event Action<Vector3> OnWindChanged;
        public event Action<int> OnTurnChanged;
        public event Action<int, int> OnTurnPlayerChanged; // (turnNumber, activePlayerNumber)
        public event Action<bool> OnInputStateChanged;
        public event Action<int> OnGameOver; // (winnerPlayerNumber: 1 or 2)
        public event Action<int, int> OnDelaysUpdated; // (p1Delay, p2Delay)
        public event Action<float> OnTurnTimerUpdated; // (remainingSeconds)
        public event Action<int> OnDoubleTurnTriggered; // (activePlayerNumber)
        public event Action<int> OnTurnTimeout; // (activePlayerNumber)
        public event Action<Projectile> OnProjectileSpawned;
        public event Action<Vector3> OnProjectileImpacted;

        public Vector3 CurrentWind => _currentWind;
        public int CurrentTurn => _currentTurn;
        public bool IsInputLocked => _isInputLocked;
        public PlayerController ActivePlayer => _activePlayer;
        public int ActivePlayerNumber => (_activePlayer == _player1) ? 1 : 2;
        public bool IsGameOver => _isGameOver;

        public PlayerController Player1 => _player1;
        public PlayerController Player2 => _player2;

        public int Player1Delay => _player1 != null ? _player1.CurrentDelay : 0;
        public int Player2Delay => _player2 != null ? _player2.CurrentDelay : 0;
        public float RemainingTurnTime => _currentTurnTimer;

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
            ResolvePlayerReferences();
            
            // Initialize delays to 0 at game start
            if (_player1 != null) _player1.ResetDelay();
            if (_player2 != null) _player2.ResetDelay();

            _activePlayer = _player1 != null ? _player1 : FindAnyObjectByType<PlayerController>();

            UpdatePlayerTurnStates();
            SubscribeHealthEvents();

            GenerateRandomWind();
            NotifyDelayChanged();

            OnTurnChanged?.Invoke(_currentTurn);
            OnTurnPlayerChanged?.Invoke(_currentTurn, ActivePlayerNumber);
            SetInputLocked(false);

            StartTurnTimer();
        }

        private void Update()
        {
            if (_isTimerActive && !_isInputLocked && !_isGameOver)
            {
                _currentTurnTimer -= Time.deltaTime;
                if (_currentTurnTimer <= 0f)
                {
                    _currentTurnTimer = 0f;
                    _isTimerActive = false;
                    HandleTurnTimeout();
                }
                OnTurnTimerUpdated?.Invoke(_currentTurnTimer);
            }
        }

        private void ResolvePlayerReferences()
        {
            if (_player1 == null)
            {
                var p1Obj = GameObject.Find("Player_Mage");
                if (p1Obj != null) _player1 = p1Obj.GetComponent<PlayerController>();
            }

            if (_player2 == null)
            {
                var p2Obj = GameObject.Find("Player_2");
                if (p2Obj != null) _player2 = p2Obj.GetComponent<PlayerController>();
            }

            if (_player1 == null) _player1 = FindAnyObjectByType<PlayerController>();
        }

        public void SetPlayers(PlayerController p1, PlayerController p2)
        {
            _player1 = p1;
            _player2 = p2;

            if (_player1 != null) _player1.ResetDelay();
            if (_player2 != null) _player2.ResetDelay();

            _activePlayer = _player1;
            UpdatePlayerTurnStates();
            SubscribeHealthEvents();
            NotifyDelayChanged();
            StartTurnTimer();
        }

        private void SubscribeHealthEvents()
        {
            if (_player1 != null && _player1.TryGetComponent<Health>(out var health1))
            {
                health1.OnDeath += () => HandlePlayerDeath(1);
            }

            if (_player2 != null && _player2.TryGetComponent<Health>(out var health2))
            {
                health2.OnDeath += () => HandlePlayerDeath(2);
            }
        }

        public void NotifyDelayChanged()
        {
            OnDelaysUpdated?.Invoke(Player1Delay, Player2Delay);
        }

        public void NotifyShotFired()
        {
            _isTimerActive = false;
        }

        private void StartTurnTimer()
        {
            if (_isGameOver) return;
            _currentTurnTimer = _turnDuration;
            _isTimerActive = true;
            OnTurnTimerUpdated?.Invoke(_currentTurnTimer);
        }

        private void HandleTurnTimeout()
        {
            if (_isGameOver) return;

            Debug.Log($"[TurnManager] Turn Timer EXPIRED for Player {ActivePlayerNumber}!");
            SetInputLocked(true);

            if (_activePlayer != null)
            {
                _activePlayer.ApplyTimeoutPenalty();
            }

            OnTurnTimeout?.Invoke(ActivePlayerNumber);
            StartCoroutine(AdvanceTurnDelayed(_postImpactDelay));
        }

        /// <summary>
        /// Generates a new random wind vector with 360° radial direction and progressive intensity (RF-02.1).
        /// </summary>
        public void GenerateRandomWind()
        {
            float angleRad = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float speed = UnityEngine.Random.Range(0f, 15.0f);
            float windX = Mathf.Cos(angleRad) * speed;
            float windY = Mathf.Sin(angleRad) * (speed * 0.25f);
            _currentWind = new Vector3(windX, windY, 0f);

            Debug.Log($"[TurnManager] 360° Radial Wind Generated: {_currentWind} (Speed: {speed:F1} m/s)");
            OnWindChanged?.Invoke(_currentWind);
        }

        /// <summary>
        /// Locks input and tracks an active projectile.
        /// </summary>
        public void RegisterProjectile(Projectile projectile)
        {
            if (projectile == null || _isGameOver) return;

            _activeProjectile = projectile;
            SetInputLocked(true);
            
            projectile.OnImpact += HandleProjectileImpact;
            projectile.OnDestroyed += HandleProjectileDestroyed;

            OnProjectileSpawned?.Invoke(projectile);
        }

        private void HandleProjectileImpact(Vector3 point, Vector3 normal)
        {
            OnProjectileImpacted?.Invoke(point);
        }

        private void HandleProjectileDestroyed()
        {
            if (_activeProjectile != null)
            {
                _activeProjectile.OnImpact -= HandleProjectileImpact;
                _activeProjectile.OnDestroyed -= HandleProjectileDestroyed;
                _activeProjectile = null;
            }

            if (!_isGameOver)
            {
                StartCoroutine(AdvanceTurnDelayed(_postImpactDelay));
            }
        }

        private IEnumerator AdvanceTurnDelayed(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);

            if (!_isGameOver)
            {
                AdvanceTurn();
            }
        }

        /// <summary>
        /// Advances turn counter, evaluates next active player based on lowest accumulated delay,
        /// checks for double turns, handles Dual item turn retention, generates new wind, and resets turn timer.
        /// </summary>
        public void AdvanceTurn()
        {
            if (_isGameOver) return;

            // Dual Item Turn Retention: If active player used Dual, retain turn for 2nd shot
            if (_activePlayer != null && _activePlayer.IsDualActive)
            {
                _activePlayer.ConsumeDual();
                _currentTurn++;

                UpdatePlayerTurnStates();
                GenerateRandomWind();
                SetInputLocked(false);

                OnTurnChanged?.Invoke(_currentTurn);
                OnTurnPlayerChanged?.Invoke(_currentTurn, ActivePlayerNumber);
                NotifyDelayChanged();
                StartTurnTimer();

                Debug.Log($"[TurnManager] DUAL Item: Player {ActivePlayerNumber} retains control for second shot!");
                return;
            }

            PlayerController previousPlayer = _activePlayer;
            PlayerController nextPlayer = null;

            int p1Delay = Player1Delay;
            int p2Delay = Player2Delay;

            if (_player1 != null && _player2 != null)
            {
                if (p1Delay < p2Delay)
                {
                    nextPlayer = _player1;
                }
                else if (p2Delay < p1Delay)
                {
                    nextPlayer = _player2;
                }
                else
                {
                    // Equal delay: swap to the other player for fair alternating
                    nextPlayer = (previousPlayer == _player1) ? _player2 : _player1;
                }
            }
            else
            {
                nextPlayer = _player1 != null ? _player1 : _player2;
            }

            bool isDoubleTurn = (nextPlayer == previousPlayer);
            _activePlayer = nextPlayer;
            _currentTurn++;

            UpdatePlayerTurnStates();
            GenerateRandomWind();
            SetInputLocked(false);

            OnTurnChanged?.Invoke(_currentTurn);
            OnTurnPlayerChanged?.Invoke(_currentTurn, ActivePlayerNumber);
            NotifyDelayChanged();

            if (isDoubleTurn)
            {
                Debug.Log($"[TurnManager] DOUBLE TURN! Player {ActivePlayerNumber} retains control (P1: {p1Delay} | P2: {p2Delay})");
                OnDoubleTurnTriggered?.Invoke(ActivePlayerNumber);
            }

            StartTurnTimer();

            Debug.Log($"[TurnManager] Turn Advanced to {_currentTurn}. Active Player: Jugador {ActivePlayerNumber} (P1 Delay: {p1Delay} | P2 Delay: {p2Delay})");
        }

        private void UpdatePlayerTurnStates()
        {
            if (_player1 != null) _player1.SetTurnActive(_activePlayer == _player1);
            if (_player2 != null) _player2.SetTurnActive(_activePlayer == _player2);
        }

        private void HandlePlayerDeath(int deadPlayerNumber)
        {
            if (_isGameOver) return;

            _isGameOver = true;
            _isTimerActive = false;
            int winnerNumber = (deadPlayerNumber == 1) ? 2 : 1;

            SetInputLocked(true);
            if (_player1 != null) _player1.SetTurnActive(false);
            if (_player2 != null) _player2.SetTurnActive(false);

            Debug.Log($"[TurnManager] GAME OVER! Player {deadPlayerNumber} was eliminated. Player {winnerNumber} wins!");
            OnGameOver?.Invoke(winnerNumber);
        }

        /// <summary>
        /// Handles automatic victory declaration on rival network disconnection timeout (RF-07.1.3).
        /// </summary>
        public void HandleDisconnectionVictory(int winnerPlayerNumber)
        {
            if (_isGameOver) return;

            _isGameOver = true;
            _isTimerActive = false;

            SetInputLocked(true);
            if (_player1 != null) _player1.SetTurnActive(false);
            if (_player2 != null) _player2.SetTurnActive(false);

            Debug.Log($"[TurnManager] DISCONNECTION VICTORY! Opponent disconnected. Player {winnerPlayerNumber} wins!");
            OnGameOver?.Invoke(winnerPlayerNumber);
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

