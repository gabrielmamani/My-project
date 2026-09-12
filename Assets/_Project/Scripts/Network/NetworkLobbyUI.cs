using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Gunbound.Player;

namespace Gunbound.Network
{
    /// <summary>
    /// UI Panel for Host / Client fast match creation and connection IP settings (RF-07.1.2).
    /// </summary>
    public class NetworkLobbyUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private InputField _ipInputField;
        [SerializeField] private InputField _portInputField;
        [SerializeField] private Button _btnHost;
        [SerializeField] private Button _btnClient;
        [SerializeField] private Button _btnDisconnect;
        [SerializeField] private Button _btnSimulateTimeout;
        [SerializeField] private Text _statusText;
        [SerializeField] private Text _roleText;

        [Header("Mobile Garage Selection (RF-5.1.1)")]
        [SerializeField] private Button _btnSelectMage;
        [SerializeField] private Button _btnSelectArmor;
        [SerializeField] private Button _btnSelectBoomer;
        [SerializeField] private Text _mobileStatsText;

        public InputField IpInputField => _ipInputField;
        public InputField PortInputField => _portInputField;
        public Button BtnHost => _btnHost;
        public Button BtnClient => _btnClient;
        public Button BtnDisconnect => _btnDisconnect;
        public Button BtnSimulateTimeout => _btnSimulateTimeout;
        public Button BtnSelectMage => _btnSelectMage;
        public Button BtnSelectArmor => _btnSelectArmor;
        public Button BtnSelectBoomer => _btnSelectBoomer;
        public Text StatusText => _statusText;
        public Text RoleText => _roleText;
        public Text MobileStatsText => _mobileStatsText;

