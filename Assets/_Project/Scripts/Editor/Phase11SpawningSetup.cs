#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Network;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 11 Spawning & Network Sync Setup:
    /// Ensures NetworkSpawnerManager exists in scene and configures NetworkPlayerSync on tanks (RF-07.2.1, RF-07.2.2, RF-07.2.3).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase11SpawningSetup
    {
        static Phase11SpawningSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    ExecuteBatchSetup();
                }
            };
        }

        [MenuItem("Gunbound/Setup Phase 11 (Authoritative Tank Spawner)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject spawnerObj = GameObject.Find("NetworkSpawnerManager");
            if (spawnerObj == null)
            {
                spawnerObj = new GameObject("NetworkSpawnerManager");
                Undo.RegisterCreatedObjectUndo(spawnerObj, "Create NetworkSpawnerManager");
            }

            NetworkSpawnerManager spawner = spawnerObj.GetComponent<NetworkSpawnerManager>();
            if (spawner == null)
            {
                spawner = spawnerObj.AddComponent<NetworkSpawnerManager>();
            }

            EnsureTankComponents("Player_Mage");
            EnsureTankComponents("Player_2");

            spawner.ResolveAndSpawnPlayers();

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase11SpawningSetup] Authoritative Tank Spawner & Sync setup completed successfully!");
        }

        private static void EnsureTankComponents(string tankName)
        {
            GameObject tankObj = GameObject.Find(tankName);
            if (tankObj == null) return;

            if (tankObj.GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                tankObj.AddComponent<Unity.Netcode.NetworkObject>();
            }

            if (tankObj.GetComponent<NetworkPlayerSync>() == null)
            {
                var sync = tankObj.AddComponent<NetworkPlayerSync>();
                sync.ResolveComponents();
            }
        }
    }
}
#endif
