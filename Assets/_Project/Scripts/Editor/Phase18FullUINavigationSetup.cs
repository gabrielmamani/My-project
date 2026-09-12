using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Gunbound.Core;
using Gunbound.Network;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 18 Setup Utility:
    /// Constructs complete 16:9 Landscape Mobile UI Flow (Login, Main Menu Hub, Room Browser with Create Modal,
    /// and Room Ready Screen with synced Ready cards and tank selection) (RF-6.1 to RF-6.4).
    /// </summary>
    public static class Phase18FullUINavigationSetup
    {
        [MenuItem("Gunbound/Setup Phase 18 (Full Mobile UI Flow)")]
        public static void SetupPhase18()
        {
            Debug.Log("[Phase18Setup] Starting Full Mobile UI Flow Setup (Login, Main Menu, Room Browser, Room Ready)...");

            // 1. Ensure Core Managers in Scene
            EnsureCoreManagers();
            AddSceneToBuildSettings.RegisterActiveSceneToBuildSettings();

            // 2. Find Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[Phase18Setup] Canvas not found in scene!");
                return;
            }

            // 3. Create or Find Top-Level UI Panels
            GameObject panelLogin = GetOrCreatePanel(canvasObj.transform, "Panel_Login");
            GameObject panelMainMenu = GetOrCreatePanel(canvasObj.transform, "Panel_MainMenu");
            GameObject panelRoomBrowser = GetOrCreatePanel(canvasObj.transform, "Panel_RoomBrowser");
            GameObject panelRoomReady = GetOrCreatePanel(canvasObj.transform, "Panel_RoomReadyScreen");

            // Find existing InGame HUD or HUD parent
            GameObject panelInGameHUD = GameObject.Find("Panel_ShiftList");
            if (panelInGameHUD == null) panelInGameHUD = GameObject.Find("Panel_SatelliteUI");

            // 4. Build Login Panel
            LoginUI loginScript = SetupLoginPanel(panelLogin);

            // 5. Build Main Menu Panel
            MainMenuUI mainMenuScript = SetupMainMenuPanel(panelMainMenu);

            // 6. Build Room Browser Panel
            RoomBrowserUI roomBrowserScript = SetupRoomBrowserPanel(panelRoomBrowser);

            // 7. Build Room Ready Screen Panel
            RoomReadyUI roomReadyScript = SetupRoomReadyPanel(panelRoomReady, canvasObj.transform);

            // 8. Configure UINavigationManager
            var navManager = Object.FindFirstObjectByType<UINavigationManager>();
            if (navManager != null)
            {
                var soNav = new SerializedObject(navManager);
                soNav.FindProperty("_panelLogin").objectReferenceValue = panelLogin;
                soNav.FindProperty("_panelMainMenu").objectReferenceValue = panelMainMenu;
                soNav.FindProperty("_panelRoomBrowser").objectReferenceValue = panelRoomBrowser;
                soNav.FindProperty("_panelRoomReady").objectReferenceValue = panelRoomReady;
                soNav.FindProperty("_panelInGameHUD").objectReferenceValue = panelInGameHUD;
                soNav.ApplyModifiedProperties();

                navManager.BindPanels(panelLogin, panelMainMenu, panelRoomBrowser, panelRoomReady, panelInGameHUD);
                navManager.ShowLogin();
                Debug.Log("[Phase18Setup] UINavigationManager successfully bound and initialized to Login screen.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Phase18Setup] Phase 18 Full Mobile UI Flow Setup completed successfully!");
        }

        private static void EnsureCoreManagers()
        {
            var userMgr = Object.FindFirstObjectByType<UserDataManager>();
            if (userMgr == null)
            {
                GameObject uObj = new GameObject("UserDataManager", typeof(UserDataManager));
                Debug.Log("[Phase18Setup] Created UserDataManager instance.");
            }

            var navMgr = Object.FindFirstObjectByType<UINavigationManager>();
            if (navMgr == null)
            {
                GameObject nObj = new GameObject("UINavigationManager", typeof(UINavigationManager));
                Debug.Log("[Phase18Setup] Created UINavigationManager instance.");
            }
        }

        private static GameObject GetOrCreatePanel(Transform parent, string panelName)
        {
            Transform pTransform = parent.Find(panelName);
            if (pTransform == null)
            {
                GameObject go = new GameObject(panelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;

                Image img = go.GetComponent<Image>();
                img.color = new Color(0.06f, 0.09f, 0.15f, 0.95f);
                img.raycastTarget = true;

                return go;
            }
            return pTransform.gameObject;
        }

        private static LoginUI SetupLoginPanel(GameObject panel)
        {
            var loginScript = panel.GetComponent<LoginUI>();
            if (loginScript == null) loginScript = panel.AddComponent<LoginUI>();

            // Title Text
            Transform titleObj = panel.transform.Find("Txt_LoginTitle");
            if (titleObj == null)
            {
                GameObject tObj = CreateTextObject(panel.transform, "Txt_LoginTitle", "GUNBOUND MOBILE 2.5D", 26, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), new Vector2(0f, 120f), new Vector2(500f, 40f));
            }

            // Nickname InputField
            Transform inputObj = panel.transform.Find("Input_Nickname");
            InputField nicknameInput;
            if (inputObj == null)
            {
                GameObject iObj = CreateInputFieldObject(panel.transform, "Input_Nickname", "Ingresa tu Nickname...", new Vector2(0f, 40f), new Vector2(300f, 40f));
                nicknameInput = iObj.GetComponent<InputField>();
            }
            else
            {
                nicknameInput = inputObj.GetComponent<InputField>();
            }

            // Avatar Buttons Container
            Transform avatarContainer = panel.transform.Find("Container_Avatars");
            Button btnAv1, btnAv2, btnAv3;
            if (avatarContainer == null)
            {
                GameObject aObj = new GameObject("Container_Avatars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                aObj.transform.SetParent(panel.transform, false);

                RectTransform aRect = aObj.GetComponent<RectTransform>();
                aRect.anchoredPosition = new Vector2(0f, -20f);
                aRect.sizeDelta = new Vector2(320f, 45f);

                HorizontalLayoutGroup hlg = aObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 15f;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                btnAv1 = CreateButtonObject(aObj.transform, "Btn_Avatar1", "Avatar #1", new Color(0.2f, 0.5f, 0.8f));
                btnAv2 = CreateButtonObject(aObj.transform, "Btn_Avatar2", "Avatar #2", new Color(0.8f, 0.4f, 0.2f));
                btnAv3 = CreateButtonObject(aObj.transform, "Btn_Avatar3", "Avatar #3", new Color(0.3f, 0.7f, 0.4f));
            }
            else
            {
                btnAv1 = avatarContainer.Find("Btn_Avatar1")?.GetComponent<Button>();
                btnAv2 = avatarContainer.Find("Btn_Avatar2")?.GetComponent<Button>();
                btnAv3 = avatarContainer.Find("Btn_Avatar3")?.GetComponent<Button>();
            }

            // Submit Button
            Transform submitObj = panel.transform.Find("Btn_SubmitLogin");
            Button btnSubmit;
            if (submitObj == null)
            {
                btnSubmit = CreateButtonObject(panel.transform, "Btn_SubmitLogin", "ENTRAR AL JUEGO", new Color(0.15f, 0.75f, 0.35f));
                RectTransform sRect = btnSubmit.GetComponent<RectTransform>();
                sRect.anchoredPosition = new Vector2(0f, -80f);
                sRect.sizeDelta = new Vector2(220f, 45f);
            }
            else
            {
                btnSubmit = submitObj.GetComponent<Button>();
            }

            // Status Feedback Text
            Transform feedbackObj = panel.transform.Find("Txt_LoginFeedback");
            Text feedbackTxt;
            if (feedbackObj == null)
            {
                GameObject fObj = CreateTextObject(panel.transform, "Txt_LoginFeedback", "Ingresa tu usuario para guardar tu perfil.", 12, FontStyle.Normal, Color.gray, new Vector2(0f, -130f), new Vector2(400f, 30f));
                feedbackTxt = fObj.GetComponent<Text>();
            }
            else
            {
                feedbackTxt = feedbackObj.GetComponent<Text>();
            }

            loginScript.BindFields(nicknameInput, btnAv1, btnAv2, btnAv3, btnSubmit, feedbackTxt);
            return loginScript;
        }

        private static MainMenuUI SetupMainMenuPanel(GameObject panel)
        {
            var menuScript = panel.GetComponent<MainMenuUI>();
            if (menuScript == null) menuScript = panel.AddComponent<MainMenuUI>();

            // Header Profile Banner
            Transform headerObj = panel.transform.Find("Header_Profile");
            Text profileTxt, goldTxt;
            if (headerObj == null)
            {
                GameObject hObj = new GameObject("Header_Profile", typeof(RectTransform), typeof(Image));
                hObj.transform.SetParent(panel.transform, false);

                RectTransform hRect = hObj.GetComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0f, 1f);
                hRect.anchorMax = new Vector2(1f, 1f);
                hRect.pivot = new Vector2(0.5f, 1f);
                hRect.anchoredPosition = Vector2.zero;
                hRect.sizeDelta = new Vector2(0f, 50f);

                Image hImg = hObj.GetComponent<Image>();
                hImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);

                GameObject pObj = CreateTextObject(hObj.transform, "Txt_ProfileBanner", "<b>Jugador1</b> | Avatar #1", 14, FontStyle.Normal, Color.white, new Vector2(-150f, 0f), new Vector2(300f, 30f));
                profileTxt = pObj.GetComponent<Text>();
                profileTxt.alignment = TextAnchor.MiddleLeft;

                GameObject gObj = CreateTextObject(hObj.transform, "Txt_Gold", "<b>Oro:</b> 5000 G", 14, FontStyle.Normal, new Color(1f, 0.85f, 0.2f), new Vector2(150f, 0f), new Vector2(200f, 30f));
                goldTxt = gObj.GetComponent<Text>();
                goldTxt.alignment = TextAnchor.MiddleRight;
            }
            else
            {
                profileTxt = headerObj.Find("Txt_ProfileBanner")?.GetComponent<Text>();
                goldTxt = headerObj.Find("Txt_Gold")?.GetComponent<Text>();
            }

            // Buttons Container
            Transform containerObj = panel.transform.Find("Container_MenuButtons");
            Button btnQuick, btnBrowser, btnGarage, btnExit;
            if (containerObj == null)
            {
                GameObject cObj = new GameObject("Container_MenuButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
                cObj.transform.SetParent(panel.transform, false);

                RectTransform cRect = cObj.GetComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(0f, -20f);
                cRect.sizeDelta = new Vector2(260f, 200f);

                VerticalLayoutGroup vlg = cObj.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 12f;
                vlg.childAlignment = TextAnchor.MiddleCenter;

                btnQuick = CreateButtonObject(cObj.transform, "Btn_QuickMatch", "¡PARTIDA RÁPIDA!", new Color(0.9f, 0.4f, 0.15f));
                btnBrowser = CreateButtonObject(cObj.transform, "Btn_RoomBrowser", "BUSCADOR DE SALAS", new Color(0.2f, 0.6f, 0.9f));
                btnGarage = CreateButtonObject(cObj.transform, "Btn_Garage", "GARAJE Y TANQUES", new Color(0.3f, 0.7f, 0.4f));
                btnExit = CreateButtonObject(cObj.transform, "Btn_Exit", "CERRAR SESIÓN", new Color(0.6f, 0.2f, 0.2f));
            }
            else
            {
                btnQuick = containerObj.Find("Btn_QuickMatch")?.GetComponent<Button>();
                btnBrowser = containerObj.Find("Btn_RoomBrowser")?.GetComponent<Button>();
                btnGarage = containerObj.Find("Btn_Garage")?.GetComponent<Button>();
                btnExit = containerObj.Find("Btn_Exit")?.GetComponent<Button>();
            }

            menuScript.BindFields(profileTxt, goldTxt, btnQuick, btnBrowser, btnGarage, btnExit);
            return menuScript;
        }

        private static RoomBrowserUI SetupRoomBrowserPanel(GameObject panel)
        {
            var browserScript = panel.GetComponent<RoomBrowserUI>();
            if (browserScript == null) browserScript = panel.AddComponent<RoomBrowserUI>();

            // Title
            Transform titleObj = panel.transform.Find("Txt_BrowserTitle");
            if (titleObj == null)
            {
                CreateTextObject(panel.transform, "Txt_BrowserTitle", "BUSCADOR DE SALAS MULTIJUGADOR", 20, FontStyle.Bold, new Color(0.3f, 0.8f, 1.0f), new Vector2(0f, 130f), new Vector2(450f, 30f));
            }

            // Room List Scroll Container
            Transform containerObj = panel.transform.Find("Container_RoomList");
            if (containerObj == null)
            {
                GameObject cObj = new GameObject("Container_RoomList", typeof(RectTransform), typeof(VerticalLayoutGroup));
                cObj.transform.SetParent(panel.transform, false);

                RectTransform cRect = cObj.GetComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(0f, 20f);
                cRect.sizeDelta = new Vector2(460f, 160f);

                VerticalLayoutGroup vlg = cObj.GetComponent<VerticalLayoutGroup>();
                vlg.spacing = 8f;
                vlg.childAlignment = TextAnchor.UpperCenter;
            }

            // Buttons Bar
            Transform barObj = panel.transform.Find("Container_BrowserButtons");
            Button btnCreate, btnRefresh, btnBack, btnConnectIp;
            InputField ipInput, portInput;
            if (barObj == null)
            {
                GameObject bObj = new GameObject("Container_BrowserButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                bObj.transform.SetParent(panel.transform, false);

                RectTransform bRect = bObj.GetComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0f, -110f);
                bRect.sizeDelta = new Vector2(460f, 40f);

                HorizontalLayoutGroup hlg = bObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                btnCreate = CreateButtonObject(bObj.transform, "Btn_CreateRoom", "CREAR SALA", new Color(0.15f, 0.75f, 0.35f));
                btnRefresh = CreateButtonObject(bObj.transform, "Btn_Refresh", "ACTUALIZAR", new Color(0.2f, 0.5f, 0.8f));
                btnBack = CreateButtonObject(bObj.transform, "Btn_Back", "VOLVER", new Color(0.5f, 0.5f, 0.5f));
            }
            else
            {
                btnCreate = barObj.Find("Btn_CreateRoom")?.GetComponent<Button>();
                btnRefresh = barObj.Find("Btn_Refresh")?.GetComponent<Button>();
                btnBack = barObj.Find("Btn_Back")?.GetComponent<Button>();
            }

            // Quick IP bar
            Transform ipBarObj = panel.transform.Find("Container_IpConnect");
            if (ipBarObj == null)
            {
                GameObject iObj = new GameObject("Container_IpConnect", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                iObj.transform.SetParent(panel.transform, false);

                RectTransform iRect = iObj.GetComponent<RectTransform>();
                iRect.anchoredPosition = new Vector2(0f, -70f);
                iRect.sizeDelta = new Vector2(460f, 35f);

                HorizontalLayoutGroup hlg = iObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8f;
                hlg.childAlignment = TextAnchor.MiddleCenter;

                GameObject ipGo = CreateInputFieldObject(iObj.transform, "Input_IP", "127.0.0.1", Vector2.zero, new Vector2(160f, 35f));
                ipInput = ipGo.GetComponent<InputField>();

                GameObject portGo = CreateInputFieldObject(iObj.transform, "Input_Port", "7777", Vector2.zero, new Vector2(80f, 35f));
                portInput = portGo.GetComponent<InputField>();

                btnConnectIp = CreateButtonObject(iObj.transform, "Btn_ConnectIP", "UNIRSE IP", new Color(0.8f, 0.4f, 0.2f));
            }
            else
            {
                ipInput = ipBarObj.Find("Input_IP")?.GetComponent<InputField>();
                portInput = ipBarObj.Find("Input_Port")?.GetComponent<InputField>();
                btnConnectIp = ipBarObj.Find("Btn_ConnectIP")?.GetComponent<Button>();
            }

            // Create Room Modal
            GameObject modalObj = SetupCreateRoomModal(panel.transform, out InputField roomNameInput, out Button btnSubmitCreate, out Button btnCloseModal);

            browserScript.BindFields(containerObj, btnCreate, btnRefresh, btnBack, modalObj, roomNameInput, btnSubmitCreate, btnCloseModal, ipInput, portInput, btnConnectIp);
            return browserScript;
        }

        private static GameObject SetupCreateRoomModal(Transform parent, out InputField roomNameInput, out Button btnSubmitCreate, out Button btnCloseModal)
        {
            Transform modalTransform = parent.Find("Modal_CreateRoom");
            GameObject modalObj;
            if (modalTransform == null)
            {
                modalObj = new GameObject("Modal_CreateRoom", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                modalObj.transform.SetParent(parent, false);

                RectTransform mRect = modalObj.GetComponent<RectTransform>();
                mRect.anchoredPosition = Vector2.zero;
                mRect.sizeDelta = new Vector2(360f, 200f);

                Image img = modalObj.GetComponent<Image>();
                img.color = new Color(0.08f, 0.12f, 0.22f, 0.98f);

                CreateTextObject(modalObj.transform, "Txt_ModalTitle", "CREAR NUEVA SALA", 16, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), new Vector2(0f, 70f), new Vector2(320f, 30f));

                GameObject inputGo = CreateInputFieldObject(modalObj.transform, "Input_RoomName", "Nombre de la sala...", new Vector2(0f, 20f), new Vector2(280f, 35f));
                roomNameInput = inputGo.GetComponent<InputField>();

                btnSubmitCreate = CreateButtonObject(modalObj.transform, "Btn_SubmitCreate", "CREAR E INICIAR HOST", new Color(0.15f, 0.75f, 0.35f));
                RectTransform bRect = btnSubmitCreate.GetComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(0f, -30f);
                bRect.sizeDelta = new Vector2(220f, 40f);

                btnCloseModal = CreateButtonObject(modalObj.transform, "Btn_CloseModal", "CANCELAR", new Color(0.6f, 0.2f, 0.2f));
                RectTransform cRect = btnCloseModal.GetComponent<RectTransform>();
                cRect.anchoredPosition = new Vector2(0f, -75f);
                cRect.sizeDelta = new Vector2(140f, 30f);

                modalObj.SetActive(false);
            }
            else
            {
                modalObj = modalTransform.gameObject;
                roomNameInput = modalTransform.Find("Input_RoomName")?.GetComponent<InputField>();
                btnSubmitCreate = modalTransform.Find("Btn_SubmitCreate")?.GetComponent<Button>();
                btnCloseModal = modalTransform.Find("Btn_CloseModal")?.GetComponent<Button>();
            }

            return modalObj;
        }

        private static RoomReadyUI SetupRoomReadyPanel(GameObject panel, Transform canvasTransform)
        {
            var readyScript = panel.GetComponent<RoomReadyUI>();
            if (readyScript == null) readyScript = panel.AddComponent<RoomReadyUI>();

            // Header Title
            Transform titleObj = panel.transform.Find("Txt_ReadyTitle");
            if (titleObj == null)
            {
                CreateTextObject(panel.transform, "Txt_ReadyTitle", "SALA DE ESPERA Y PREPARACIÓN DE COMBATE", 18, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f), new Vector2(0f, 135f), new Vector2(500f, 30f));
            }

            // Cards Container (Side by Side)
            Transform p1Card = panel.transform.Find("Card_P1");
            Text p1Name, p1Mobile, p1Ready;
            if (p1Card == null)
            {
                GameObject card = CreatePlayerCardObject(panel.transform, "Card_P1", "P1 (Host)", new Vector2(-120f, 45f), new Color(0.12f, 0.25f, 0.45f, 0.9f), out p1Name, out p1Mobile, out p1Ready);
            }
            else
            {
                p1Name = p1Card.Find("Txt_P1Name")?.GetComponent<Text>();
                p1Mobile = p1Card.Find("Txt_P1Mobile")?.GetComponent<Text>();
                p1Ready = p1Card.Find("Txt_P1Ready")?.GetComponent<Text>();
            }

            Transform p2Card = panel.transform.Find("Card_P2");
            Text p2Name, p2Mobile, p2Ready;
            if (p2Card == null)
            {
                GameObject card = CreatePlayerCardObject(panel.transform, "Card_P2", "P2 (Rival)", new Vector2(120f, 45f), new Color(0.45f, 0.2f, 0.12f, 0.9f), out p2Name, out p2Mobile, out p2Ready);
            }
            else
            {
                p2Name = p2Card.Find("Txt_P2Name")?.GetComponent<Text>();
                p2Mobile = p2Card.Find("Txt_P2Mobile")?.GetComponent<Text>();
                p2Ready = p2Card.Find("Txt_P2Ready")?.GetComponent<Text>();
            }

            // Control Buttons
            Transform btnReadyObj = panel.transform.Find("Btn_ToggleReady");
            Button btnReady;
            if (btnReadyObj == null)
            {
                btnReady = CreateButtonObject(panel.transform, "Btn_ToggleReady", "¡LISTO / PREPARADO!", new Color(0.15f, 0.75f, 0.35f));
                RectTransform bRect = btnReady.GetComponent<RectTransform>();
                bRect.anchoredPosition = new Vector2(-120f, -110f);
                bRect.sizeDelta = new Vector2(180f, 40f);
            }
            else
            {
                btnReady = btnReadyObj.GetComponent<Button>();
            }

            Transform btnStartObj = panel.transform.Find("Btn_StartMatch");
            Button btnStart;
            if (btnStartObj == null)
            {
                btnStart = CreateButtonObject(panel.transform, "Btn_StartMatch", "INICIAR COMBATE", new Color(0.9f, 0.4f, 0.15f));
                RectTransform sRect = btnStart.GetComponent<RectTransform>();
                sRect.anchoredPosition = new Vector2(70f, -110f);
                sRect.sizeDelta = new Vector2(180f, 40f);
            }
            else
            {
                btnStart = btnStartObj.GetComponent<Button>();
            }

            Transform btnLeaveObj = panel.transform.Find("Btn_LeaveRoom");
            Button btnLeave;
            if (btnLeaveObj == null)
            {
                btnLeave = CreateButtonObject(panel.transform, "Btn_LeaveRoom", "SALIR", new Color(0.5f, 0.5f, 0.5f));
                RectTransform lRect = btnLeave.GetComponent<RectTransform>();
                lRect.anchoredPosition = new Vector2(210f, -110f);
                lRect.sizeDelta = new Vector2(80f, 40f);
            }
            else
            {
                btnLeave = btnLeaveObj.GetComponent<Button>();
            }

            // Status Text
            Transform statusObj = panel.transform.Find("Txt_RoomStatus");
            Text statusTxt;
            if (statusObj == null)
            {
                GameObject stObj = CreateTextObject(panel.transform, "Txt_RoomStatus", "Esperando confirmación de preparación de ambos jugadores...", 12, FontStyle.Normal, Color.yellow, new Vector2(0f, -145f), new Vector2(460f, 25f));
                statusTxt = stObj.GetComponent<Text>();
            }
            else
            {
                statusTxt = statusObj.GetComponent<Text>();
            }

            readyScript.BindFields(p1Name, p1Mobile, p1Ready, p2Name, p2Mobile, p2Ready, btnReady, btnStart, btnLeave, statusTxt);
            return readyScript;
        }

        private static GameObject CreatePlayerCardObject(Transform parent, string cardName, string defaultTitle, Vector2 pos, Color bgColor, out Text nameTxt, out Text mobileTxt, out Text readyTxt)
        {
            GameObject cardObj = new GameObject(cardName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(210f, 130f);

            Image img = cardObj.GetComponent<Image>();
            img.color = bgColor;

            GameObject tGo = CreateTextObject(cardObj.transform, "Txt_" + cardName + "Name", "<b>" + defaultTitle + "</b>", 13, FontStyle.Bold, Color.white, new Vector2(0f, 40f), new Vector2(190f, 25f));
            nameTxt = tGo.GetComponent<Text>();

            GameObject mGo = CreateTextObject(cardObj.transform, "Txt_" + cardName + "Mobile", "Móvil: Mage", 12, FontStyle.Normal, Color.lightGray, new Vector2(0f, 5f), new Vector2(190f, 25f));
            mobileTxt = mGo.GetComponent<Text>();

            GameObject rGo = CreateTextObject(cardObj.transform, "Txt_" + cardName + "Ready", "PREPARANDO", 14, FontStyle.Bold, Color.yellow, new Vector2(0f, -35f), new Vector2(190f, 25f));
            readyTxt = rGo.GetComponent<Text>();

            return cardObj;
        }

        private static GameObject CreateTextObject(Transform parent, string objectName, string text, int fontSize, FontStyle style, Color color, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Text txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = text;
            txt.supportRichText = true;
            txt.raycastTarget = false;

            return go;
        }

        private static Button CreateButtonObject(Transform parent, string objectName, string label, Color color)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140f, 35f);

            Image img = go.GetComponent<Image>();
            img.color = color;

            Button btn = go.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.25f;
            cb.pressedColor = color * 0.75f;
            btn.colors = cb;

            GameObject tGo = CreateTextObject(go.transform, "Text", label, 12, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero);
            RectTransform tRect = tGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;

            return btn;
        }

        private static GameObject CreateInputFieldObject(Transform parent, string objectName, string placeholderText, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.25f, 0.95f);

            InputField input = go.GetComponent<InputField>();

            // Text component
            GameObject tGo = CreateTextObject(go.transform, "Text", "", 13, FontStyle.Normal, Color.white, Vector2.zero, Vector2.zero);
            RectTransform tRect = tGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-10f, 0f);
            Text textComp = tGo.GetComponent<Text>();
            textComp.alignment = TextAnchor.MiddleLeft;

            // Placeholder component
            GameObject pGo = CreateTextObject(go.transform, "Placeholder", placeholderText, 13, FontStyle.Italic, Color.gray, Vector2.zero, Vector2.zero);
            RectTransform pRect = pGo.GetComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero;
            pRect.anchorMax = Vector2.one;
            pRect.sizeDelta = new Vector2(-10f, 0f);
            Text placeComp = pGo.GetComponent<Text>();
            placeComp.alignment = TextAnchor.MiddleLeft;

            input.textComponent = textComp;
            input.placeholder = placeComp;

            return go;
        }
    }
}
