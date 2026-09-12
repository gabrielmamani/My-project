#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Environment;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Editor setup utility to automatically attach DestructibleTerrain to Ground
    /// and spawn a DeathZone trigger below the map in SampleScene.
    /// </summary>
    [InitializeOnLoad]
    public static class TerrainSetup
    {
        static TerrainSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    SetupTerrainAndDeathZone();
                }
            };
        }

        public static void ExecuteTerrainBatchSetup()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                SetupTerrainAndDeathZone();
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"[TerrainSetup] Batch terrain setup executed. Scene Saved: {saved}");
            }
            else
            {
                Debug.LogError($"[TerrainSetup] Failed to open scene at path: {scenePath}");
            }
        }

        [MenuItem("Gunbound/Setup Destructible Terrain & DeathZone")]
        public static void SetupTerrainAndDeathZone()
        {
            // 1. Setup Ground for DestructibleTerrain
            GameObject groundObj = GameObject.Find("Ground");
            if (groundObj != null)
            {
                DestructibleTerrain terrain = groundObj.GetComponent<DestructibleTerrain>();
                if (terrain == null)
                {
                    terrain = groundObj.AddComponent<DestructibleTerrain>();
                    Undo.RegisterCreatedObjectUndo(terrain, "Add DestructibleTerrain Component");
                }

                // Convert BoxCollider2D to PolygonCollider2D if present
                BoxCollider2D boxCol = groundObj.GetComponent<BoxCollider2D>();
                if (boxCol != null)
                {
                    Undo.DestroyObjectImmediate(boxCol);
                }

                PolygonCollider2D polyCol = groundObj.GetComponent<PolygonCollider2D>();
                if (polyCol == null)
                {
                    polyCol = groundObj.AddComponent<PolygonCollider2D>();
                    Undo.RegisterCreatedObjectUndo(polyCol, "Add PolygonCollider2D");
                }

                Debug.Log("[TerrainSetup] DestructibleTerrain and PolygonCollider2D successfully configured on Ground.");
            }
            else
            {
                Debug.LogWarning("[TerrainSetup] Ground object not found in scene!");
            }

            // 2. Setup DeathZone below map (Y = -12.0)
            GameObject deathZoneObj = GameObject.Find("DeathZone");
            if (deathZoneObj == null)
            {
                deathZoneObj = new GameObject("DeathZone");
                Undo.RegisterCreatedObjectUndo(deathZoneObj, "Create DeathZone GameObject");
            }

            deathZoneObj.transform.position = new Vector3(0f, -12.0f, 0f);

            BoxCollider2D deathCollider = deathZoneObj.GetComponent<BoxCollider2D>();
            if (deathCollider == null)
            {
                deathCollider = deathZoneObj.AddComponent<BoxCollider2D>();
                Undo.RegisterCreatedObjectUndo(deathCollider, "Add DeathZone BoxCollider2D");
            }

            deathCollider.isTrigger = true;
            deathCollider.size = new Vector2(60f, 4f);

            DeathZone deathZoneComponent = deathZoneObj.GetComponent<DeathZone>();
            if (deathZoneComponent == null)
            {
                deathZoneComponent = deathZoneObj.AddComponent<DeathZone>();
                Undo.RegisterCreatedObjectUndo(deathZoneComponent, "Add DeathZone Component");
            }

            Debug.Log("[TerrainSetup] DeathZone configured at Y = -12.0 with width 60.");

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }
}
#endif
