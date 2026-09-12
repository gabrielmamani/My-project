using System;
using UnityEngine;
using Unity.Netcode;
using Gunbound.Player;
using Gunbound.Managers;

namespace Gunbound.Network
{
    /// <summary>
    /// Manages server-authoritative firing RPC commands, anti-cheat input validation, 
    /// and client-side shot replication for online 1v1 matches (RF-4.3.1, RF-4.3.2, RF-4.3.3).
    /// </summary>
    public class NetworkShotManager : NetworkBehaviour
    {
        public static NetworkShotManager Instance { get; private set; }

        [Header("Anti-Cheat Validation Settings")]
        [SerializeField] private bool _enableAntiCheatValidation = true;
        [SerializeField] private float _minAllowedAngle = -90.0f;
        [SerializeField] private float _maxAllowedAngle = 90.0f;

        // Events
        public event Action<int, float, float, WeaponType> OnServerShotValidated; // (playerNumber, angle, powerRatio, weapon)
        public event Action<int, string> OnAntiCheatViolation; // (playerNumber, reason)
        public event Action<int, float, float, WeaponType> OnShotReplicatedToClients; // (playerNumber, angle, powerRatio, weapon)

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

        /// <summary>
        /// Sends a shot firing request from local client to Host/Server for authoritative validation (RF-4.3.1).
        /// Falls back to local execution if network is inactive or in singleplayer mode.
        /// </summary>
        public void SendFireRequest(int playerNumber, float angle, float powerRatio, WeaponType weapon, bool isDualActive, bool isTeleportActive)
        {
            float clampedPowerRatio = Mathf.Clamp01(powerRatio);

            if (IsNetworkActive && IsSpawned)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"[NetworkShotManager] Invoking CmdFireServerRpc from ClientId={localClientId} (Player {playerNumber}): Angle={angle:F1}°, Power={clampedPowerRatio:P0}, Weapon={weapon}, Dual={isDualActive}, Teleport={isTeleportActive}");