        private void Start()
        {
            SetupListeners();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
                NetworkGameManager.Instance.OnStatusMessageChanged += HandleStatusChanged;
                UpdateUIState(NetworkGameManager.Instance.CurrentState);
            }
            else if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.OnNetworkRoleChanged += HandleRoleChanged;
                NetworkLobbyManager.Instance.OnStatusMessageChanged += HandleStatusChanged;
                UpdateRoleState(NetworkLobbyManager.Instance.CurrentRole);
            }
        }

        private void OnDestroy()
        {
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
                NetworkGameManager.Instance.OnStatusMessageChanged -= HandleStatusChanged;
            }

            if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.OnNetworkRoleChanged -= HandleRoleChanged;
                NetworkLobbyManager.Instance.OnStatusMessageChanged -= HandleStatusChanged;
            }
        }

        public void BindFields(InputField ipInput, InputField portInput, Button hostBtn, Button clientBtn, Button disconnectBtn, Button timeoutBtn, Text statusTxt, Text roleTxt)
        {
            _ipInputField = ipInput;
            _portInputField = portInput;
            _btnHost = hostBtn;
            _btnClient = clientBtn;
            _btnDisconnect = disconnectBtn;
            _btnSimulateTimeout = timeoutBtn;
            _statusText = statusTxt;
            _roleText = roleTxt;

            SetupListeners();
        }

        private void SetupListeners()
        {
            if (_btnHost != null)
            {
                _btnHost.onClick.RemoveAllListeners();
                _btnHost.onClick.AddListener(OnHostClicked);
            }

            if (_btnClient != null)
            {
                _btnClient.onClick.RemoveAllListeners();
                _btnClient.onClick.AddListener(OnClientClicked);
            }

            if (_btnDisconnect != null)
            {
                _btnDisconnect.onClick.RemoveAllListeners();
                _btnDisconnect.onClick.AddListener(OnDisconnectClicked);
            }

            if (_btnSimulateTimeout != null)
            {
                _btnSimulateTimeout.onClick.RemoveAllListeners();
                _btnSimulateTimeout.onClick.AddListener(OnSimulateTimeoutClicked);
            }

            if (_btnSelectMage != null)
            {
                _btnSelectMage.onClick.RemoveAllListeners();
                _btnSelectMage.onClick.AddListener(() => OnMobileSelected(MobileType.Mage));
            }

            if (_btnSelectArmor != null)
            {
                _btnSelectArmor.onClick.RemoveAllListeners();
                _btnSelectArmor.onClick.AddListener(() => OnMobileSelected(MobileType.Armor));
            }

            if (_btnSelectBoomer != null)
            {
                _btnSelectBoomer.onClick.RemoveAllListeners();
                _btnSelectBoomer.onClick.AddListener(() => OnMobileSelected(MobileType.Boomer));
            }
        }

        public void BindMobileButtons(Button btnMage, Button btnArmor, Button btnBoomer, Text statsTxt)
        {
            _btnSelectMage = btnMage;
            _btnSelectArmor = btnArmor;
            _btnSelectBoomer = btnBoomer;
            _mobileStatsText = statsTxt;

            SetupListeners();
            UpdateStatsDisplay(MobileType.Mage);
        }

        public void OnMobileSelected(MobileType type)
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();

            int playerIndex = 1;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsServer)
            {
                playerIndex = 2;
            }

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RequestSelectMobile(playerIndex, type);
            }

            UpdateStatsDisplay(type);
        }

        public void UpdateStatsDisplay(MobileType type)
        {
            if (_mobileStatsText == null) return;

            MobileData data = NetworkSpawnerManager.Instance != null ? NetworkSpawnerManager.Instance.GetMobileData(type) : null;

            if (data != null)
            {
                _mobileStatsText.text = $"<b>{data.MobileName}</b> | HP: {data.MaxHealth} | Armadura: {data.ArmorDefense * 100:F0}%\nÁngulo: {data.MinAngle}°–{data.MaxAngle}° | Vel: {data.MoveSpeed} | Delay: {data.BaseDelay}";
            }
            else
            {
                switch (type)
                {
                    case MobileType.Armor:
                        _mobileStatsText.text = "<b>Armor Tank</b> | HP: 1100 | Armadura: 15%\nÁngulo: 15°–60° | Vel: 3.2 | Delay: 270";
                        break;
                    case MobileType.Boomer:
                        _mobileStatsText.text = "<b>Boomer Mobile</b> | HP: 900 | Armadura: 0%\nÁngulo: 10°–75° | Vel: 4.5 | Delay: 230";
                        break;
                    default:
                        _mobileStatsText.text = "<b>Mage Mobile</b> | HP: 1000 | Armadura: 5%\nÁngulo: 20°–70° | Vel: 4.0 | Delay: 250";
                        break;
                }
            }
        }

        public void OnHostClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();
            ApplySettingsToManager();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.StartHostSession();
            }
            else if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.StartHost();
            }
        }

        public void OnClientClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();
            ApplySettingsToManager();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.StartClientSession();
            }
            else if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.StartClient();
            }
        }

        public void OnDisconnectClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ShutdownSession();
            }
            else if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.Disconnect();
            }
        }

        public void OnSimulateTimeoutClicked()
        {
            Gunbound.Core.AudioManager.Instance?.PlayClickSFX();

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SimulateClientDisconnection();
            }
            else if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.SimulateClientDisconnection();
            }
        }

        private void ApplySettingsToManager()
        {
            string ip = _ipInputField != null ? _ipInputField.text : "127.0.0.1";
            ushort port = 7777;
            if (_portInputField != null && ushort.TryParse(_portInputField.text, out ushort parsedPort))
            {
                port = parsedPort;
            }

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SetConnectionInfo(ip, port);
            }

            if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.SetAddressAndPort(ip, port);
            }
        }

        private void HandleGameStateChanged(NetworkGameState state)
        {
            UpdateUIState(state);
        }

        private void HandleRoleChanged(NetworkRole role)
        {
            UpdateRoleState(role);
        }

        private void HandleStatusChanged(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        private void UpdateUIState(NetworkGameState state)
        {
            bool isOffline = state == NetworkGameState.Offline;
            bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

            if (_btnHost != null) _btnHost.interactable = isOffline;
            if (_btnClient != null) _btnClient.interactable = isOffline;
            if (_btnDisconnect != null) _btnDisconnect.interactable = !isOffline;
            if (_btnSimulateTimeout != null) _btnSimulateTimeout.interactable = !isOffline && (isHost || state == NetworkGameState.InGame || state == NetworkGameState.InLobby);

            if (_roleText != null)
            {
                _roleText.text = $"Estado: {state.ToString().ToUpper()}";
                _roleText.color = state == NetworkGameState.InGame ? new Color(0.2f, 0.85f, 1.0f) :
                                 (state == NetworkGameState.Connecting ? new Color(1.0f, 0.7f, 0.2f) :
                                 (state == NetworkGameState.InLobby ? new Color(0.3f, 0.9f, 0.4f) : Color.gray));
            }
        }

        private void UpdateRoleState(NetworkRole role)
        {
            bool isOffline = role == NetworkRole.Offline;

            if (_btnHost != null) _btnHost.interactable = isOffline;
            if (_btnClient != null) _btnClient.interactable = isOffline;
            if (_btnDisconnect != null) _btnDisconnect.interactable = !isOffline;
            if (_btnSimulateTimeout != null) _btnSimulateTimeout.interactable = (role == NetworkRole.Host);

            if (_roleText != null)
            {
                _roleText.text = $"Rol: {role.ToString().ToUpper()}";
                _roleText.color = role == NetworkRole.Host ? new Color(0.2f, 0.85f, 1.0f) : (role == NetworkRole.Client ? new Color(1.0f, 0.7f, 0.2f) : Color.gray);
            }
        }
    }
}
