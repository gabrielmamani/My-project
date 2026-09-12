using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Environment;
using Gunbound.UI;

namespace Gunbound.Editor
{
    public static class Phase15SatelliteSetup
    {
        [MenuItem("Gunbound/Setup Phase 15: Satellite & Weather System")]
        public static void SetupSatelliteSystem()
        {
            // 1. Ensure [SatelliteManager] GameObject exists in scene
            SatelliteManager mgr = Object.FindFirstObjectByType<SatelliteManager>();
            if (mgr == null)
            {
                GameObject mgrObj = GameObject.Find("[SatelliteManager]");
                if (mgrObj == null)
                {
                    mgrObj = new GameObject("[SatelliteManager]");
                    Undo.RegisterCreatedObjectUndo(mgrObj, "Create [SatelliteManager]");
                }
                mgr = mgrObj.AddComponent<SatelliteManager>();
                Debug.Log("[Phase15SatelliteSetup] Created '[SatelliteManager]' GameObject with SatelliteManager component.");
            }

            // 2. Locate Canvas
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // 3. Create Panel_SatelliteUI in Canvas (Top-Center)
            Transform canvasTransform = canvas.transform;
            Transform existingPanel = canvasTransform.Find("Panel_SatelliteUI");
            GameObject panelObj;

            if (existingPanel != null)
            {
                panelObj = existingPanel.gameObject;
                Undo.RecordObject(panelObj, "Update Panel_SatelliteUI");
            }
            else
            {
                panelObj = new GameObject("Panel_SatelliteUI");
                panelObj.transform.SetParent(canvasTransform, false);
                Undo.RegisterCreatedObjectUndo(panelObj, "Create Panel_SatelliteUI");
            }

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1.0f);
            panelRect.anchorMax = new Vector2(0.5f, 1.0f);
            panelRect.pivot = new Vector2(0.5f, 1.0f);
            panelRect.sizeDelta = new Vector2(340f, 52f);
            panelRect.anchoredPosition = new Vector2(0f, -32f);

            Image panelBg = panelObj.GetComponent<Image>();
            if (panelBg == null) panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.1f, 0.12f, 0.18f, 0.88f);
            panelBg.raycastTarget = false;

            Outline outline = panelObj.GetComponent<Outline>();
            if (outline == null) outline = panelObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.3f, 0.7f, 1.0f, 0.5f);
            outline.effectDistance = new Vector2(2f, -2f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // Icon Image
            Transform existingIcon = panelObj.transform.Find("Img_SatelliteIcon");
            GameObject iconObj = existingIcon != null ? existingIcon.gameObject : new GameObject("Img_SatelliteIcon");
            if (existingIcon == null) iconObj.transform.SetParent(panelObj.transform, false);

            Image iconImg = iconObj.GetComponent<Image>();
            if (iconImg == null) iconImg = iconObj.AddComponent<Image>();
            iconImg.color = new Color(0.1f, 0.9f, 1.0f, 1.0f);

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.04f, 0.15f);
            iconRect.anchorMax = new Vector2(0.15f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            // Title Text
            Transform existingTitle = panelObj.transform.Find("Txt_SatelliteTitle");
            GameObject titleObj = existingTitle != null ? existingTitle.gameObject : new GameObject("Txt_SatelliteTitle");
            if (existingTitle == null) titleObj.transform.SetParent(panelObj.transform, false);

            Text titleText = titleObj.GetComponent<Text>();
            if (titleText == null) titleText = titleObj.AddComponent<Text>();
            titleText.font = font;
            titleText.fontSize = 16;
            titleText.fontStyle = FontStyle.Bold;
            titleText.text = "SATÉLITE: NORMAL";
            titleText.color = new Color(0.8f, 0.85f, 0.9f, 1.0f);
            titleText.alignment = TextAnchor.MiddleLeft;

            Outline titleOutline = titleObj.GetComponent<Outline>();
            if (titleOutline == null) titleOutline = titleObj.AddComponent<Outline>();
            titleOutline.effectColor = Color.black;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.18f, 0.45f);
            titleRect.anchorMax = new Vector2(0.96f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Description Text
            Transform existingDesc = panelObj.transform.Find("Txt_SatelliteDesc");
            GameObject descObj = existingDesc != null ? existingDesc.gameObject : new GameObject("Txt_SatelliteDesc");
            if (existingDesc == null) descObj.transform.SetParent(panelObj.transform, false);

            Text descText = descObj.GetComponent<Text>();
            if (descText == null) descText = descObj.AddComponent<Text>();
            descText.font = font;
            descText.fontSize = 12;
            descText.fontStyle = FontStyle.Italic;
            descText.text = "Condiciones Climáticas Estándar";
            descText.color = new Color(0.7f, 0.75f, 0.8f, 1.0f);
            descText.alignment = TextAnchor.MiddleLeft;

            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.18f, 0.05f);
            descRect.anchorMax = new Vector2(0.96f, 0.5f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            // Component Assignment
            SatelliteUI satUI = panelObj.GetComponent<SatelliteUI>();
            if (satUI == null) satUI = panelObj.AddComponent<SatelliteUI>();

            SerializedObject so = new SerializedObject(satUI);
            so.FindProperty("_satelliteIcon").objectReferenceValue = iconImg;
            so.FindProperty("_satelliteTitleText").objectReferenceValue = titleText;
            so.FindProperty("_satelliteDescText").objectReferenceValue = descText;
            so.FindProperty("_badgeBackground").objectReferenceValue = panelBg;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Phase15SatelliteSetup] Satellite System & Panel_SatelliteUI setup completed successfully!");
        }
    }
}
