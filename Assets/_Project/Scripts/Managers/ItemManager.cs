using System;
using UnityEngine;
using Gunbound.Player;

namespace Gunbound.Managers
{
    /// <summary>
    /// Manages tactical items (Dual, Teleport, Heal).
    /// Enforces single-use per match rule for items per player and communicates with active PlayerController.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        public static ItemManager Instance { get; private set; }

        public event Action<string> OnItemUsed; // ("Dual", "Teleport", or "Heal")

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private PlayerController GetActivePlayer()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null)
            {
                return TurnManager.Instance.ActivePlayer;
            }
            return FindAnyObjectByType<PlayerController>();
        }

        public bool IsDualUsedForActivePlayer()
        {
            PlayerController player = GetActivePlayer();
            return player != null && player.IsDualUsed;
        }

        public bool IsTeleportUsedForActivePlayer()
        {
            PlayerController player = GetActivePlayer();
            return player != null && player.IsTeleportUsed;
        }

        public bool IsHealUsedForActivePlayer()
        {
            PlayerController player = GetActivePlayer();
            return player != null && player.IsHealUsed;
        }

        /// <summary>
        /// Attempts to use the Dual item on the current active player.
        /// </summary>
        /// <returns>True if item was successfully activated, false otherwise.</returns>
        public bool UseDual()
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use Dual: No active player!");
                return false;
            }

            if (activePlayer.IsDualUsed)
            {
                Debug.LogWarning($"[ItemManager] Dual item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                Debug.LogWarning("[ItemManager] Cannot use Dual: Input is currently locked!");
                return false;
            }

            if (activePlayer.ActivateDual())
            {
                OnItemUsed?.Invoke("Dual");
                Debug.Log($"[ItemManager] Item DUAL activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to use the Teleport item on the current active player.
        /// </summary>
        /// <returns>True if item was successfully activated, false otherwise.</returns>
        public bool UseTeleport()
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use Teleport: No active player!");
                return false;
            }

            if (activePlayer.IsTeleportUsed)
            {
                Debug.LogWarning($"[ItemManager] Teleport item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                Debug.LogWarning("[ItemManager] Cannot use Teleport: Input is currently locked!");
                return false;
            }

            if (activePlayer.ActivateTeleport())
            {
                OnItemUsed?.Invoke("Teleport");
                Debug.Log($"[ItemManager] Item TELEPORT activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to use the Heal item on the current active player (restores 200 HP).
        /// </summary>
        /// <returns>True if item was successfully activated, false otherwise.</returns>
        public bool UseHeal(float amount = 200f)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use Heal: No active player!");
                return false;
            }

            if (activePlayer.IsHealUsed)
            {
                Debug.LogWarning($"[ItemManager] Heal item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                Debug.LogWarning("[ItemManager] Cannot use Heal: Input is currently locked!");
                return false;
            }

            if (activePlayer.ActivateHeal(amount))
            {
                OnItemUsed?.Invoke("Heal");
                Debug.Log($"[ItemManager] Item HEAL (+{amount} HP) activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        public bool UseChangeWind()
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use ChangeWind: No active player!");
                return false;
            }

            if (activePlayer.IsChangeWindUsed)
            {
                Debug.LogWarning($"[ItemManager] Change Wind item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (activePlayer.ActivateChangeWind())
            {
                OnItemUsed?.Invoke("ChangeWind");
                Debug.Log($"[ItemManager] Item CHANGE WIND activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        public bool UseShield(float amount = 200f)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use Shield: No active player!");
                return false;
            }

            if (activePlayer.IsShieldUsed)
            {
                Debug.LogWarning($"[ItemManager] Shield item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (activePlayer.ActivateShield(amount))
            {
                OnItemUsed?.Invoke("Shield");
                Debug.Log($"[ItemManager] Item SHIELD activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        public bool UseDualPlus()
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer == null)
            {
                Debug.LogWarning("[ItemManager] Cannot use Dual+: No active player!");
                return false;
            }

            if (activePlayer.IsDualPlusUsed)
            {
                Debug.LogWarning($"[ItemManager] Dual+ item has already been used by {activePlayer.name} in this match!");
                return false;
            }

            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked)
            {
                Debug.LogWarning("[ItemManager] Cannot use Dual+: Input is currently locked!");
                return false;
            }

            if (activePlayer.ActivateDualPlus())
            {
                OnItemUsed?.Invoke("DualPlus");
                Debug.Log($"[ItemManager] Item DUAL+ activated for {activePlayer.name}.");
                return true;
            }
            return false;
        }

        public bool UseItem(ItemType type)
        {
            switch (type)
            {
                case ItemType.Dual: return UseDual();
                case ItemType.Teleport: return UseTeleport();
                case ItemType.Heal: return UseHeal();
                case ItemType.ChangeWind: return UseChangeWind();
                case ItemType.Shield: return UseShield();
                case ItemType.DualPlus: return UseDualPlus();
                default: return false;
            }
        }

        public void ResetItems()
        {
            if (TurnManager.Instance != null)
            {
                if (TurnManager.Instance.Player1 != null) TurnManager.Instance.Player1.ResetItems();
                if (TurnManager.Instance.Player2 != null) TurnManager.Instance.Player2.ResetItems();
            }
            foreach (var player in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                player.ResetItems();
            }
        }
    }
}
