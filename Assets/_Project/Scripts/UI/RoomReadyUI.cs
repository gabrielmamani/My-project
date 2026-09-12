using UnityEngine;
using UnityEngine.UI;
using Gunbound.Core;
using Gunbound.Network;
using Gunbound.Player;

namespace Gunbound.UI
{
    /// <summary>
    /// Room Ready Screen UI controller for P1 (Host) and P2 (Client) readiness cards, tank selection,
    /// and authoritative match start (RF-6.4.1, RF-6.4.2).
    /// </summary>
    public class RoomReadyUI : MonoBehaviour
    {
        [Header("Player 1 (Host) Card References")]
        [SerializeField] private Text _p1NameText;
        [SerializeField] private Text _p1MobileText;
        [SerializeField] private Text _p1ReadyStatusText;

        [Header("Player 2 (Client) Card References")]
        [SerializeField] private Text _p2NameText;
        [SerializeField] private Text _p2MobileText;
        [SerializeField] private Text _p2ReadyStatusText;

        [Header("Control Buttons")]
        [SerializeField] private Button _btnToggleReady;
        [SerializeField] private Button _btnStartMatch;
        [SerializeField] private Button _btnLeaveRoom;
        [SerializeField] private Text _roomStatusText;

        [Header("Map Selection (RF-5.4.1)")]
        [SerializeField] private Button _btnMapMiramo;
        [SerializeField] private Button _btnMapMetamine;
        [SerializeField] private Button _btnMapCozy;
        [SerializeField] private Text _selectedMapText;

        private void Start()
        {
            SetupListeners();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnReadyStateChanged += HandleReadyStateChanged;
                NetworkGameManager.Instance.OnMobileSelectionChanged += HandleMobileSelectionChanged;
                NetworkGameManager.Instance.OnStatusMessageChanged += HandleStatusMessageChanged;
                NetworkGameManager.Instance.OnMapSelectionChanged += HandleMapSelectionChanged;
                UpdateRoomStateUI();
            }
        }

