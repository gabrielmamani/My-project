#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.UI;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class Phase3Setup
    {
        static Phase3Setup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    SetupPhase3HUD();
                }
            };
        }

        public static void ExecuteBatchSetup()
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.name == "SampleScene")
            {
                SetupPhase3HUD();
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
                Debug.Log("[Phase3Setup] Batch setup executed on active SampleScene.");
                return;
            }

            if (!Application.isPlaying)
            {
                string scenePath = "Assets/Scenes/SampleScene.unity";
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (scene.IsValid())
                {
                    SetupPhase3HUD();
                    EditorSceneManager.MarkSceneDirty(scene);
                    bool saved = EditorSceneManager.SaveScene(scene);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Phase3Setup] Batch setup executed. Scene Saved: {saved}");
                }
            }
        }

        [MenuItem("Gunbound/Setup Phase 3 HUD & Weapon Selector")]
        public static void SetupPhase3HUD()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Phase3Setup] Canvas not found in active scene.");
                return;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvasObj.AddComponent<UIManager>();
            }

            SerializedObject serializedUI = new SerializedObject(uiManager);

            // 1. Setup DelayText (P1: 0 | P2: 0)
            Transform delayTrans = canvasObj.transform.Find("DelayText");
            GameObject delayObj;
            if (delayTrans != null)
            {
                delayObj = delayTrans.gameObject;
            }
            else
            {
                delayObj = new GameObject("DelayText");
                Undo.RegisterCreatedObjectUndo(delayObj, "Create DelayText");
                delayObj.transform.SetParent(canvasObj.transform, false);
            }

            Text delayText = delayObj.GetComponent<Text>();
            if (delayText == null) delayText = delayObj.AddComponent<Text>();
            delayText.font = defaultFont;
            delayText.fontSize = 20;
            delayText.fontStyle = FontStyle.Bold;
            delayText.alignment = TextAnchor.MiddleRight;
            delayText.color = new Color(1.0f, 0.9f, 0.2f); // Golden yellow
            delayText.text = "P1: 0 | P2: 0";

            RectTransform delayRect = delayObj.GetComponent<RectTransform>();
            delayRect.anchorMin = new Vector2(1f, 1f);
            delayRect.anchorMax = new Vector2(1f, 1f);
            delayRect.pivot = new Vector2(1f, 1f);
            delayRect.anchoredPosition = new Vector2(-20f, -20f);
            delayRect.sizeDelta = new Vector2(300f, 40f);

            // 2. Setup TimerText (Tiempo: 20s)
            Transform timerTrans = canvasObj.transform.Find("TimerText");
            GameObject timerObj;
            if (timerTrans != null)
            {
                timerObj = timerTrans.gameObject;
            }
            else
            {
                timerObj = new GameObject("TimerText");
                Undo.RegisterCreatedObjectUndo(timerObj, "Create TimerText");
                timerObj.transform.SetParent(canvasObj.transform, false);
            }

            Text timerText = timerObj.GetComponent<Text>();
            if (timerText == null) timerText = timerObj.AddComponent<Text>();
            timerText.font = defaultFont;
            timerText.fontSize = 24;
            timerText.fontStyle = FontStyle.Bold;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = Color.white;
            timerText.text = "Tiempo: 20s";

            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1f);
            timerRect.anchorMax = new Vector2(0.5f, 1f);
            timerRect.pivot = new Vector2(0.5f, 1f);
            timerRect.anchoredPosition = new Vector2(0f, -60f);
            timerRect.sizeDelta = new Vector2(250f, 40f);

            // 3. Setup DoubleTurnText ("¡Doble Turno!")
            Transform doubleTurnTrans = canvasObj.transform.Find("DoubleTurnText");
            GameObject doubleTurnObj;
            if (doubleTurnTrans != null)
            {
                doubleTurnObj = doubleTurnTrans.gameObject;
            }
            else
            {
                doubleTurnObj = new GameObject("DoubleTurnText");
                Undo.RegisterCreatedObjectUndo(doubleTurnObj, "Create DoubleTurnText");
                doubleTurnObj.transform.SetParent(canvasObj.transform, false);
            }

            Text doubleTurnText = doubleTurnObj.GetComponent<Text>();
            if (doubleTurnText == null) doubleTurnText = doubleTurnObj.AddComponent<Text>();
            doubleTurnText.font = defaultFont;
            doubleTurnText.fontSize = 38;
            doubleTurnText.fontStyle = FontStyle.Bold;
            doubleTurnText.alignment = TextAnchor.MiddleCenter;
            doubleTurnText.color = new Color(1.0f, 0.25f, 0.25f); // Vivid Red Alert
            doubleTurnText.text = "¡Doble Turno!";
            doubleTurnObj.SetActive(false);

            RectTransform doubleTurnRect = doubleTurnObj.GetComponent<RectTransform>();
            doubleTurnRect.anchorMin = new Vector2(0.5f, 0.5f);
            doubleTurnRect.anchorMax = new Vector2(0.5f, 0.5f);
            doubleTurnRect.pivot = new Vector2(0.5f, 0.5f);
            doubleTurnRect.anchoredPosition = new Vector2(0f, 120f);
            doubleTurnRect.sizeDelta = new Vector2(400f, 60f);

            // 4. Setup Weapon Selection Buttons (Btn_Shot1 & Btn_Shot2)
            Button btnShot1 = CreateOrGetWeaponButton(canvasObj, "Btn_Shot1", "Tiro 1\n[1]", new Vector2(-190f, 90f), defaultFont);
            Button btnShot2 = CreateOrGetWeaponButton(canvasObj, "Btn_Shot2", "Tiro 2\n[2]", new Vector2(-105f, 90f), defaultFont);

            // 5. Ensure Projectile Prefabs (Shot1 & Shot2)
            GameObject shot1Prefab = EnsureWeaponPrefab("Assets/_Project/Prefabs/Projectile_Shot1.prefab", 250f, 2.5f);
            GameObject shot2Prefab = EnsureWeaponPrefab("Assets/_Project/Prefabs/Projectile_Shot2.prefab", 400f, 4.0f);

            // Bind UI references
            SerializedProperty delayProp = serializedUI.FindProperty("_delayText");
            SerializedProperty timerProp = serializedUI.FindProperty("_timerText");
            SerializedProperty doubleTurnProp = serializedUI.FindProperty("_doubleTurnText");
            SerializedProperty btn1Prop = serializedUI.FindProperty("_btnShot1");
            SerializedProperty btn2Prop = serializedUI.FindProperty("_btnShot2");
            SerializedProperty btn1BgProp = serializedUI.FindProperty("_btnShot1Bg");
            SerializedProperty btn2BgProp = serializedUI.FindProperty("_btnShot2Bg");

            if (delayProp != null) delayProp.objectReferenceValue = delayText;
            if (timerProp != null) timerProp.objectReferenceValue = timerText;
            if (doubleTurnProp != null) doubleTurnProp.objectReferenceValue = doubleTurnText;
            if (btn1Prop != null) btn1Prop.objectReferenceValue = btnShot1;
            if (btn2Prop != null) btn2Prop.objectReferenceValue = btnShot2;
            if (btn1BgProp != null && btnShot1 != null) btn1BgProp.objectReferenceValue = btnShot1.GetComponent<Image>();
            if (btn2BgProp != null && btnShot2 != null) btn2BgProp.objectReferenceValue = btnShot2.GetComponent<Image>();

            serializedUI.ApplyModifiedProperties();

            // Configure Player Controllers with prefabs
            ConfigurePlayerWeaponPrefabs(shot1Prefab, shot2Prefab);

            uiManager.SetupWeaponButtons();

            if (!Application.isPlaying)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.IsValid() && activeScene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(activeScene);
                }
            }

            Debug.Log("[Phase3Setup] Successfully configured Weapon Selection UI (Btn_Shot1, Btn_Shot2) and dynamic delay HUD!");
        }

        private static Button CreateOrGetWeaponButton(GameObject canvasObj, string name, string labelText, Vector2 anchoredPos, Font font)
        {
            Transform btnTrans = canvasObj.transform.Find(name);
            GameObject btnObj;
            if (btnTrans != null)
            {
                btnObj = btnTrans.gameObject;
            }
            else
            {
                btnObj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(btnObj, $"Create {name}");
                btnObj.transform.SetParent(canvasObj.transform, false);
            }

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = new Color(0.25f, 0.28f, 0.35f, 0.8f);

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(75f, 65f);

            Transform labelTrans = btnObj.transform.Find("Text");
            GameObject labelObj;
            if (labelTrans != null)
            {
                labelObj = labelTrans.gameObject;
            }
            else
            {
                labelObj = new GameObject("Text");
                labelObj.transform.SetParent(btnObj.transform, false);
            }

            Text txt = labelObj.GetComponent<Text>();
            if (txt == null) txt = labelObj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = labelText;

            RectTransform txtRect = labelObj.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            return btn;
        }

        private static GameObject EnsureWeaponPrefab(string prefabPath, float damage, float radius)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                string basePrefabPath = "Assets/_Project/Prefabs/Projectile.prefab";
                GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
                if (basePrefab != null)
                {
                    AssetDatabase.CopyAsset(basePrefabPath, prefabPath);
                    AssetDatabase.ImportAsset(prefabPath);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                }
            }

            if (prefab != null)
            {
                if (prefab.TryGetComponent<Gunbound.Combat.Projectile>(out var proj))
                {
                    proj.SetStats(damage, radius);
                    EditorUtility.SetDirty(prefab);
                }
            }

            return prefab;
        }

        private static void ConfigurePlayerWeaponPrefabs(GameObject shot1Prefab, GameObject shot2Prefab)
        {
            PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                SerializedObject serializedPlayer = new SerializedObject(p);
                SerializedProperty p1Prop = serializedPlayer.FindProperty("_projectilePrefabShot1");
                SerializedProperty p2Prop = serializedPlayer.FindProperty("_projectilePrefabShot2");
                SerializedProperty delayPerUnitProp = serializedPlayer.FindProperty("_delayPerDistanceUnit");

                if (p1Prop != null && shot1Prefab != null) p1Prop.objectReferenceValue = shot1Prefab;
                if (p2Prop != null && shot2Prefab != null) p2Prop.objectReferenceValue = shot2Prefab;
                if (delayPerUnitProp != null) delayPerUnitProp.floatValue = 5.0f;

                serializedPlayer.ApplyModifiedProperties();
                EditorUtility.SetDirty(p);
            }
        }
    }
}
#endif
