using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Gunbound.Environment;
using Gunbound.Gameplay;
using Gunbound.Managers;

namespace Gunbound.Network
{
    /// <summary>
    /// Manages network crater synchronization (ClientRpc) and authoritative terrain destruction (RF-4.4.1, RF-4.4.2, RF-4.4.3).
    /// Transmits impact coordinates and explosion radius to all connected clients to carve matching craters locally.
    /// </summary>
    public class NetworkCraterManager : NetworkBehaviour
    {
        public static NetworkCraterManager Instance { get; private set; }

        // Events
        public event Action<Vector2, float> OnCraterCarved; // (impactPos, radius)
        public event Action<int, int> OnAuthoritativeHealthUpdated; // (playerNumber, currentHealth)

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
        /// Broadcasts crater carving impact coordinates and explosion radius to all connected clients (RF-4.4.1).
        /// </summary>
        public void BroadcastCarveHole(Vector2 impactPosition, float explosionRadius)
        {
            if (IsNetworkActive && IsSpawned)
            {
                Debug.Log($"[NetworkCraterManager] Invoking RpcCarveHoleClientRpc at {impactPosition} (Radius: {explosionRadius}m)...");
                RpcCarveHoleClientRpc(impactPosition, explosionRadius);
            }
            else
            {
                CarveHoleLocally(impactPosition, explosionRadius);
            }
        }

        /// <summary>
        /// Client RPC endpoint received by Host and Client to execute identical terrain texture carving locally (RF-4.4.1).
        /// </summary>
        [ClientRpc]
        public void RpcCarveHoleClientRpc(Vector2 impactPosition, float explosionRadius)
        {
            Debug.Log($"[ClientRpc RpcCarveHole] Received crater impact at {impactPosition} with radius {explosionRadius}m.");
            CarveHoleLocally(impactPosition, explosionRadius);
        }

        /// <summary>
        /// Executes local terrain hole carving on all active DestructibleTerrain components in range.
        /// </summary>
        public void CarveHoleLocally(Vector2 impactPosition, float explosionRadius)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(impactPosition, explosionRadius + 1.0f);
            HashSet<DestructibleTerrain> carvedTerrains = new HashSet<DestructibleTerrain>();

            foreach (var col in hitColliders)
            {
                if (col == null) continue;
                DestructibleTerrain terrain = col.GetComponent<DestructibleTerrain>() ?? col.GetComponentInParent<DestructibleTerrain>();

                if (terrain != null && !carvedTerrains.Contains(terrain))
                {
                    carvedTerrains.Add(terrain);
                    terrain.CarveHole(impactPosition, explosionRadius);
                }
            }

            // Fallback: If overlap did not find terrain directly, check all scene terrains
            if (carvedTerrains.Count == 0)
            {
                var allTerrains = FindObjectsByType<DestructibleTerrain>(FindObjectsSortMode.None);
                foreach (var terrain in allTerrains)
                {
                    if (terrain != null)
                    {
                        terrain.CarveHole(impactPosition, explosionRadius);
                    }
                }
            }

            OnCraterCarved?.Invoke(impactPosition, explosionRadius);
        }

        /// <summary>
        /// Evaluates radial health damage authoritatively on the Host/Server (RF-4.4.2).
        /// </summary>
        public void ProcessAuthoritativeDamage(Vector2 impactPosition, float explosionRadius, float maxDamage)
        {
            if (IsNetworkActive && !IsServer) return; // Only execute damage processing on Host/Server

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(impactPosition, explosionRadius);
            HashSet<Health> processedTargets = new HashSet<Health>();

            foreach (var col in hitColliders)
            {
                Health health = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();
                if (health != null && !processedTargets.Contains(health))
                {
                    processedTargets.Add(health);

                    float distance = Vector2.Distance(impactPosition, col.transform.position);
                    float damageFactor = Mathf.Clamp01(1f - (distance / explosionRadius));
                    int damage = Mathf.RoundToInt(maxDamage * damageFactor);

                    health.TakeDamage(damage);
                    Debug.Log($"[NetworkCraterManager] Authoritative radial damage applied to '{health.gameObject.name}': {damage} HP.");
                }
            }
        }
    }
}
