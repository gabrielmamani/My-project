#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Managers;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 5 Setup Utility:
    /// Instantiates/configures ItemPanel container and buttons (Btn_ItemDual, Btn_ItemTeleport, Btn_ItemHeal)
    /// anchored for 16:9 mobile screen in Canvas HUD, and connects UIManager references.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase5ItemUISetup
    {
        static Phase5ItemUISetup()
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

        [MenuItem("Gunbound/Setup Phase 5 (HUD Items Integration)")]
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
                Debug.LogError("[Phase5ItemUISetup] Could not open SampleScene.unity");
                return;
            }

            SetupItemManager();
            SetupHUDItemUI();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Phase5ItemUISetup] Completed HUD Item Buttons (Dual, Teleport, Heal) Integration Setup!");
        }

        private static void SetupItemManager()
        {
            ItemManager itemManager = Object.FindAnyObjectByType<ItemManager>();
            if (itemManager == null)
            {
                GameObject managerObj = GameObject.Find("Managers") ?? GameObject.Find("TurnManager") ?? new GameObject("ItemManager");
                itemManager = Undo.AddComponent<ItemManager>(managerObj);
            }
            Debug.Log("[Phase5ItemUISetup] ItemManager component verified.");
        }

        private static void SetupHUDItemUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Phase5ItemUISetup] Canvas not found in active scene.");
                return;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvasObj.AddComponent<UIManager>();
            }

            // Create or get ItemPanel horizontal container
            Transform panelTrans = canvasObj.transform.Find("ItemPanel");
            GameObject itemPanel;
            if (panelTrans != null)
            {
                itemPanel = panelTrans.gameObject;
            }
            else
            {
                itemPanel = new GameObject("ItemPanel");
                Undo.RegisterCreatedObjectUndo(itemPanel, "Create ItemPanel");
                itemPanel.transform.SetParent(canvasObj.transform, false);
            }

            RectTransform panelRect = itemPanel.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = itemPanel.AddComponent<RectTransform>();

            // Anchor ItemPanel to bottom-right corner for 16:9 mobile HUD (above action/weapon buttons)
            panelRect.anchorMin = new Vector2(1f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-20f, 160f);
            panelRect.sizeDelta = new Vector2(250f, 65f);

            HorizontalLayoutGroup layoutGroup = itemPanel.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup == null) layoutGroup = itemPanel.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleRight;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            // Create the 3 Item Buttons: Btn_ItemDual, Btn_ItemTeleport, Btn_ItemHeal
            Button btnDual = CreateItemButton(itemPanel, "Btn_ItemDual", "DUAL\n[3]", defaultFont, new Color(0.85f, 0.65f, 0.1f, 0.9f));
            Button btnTeleport = CreateItemButton(itemPanel, "Btn_ItemTeleport", "TELEPORT\n[4]", defaultFont, new Color(0.2f, 0.65f, 0.85f, 0.9f));
            Button btnHeal = CreateItemButton(itemPanel, "Btn_ItemHeal", "HEAL\n[5]", defaultFont, new Color(0.2f, 0.85f, 0.4f, 0.9f));

            // Clean up old standalone buttons under Canvas root if present
            Transform oldDual = canvasObj.transform.Find("Btn_Dual");
            if (oldDual != null && oldDual.parent == canvasObj.transform) Object.DestroyImmediate(oldDual.gameObject);
            Transform oldTeleport = canvasObj.transform.Find("Btn_Teleport");
            if (oldTeleport != null && oldTeleport.parent == canvasObj.transform) Object.DestroyImmediate(oldTeleport.gameObject);

            // Wire references in UIManager
            SerializedObject serializedUI = new SerializedObject(uiManager);
            serializedUI.FindProperty("_btnDual").objectReferenceValue = btnDual;
            serializedUI.FindProperty("_btnTeleport").objectReferenceValue = btnTeleport;
            serializedUI.FindProperty("_btnHeal").objectReferenceValue = btnHeal;
            serializedUI.FindProperty("_btnDualBg").objectReferenceValue = btnDual != null ? btnDual.GetComponent<Image>() : null;
            serializedUI.FindProperty("_btnTeleportBg").objectReferenceValue = btnTeleport != null ? btnTeleport.GetComponent<Image>() : null;
            serializedUI.FindProperty("_btnHealBg").objectReferenceValue = btnHeal != null ? btnHeal.GetComponent<Image>() : null;
            serializedUI.ApplyModifiedProperties();

            uiManager.SetupItemButtons();
            Debug.Log("[Phase5ItemUISetup] Successfully configured ItemPanel with Btn_ItemDual, Btn_ItemTeleport, and Btn_ItemHeal!");
        }

        private static Button CreateItemButton(GameObject parent, string name, string labelText, Font font, Color bgColor)
        {
            Transform btnTrans = parent.transform.Find(name);
            GameObject btnObj;
            if (btnTrans != null)
            {
                btnObj = btnTrans.gameObject;
            }
            else
            {
                btnObj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(btnObj, $"Create {name}");
                btnObj.transform.SetParent(parent.transform, false);
            }

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(70f, 55f);

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
            txt.fontSize = 11;
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
