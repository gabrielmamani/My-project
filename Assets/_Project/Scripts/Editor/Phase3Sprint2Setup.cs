#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.CameraSystem;
using Gunbound.Managers;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class Phase3Sprint2Setup
    {
        static Phase3Sprint2Setup()
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

        [MenuItem("Gunbound/Setup Phase 3 Sprint 2 (Camera & Items)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                string scenePath = "Assets/Scenes/SampleScene.unity";
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            if (!activeScene.IsValid())
            {
                Debug.LogError("[Phase3Sprint2Setup] Could not open SampleScene.unity");
                return;
            }

            SetupCameraController();
            SetupItemManager();
            SetupItemUI();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Phase3Sprint2Setup] Completed setup for Dynamic Camera and Tactical Items (Dual & Teleport) UI!");
        }

        private static void SetupCameraController()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = GameObject.Find("Main Camera");
                if (camObj != null) mainCam = camObj.GetComponent<Camera>();
            }

            if (mainCam != null)
            {
                var controller = mainCam.GetComponent<CameraController>();
                if (controller == null)
                {
                    controller = Undo.AddComponent<CameraController>(mainCam.gameObject);
                }
                Debug.Log("[Phase3Sprint2Setup] CameraController verified on Main Camera.");
            }
            else
            {
                Debug.LogWarning("[Phase3Sprint2Setup] Main Camera not found in active scene.");
            }
        }

        private static void SetupItemManager()
        {
            ItemManager itemManager = Object.FindAnyObjectByType<ItemManager>();
            if (itemManager == null)
            {
                GameObject managerObj = GameObject.Find("Managers") ?? GameObject.Find("TurnManager") ?? new GameObject("ItemManager");
                itemManager = Undo.AddComponent<ItemManager>(managerObj);
            }
            Debug.Log("[Phase3Sprint2Setup] ItemManager component verified.");
        }

        private static void SetupItemUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Phase3Sprint2Setup] Canvas not found in active scene.");
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

            // Create or update Btn_Dual & Btn_Teleport aligned above Btn_Shot1 and Btn_Shot2
            Button btnDual = CreateOrGetItemButton(canvasObj, "Btn_Dual", "DUAL\n[3]", new Vector2(-190f, 165f), defaultFont, new Color(0.85f, 0.65f, 0.1f, 0.9f));
            Button btnTeleport = CreateOrGetItemButton(canvasObj, "Btn_Teleport", "TELEPORT\n[4]", new Vector2(-105f, 165f), defaultFont, new Color(0.2f, 0.65f, 0.85f, 0.9f));

            SerializedProperty btnDualProp = serializedUI.FindProperty("_btnDual");
            SerializedProperty btnTeleportProp = serializedUI.FindProperty("_btnTeleport");
            SerializedProperty btnDualBgProp = serializedUI.FindProperty("_btnDualBg");
            SerializedProperty btnTeleportBgProp = serializedUI.FindProperty("_btnTeleportBg");

            if (btnDualProp != null) btnDualProp.objectReferenceValue = btnDual;
            if (btnTeleportProp != null) btnTeleportProp.objectReferenceValue = btnTeleport;
            if (btnDualBgProp != null && btnDual != null) btnDualBgProp.objectReferenceValue = btnDual.GetComponent<Image>();
            if (btnTeleportBgProp != null && btnTeleport != null) btnTeleportBgProp.objectReferenceValue = btnTeleport.GetComponent<Image>();

            serializedUI.ApplyModifiedProperties();

            uiManager.SetupItemButtons();
            Debug.Log("[Phase3Sprint2Setup] Configured Btn_Dual and Btn_Teleport on Canvas.");
        }

        private static Button CreateOrGetItemButton(GameObject canvasObj, string name, string labelText, Vector2 anchoredPos, Font font, Color bgColor)
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
            img.color = bgColor;

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
            txt.fontSize = 12;
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
    }
}
#endif
