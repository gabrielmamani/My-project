using System;
using UnityEngine;
using Unity.Netcode;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.Network
{
    /// <summary>
    /// Synchronizes turn passing, active player selection (ActivePlayerNetworkId),
    /// and 20-second turn countdown timer authoritatively via NetworkVariables (RF-4.5.1, RF-4.5.2).
    /// </summary>
    public class NetworkTurnManager : NetworkBehaviour
    {
        public static NetworkTurnManager Instance { get; private set; }

        [Header("Synchronized Turn Variables")]
        private readonly NetworkVariable<int> _activePlayerNetworkId = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private readonly NetworkVariable<float> _networkTurnTimer = new NetworkVariable<float>(
            20.0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Events
        public event Action<int> OnActivePlayerChanged; // (activePlayerNumber: 1 or 2)
        public event Action<float> OnTimerSynced; // (remainingSeconds)

        public int ActivePlayerNetworkId => _activePlayerNetworkId.Value;
        public float NetworkTurnTimer => _networkTurnTimer.Value;
        public bool IsNetworkActive => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _activePlayerNetworkId.OnValueChanged += HandleActivePlayerChanged;
            _networkTurnTimer.OnValueChanged += HandleTimerChanged;

            if (IsServer && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnPlayerChanged += HandleLocalTurnPlayerChanged;
                TurnManager.Instance.OnTurnTimerUpdated += HandleLocalTimerUpdated;

                _activePlayerNetworkId.Value = TurnManager.Instance.ActivePlayerNumber;
                _networkTurnTimer.Value = TurnManager.Instance.RemainingTurnTime;
            }
        }

        public override void OnNetworkDespawn()
        {
            _activePlayerNetworkId.OnValueChanged -= HandleActivePlayerChanged;
            _networkTurnTimer.OnValueChanged -= HandleTimerChanged;

            if (IsServer && TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnPlayerChanged -= HandleLocalTurnPlayerChanged;
                TurnManager.Instance.OnTurnTimerUpdated -= HandleLocalTimerUpdated;
            }

            base.OnNetworkDespawn();
        }

        private void HandleLocalTurnPlayerChanged(int turnNum, int activePlayerNum)
        {
            if (IsServer)
            {
                _activePlayerNetworkId.Value = activePlayerNum;
                Debug.Log($"[NetworkTurnManager] Server updated ActivePlayerNetworkId to Player {activePlayerNum} for Turn {turnNum}");
            }
        }

        private void HandleLocalTimerUpdated(float remainingTime)
        {
            if (IsServer)
            {
                _networkTurnTimer.Value = remainingTime;
            }
        }

        private void HandleActivePlayerChanged(int previousPlayer, int newPlayer)
        {
            Debug.Log($"[NetworkTurnManager] Active Player Replicated: Player {previousPlayer} -> Player {newPlayer}");
            OnActivePlayerChanged?.Invoke(newPlayer);
            UpdateLocalInputEnablement(newPlayer);
        }

        private void HandleTimerChanged(float previousTime, float newTime)
        {
            OnTimerSynced?.Invoke(newTime);
        }

        private void UpdateLocalInputEnablement(int activePlayerNum)
        {
            NetworkRole role = NetworkLobbyManager.Instance != null ? NetworkLobbyManager.Instance.CurrentRole : NetworkRole.Offline;

            int localPlayerNum = 1;
            if (role == NetworkRole.Client) localPlayerNum = 2;
            else if (role == NetworkRole.Host || role == NetworkRole.Offline) localPlayerNum = 1;

            bool isLocalTurn = (localPlayerNum == activePlayerNum);

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.SetInputLocked(!isLocalTurn);
            }

            Debug.Log($"[NetworkTurnManager] Local Input Enablement updated: LocalPlayer={localPlayerNum}, ActivePlayer={activePlayerNum}, IsInputEnabled={isLocalTurn}");
        }
    }
}