                CmdFireServerRpc(playerNumber, angle, clampedPowerRatio, (int)weapon, isDualActive, isTeleportActive);
            }
            else
            {
                Debug.Log($"[NetworkShotManager] Offline/Singleplayer mode active: Firing locally for Player {playerNumber}.");
                ExecuteLocalFire(playerNumber, angle, clampedPowerRatio, weapon, isDualActive, isTeleportActive);
            }
        }

        /// <summary>
        /// Sends an item usage request from local client to Server for authoritative processing.
        /// </summary>
        public void SendItemUseRequest(int playerNumber, ItemType itemType)
        {
            if (IsNetworkActive && IsSpawned)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                Debug.Log($"[NetworkShotManager] Invoking CmdUseItemServerRpc from ClientId={localClientId} (Player {playerNumber}): Item={itemType}");
                CmdUseItemServerRpc(playerNumber, (int)itemType);
            }
            else
            {
                ExecuteLocalItemUse(playerNumber, itemType);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdUseItemServerRpc(int playerNumber, int itemTypeInt, ServerRpcParams rpcParams = default)
        {
            ItemType itemType = (ItemType)itemTypeInt;
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayerNumber != playerNumber)
            {
                Debug.LogWarning($"[AntiCheat Reject] CmdUseItem rejected from ClientId={senderClientId}: Not player {playerNumber}'s turn!");
                return;
            }

            Debug.Log($"[ServerRpc SUCCESS] Player {playerNumber} used item {itemType} on Server.");
            ExecuteLocalItemUse(playerNumber, itemType);
            BroadcastItemUsedClientRpc(playerNumber, itemTypeInt);
        }

        [ClientRpc]
        public void BroadcastItemUsedClientRpc(int playerNumber, int itemTypeInt)
        {
            ItemType itemType = (ItemType)itemTypeInt;
            Debug.Log($"[ClientRpc Broadcast] Item {itemType} used by Player {playerNumber}. Replicating...");
            if (!IsServer)
            {
                ExecuteLocalItemUse(playerNumber, itemType);
            }
        }

        private void ExecuteLocalItemUse(int playerNumber, ItemType itemType)
        {
            PlayerController targetTank = (TurnManager.Instance != null && TurnManager.Instance.Player2 != null && playerNumber == 2) 
                ? TurnManager.Instance.Player2 
                : (TurnManager.Instance != null ? TurnManager.Instance.Player1 : FindAnyObjectByType<PlayerController>());

            if (targetTank != null)
            {
                switch (itemType)
                {
                    case ItemType.Dual: targetTank.ActivateDual(); break;
                    case ItemType.Teleport: targetTank.ActivateTeleport(); break;
                    case ItemType.Heal: targetTank.ActivateHeal(200f); break;
                    case ItemType.ChangeWind: targetTank.ActivateChangeWind(); break;
                    case ItemType.Shield: targetTank.ActivateShield(200f); break;
                    case ItemType.DualPlus: targetTank.ActivateDualPlus(); break;
                }
            }
        }

        /// <summary>
        /// Server RPC endpoint that receives client shot requests and validates inputs against anti-cheat rules (RF-4.3.1, RF-4.3.2).
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void CmdFireServerRpc(int playerNumber, float angle, float powerRatio, int weaponTypeInt, bool isDualActive, bool isTeleportActive, ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            WeaponType weapon = (WeaponType)weaponTypeInt;

            if (_enableAntiCheatValidation)
            {
                if (!ValidateFireRequest(senderClientId, playerNumber, angle, powerRatio, weapon, isDualActive, isTeleportActive, out string rejectionReason))
                {
                    Debug.LogWarning($"[AntiCheat Reject] CmdFire rejected from ClientId={senderClientId} (Player {playerNumber}): {rejectionReason}");
                    OnAntiCheatViolation?.Invoke(playerNumber, rejectionReason);
                    return;
                }
            }

            Debug.Log($"[ServerRpc SUCCESS] Player {playerNumber} shot request VALIDATED on Server! Executing authoritative shot...");
            OnServerShotValidated?.Invoke(playerNumber, angle, powerRatio, weapon);

            // Execute shot authoritatively on Server host
            ExecuteServerShot(playerNumber, angle, powerRatio, weapon, isDualActive, isTeleportActive);

            // Replicate shot event to all connected clients (RF-4.3.3)
            BroadcastShotClientRpc(playerNumber, angle, powerRatio, weaponTypeInt, isDualActive, isTeleportActive);
        }

        /// <summary>
        /// Client RPC endpoint that replicates shot events across all connected clients for visual & audio FX (RF-4.3.3).
        /// </summary>
        [ClientRpc]
        public void BroadcastShotClientRpc(int playerNumber, float angle, float powerRatio, int weaponTypeInt, bool isDualActive, bool isTeleportActive)
        {
            WeaponType weapon = (WeaponType)weaponTypeInt;
            Debug.Log($"[ClientRpc Broadcast] Replicating shot event for Player {playerNumber}: Angle={angle:F1}°, Power={powerRatio:P0}, Weapon={weapon}");
            OnShotReplicatedToClients?.Invoke(playerNumber, angle, powerRatio, weapon);
        }

        /// <summary>
        /// Validates player shot input on server to prevent anti-cheat tampering (RF-4.3.2).
        /// Checks turn authority, power ratio bounds [0,1], angle limits, and SS weapon cooldown.
        /// </summary>
        public bool ValidateFireRequest(ulong senderClientId, int playerNumber, float angle, float powerRatio, WeaponType weapon, bool isDualActive, bool isTeleportActive, out string rejectionReason)
        {
            rejectionReason = string.Empty;

            // 1. Validate Turn Authority
            if (TurnManager.Instance != null)
            {
                if (TurnManager.Instance.IsInputLocked)
                {
                    rejectionReason = "Input is currently locked by TurnManager!";
                    return false;
                }

                if (TurnManager.Instance.ActivePlayerNumber != playerNumber)
                {
                    rejectionReason = $"Not Player {playerNumber}'s turn! Current active player is {TurnManager.Instance.ActivePlayerNumber}.";
                    return false;
                }
            }

            // 2. Validate Power Ratio Range [0.0, 1.0]
            if (float.IsNaN(powerRatio) || float.IsInfinity(powerRatio) || powerRatio < -0.05f || powerRatio > 1.05f)
            {
                rejectionReason = $"Invalid power ratio value ({powerRatio:F2}) outside allowed range [0.0, 1.0]!";
                return false;
            }

            // 3. Validate Angle Range
            if (float.IsNaN(angle) || angle < _minAllowedAngle - 5.0f || angle > _maxAllowedAngle + 5.0f)
            {
                rejectionReason = $"Invalid aim angle ({angle:F1}°) outside allowed bounds [{_minAllowedAngle}°, {_maxAllowedAngle}°]!";
                return false;
            }

            // 4. Validate SS Weapon Cooldown Status
            if (weapon == WeaponType.SS && TurnManager.Instance != null)
            {
                PlayerController shooter = (playerNumber == 1) ? TurnManager.Instance.Player1 : TurnManager.Instance.Player2;
                if (shooter != null && !shooter.IsSSReady)
                {
                    rejectionReason = $"SS weapon is currently on cooldown ({shooter.CurrentSSCooldown} turns remaining)!";
                    return false;
                }
            }

            return true;
        }

        private void ExecuteServerShot(int playerNumber, float angle, float powerRatio, WeaponType weapon, bool isDualActive, bool isTeleportActive)
        {
            PlayerController targetTank = null;

            if (TurnManager.Instance != null)
            {
                targetTank = (playerNumber == 1) ? TurnManager.Instance.Player1 : TurnManager.Instance.Player2;
            }

            if (targetTank == null)
            {
                targetTank = FindAnyObjectByType<PlayerController>();
            }

            if (targetTank != null)
            {
                targetTank.ExecuteFireFromNetworkServer(powerRatio, angle, weapon, isDualActive, isTeleportActive);
            }
            else
            {
                Debug.LogError($"[NetworkShotManager] Could not find PlayerController for Player {playerNumber} on Server!");
            }
        }

        private void ExecuteLocalFire(int playerNumber, float angle, float powerRatio, WeaponType weapon, bool isDualActive, bool isTeleportActive)
        {
            ExecuteServerShot(playerNumber, angle, powerRatio, weapon, isDualActive, isTeleportActive);
        }
    }
}
