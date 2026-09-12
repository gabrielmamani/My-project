#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Gunbound.Gameplay;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class Player2Setup
    {
        static Player2Setup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    SetupPlayer2AndDuel();
                }
            };
        }

        public static void ExecutePlayer2BatchSetup()
        {
            string scenePath = "Assets/Scenes/SampleScene.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (scene.IsValid())
            {
                SetupPlayer2AndDuel();
                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Player2Setup] Batch setup executed. Scene Saved: {saved}");
            }
            else
            {
                Debug.LogError($"[Player2Setup] Failed to open scene at path: {scenePath}");
            }
        }

        [MenuItem("Gunbound/Setup Player 2 & 1v1 Duel")]
        public static void SetupPlayer2AndDuel()
        {
            GameObject p1Obj = GameObject.Find("Player_Mage");
            if (p1Obj == null)
            {
                Debug.LogWarning("[Player2Setup] Could not find 'Player_Mage' in active scene.");
                return;
            }

            // 1. Ensure Health & Floating HealthBar on Player 1
            HealthBarSetup.SetupPlayerHealthAndHealthBar();

            // 1b. Ensure Destructible Terrain & DeathZone
            TerrainSetup.SetupTerrainAndDeathZone();

            // 2. Find or create Player_2
            GameObject p2Obj = GameObject.Find("Player_2");
            if (p2Obj == null)
            {
                p2Obj = Object.Instantiate(p1Obj);
                p2Obj.name = "Player_2";
                Undo.RegisterCreatedObjectUndo(p2Obj, "Create Player_2");
                Debug.Log("[Player2Setup] Instantiated 'Player_2' from 'Player_Mage'.");
            }

            // Set Position on ground level (X = 5.0, Y = -2.0, Z = 0) within Camera FOV (Camera orthographic size = 6.5)
            p2Obj.transform.position = new Vector3(5.0f, -2.0f, 0.0f);

            // Invert X scale so Player 2 faces left towards center/Player_1
            Vector3 s = p2Obj.transform.localScale;
            p2Obj.transform.localScale = new Vector3(-Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

            // Ensure Rigidbody2D with FreezeRotationZ
            Rigidbody2D rb2d = p2Obj.GetComponent<Rigidbody2D>();
            if (rb2d == null) rb2d = p2Obj.AddComponent<Rigidbody2D>();
            rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Ensure BoxCollider2D
            BoxCollider2D box2d = p2Obj.GetComponent<BoxCollider2D>();
            if (box2d == null) box2d = p2Obj.AddComponent<BoxCollider2D>();
            box2d.size = new Vector2(1.5f, 1.0f);

            // Fix HealthCanvas scale on Player 2 so floating UI text is not mirrored
            Transform p2Canvas = p2Obj.transform.Find("HealthCanvas");
            if (p2Canvas != null)
            {
                p2Canvas.localScale = new Vector3(-0.01f, 0.01f, 1.0f);
            }

            // Distinct vivid tint color for Player 2 (Crimson Red) & Order in Layer = 1 (visible above ground/background)
            Color p2Color = new Color(0.95f, 0.2f, 0.2f, 1.0f);
            SpriteRenderer[] p2Renderers = p2Obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in p2Renderers)
            {
                sr.color = p2Color;
                if (sr.sortingOrder < 1)
                {
                    sr.sortingOrder = 1;
                }
            }

            // Ensure Health component on Player 2
            Health p2Health = p2Obj.GetComponent<Health>();
            if (p2Health == null)
            {
                p2Health = p2Obj.AddComponent<Health>();
            }

            // Ensure PlayerController components
            PlayerController p1Controller = p1Obj.GetComponent<PlayerController>();
            PlayerController p2Controller = p2Obj.GetComponent<PlayerController>();

            // 3. Bind player references in TurnManager
            TurnManager turnManager = Object.FindAnyObjectByType<TurnManager>();
            if (turnManager != null)
            {
                turnManager.SetPlayers(p1Controller, p2Controller);

                SerializedObject serializedTurnManager = new SerializedObject(turnManager);
                SerializedProperty p1Prop = serializedTurnManager.FindProperty("_player1");
                SerializedProperty p2Prop = serializedTurnManager.FindProperty("_player2");

                if (p1Prop != null) p1Prop.objectReferenceValue = p1Controller;
                if (p2Prop != null) p2Prop.objectReferenceValue = p2Controller;

                serializedTurnManager.ApplyModifiedProperties();
                Debug.Log("[Player2Setup] Successfully bound Player_1 and Player_2 in TurnManager.");
            }

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            Debug.Log("[Player2Setup] 1v1 Local Duel Setup complete!");
        }
    }
}
#endif
