using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.UI;

namespace Gunbound.Editor
{
    public static class Phase14MatchResultSetup
    {
        [MenuItem("Gunbound/Setup Phase 14: Match Result & Overhead UI")]
        public static void SetupMatchResultAndOverheadUI()
        {
            // 1. Locate Canvas
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

            // 2. Setup Panel_MatchResult
            Transform canvasTransform = canvas.transform;
            Transform existingPanel = canvasTransform.Find("Panel_MatchResult");
            GameObject panelObj;

            if (existingPanel != null)
            {
                panelObj = existingPanel.gameObject;
                Undo.RecordObject(panelObj, "Update Panel_MatchResult");
            }
            else
            {
                panelObj = new GameObject("Panel_MatchResult");
                panelObj.transform.SetParent(canvasTransform, false);
                Undo.RegisterCreatedObjectUndo(panelObj, "Create Panel_MatchResult");
            }

            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panelObj.GetComponent<Image>();
            if (panelBg == null) panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.05f, 0.1f, 0.88f);
            panelBg.raycastTarget = true;

            // Card Container (Central Dialog Box)
            Transform existingCard = panelObj.transform.Find("ResultCard");
            GameObject cardObj;
            if (existingCard != null) cardObj = existingCard.gameObject;
            else
            {
                cardObj = new GameObject("ResultCard");
                cardObj.transform.SetParent(panelObj.transform, false);
            }

            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            if (cardRect == null) cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(500f, 380f);
            cardRect.anchoredPosition = Vector2.zero;

            Image cardBg = cardObj.GetComponent<Image>();
            if (cardBg == null) cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            Outline cardOutline = cardObj.GetComponent<Outline>();
            if (cardOutline == null) cardOutline = cardObj.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.3f, 0.7f, 1.0f, 0.6f);
            cardOutline.effectDistance = new Vector2(3f, -3f);

