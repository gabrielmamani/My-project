#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Network;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 12 Server Firing RPC Setup:
    /// Ensures NetworkShotManager exists in scene for authoritative RPC fire commands and anti-cheat validation (RF-07.3.1, RF-07.3.2, RF-07.3.3).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase12ShotRpcSetup
    {
        static Phase12ShotRpcSetup()
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

        [MenuItem("Gunbound/Setup Phase 12 (Server Authoritative Firing RPC)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject shotMgrObj = GameObject.Find("NetworkShotManager");
            if (shotMgrObj == null)
            {
                shotMgrObj = new GameObject("NetworkShotManager");
                Undo.RegisterCreatedObjectUndo(shotMgrObj, "Create NetworkShotManager");
            }

            if (shotMgrObj.GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                shotMgrObj.AddComponent<Unity.Netcode.NetworkObject>();
            }

            NetworkShotManager shotMgr = shotMgrObj.GetComponent<NetworkShotManager>();
            if (shotMgr == null)
            {
                shotMgr = shotMgrObj.AddComponent<NetworkShotManager>();
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase12ShotRpcSetup] NetworkShotManager & Authoritative Firing RPC setup completed successfully!");
        }
    }
}
#endif