        private void OnDestroy()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnReadyStateChanged -= HandleReadyStateChanged;
                NetworkGameManager.Instance.OnMobileSelectionChanged -= HandleMobileSelectionChanged;
                NetworkGameManager.Instance.OnStatusMessageChanged -= HandleStatusMessageChanged;
                NetworkGameManager.Instance.OnMapSelectionChanged -= HandleMapSelectionChanged;
            }
        }

        public void BindFields(Text p1Name, Text p1Mobile, Text p1Ready, Text p2Name, Text p2Mobile, Text p2Ready, Button btnReady, Button btnStart, Button btnLeave, Text statusText)
        {
            _p1NameText = p1Name;
            _p1MobileText = p1Mobile;
            _p1ReadyStatusText = p1Ready;
            _p2NameText = p2Name;
            _p2MobileText = p2Mobile;
            _p2ReadyStatusText = p2Ready;
            _btnToggleReady = btnReady;
            _btnStartMatch = btnStart;
            _btnLeaveRoom = btnLeave;
            _roomStatusText = statusText;

            SetupListeners();
            UpdateRoomStateUI();
        }

        private void SetupListeners()
        {
            if (_btnToggleReady != null)
            {
                _btnToggleReady.onClick.RemoveAllListeners();
                _btnToggleReady.onClick.AddListener(OnToggleReadyClicked);
            }
            if (_btnStartMatch != null)
            {
                _btnStartMatch.onClick.RemoveAllListeners();
                _btnStartMatch.onClick.AddListener(OnStartMatchClicked);
            }
            if (_btnLeaveRoom != null)
            {
                _btnLeaveRoom.onClick.RemoveAllListeners();
                _btnLeaveRoom.onClick.AddListener(OnLeaveRoomClicked);
            }

            if (_btnMapMiramo != null)
            {
                _btnMapMiramo.onClick.RemoveAllListeners();
                _btnMapMiramo.onClick.AddListener(() => OnSelectMapClicked(Gunbound.Environment.MapType.MiramoTown));
            }

            if (_btnMapMetamine != null)
            {
                _btnMapMetamine.onClick.RemoveAllListeners();
                _btnMapMetamine.onClick.AddListener(() => OnSelectMapClicked(Gunbound.Environment.MapType.Metamine));
            }

            if (_btnMapCozy != null)
            {
                _btnMapCozy.onClick.RemoveAllListeners();
                _btnMapCozy.onClick.AddListener(() => OnSelectMapClicked(Gunbound.Environment.MapType.CozyCave));
            }
        }

        public void BindMapButtons(Button btnMiramo, Button btnMetamine, Button btnCozy, Text selectedMapTxt)
        {
            _btnMapMiramo = btnMiramo;
            _btnMapMetamine = btnMetamine;
            _btnMapCozy = btnCozy;
            _selectedMapText = selectedMapTxt;
            SetupListeners();
            UpdateRoomStateUI();
        }

        private void OnSelectMapClicked(Gunbound.Environment.MapType mapType)
        {
            AudioManager.Instance?.PlayClickSFX();
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RequestSelectMap(mapType);
            }
            UpdateRoomStateUI();
        }

        private void HandleMapSelectionChanged(Gunbound.Environment.MapType mapType)
        {
            UpdateRoomStateUI();
        }

        private void HandleReadyStateChanged(int playerIndex, bool isReady)
        {
            UpdateRoomStateUI();
        }

        private void HandleMobileSelectionChanged(int playerIndex, MobileType mobileType)
        {
            UpdateRoomStateUI();
        }

        private void HandleStatusMessageChanged(string message)
        {
            if (_roomStatusText != null)
            {
                _roomStatusText.text = message;
            }
        }

        public void UpdateRoomStateUI()
        {
            if (NetworkGameManager.Instance == null) return;

            string myName = UserDataManager.Instance != null ? UserDataManager.Instance.Nickname : "Jugador";
            bool isHost = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsServer;

            // Player 1 Card (Host)
            if (_p1NameText != null) _p1NameText.text = isHost ? $"<b>P1 (Tú - Host):</b> {myName}" : "<b>P1 (Host):</b> Matias";
            if (_p1MobileText != null) _p1MobileText.text = $"Móvil: <b>{NetworkGameManager.Instance.P1SelectedMobile}</b>";
            if (_p1ReadyStatusText != null)
            {
                bool r1 = NetworkGameManager.Instance.IsP1Ready;
                _p1ReadyStatusText.text = r1 ? "<color=#33FF66>¡LISTO!</color>" : "<color=#FFCC00>PREPARANDO</color>";
            }

            // Player 2 Card (Client)
            if (_p2NameText != null) _p2NameText.text = !isHost ? $"<b>P2 (Tú - Rival):</b> {myName}" : "<b>P2 (Rival):</b> Esperando oponente...";
            if (_p2MobileText != null) _p2MobileText.text = $"Móvil: <b>{NetworkGameManager.Instance.P2SelectedMobile}</b>";
            if (_p2ReadyStatusText != null)
            {
                bool r2 = NetworkGameManager.Instance.IsP2Ready;
                _p2ReadyStatusText.text = r2 ? "<color=#33FF66>¡LISTO!</color>" : "<color=#FFCC00>PREPARANDO</color>";
            }

            // Map Selection Card
            if (_selectedMapText != null)
            {
                _selectedMapText.text = $"Mapa Seleccionado: <b>{NetworkGameManager.Instance.SelectedMap}</b>";
            }

            if (_btnMapMiramo != null) _btnMapMiramo.interactable = isHost;
            if (_btnMapMetamine != null) _btnMapMetamine.interactable = isHost;
            if (_btnMapCozy != null) _btnMapCozy.interactable = isHost;

            // Button States
            bool bothReady = NetworkGameManager.Instance.IsP1Ready && NetworkGameManager.Instance.IsP2Ready;
            if (_btnStartMatch != null)
            {
                _btnStartMatch.gameObject.SetActive(isHost);
                _btnStartMatch.interactable = bothReady || isHost;
            }

            if (_roomStatusText != null)
            {
                _roomStatusText.text = bothReady ? "¡Ambos jugadores LISTOS! Pulsa INICIAR COMBATE." : "Esperando confirmación de preparación de ambos jugadores...";
            }
        }

        public void OnToggleReadyClicked()
        {
            AudioManager.Instance?.PlayClickSFX();

            int localPlayerIndex = 1;
            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening && !Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                localPlayerIndex = 2;
            }

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RequestToggleReady(localPlayerIndex);
            }

            UpdateRoomStateUI();
        }

        public void OnStartMatchClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[RoomReadyUI] Host started match! Transitioning to InGame HUD & spawning tanks.");

            if (NetworkSpawnerManager.Instance != null)
            {
                NetworkSpawnerManager.Instance.ResolveAndSpawnPlayers();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowInGameHUD();
            }
        }

        public void OnLeaveRoomClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[RoomReadyUI] Leaving room -> Shutting down session");

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ShutdownSession();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowMainMenu();
            }
        }
    }
}
