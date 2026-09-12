#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Gameplay;
using Gunbound.Managers;
using Gunbound.Player;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Canvas Reorganization & Strict Layout Cleanup Setup Script:
    /// - Destroys orphan buttons, Btn_Fire, floating red arrows, and legacy duplicate button matrices.
    /// - Movement Controls (Bottom-Left): Panel_Movement (HorizontalLayoutGroup) with Btn_Left, Btn_Right, Btn_Down, Btn_Up (70x70).
    /// - Actions & Ammo (Bottom-Right): Panel_Combat -> Items_Row (Btn_Dual, Btn_Teleport, Btn_Heal 60x60) & Ammo_Row (Btn_T1, Btn_T2, Btn_SS 80x80).
    /// - PowerBar (Bottom-Center): Anchored at 500x30.
    /// - Global Indicators (Top-Center): Panel_TopHeader for Wind, Timer, Satellite, Turn text.
    /// - Raycast Target Audit: Ensures all container panels have raycastTarget = false so Drag-to-Power works smoothly.
    /// </summary>
    [InitializeOnLoad]
    public static class CanvasRestructureSetup
    {
        static CanvasRestructureSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    ExecuteSetup();
                }
            };
        }

        [MenuItem("Gunbound/Restructure Canvas UI")]
        public static void ExecuteSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                string scenePath = "Assets/Scenes/SampleScene.unity";
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            if (!activeScene.IsValid())
            {
                Debug.LogError("[CanvasRestructureSetup] Could not open SampleScene.unity");
                return;
            }

            Debug.Log("[CanvasRestructureSetup] Starting strict Canvas UI reorganization...");

            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[CanvasRestructureSetup] Root Canvas not found in active scene!");
                return;
            }

            ConfigureRootCanvas(canvasObj);

            Transform parentTransform = canvasObj.transform.Find("HUDPanel");
            if (parentTransform == null) parentTransform = canvasObj.transform;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // 1. Purge obsolete, orphan and duplicate UI elements (including Btn_Fire, floating arrows, legacy panels)
            PurgeObsoleteElements(canvasObj);

            // 2. Configure Player Overhead World Space UI
            SetupPlayerOverheadHUD(defaultFont);

            // 3. Movement Controls Panel (Bottom-Left)
            SetupPanelMovement(parentTransform, defaultFont, canvasObj);

            // 4. Combat & Item Panel (Bottom-Right)
            SetupPanelCombat(parentTransform, defaultFont, canvasObj);

            // 5. PowerBar (Bottom-Center) and Top-Header Indicators
            SetupPowerBarAndTopHeader(parentTransform, canvasObj);

            // 6. Raycast Target Audit & UIManager Linking
            AuditRaycastTargetsAndLinkUIManager(canvasObj);

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[CanvasRestructureSetup] Canvas visual hierarchy successfully reorganized, cleaned, and saved!");
        }

        private static void ConfigureRootCanvas(GameObject canvasObj)
        {
            CanvasScaler scaler = GetOrAdd<CanvasScaler>(canvasObj);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Ensure Canvas container itself does not block raycasts
            GraphicRaycaster gr = GetOrAdd<GraphicRaycaster>(canvasObj);
            gr.ignoreReversedGraphics = true;
        }

        private static void PurgeObsoleteElements(GameObject canvasObj)
        {
            string[] obsoleteNames = new string[]
            {
                "Btn_Fire", "FireButton", "Btn_Shoot", "RedArrow", "AimArrow", "Red_Arrow",
                "FloatingPanel", "ResidualButtons", "SquareButton", "ArrowButtons", "LegacyControls",
                "Panel_Controls", "Panel_Actions", "Row_Items", "Row_Ammo", "Row_Angle", "Row_Movement",
                "ItemPanel", "WeaponPanel", "AmmoRow", "ItemRow", "ShotPanel", "TouchFireBtn"
            };

            foreach (string objName in obsoleteNames)
            {
                DestroyAllMatching(canvasObj.transform, objName);
            }

            // Also purge any orphaned red arrows or floating buttons in scene root if present
            var rootObjs = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var rootObj in rootObjs)
            {
                if (rootObj.name.Contains("RedArrow") || rootObj.name.Contains("Btn_Fire") || rootObj.name.Contains("AimArrow"))
                {
                    Object.DestroyImmediate(rootObj);
                    Debug.Log($"[CanvasRestructureSetup] Destroyed root obsolete object: {rootObj.name}");
                }
            }
        }

        private static void DestroyAllMatching(Transform parent, string targetName)
        {
            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != null && child != parent && child.name == targetName)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"[CanvasRestructureSetup] Purged obsolete element: {targetName}");
                }
            }
        }

        private static void SetupPlayerOverheadHUD(Font defaultFont)
        {
            GameObject p1 = GameObject.Find("Player_1") ?? GameObject.Find("Player_Mage");
            if (p1 != null) p1.name = "Player_1";
            GameObject p2 = GameObject.Find("Player_2");

            GameObject[] players = new GameObject[] { p1, p2 };
            int pIndex = 1;

            foreach (GameObject player in players)
            {
                if (player == null) continue;

                Transform oldHC = player.transform.Find("HealthCanvas");
                if (oldHC != null) Object.DestroyImmediate(oldHC.gameObject);

                Transform overheadTrans = player.transform.Find("Overhead_HUD");
                GameObject overheadObj = overheadTrans != null ? overheadTrans.gameObject : new GameObject("Overhead_HUD");
                if (overheadTrans == null)
                {
                    Undo.RegisterCreatedObjectUndo(overheadObj, "Create Overhead_HUD");
                    overheadObj.transform.SetParent(player.transform, false);
                }

                Canvas worldCanvas = GetOrAdd<Canvas>(overheadObj);
                worldCanvas.renderMode = RenderMode.WorldSpace;

                CanvasScaler scaler = GetOrAdd<CanvasScaler>(overheadObj);
                scaler.dynamicPixelsPerUnit = 100;

                GetOrAdd<GraphicRaycaster>(overheadObj);

                RectTransform rt = overheadObj.GetComponent<RectTransform>();
                rt.localPosition = new Vector3(0f, 1.5f, 0f);
                float signX = player.transform.localScale.x < 0 ? -1f : 1f;
                rt.localScale = new Vector3(0.01f * signX, 0.01f, 0.01f);
                rt.sizeDelta = new Vector2(200f, 50f);
                rt.localRotation = Quaternion.identity;

                // Name Text
                Transform nameTrans = overheadObj.transform.Find("NameText");
                GameObject nameObj = nameTrans != null ? nameTrans.gameObject : new GameObject("NameText");
                nameObj.transform.SetParent(overheadObj.transform, false);

                RectTransform nameRt = GetOrAdd<RectTransform>(nameObj);
                nameRt.anchorMin = new Vector2(0f, 0.5f);
                nameRt.anchorMax = new Vector2(1f, 1f);
                nameRt.offsetMin = Vector2.zero;
                nameRt.offsetMax = Vector2.zero;

                Text nameText = GetOrAdd<Text>(nameObj);
                nameText.text = $"Player {pIndex}";
                nameText.font = defaultFont;
                nameText.fontSize = 18;
                nameText.fontStyle = FontStyle.Bold;
                nameText.alignment = TextAnchor.MiddleCenter;
                nameText.color = Color.white;
                nameText.raycastTarget = false;

                // HealthBar Slider
                Transform hbTrans = overheadObj.transform.Find("HealthBar");
                GameObject hbObj = hbTrans != null ? hbTrans.gameObject : new GameObject("HealthBar");
                hbObj.transform.SetParent(overheadObj.transform, false);

                RectTransform hbRt = GetOrAdd<RectTransform>(hbObj);
                hbRt.anchorMin = new Vector2(0.05f, 0.05f);
                hbRt.anchorMax = new Vector2(0.95f, 0.45f);
                hbRt.offsetMin = Vector2.zero;
                hbRt.offsetMax = Vector2.zero;

                Slider slider = GetOrAdd<Slider>(hbObj);
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 1f;
                slider.interactable = false;

                Transform bgTrans = hbObj.transform.Find("Background");
                GameObject bgObj = bgTrans != null ? bgTrans.gameObject : new GameObject("Background");
                bgObj.transform.SetParent(hbObj.transform, false);
                RectTransform bgRt = GetOrAdd<RectTransform>(bgObj);
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
                Image bgImg = GetOrAdd<Image>(bgObj);
                bgImg.color = new Color(0.2f, 0.05f, 0.05f, 0.8f);
                bgImg.raycastTarget = false;

                Transform faTrans = hbObj.transform.Find("Fill Area");
                GameObject faObj = faTrans != null ? faTrans.gameObject : new GameObject("Fill Area");
                faObj.transform.SetParent(hbObj.transform, false);
                RectTransform faRt = GetOrAdd<RectTransform>(faObj);
                faRt.anchorMin = Vector2.zero;
                faRt.anchorMax = Vector2.one;
                faRt.sizeDelta = Vector2.zero;

                Transform fillTrans = faObj.transform.Find("Fill");
                GameObject fillObj = fillTrans != null ? fillTrans.gameObject : new GameObject("Fill");
                fillObj.transform.SetParent(faObj.transform, false);
                RectTransform fillRt = GetOrAdd<RectTransform>(fillObj);
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.sizeDelta = Vector2.zero;
                Image fillImg = GetOrAdd<Image>(fillObj);
                fillImg.color = pIndex == 1 ? new Color(0.2f, 0.85f, 0.3f, 1f) : new Color(0.9f, 0.3f, 0.2f, 1f);
                fillImg.raycastTarget = false;

                slider.fillRect = fillRt;

                HealthBar hbScript = GetOrAdd<HealthBar>(hbObj);
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth == null) playerHealth = Undo.AddComponent<Health>(player);
                hbScript.SetTargetHealth(playerHealth);

                pIndex++;
            }
        }

        private static void SetupPanelMovement(Transform parentTransform, Font defaultFont, GameObject canvasObj)
        {
            // Panel_Movement (Bottom-Left)
            GameObject panelMovement = GetOrCreateContainer(parentTransform, "Panel_Movement", new Vector2(0f, 0f), new Vector2(20f, 20f), new Vector2(330f, 80f));
            RectTransform pmRt = panelMovement.GetComponent<RectTransform>();
            pmRt.pivot = new Vector2(0f, 0f);

            // Container image with raycastTarget = false
            Image pmImg = GetOrAdd<Image>(panelMovement);
            pmImg.color = new Color(0f, 0f, 0f, 0f); // Transparent container
            pmImg.raycastTarget = false;

            HorizontalLayoutGroup hlg = GetOrAdd<HorizontalLayoutGroup>(panelMovement);
            hlg.spacing = 10f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            UIManager uiManager = canvasObj.GetComponent<UIManager>();

            // 4 Movement/Angle buttons: Btn_Left, Btn_Right, Btn_Down, Btn_Up (70x70)
            Button btnLeft = GetOrCreateOrMoveButton(panelMovement.transform, canvasObj, "Btn_Left", "◄", defaultFont, new Vector2(70f, 70f), new Color(0.22f, 0.26f, 0.32f, 0.95f), "Move_Left");
            HoldButtonHandler holdLeft = GetOrAdd<HoldButtonHandler>(btnLeft.gameObject);
            if (player != null) SafeAddHoldListener(holdLeft, player.OnMoveLeftHold);

            Button btnRight = GetOrCreateOrMoveButton(panelMovement.transform, canvasObj, "Btn_Right", "►", defaultFont, new Vector2(70f, 70f), new Color(0.22f, 0.26f, 0.32f, 0.95f), "Move_Right");
            HoldButtonHandler holdRight = GetOrAdd<HoldButtonHandler>(btnRight.gameObject);
            if (player != null) SafeAddHoldListener(holdRight, player.OnMoveRightHold);

            Button btnDown = GetOrCreateOrMoveButton(panelMovement.transform, canvasObj, "Btn_Down", "-", defaultFont, new Vector2(70f, 70f), new Color(0.22f, 0.26f, 0.32f, 0.95f), "Angle_Down", "Btn_AngleDown");
            HoldButtonHandler holdDown = GetOrAdd<HoldButtonHandler>(btnDown.gameObject);
            if (uiManager != null) SafeAddHoldListener(holdDown, uiManager.OnAngleDownHold);

            Button btnUp = GetOrCreateOrMoveButton(panelMovement.transform, canvasObj, "Btn_Up", "+", defaultFont, new Vector2(70f, 70f), new Color(0.22f, 0.26f, 0.32f, 0.95f), "Angle_Up", "Btn_AngleUp");
            HoldButtonHandler holdUp = GetOrAdd<HoldButtonHandler>(btnUp.gameObject);
            if (uiManager != null) SafeAddHoldListener(holdUp, uiManager.OnAngleUpHold);

            Debug.Log("[CanvasRestructureSetup] Panel_Movement configured at Bottom-Left with Btn_Left, Btn_Right, Btn_Down, Btn_Up (70x70).");
        }

        private static void SetupPanelCombat(Transform parentTransform, Font defaultFont, GameObject canvasObj)
        {
            // Panel_Combat (Bottom-Right)
            GameObject panelCombat = GetOrCreateContainer(parentTransform, "Panel_Combat", new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(280f, 160f));
            RectTransform pcRt = panelCombat.GetComponent<RectTransform>();
            pcRt.pivot = new Vector2(1f, 0f);

            Image pcImg = GetOrAdd<Image>(panelCombat);
            pcImg.color = new Color(0f, 0f, 0f, 0f); // Transparent
            pcImg.raycastTarget = false;

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(panelCombat);
            vlg.spacing = 10f;
            vlg.childAlignment = TextAnchor.LowerRight;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            // Fila Superior: Items_Row (Btn_Dual, Btn_Teleport, Btn_Heal 60x60)
            GameObject itemsRow = GetOrCreateContainer(panelCombat.transform, "Items_Row", new Vector2(1f, 0f), Vector2.zero, new Vector2(210f, 65f));
            Image itemsImg = GetOrAdd<Image>(itemsRow);
            itemsImg.color = new Color(0f, 0f, 0f, 0f);
            itemsImg.raycastTarget = false;

            HorizontalLayoutGroup hlgItems = GetOrAdd<HorizontalLayoutGroup>(itemsRow);
            hlgItems.spacing = 10f;
            hlgItems.childAlignment = TextAnchor.MiddleRight;
            hlgItems.childControlWidth = false;
            hlgItems.childControlHeight = false;
            hlgItems.childForceExpandWidth = false;
            hlgItems.childForceExpandHeight = false;

            Button btnDual = GetOrCreateOrMoveButton(itemsRow.transform, canvasObj, "Btn_Dual", "DUAL", defaultFont, new Vector2(60f, 60f), new Color(0.85f, 0.65f, 0.1f, 0.95f), "Btn_ItemDual");
            Button btnTeleport = GetOrCreateOrMoveButton(itemsRow.transform, canvasObj, "Btn_Teleport", "TELEPORT", defaultFont, new Vector2(60f, 60f), new Color(0.2f, 0.65f, 0.85f, 0.95f), "Btn_ItemTeleport");
            Button btnHeal = GetOrCreateOrMoveButton(itemsRow.transform, canvasObj, "Btn_Heal", "HEAL", defaultFont, new Vector2(60f, 60f), new Color(0.2f, 0.85f, 0.4f, 0.95f), "Btn_ItemHeal");

            // Fila Inferior: Ammo_Row (Btn_T1, Btn_T2, Btn_SS 80x80)
            GameObject ammoRow = GetOrCreateContainer(panelCombat.transform, "Ammo_Row", new Vector2(1f, 0f), Vector2.zero, new Vector2(270f, 85f));
            Image ammoImg = GetOrAdd<Image>(ammoRow);
            ammoImg.color = new Color(0f, 0f, 0f, 0f);
            ammoImg.raycastTarget = false;

            HorizontalLayoutGroup hlgAmmo = GetOrAdd<HorizontalLayoutGroup>(ammoRow);
            hlgAmmo.spacing = 10f;
            hlgAmmo.childAlignment = TextAnchor.MiddleRight;
            hlgAmmo.childControlWidth = false;
            hlgAmmo.childControlHeight = false;
            hlgAmmo.childForceExpandWidth = false;
            hlgAmmo.childForceExpandHeight = false;

            Button btnT1 = GetOrCreateOrMoveButton(ammoRow.transform, canvasObj, "Btn_T1", "T1", defaultFont, new Vector2(80f, 80f), new Color(0.2f, 0.3f, 0.45f, 0.95f), "Btn_Shot1");
            Button btnT2 = GetOrCreateOrMoveButton(ammoRow.transform, canvasObj, "Btn_T2", "T2", defaultFont, new Vector2(80f, 80f), new Color(0.2f, 0.3f, 0.45f, 0.95f), "Btn_Shot2");
            Button btnSS = GetOrCreateOrMoveButton(ammoRow.transform, canvasObj, "Btn_SS", "SS", defaultFont, new Vector2(80f, 80f), new Color(0.85f, 0.25f, 0.25f, 0.95f));

            Debug.Log("[CanvasRestructureSetup] Panel_Combat configured at Bottom-Right (Items_Row 60x60 & Ammo_Row 80x80).");
        }

        private static void SetupPowerBarAndTopHeader(Transform parentTransform, GameObject canvasObj)
        {
            // 1. PowerBar at Bottom-Center (500x30)
            Transform powerTrans = parentTransform.Find("PowerBar") ?? parentTransform.Find("PowerSlider") ?? canvasObj.transform.Find("PowerBar") ?? canvasObj.transform.Find("PowerSlider");
            GameObject powerObj;
            if (powerTrans != null)
            {
                powerObj = powerTrans.gameObject;
                powerObj.transform.SetParent(parentTransform, false);
                powerObj.name = "PowerBar";
            }
            else
            {
                powerObj = new GameObject("PowerBar");
                powerObj.transform.SetParent(parentTransform, false);
                Slider powerSlider = powerObj.AddComponent<Slider>();
                powerSlider.minValue = 0f;
                powerSlider.maxValue = 100f;

                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(powerObj.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                bgImg.raycastTarget = false;

                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(powerObj.transform, false);
                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = new Color(0.9f, 0.7f, 0.1f, 1f);
                fillImg.raycastTarget = false;
                powerSlider.fillRect = fill.GetComponent<RectTransform>();
            }

            RectTransform powerRt = powerObj.GetComponent<RectTransform>();
            powerRt.anchorMin = new Vector2(0.5f, 0f);
            powerRt.anchorMax = new Vector2(0.5f, 0f);
            powerRt.pivot = new Vector2(0.5f, 0f);
            powerRt.anchoredPosition = new Vector2(0f, 25f);
            powerRt.sizeDelta = new Vector2(500f, 30f);

            // Ensure PowerBar container background doesn't block touch drag
            Image powerBg = powerObj.GetComponent<Image>();
            if (powerBg != null) powerBg.raycastTarget = false;

            // 2. Global Indicators (Top-Center)
            GameObject topPanel = GetOrCreateContainer(parentTransform, "Panel_TopHeader", new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(550f, 50f));
            RectTransform topRt = topPanel.GetComponent<RectTransform>();
            topRt.pivot = new Vector2(0.5f, 1f);

            Image topImg = GetOrAdd<Image>(topPanel);
            topImg.color = new Color(0f, 0f, 0f, 0f);
            topImg.raycastTarget = false;

            HorizontalLayoutGroup topHlg = GetOrAdd<HorizontalLayoutGroup>(topPanel);
            topHlg.spacing = 20f;
            topHlg.childAlignment = TextAnchor.MiddleCenter;
            topHlg.childControlWidth = false;
            topHlg.childControlHeight = false;
            topHlg.childForceExpandWidth = false;
            topHlg.childForceExpandHeight = false;

            MoveChildToParent(parentTransform, canvasObj, "WindText", topPanel.transform);
            MoveChildToParent(parentTransform, canvasObj, "TimerText", topPanel.transform);
            MoveChildToParent(parentTransform, canvasObj, "TurnText", topPanel.transform);
            MoveChildToParent(parentTransform, canvasObj, "SatelliteText", topPanel.transform);
            MoveChildToParent(parentTransform, canvasObj, "StatusText", topPanel.transform);

            Debug.Log("[CanvasRestructureSetup] PowerBar (500x30) anchored at Bottom-Center and Panel_TopHeader at Top-Center.");
        }

        private static void AuditRaycastTargetsAndLinkUIManager(GameObject canvasObj)
        {
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>();

            Button btnT1 = FindButtonInCanvas(canvasObj, "Btn_T1");
            Button btnT2 = FindButtonInCanvas(canvasObj, "Btn_T2");
            Button btnSS = FindButtonInCanvas(canvasObj, "Btn_SS");
            Button btnDual = FindButtonInCanvas(canvasObj, "Btn_Dual");
            Button btnTeleport = FindButtonInCanvas(canvasObj, "Btn_Teleport");
            Button btnHeal = FindButtonInCanvas(canvasObj, "Btn_Heal");

            SerializedObject serializedUI = new SerializedObject(uiManager);
            SetReference(serializedUI, "_btnShot1", btnT1);
            SetReference(serializedUI, "_btnShot2", btnT2);
            SetReference(serializedUI, "_btnSS", btnSS);
            SetReference(serializedUI, "_btnDual", btnDual);
            SetReference(serializedUI, "_btnTeleport", btnTeleport);
            SetReference(serializedUI, "_btnHeal", btnHeal);

            if (btnT1 != null) SetReference(serializedUI, "_btnShot1Bg", btnT1.GetComponent<Image>());
            if (btnT2 != null) SetReference(serializedUI, "_btnShot2Bg", btnT2.GetComponent<Image>());
            if (btnSS != null) SetReference(serializedUI, "_btnSSBg", btnSS.GetComponent<Image>());
            if (btnDual != null) SetReference(serializedUI, "_btnDualBg", btnDual.GetComponent<Image>());
            if (btnTeleport != null) SetReference(serializedUI, "_btnTeleportBg", btnTeleport.GetComponent<Image>());
            if (btnHeal != null) SetReference(serializedUI, "_btnHealBg", btnHeal.GetComponent<Image>());

            Transform powerBarTrans = canvasObj.transform.Find("HUDPanel/PowerBar") ?? canvasObj.transform.Find("PowerBar");
            if (powerBarTrans != null && powerBarTrans.GetComponent<Slider>() != null)
            {
                SetReference(serializedUI, "_powerSlider", powerBarTrans.GetComponent<Slider>());
            }

            Transform windText = canvasObj.transform.Find("HUDPanel/Panel_TopHeader/WindText") ?? canvasObj.transform.Find("Panel_TopHeader/WindText");
            if (windText != null) SetReference(serializedUI, "_windText", windText.GetComponent<Text>());

            Transform timerText = canvasObj.transform.Find("HUDPanel/Panel_TopHeader/TimerText") ?? canvasObj.transform.Find("Panel_TopHeader/TimerText");
            if (timerText != null) SetReference(serializedUI, "_timerText", timerText.GetComponent<Text>());

            Transform turnText = canvasObj.transform.Find("HUDPanel/Panel_TopHeader/TurnText") ?? canvasObj.transform.Find("Panel_TopHeader/TurnText");
            if (turnText != null) SetReference(serializedUI, "_turnText", turnText.GetComponent<Text>());

            Transform satelliteText = canvasObj.transform.Find("HUDPanel/Panel_TopHeader/SatelliteText") ?? canvasObj.transform.Find("Panel_TopHeader/SatelliteText");
            if (satelliteText != null) SetReference(serializedUI, "_satelliteText", satelliteText.GetComponent<Text>());

            serializedUI.ApplyModifiedProperties();

            // Strict Raycast Target Audit
            // Containers and non-interactive graphics MUST have raycastTarget = false to allow Drag-to-Power anywhere on screen.
            Graphic[] allGraphics = canvasObj.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in allGraphics)
            {
                bool isButtonGraphic = g.GetComponentInParent<Button>() != null;
                bool isInputFieldGraphic = g.GetComponentInParent<InputField>() != null;
                bool isTouchArea = g.gameObject.name == "TouchShootArea" || g.GetComponentInParent<TouchShootArea>() != null;
                bool isInteractive = isButtonGraphic || isInputFieldGraphic || isTouchArea;

                // Disable raycastTarget on container background images/texts unless explicitly interactive
                if (!isInteractive)
                {
                    g.raycastTarget = false;
                }
            }

            Debug.Log("[CanvasRestructureSetup] UIManager references bound and all container raycast targets un-checked.");
        }

        private static Button GetOrCreateOrMoveButton(Transform targetParent, GameObject canvasObj, string targetName, string label, Font defaultFont, Vector2 size, Color bgColor, params string[] altNames)
        {
            Transform existing = targetParent.Find(targetName);

            if (existing == null)
            {
                foreach (string alt in altNames)
                {
                    existing = canvasObj.transform.Find(alt) ?? targetParent.Find(alt);
                    if (existing != null) break;
                }
            }

            GameObject btnObj;
            if (existing != null)
            {
                btnObj = existing.gameObject;
                btnObj.name = targetName;
                btnObj.transform.SetParent(targetParent, false);
            }
            else
            {
                btnObj = new GameObject(targetName);
                btnObj.transform.SetParent(targetParent, false);
                Image img = btnObj.AddComponent<Image>();
                img.color = bgColor;
                img.raycastTarget = true;
                Button btn = btnObj.AddComponent<Button>();

                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
                RectTransform textRt = textObj.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.sizeDelta = Vector2.zero;
                Text txt = textObj.AddComponent<Text>();
                txt.text = label;
                txt.font = defaultFont;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.fontSize = 16;
                txt.fontStyle = FontStyle.Bold;
                txt.raycastTarget = false;
            }

            RectTransform rt = GetOrAdd<RectTransform>(btnObj);
            rt.sizeDelta = size;

            Text t = btnObj.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = label;
                t.font = defaultFont;
                t.raycastTarget = false;
            }

            Image bg = btnObj.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = bgColor;
                bg.raycastTarget = true;
            }

            return GetOrAdd<Button>(btnObj);
        }

        private static GameObject GetOrCreateContainer(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            Transform t = parent.Find(name);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }

            RectTransform rt = GetOrAdd<RectTransform>(go);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static void MoveChildToParent(Transform search1, GameObject search2, string childName, Transform newParent)
        {
            Transform t = search1.Find(childName) ?? search2.transform.Find(childName);
            if (t != null)
            {
                t.SetParent(newParent, false);
            }
        }

        private static Button FindButtonInCanvas(GameObject canvasObj, string buttonName)
        {
            Button[] buttons = canvasObj.GetComponentsInChildren<Button>(true);
            foreach (Button b in buttons)
            {
                if (b.gameObject.name == buttonName) return b;
            }
            return null;
        }

        private static void SetReference(SerializedObject so, string propName, Object value)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void SafeAddHoldListener(HoldButtonHandler hold, UnityEngine.Events.UnityAction action)
        {
            if (hold == null || action == null) return;
            var onHeldProp = hold.OnHeld;
            if (onHeldProp == null)
            {
                SerializedObject so = new SerializedObject(hold);
                SerializedProperty prop = so.FindProperty("_onHeld");
                if (prop != null)
                {
                    so.Update();
                }
                onHeldProp = hold.OnHeld;
            }
            if (onHeldProp != null)
            {
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(onHeldProp, action);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(onHeldProp, action);
            }
        }
    }
}
#endif