            Font mainFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mainFont == null) mainFont = Font.CreateDynamicFontFromOSFont("Arial", 28);

            // Title Text
            Text titleText = CreateOrGetText(cardObj.transform, "Txt_Title", mainFont, 40, FontStyle.Bold, "¡VICTORIA!", new Color(0.1f, 0.95f, 0.4f, 1.0f));
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.78f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Subtitle Text
            Text subtitleText = CreateOrGetText(cardObj.transform, "Txt_Subtitle", mainFont, 16, FontStyle.Italic, "¡Has dominado el campo de batalla!", new Color(0.8f, 0.85f, 0.9f, 1.0f));
            RectTransform subRect = subtitleText.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.05f, 0.68f);
            subRect.anchorMax = new Vector2(0.95f, 0.78f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            // Stats Container
            Transform existingStats = cardObj.transform.Find("StatsBox");
            GameObject statsObj;
            if (existingStats != null) statsObj = existingStats.gameObject;
            else
            {
                statsObj = new GameObject("StatsBox");
                statsObj.transform.SetParent(cardObj.transform, false);
            }

            RectTransform statsRect = statsObj.GetComponent<RectTransform>();
            if (statsRect == null) statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.08f, 0.28f);
            statsRect.anchorMax = new Vector2(0.92f, 0.65f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            Image statsBg = statsObj.GetComponent<Image>();
            if (statsBg == null) statsBg = statsObj.AddComponent<Image>();
            statsBg.color = new Color(0.08f, 0.09f, 0.14f, 0.8f);

            // Winner Text
            Text winnerText = CreateOrGetText(statsObj.transform, "Txt_Winner", mainFont, 18, FontStyle.Bold, "Ganador: Jugador 1 (Host)", new Color(1.0f, 0.9f, 0.3f, 1.0f));
            RectTransform winnerRect = winnerText.GetComponent<RectTransform>();
            winnerRect.anchorMin = new Vector2(0.05f, 0.65f);
            winnerRect.anchorMax = new Vector2(0.95f, 0.95f);
            winnerRect.offsetMin = Vector2.zero;
            winnerRect.offsetMax = Vector2.zero;

            // Reason Text
            Text reasonText = CreateOrGetText(statsObj.transform, "Txt_Reason", mainFont, 15, FontStyle.Normal, "Motivo: Victoria por K.O.", new Color(0.9f, 0.9f, 0.95f, 1.0f));
            RectTransform reasonRect = reasonText.GetComponent<RectTransform>();
            reasonRect.anchorMin = new Vector2(0.05f, 0.35f);
            reasonRect.anchorMax = new Vector2(0.95f, 0.65f);
            reasonRect.offsetMin = Vector2.zero;
            reasonRect.offsetMax = Vector2.zero;

            // Turns Text
            Text turnsText = CreateOrGetText(statsObj.transform, "Txt_Turns", mainFont, 15, FontStyle.Normal, "Turnos jugados: 1", new Color(0.75f, 0.85f, 0.95f, 1.0f));
            RectTransform turnsRect = turnsText.GetComponent<RectTransform>();
            turnsRect.anchorMin = new Vector2(0.05f, 0.05f);
            turnsRect.anchorMax = new Vector2(0.95f, 0.35f);
            turnsRect.offsetMin = Vector2.zero;
            turnsRect.offsetMax = Vector2.zero;

            // Buttons Container
            Transform existingBtns = cardObj.transform.Find("ButtonsLayout");
            GameObject btnsObj;
            if (existingBtns != null) btnsObj = existingBtns.gameObject;
            else
            {
                btnsObj = new GameObject("ButtonsLayout");
                btnsObj.transform.SetParent(cardObj.transform, false);
            }

            RectTransform btnsRect = btnsObj.GetComponent<RectTransform>();
            if (btnsRect == null) btnsRect = btnsObj.AddComponent<RectTransform>();
            btnsRect.anchorMin = new Vector2(0.08f, 0.06f);
            btnsRect.anchorMax = new Vector2(0.92f, 0.22f);
            btnsRect.offsetMin = Vector2.zero;
            btnsRect.offsetMax = Vector2.zero;

            HorizontalLayoutGroup layout = btnsObj.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = btnsObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            // Rematch Button
            Button btnRematch = CreateOrGetButton(btnsObj.transform, "Btn_Rematch", mainFont, "REVANCHA", new Color(0.15f, 0.65f, 0.3f, 1.0f));

            // Lobby Button
            Button btnLobby = CreateOrGetButton(btnsObj.transform, "Btn_ReturnToLobby", mainFont, "LOBBY", new Color(0.15f, 0.45f, 0.85f, 1.0f));

            // MatchResultUI Component
            MatchResultUI resultUI = panelObj.GetComponent<MatchResultUI>();
            if (resultUI == null) resultUI = panelObj.AddComponent<MatchResultUI>();

            // Assign Serialized Private Fields via SerializedObject
            SerializedObject so = new SerializedObject(resultUI);
            so.FindProperty("_resultPanel").objectReferenceValue = panelObj;
            so.FindProperty("_titleText").objectReferenceValue = titleText;
            so.FindProperty("_subtitleText").objectReferenceValue = subtitleText;
            so.FindProperty("_winnerText").objectReferenceValue = winnerText;
            so.FindProperty("_statsTurnsText").objectReferenceValue = turnsText;
            so.FindProperty("_statsReasonText").objectReferenceValue = reasonText;
            so.FindProperty("_btnRematch").objectReferenceValue = btnRematch;
            so.FindProperty("_btnReturnToLobby").objectReferenceValue = btnLobby;
            so.ApplyModifiedProperties();

            // Hide panel by default
            panelObj.SetActive(false);

            // 3. Setup Overhead Health Bars for Players in scene
            GameObject player1Obj = GameObject.Find("Player1");
            if (player1Obj != null && player1Obj.transform.Find("OverheadHealthBar_Canvas") == null)
            {
                OverheadHealthBar.CreateOverheadBar(player1Obj, "Jugador 1 (P1)", new Color(0.2f, 0.9f, 1.0f, 1.0f));
            }

            GameObject player2Obj = GameObject.Find("Player2");
            if (player2Obj != null && player2Obj.transform.Find("OverheadHealthBar_Canvas") == null)
            {
                OverheadHealthBar.CreateOverheadBar(player2Obj, "Jugador 2 (P2)", new Color(1.0f, 0.4f, 0.9f, 1.0f));
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Phase14MatchResultSetup] Panel_MatchResult and Overhead Health Bars setup successfully!");
        }

        private static Text CreateOrGetText(Transform parent, string name, Font font, int size, FontStyle style, string defaultText, Color color)
        {
            Transform existing = parent.Find(name);
            GameObject textObj = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null) textObj.transform.SetParent(parent, false);

            Text text = textObj.GetComponent<Text>();
            if (text == null) text = textObj.AddComponent<Text>();

            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.text = defaultText;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;

            Outline outline = textObj.GetComponent<Outline>();
            if (outline == null) outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            return text;
        }

        private static Button CreateOrGetButton(Transform parent, string name, Font font, string textContent, Color bgColor)
        {
            Transform existing = parent.Find(name);
            GameObject btnObj = existing != null ? existing.gameObject : new GameObject(name);
            if (existing == null) btnObj.transform.SetParent(parent, false);

            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.GetComponent<Button>();
            if (btn == null) btn = btnObj.AddComponent<Button>();

            ColorBlock colors = btn.colors;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            Text textComp = CreateOrGetText(btnObj.transform, "Text", font, 16, FontStyle.Bold, textContent, Color.white);
            RectTransform textRect = textComp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
