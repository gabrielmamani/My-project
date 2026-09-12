#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Gameplay;
using Gunbound.Managers;
using Gunbound.Player;
using Gunbound.Combat;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 4 Setup Utility:
    /// 1. Configures Rigidbody2D on Player 1 & Player 2 (Dynamic, Gravity = 1, NeverSleep).
    /// 2. Creates AimArrow sprite asset & child transform under Turret.
    /// 3. Configures TrailRenderer on Projectile prefab & active scene instances.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase4Setup
    {
        static Phase4Setup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    SetupPhase4();
                }
            };
        }

        public static void ExecuteBatchSetup()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                SetupPhase4();
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Phase4Setup] Batch setup executed. Scene Saved: {saved}");
            }
            else
            {
                Debug.LogError($"[Phase4Setup] Failed to open scene at path: {scenePath}");
            }
        }

        [MenuItem("Gunbound/Setup Phase 4 (Physics, Aim Arrow & Trail)")]
        public static void SetupPhase4()
        {
            // 1. Ensure AimArrow Sprite Asset exists
            Sprite arrowSprite = EnsureAimArrowSpriteAsset();

            // 2. Configure Player 1 & Player 2 Physics and AimArrow
            GameObject p1Obj = GameObject.Find("Player_Mage");
            GameObject p2Obj = GameObject.Find("Player_2");

            if (p1Obj != null)
            {
                ConfigurePlayerPhysics(p1Obj);
                ConfigureAimArrow(p1Obj, arrowSprite, new Color(0.0f, 0.9f, 1.0f)); // Cyan arrow for P1
            }
            else
            {
                Debug.LogWarning("[Phase4Setup] Player_Mage not found in active scene.");
            }

            if (p2Obj != null)
            {
                ConfigurePlayerPhysics(p2Obj);
                ConfigureAimArrow(p2Obj, arrowSprite, new Color(1.0f, 0.7f, 0.0f)); // Amber/Orange arrow for P2
            }
            else
            {
                Debug.LogWarning("[Phase4Setup] Player_2 not found in active scene.");
            }

            // 3. Configure Projectile TrailRenderer in Prefab & Scene
            ConfigureProjectilePrefabAndTrail();

            // 4. Mark Scene Dirty & Save
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Phase4Setup] Phase 4 setup complete: Dynamic Rigidbody2D (NeverSleep), Aim Arrow, and Trajectory Trail configured!");
        }

        private static void ConfigurePlayerPhysics(GameObject playerObj)
        {
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = playerObj.AddComponent<Rigidbody2D>();
                Undo.RegisterCreatedObjectUndo(rb, "Add Rigidbody2D");
            }

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1.0f;
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            BoxCollider2D boxCol = playerObj.GetComponent<BoxCollider2D>();
            if (boxCol == null)
            {
                boxCol = playerObj.AddComponent<BoxCollider2D>();
                Undo.RegisterCreatedObjectUndo(boxCol, "Add BoxCollider2D");
                boxCol.size = new Vector2(1.5f, 1.0f);
            }

            Debug.Log($"[Phase4Setup] Rigidbody2D configured for {playerObj.name}: Dynamic, GravityScale=1, NeverSleep");
        }

        private static void ConfigureAimArrow(GameObject playerObj, Sprite arrowSprite, Color arrowColor)
        {
            TurretAim turretAim = playerObj.GetComponentInChildren<TurretAim>();
            Transform turretTrans = turretAim != null ? turretAim.transform : playerObj.transform.Find("Turret");

            if (turretTrans == null)
            {
                turretTrans = playerObj.transform;
            }

            Transform aimArrowTrans = turretTrans.Find("AimArrow");
            GameObject aimArrowObj;

            if (aimArrowTrans != null)
            {
                aimArrowObj = aimArrowTrans.gameObject;
            }
            else
            {
                aimArrowObj = new GameObject("AimArrow");
                Undo.RegisterCreatedObjectUndo(aimArrowObj, "Create AimArrow");
                aimArrowObj.transform.SetParent(turretTrans, false);
            }

            aimArrowObj.transform.localPosition = new Vector3(1.2f, 0f, 0f);
            aimArrowObj.transform.localRotation = Quaternion.identity;
            aimArrowObj.transform.localScale = new Vector3(0.8f, 0.8f, 1.0f);

            SpriteRenderer sr = aimArrowObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = aimArrowObj.AddComponent<SpriteRenderer>();
            }

            sr.sprite = arrowSprite;
            sr.color = arrowColor;
            sr.sortingOrder = 5;

            Debug.Log($"[Phase4Setup] AimArrow attached under turret of {playerObj.name}");
        }

        private static Sprite EnsureAimArrowSpriteAsset()
        {
            string folderPath = "Assets/_Project/Sprites";
            string assetPath = "Assets/_Project/Sprites/AimArrow.png";

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            Sprite existingSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existingSprite != null)
            {
                return existingSprite;
            }

            // Generate procedural 64x32 Arrow Texture
            int width = 64;
            int height = 32;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Color transparent = new Color(0, 0, 0, 0);
            Color white = Color.white;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }

            // Draw shaft (from X=0 to X=40, Y centered around 16)
            int shaftHalfHeight = 4;
            int centerY = height / 2;

            for (int x = 4; x <= 40; x++)
            {
                for (int y = centerY - shaftHalfHeight; y <= centerY + shaftHalfHeight; y++)
                {
                    tex.SetPixel(x, y, white);
                }
            }

            // Draw arrowhead tip pointing right (X from 40 to 60)
            int headLength = 20;
            for (int dx = 0; dx < headLength; dx++)
            {
                int x = 40 + dx;
                int maxDy = Mathf.RoundToInt((1f - ((float)dx / headLength)) * 14f);
                for (int dy = -maxDy; dy <= maxDy; dy++)
                {
                    int y = centerY + dy;
                    if (y >= 0 && y < height)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(assetPath, bytes);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Configure TextureImporter to Sprite (2D and UI) with pivot at Left Center (0, 0.5)
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePivot = new Vector2(0f, 0.5f);
                importer.spritePixelsPerUnit = 32;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void ConfigureProjectilePrefabAndTrail()
        {
            string prefabPath = "Assets/_Project/Prefabs/Projectile.prefab";
            GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefabObj != null)
            {
                TrailRenderer trail = prefabObj.GetComponent<TrailRenderer>();
                if (trail == null)
                {
                    trail = prefabObj.AddComponent<TrailRenderer>();
                }

                trail.time = 0.8f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.02f;
                trail.numCornerVertices = 4;
                trail.numCapVertices = 4;
                trail.minVertexDistance = 0.05f;

                Shader defaultShader = Shader.Find("Sprites/Default");
                if (defaultShader != null)
                {
                    trail.material = new Material(defaultShader);
                }

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0.0f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trail.colorGradient = gradient;

                EditorUtility.SetDirty(prefabObj);
                Debug.Log("[Phase4Setup] Configured TrailRenderer on Projectile prefab.");
            }
        }
    }
}
#endif
