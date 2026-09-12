#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Network;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 13 Crater & Health Sync Setup:
    /// Ensures NetworkCraterManager exists in scene for authoritative crater RPC carving and health/bunge evaluation (RF-07.4.1, RF-07.4.2, RF-07.4.3).
    /// </summary>
    [InitializeOnLoad]
    public static class Phase13CraterSyncSetup
    {
        static Phase13CraterSyncSetup()
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

        [MenuItem("Gunbound/Setup Phase 13 (Crater & Health RPC Sync)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject craterMgrObj = GameObject.Find("NetworkCraterManager");
            if (craterMgrObj == null)
            {
                craterMgrObj = new GameObject("NetworkCraterManager");
                Undo.RegisterCreatedObjectUndo(craterMgrObj, "Create NetworkCraterManager");
            }

            if (craterMgrObj.GetComponent<Unity.Netcode.NetworkObject>() == null)
            {
                craterMgrObj.AddComponent<Unity.Netcode.NetworkObject>();
            }

            NetworkCraterManager craterMgr = craterMgrObj.GetComponent<NetworkCraterManager>();
            if (craterMgr == null)
            {
                craterMgr = craterMgrObj.AddComponent<NetworkCraterManager>();
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase13CraterSyncSetup] NetworkCraterManager & Crater RPC Sync setup completed successfully!");
        }
    }
}
#endif
