#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Player;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 7 Aim Arc Visualizer Setup:
    /// Attaches and configures AimArcVisualizer component on Player_Mage and Player_2 under TurretAim.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase7AimArcSetup
    {
        static Phase7AimArcSetup()
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

        [MenuItem("Gunbound/Setup Phase 7 (Aim Arc Visualizer)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject p1Obj = GameObject.Find("Player_Mage");
            GameObject p2Obj = GameObject.Find("Player_2");

            if (p1Obj != null) ConfigurePlayerArc(p1Obj);
            if (p2Obj != null) ConfigurePlayerArc(p2Obj);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase7AimArcSetup] AimArcVisualizer setup completed successfully for all active tanks.");
        }

        private static void ConfigurePlayerArc(GameObject playerObj)
        {
            TurretAim turretAim = playerObj.GetComponentInChildren<TurretAim>();
            Transform turretTrans = turretAim != null ? turretAim.transform : playerObj.transform;

            Transform arcObjTrans = turretTrans.Find("AimArcVisualizer");
            GameObject arcObj;

            if (arcObjTrans != null)
            {
                arcObj = arcObjTrans.gameObject;
            }
            else
            {
                arcObj = new GameObject("AimArcVisualizer");
                Undo.RegisterCreatedObjectUndo(arcObj, "Create AimArcVisualizer");
                arcObj.transform.SetParent(turretTrans, false);
            }

            AimArcVisualizer visualizer = arcObj.GetComponent<AimArcVisualizer>();
            if (visualizer == null)
            {
                visualizer = arcObj.AddComponent<AimArcVisualizer>();
            }

            visualizer.ResolveReferences();
            visualizer.UpdateArcVisuals();
            Debug.Log($"[Phase7AimArcSetup] AimArcVisualizer configured on {playerObj.name}");
        }
    }
}
#endif
