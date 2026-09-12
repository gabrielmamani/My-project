using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.Network
{
    public enum NetworkGameState
    {
        Offline,
        Connecting,
        InLobby,
        InGame,
        PostMatch
    }

    /// <summary>
    /// NetworkGameManager for 1v1 authoritative multiplayer (RF-4.1.1, RF-4.1.3).
    /// Manages network lifecycle, game state synchronization via NetworkVariable,
    /// UDP connection parameters with UnityTransport, and 10-second client disconnection timeout.
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        public static NetworkGameManager Instance { get; private set; }

        [Header("Connection Settings")]
        [SerializeField] private string _serverIp = "127.0.0.1";
        [SerializeField] private ushort _serverPort = 7777;
        [SerializeField] private float _disconnectTimeout = 10.0f;

        [Header("State Tracking")]
        [SerializeField] private NetworkGameState _localState = NetworkGameState.Offline;
        private NetworkVariable<NetworkGameState> _syncedGameState = new NetworkVariable<NetworkGameState>(
            NetworkGameState.Offline,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        [Header("Mobile Selection Synchronization (RF-5.1.2)")]
        private NetworkVariable<int> _p1MobileType = new NetworkVariable<int>(
            (int)MobileType.Mage,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private NetworkVariable<int> _p2MobileType = new NetworkVariable<int>(
            (int)MobileType.Mage,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Room Readiness Synchronization (RF-6.4.2)")]
        private NetworkVariable<bool> _p1Ready = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
        private NetworkVariable<bool> _p2Ready = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        [Header("Map Environment Synchronization (RF-5.4.1)")]
        private NetworkVariable<int> _selectedMapType = new NetworkVariable<int>(
            (int)Gunbound.Environment.MapType.MiramoTown,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private float _disconnectTimer = 0f;
        private bool _isTrackingDisconnect = false;
        private Coroutine _disconnectCoroutine;

        // Events
        public event Action<NetworkGameState> OnGameStateChanged;
        public event Action<string> OnStatusMessageChanged;
        public event Action<ulong> OnPlayerJoined;
        public event Action<ulong> OnPlayerLeft;
        public event Action<int> OnDisconnectionVictory; // (winnerPlayerNumber: 1 or 2)
        public event Action<int, MobileType> OnMobileSelectionChanged; // (playerIndex: 1 or 2, selectedType)
        public event Action<int, bool> OnReadyStateChanged; // (playerIndex: 1 or 2, isReady)
        public event Action<Gunbound.Environment.MapType> OnMapSelectionChanged;

        public MobileType P1SelectedMobile => (MobileType)_p1MobileType.Value;
        public MobileType P2SelectedMobile => (MobileType)_p2MobileType.Value;
        public bool IsP1Ready => _p1Ready.Value;
        public bool IsP2Ready => _p2Ready.Value;
        public Gunbound.Environment.MapType SelectedMap => (Gunbound.Environment.MapType)_selectedMapType.Value;

        public NetworkGameState CurrentState => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening 
            ? _syncedGameState.Value 
            : _localState;

        public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        public string ServerIp => _serverIp;
        public ushort ServerPort => _serverPort;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
            }

            _syncedGameState.OnValueChanged += HandleSyncedStateChanged;
            _p1MobileType.OnValueChanged += (prev, current) => OnMobileSelectionChanged?.Invoke(1, (MobileType)current);
            _p2MobileType.OnValueChanged += (prev, current) => OnMobileSelectionChanged?.Invoke(2, (MobileType)current);
            _p1Ready.OnValueChanged += (prev, current) => OnReadyStateChanged?.Invoke(1, current);
            _p2Ready.OnValueChanged += (prev, current) => OnReadyStateChanged?.Invoke(2, current);
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            }

            _syncedGameState.OnValueChanged -= HandleSyncedStateChanged;

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _selectedMapType.OnValueChanged += HandleMapTypeChanged;
            if (IsServer)
            {
                _syncedGameState.Value = NetworkGameState.InLobby;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _selectedMapType.OnValueChanged -= HandleMapTypeChanged;
        }

        private void HandleMapTypeChanged(int previousValue, int newValue)
        {
            Gunbound.Environment.MapType mapType = (Gunbound.Environment.MapType)newValue;
            Debug.Log($"[NetworkGameManager] Map selection changed in network: {(Gunbound.Environment.MapType)previousValue} -> {mapType}");
            OnMapSelectionChanged?.Invoke(mapType);
        }

        /// <summary>
        /// Requests selecting map environment by Host (RF-5.4.1).
        /// </summary>
        public void RequestSelectMap(Gunbound.Environment.MapType mapType)
        {
            if (IsServer)
            {
                _selectedMapType.Value = (int)mapType;
            }
            else if (IsClient)
            {
                SelectMapServerRpc((int)mapType);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectMapServerRpc(int mapTypeIndex)
        {
            _selectedMapType.Value = mapTypeIndex;
            Debug.Log($"[NetworkGameManager] ServerRpc SelectMap: Map set to {(Gunbound.Environment.MapType)mapTypeIndex}");
        }

        /// <summary>
        /// Requests selecting a mobile type for player 1 or 2 (RF-5.1.2).
        /// </summary>
        public void RequestSelectMobile(int playerIndex, MobileType type)
        {
            if (IsServer)
            {
                if (playerIndex == 1) _p1MobileType.Value = (int)type;
                else if (playerIndex == 2) _p2MobileType.Value = (int)type;
            }
            else if (IsClient)
            {
                SelectMobileServerRpc(playerIndex, (int)type);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SelectMobileServerRpc(int playerIndex, int mobileTypeIndex)
        {
            if (playerIndex == 1) _p1MobileType.Value = mobileTypeIndex;
            else if (playerIndex == 2) _p2MobileType.Value = mobileTypeIndex;
            Debug.Log($"[NetworkGameManager] ServerRpc SelectMobile: P{playerIndex} set to {(MobileType)mobileTypeIndex}");
        }

        /// <summary>
        /// Requests toggling readiness state for player 1 or 2 (RF-6.4.2).
        /// </summary>
        public void RequestToggleReady(int playerIndex)
        {
            if (IsServer)
            {
                if (playerIndex == 1) _p1Ready.Value = !_p1Ready.Value;
                else if (playerIndex == 2) _p2Ready.Value = !_p2Ready.Value;
            }
            else if (IsClient)
            {
                ToggleReadyServerRpc(playerIndex);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleReadyServerRpc(int playerIndex)
        {
            if (playerIndex == 1) _p1Ready.Value = !_p1Ready.Value;
            else if (playerIndex == 2) _p2Ready.Value = !_p2Ready.Value;
            Debug.Log($"[NetworkGameManager] ServerRpc ToggleReady: P{playerIndex} ready state toggled.");
        }

        /// <summary>
        /// Updates the target IP address and UDP port on UnityTransport component.
        /// </summary>
        public void SetConnectionInfo(string ip, ushort port)
        {
            _serverIp = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
            _serverPort = port > 0 ? port : (ushort)7777;

            if (NetworkManager.Singleton != null)
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetConnectionData(_serverIp, _serverPort);
                }
            }
        }

        /// <summary>
        /// Starts session as Authoritative Host (Server + Player 1 client).
        /// </summary>
        public bool StartHostSession()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameManager] NetworkManager.Singleton missing!");
                UpdateStatus("Error: NetworkManager no existe");
                return false;
            }

            SetConnectionInfo(_serverIp, _serverPort);
            bool success = NetworkManager.Singleton.StartHost();

            if (success)
            {
                _localState = NetworkGameState.InLobby;
                Debug.Log($"[NetworkGameManager] HOST iniciado en {_serverIp}:{_serverPort} (Jugador 1)");
                UpdateStatus($"HOST Activo en {_serverIp}:{_serverPort} (Esperando P2...)");
                NotifyStateChanged(_localState);

                if (NetworkLobbyManager.Instance != null)
                {
                    NetworkLobbyManager.Instance.StartHost();
                }
            }
            else
            {
                Debug.LogError("[NetworkGameManager] Fallo al iniciar HOST.");
                UpdateStatus("Error al iniciar HOST.");
            }

            return success;
        }

        /// <summary>
        /// Connects as Client (Player 2) to target Host IP/Port.
        /// </summary>
        public bool StartClientSession()
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[NetworkGameManager] NetworkManager.Singleton missing!");
                UpdateStatus("Error: NetworkManager no existe");
                return false;
            }

            SetConnectionInfo(_serverIp, _serverPort);
            _localState = NetworkGameState.Connecting;
            UpdateStatus($"Conectando a Host {_serverIp}:{_serverPort}...");
            NotifyStateChanged(_localState);

            bool success = NetworkManager.Singleton.StartClient();
            if (success)
            {
                Debug.Log($"[NetworkGameManager] Conectando como CLIENTE a {_serverIp}:{_serverPort}...");
                if (NetworkLobbyManager.Instance != null)
                {
                    NetworkLobbyManager.Instance.StartClient();
                }
            }
            else
            {
                _localState = NetworkGameState.Offline;
                Debug.LogError("[NetworkGameManager] Fallo al conectar CLIENTE.");
                UpdateStatus("Error al conectar Cliente.");
                NotifyStateChanged(_localState);
            }

            return success;
        }

        /// <summary>
        /// Shutdowns current network session and resets state to Offline.
        /// </summary>
        public void ShutdownSession()
        {
            if (_disconnectCoroutine != null)
            {
                StopCoroutine(_disconnectCoroutine);
                _disconnectCoroutine = null;
            }
            _isTrackingDisconnect = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            _localState = NetworkGameState.Offline;
            UpdateStatus("Desconectado de la red.");
            NotifyStateChanged(_localState);

            if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.Disconnect();
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"[NetworkGameManager] Cliente conectado: ClientId={clientId}");
            OnPlayerJoined?.Invoke(clientId);

            if (NetworkManager.Singleton.IsServer)
            {
                int connectedClientsCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
                if (connectedClientsCount >= 2)
                {
                    _syncedGameState.Value = NetworkGameState.InGame;
                    UpdateStatus("¡Ambos jugadores conectados! Partida en curso.");
                }
                else
                {
                    _syncedGameState.Value = NetworkGameState.InLobby;
                    UpdateStatus($"Esperando oponentes... ({connectedClientsCount}/2)");
                }

                if (_isTrackingDisconnect)
                {
                    CancelDisconnectionTracking();
                }
            }
            else
            {
                _localState = NetworkGameState.InGame;
                UpdateStatus("Conectado con éxito al Host. ¡Partida en curso!");
                NotifyStateChanged(_localState);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.LogWarning($"[NetworkGameManager] Cliente desconectado: ClientId={clientId}");
            OnPlayerLeft?.Invoke(clientId);

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                // Host stays alive, check if rival client left mid-game
                if (_syncedGameState.Value == NetworkGameState.InGame || _syncedGameState.Value == NetworkGameState.InLobby)
                {
                    SimulateClientDisconnection();
                }
            }
            else
            {
                _localState = NetworkGameState.Offline;
                UpdateStatus("Desconectado del Servidor.");
                NotifyStateChanged(_localState);
            }
        }

        /// <summary>
        /// Starts 10-second timeout tracking routine when a player disconnects mid-game (RF-4.1.3).
        /// </summary>
        public void SimulateClientDisconnection()
        {
            if (_isTrackingDisconnect) return;

            _isTrackingDisconnect = true;
            _disconnectTimer = 0f;
            if (_disconnectCoroutine != null) StopCoroutine(_disconnectCoroutine);
            _disconnectCoroutine = StartCoroutine(DisconnectionTimeoutRoutine());
        }

        public void CancelDisconnectionTracking()
        {
            if (!_isTrackingDisconnect) return;
            _isTrackingDisconnect = false;
            if (_disconnectCoroutine != null)
            {
                StopCoroutine(_disconnectCoroutine);
                _disconnectCoroutine = null;
            }
            UpdateStatus("Jugador reconectado. Reanudando partida.");
        }

        private IEnumerator DisconnectionTimeoutRoutine()
        {
            Debug.LogWarning($"[NetworkGameManager] Cliente desconectado. Iniciando temporizador de {_disconnectTimeout}s (RF-4.1.3)...");

            while (_isTrackingDisconnect && _disconnectTimer < _disconnectTimeout)
            {
                _disconnectTimer += Time.deltaTime;
                float remaining = Mathf.Max(0f, _disconnectTimeout - _disconnectTimer);
                UpdateStatus($"¡Rival desconectado! Reconexión: {remaining:F1}s...");
                yield return null;
            }

            if (_isTrackingDisconnect && _disconnectTimer >= _disconnectTimeout)
            {
                _isTrackingDisconnect = false;
                if (IsServer)
                {
                    _syncedGameState.Value = NetworkGameState.PostMatch;
                }
                _localState = NetworkGameState.PostMatch;

                Debug.LogError($"[NetworkGameManager] TIEMPO EXSPIRADO ({_disconnectTimeout}s)! Victoria por Desconexión para Host (Jugador 1)");
                UpdateStatus("¡Victoria Técnica por Desconexión Rival! (Jugador 1)");
                OnDisconnectionVictory?.Invoke(1);

                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.HandleDisconnectionVictory(1);
                }

                if (NetworkLobbyManager.Instance != null)
                {
                    NetworkLobbyManager.Instance.SimulateClientDisconnection();
                }
            }
        }

        private void HandleSyncedStateChanged(NetworkGameState previousState, NetworkGameState newState)
        {
            _localState = newState;
            Debug.Log($"[NetworkGameManager] GameState replicado: {previousState} -> {newState}");
            NotifyStateChanged(newState);
        }

        private void NotifyStateChanged(NetworkGameState state)
        {
            OnGameStateChanged?.Invoke(state);
        }

        private void UpdateStatus(string message)
        {
            OnStatusMessageChanged?.Invoke(message);
        }
    }
}
