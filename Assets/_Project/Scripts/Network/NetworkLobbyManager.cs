using System;
using System.Collections;
using UnityEngine;
using Gunbound.Managers;

namespace Gunbound.Network
{
    public enum NetworkRole
    {
        Offline,
        Host,
        Client,
        Server
    }

    /// <summary>
    /// Network Manager for 1v1 authoritative multiplayer lobby, host/client connection tracking,
    /// and 10-second disconnection timeout detection (RF-07.1.1, RF-07.1.3).
    /// </summary>
    public class NetworkLobbyManager : MonoBehaviour
    {
        public static NetworkLobbyManager Instance { get; private set; }

        [Header("Connection Settings")]
        [SerializeField] private string _ipAddress = "127.0.0.1";
        [SerializeField] private ushort _port = 7777;
        [SerializeField] private float _disconnectTimeoutThreshold = 10.0f;

        [Header("State Tracking")]
        [SerializeField] private NetworkRole _currentRole = NetworkRole.Offline;
        [SerializeField] private bool _isConnected = false;

        private float _clientDisconnectTimer = 0f;
        private bool _isTrackingDisconnect = false;

        // Events
        public event Action<NetworkRole> OnNetworkRoleChanged;
        public event Action<string> OnStatusMessageChanged;
        public event Action<int> OnPlayerConnected; // (playerNumber: 1 for Host, 2 for Client)
        public event Action<int> OnPlayerDisconnected;
        public event Action<int> OnDisconnectionVictory; // (winnerPlayerNumber: 1 or 2)

        public NetworkRole CurrentRole => _currentRole;
        public bool IsConnected => _isConnected;
        public string IpAddress => _ipAddress;
        public ushort Port => _port;

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

        public void SetAddressAndPort(string ip, ushort port)
        {
            _ipAddress = string.IsNullOrEmpty(ip) ? "127.0.0.1" : ip;
            _port = port > 0 ? port : (ushort)7777;
        }

        /// <summary>
        /// Starts game as Host (Server + Player 1).
        /// </summary>
        public bool StartHost()
        {
            _currentRole = NetworkRole.Host;
            _isConnected = true;

            Debug.Log($"[NetworkLobbyManager] Started HOST at {_ipAddress}:{_port}. Waiting for Client (Player 2)...");
            OnNetworkRoleChanged?.Invoke(_currentRole);
            OnStatusMessageChanged?.Invoke($"HOST Activo en {_ipAddress}:{_port} (Jugador 1)");
            OnPlayerConnected?.Invoke(1);

            return true;
        }

        /// <summary>
        /// Connects to host as Client (Player 2).
        /// </summary>
        public bool StartClient()
        {
            _currentRole = NetworkRole.Client;
            _isConnected = true;

            Debug.Log($"[NetworkLobbyManager] Connecting as CLIENT to {_ipAddress}:{_port}...");
            OnNetworkRoleChanged?.Invoke(_currentRole);
            OnStatusMessageChanged?.Invoke($"Conectado a Host {_ipAddress}:{_port} (Jugador 2)");
            OnPlayerConnected?.Invoke(2);

            return true;
        }

        /// <summary>
        /// Disconnects network session and notifies timeout or quit.
        /// </summary>
        public void Disconnect()
        {
            NetworkRole previousRole = _currentRole;
            _currentRole = NetworkRole.Offline;
            _isConnected = false;
            _isTrackingDisconnect = false;

            Debug.Log($"[NetworkLobbyManager] Disconnected from network session.");
            OnNetworkRoleChanged?.Invoke(_currentRole);
            OnStatusMessageChanged?.Invoke("Desconectado de la red.");
            OnPlayerDisconnected?.Invoke(previousRole == NetworkRole.Host ? 1 : 2);
        }

        /// <summary>
        /// Simulates client connection loss to test 10-second timeout victory (RF-07.1.3).
        /// </summary>
        public void SimulateClientDisconnection()
        {
            if (_currentRole == NetworkRole.Host && _isTrackingDisconnect)
            {
                Debug.LogWarning("[NetworkLobbyManager] Client disconnection tracking already in progress.");
                return;
            }

            _isTrackingDisconnect = true;
            _clientDisconnectTimer = 0f;
            StartCoroutine(TrackDisconnectionRoutine());
        }

        private IEnumerator TrackDisconnectionRoutine()
        {
            Debug.LogWarning($"[NetworkLobbyManager] Client lost connection! Starting {_disconnectTimeoutThreshold}s timeout timer (RF-07.1.3)...");
            OnStatusMessageChanged?.Invoke($"¡Cliente desconectado! Esperando reconexión ({_disconnectTimeoutThreshold:F0}s)...");

            while (_isTrackingDisconnect && _clientDisconnectTimer < _disconnectTimeoutThreshold)
            {
                _clientDisconnectTimer += Time.deltaTime;
                float remaining = Mathf.Max(0f, _disconnectTimeoutThreshold - _clientDisconnectTimer);
                OnStatusMessageChanged?.Invoke($"Reconexión: {remaining:F1}s restantes...");
                yield return null;
            }

            if (_isTrackingDisconnect && _clientDisconnectTimer >= _disconnectTimeoutThreshold)
            {
                _isTrackingDisconnect = false;
                Debug.LogError($"[NetworkLobbyManager] DISCONNECTION TIMEOUT EXPIRED ({_disconnectTimeoutThreshold}s)! Declaring Host (Player 1) VICTORY!");
                OnStatusMessageChanged?.Invoke("¡Victoria por Desconexión Rival! (Jugador 1)");
                OnDisconnectionVictory?.Invoke(1);

                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.HandleDisconnectionVictory(1);
                }
            }
        }
    }
}
