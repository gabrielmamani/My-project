using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Gunbound.Managers;
using Gunbound.Network;
using Gunbound.Gameplay;

namespace Gunbound.UI
{
    /// <summary>
    /// UI Manager for Panel_MatchResult modal displayed at the end of a match.
    /// Handles victory/defeat headers, victory reasons (K.O., Bunge, Disconnection),
    /// match statistics, and lobby return/rematch actions.
    /// </summary>
    public class MatchResultUI : MonoBehaviour
    {
        public static MatchResultUI Instance { get; private set; }

        [Header("UI Component References")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _subtitleText;
        [SerializeField] private Text _winnerText;
        [SerializeField] private Text _statsTurnsText;
        [SerializeField] private Text _statsReasonText;
        [SerializeField] private Button _btnRematch;
        [SerializeField] private Button _btnReturnToLobby;

        [Header("Colors")]
        [SerializeField] private Color _victoryColor = new Color(0.1f, 0.95f, 0.4f, 1.0f);
        [SerializeField] private Color _defeatColor = new Color(0.95f, 0.2f, 0.25f, 1.0f);

        private bool _hasMatchEnded = false;
        private string _lastVictoryReason = "Victoria por K.O.";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }

            BindButtons();
        }

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnGameOver += HandleGameOver;
            }

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnDisconnectionVictory += HandleDisconnectionVictory;
                NetworkGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnGameOver -= HandleGameOver;
            }

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnDisconnectionVictory -= HandleDisconnectionVictory;
                NetworkGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }

        private void BindButtons()
        {
            if (_btnRematch != null)
            {
                _btnRematch.onClick.RemoveAllListeners();
                _btnRematch.onClick.AddListener(OnRematchClicked);
            }

            if (_btnReturnToLobby != null)
            {
                _btnReturnToLobby.onClick.RemoveAllListeners();
                _btnReturnToLobby.onClick.AddListener(OnReturnToLobbyClicked);
            }
        }

        /// <summary>
        /// Explicitly sets the reason for victory before game over triggers (e.g. Bunge vs K.O.).
        /// </summary>
        public void SetVictoryReason(string reason)
        {
            _lastVictoryReason = reason;
        }

        private void HandleGameOver(int winnerPlayerNumber)
        {
            ShowMatchResult(winnerPlayerNumber, _lastVictoryReason);
        }

        private void HandleDisconnectionVictory(int winnerPlayerNumber)
        {
            ShowMatchResult(winnerPlayerNumber, "Victoria Técnica por Desconexión del Rival (10s)");
        }

        private void HandleGameStateChanged(NetworkGameState state)
        {
            if (state == NetworkGameState.PostMatch && !_hasMatchEnded)
            {
                ShowMatchResult(1, _lastVictoryReason);
            }
        }

        /// <summary>
        /// Displays the Panel_MatchResult modal with contextual victory/defeat info and stats.
        /// </summary>
        public void ShowMatchResult(int winnerPlayerNumber, string reason)
        {
            if (_hasMatchEnded) return;
            _hasMatchEnded = true;

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
            }

            int localPlayerNum = 1; // Default Host = 1
            if (NetworkGameManager.Instance != null && NetworkGameManager.Instance.IsConnected)
            {
                localPlayerNum = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer ? 1 : 2;
            }

            bool isLocalWinner = (winnerPlayerNumber == localPlayerNum);

            if (_titleText != null)
            {
                _titleText.text = isLocalWinner ? "¡VICTORIA!" : "¡DERROTA!";
                _titleText.color = isLocalWinner ? _victoryColor : _defeatColor;
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = isLocalWinner ? "¡Has dominado el campo de batalla!" : "¡Mejor suerte en la próxima ronda!";
            }

            if (_winnerText != null)
            {
                _winnerText.text = $"Ganador: Jugador {winnerPlayerNumber} {(winnerPlayerNumber == 1 ? "(Host)" : "(Cliente)")}";
            }

            if (_statsReasonText != null)
            {
                _statsReasonText.text = $"Motivo: {reason}";
            }

            if (_statsTurnsText != null && TurnManager.Instance != null)
            {
                _statsTurnsText.text = $"Turnos jugados: {TurnManager.Instance.CurrentTurn}";
            }

            Debug.Log($"[MatchResultUI] Panel_MatchResult activado. Ganador: Jugador {winnerPlayerNumber} | Motivo: {reason}");
        }

        private void OnRematchClicked()
        {
            Debug.Log("[MatchResultUI] Btn_Rematch pulsado. Recargando escena actual...");
            _hasMatchEnded = false;
            if (_resultPanel != null) _resultPanel.SetActive(false);

            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }

        private void OnReturnToLobbyClicked()
        {
            Debug.Log("[MatchResultUI] Btn_ReturnToLobby pulsado. Desconectando red y regresando a Lobby...");
            _hasMatchEnded = false;
            if (_resultPanel != null) _resultPanel.SetActive(false);

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ShutdownSession();
            }

            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.name);
        }
    }
}
