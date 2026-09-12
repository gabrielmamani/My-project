#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Managers;
using Gunbound.UI;
using Gunbound.Player;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 6 HUD Fix & Gesture Controls Setup:
    /// - Removes generic Btn_Fire button.
    /// - Implements transparent TouchShootArea in bottom-right quadrant.
    /// - Configures bottom-left dedicated movement (Move_Left, Move_Right) and angle (Angle_Up, Angle_Down) buttons with HoldButtonHandler.
    /// - Creates AmmoRow (Btn_T1, Btn_T2, Btn_SS at 70x70) and ItemRow (Btn_Dual, Btn_Teleport, Btn_Heal at 60x60) with HorizontalLayoutGroup.
    /// - Performs Raycast Target audit across Canvas graphics.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase6HUDFixSetup
    {
        static Phase6HUDFixSetup()
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

        [MenuItem("Gunbound/Setup Phase 6 (HUD Input & Gesture Fix)")]
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
                Debug.LogError("[Phase6HUDFixSetup] Could not open SampleScene.unity");
                return;
            }

            SetupCanvasControls();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Phase6HUDFixSetup] Successfully completed HUD Input & Gesture Fix Setup!");
        }

        private static void SetupCanvasControls()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Phase6HUDFixSetup] Canvas not found in active scene.");
                return;
            }

            Transform hudPanel = canvasObj.transform.Find("HUDPanel");
            Transform parentTransform = hudPanel != null ? hudPanel : canvasObj.transform;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // 1. Remove generic Btn_Fire
            Transform btnFire = parentTransform.Find("Btn_Fire");
            if (btnFire == null) btnFire = canvasObj.transform.Find("Btn_Fire");
            if (btnFire != null)
            {
                Object.DestroyImmediate(btnFire.gameObject);
                Debug.Log("[Phase6HUDFixSetup] Destroyed Btn_Fire.");
            }

            // 2. Setup TouchShootArea transparent gesture zone
            Transform touchTrans = parentTransform.Find("TouchShootArea");
            GameObject touchObj;
            if (touchTrans != null)
            {
                touchObj = touchTrans.gameObject;
            }
            else
            {
                touchObj = new GameObject("TouchShootArea");
                touchObj.transform.SetParent(parentTransform, false);
            }

            RectTransform touchRt = touchObj.GetComponent<RectTransform>();
            if (touchRt == null) touchRt = touchObj.AddComponent<RectTransform>();
            touchRt.anchorMin = new Vector2(0.5f, 0.0f);
            touchRt.anchorMax = new Vector2(1.0f, 0.5f);
            touchRt.offsetMin = Vector2.zero;
            touchRt.offsetMax = Vector2.zero;

            Image touchImg = touchObj.GetComponent<Image>();
            if (touchImg == null) touchImg = touchObj.AddComponent<Image>();
            touchImg.color = new Color(0f, 0f, 0f, 0f);
            touchImg.raycastTarget = true;

            if (touchObj.GetComponent<TouchShootArea>() == null)
            {
                touchObj.AddComponent<TouchShootArea>();
            }
            touchObj.transform.SetAsFirstSibling();

            // 3. Setup Dedicated Bottom-Left Controls
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>();

            ConfigureControlBtn(parentTransform, "Move_Left", "◄", new Vector2(20f, 20f), new Vector2(65f, 65f), defaultFont, (btn) => {
                HoldButtonHandler hold = GetOrAdd<HoldButtonHandler>(btn);
                if (player != null)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(hold.OnHeld, player.OnMoveLeftHold);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(hold.OnHeld, player.OnMoveLeftHold);
                }
            });

            ConfigureControlBtn(parentTransform, "Move_Right", "►", new Vector2(95f, 20f), new Vector2(65f, 65f), defaultFont, (btn) => {
                HoldButtonHandler hold = GetOrAdd<HoldButtonHandler>(btn);
                if (player != null)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(hold.OnHeld, player.OnMoveRightHold);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(hold.OnHeld, player.OnMoveRightHold);
                }
            });

            ConfigureControlBtn(parentTransform, "Angle_Down", "▼", new Vector2(20f, 95f), new Vector2(65f, 65f), defaultFont, (btn) => {
                HoldButtonHandler hold = GetOrAdd<HoldButtonHandler>(btn);
                if (uiManager != null)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(hold.OnHeld, uiManager.OnAngleDownHold);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(hold.OnHeld, uiManager.OnAngleDownHold);
                }
            });

            ConfigureControlBtn(parentTransform, "Angle_Up", "▲", new Vector2(95f, 95f), new Vector2(65f, 65f), defaultFont, (btn) => {
                HoldButtonHandler hold = GetOrAdd<HoldButtonHandler>(btn);
                if (uiManager != null)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(hold.OnHeld, uiManager.OnAngleUpHold);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(hold.OnHeld, uiManager.OnAngleUpHold);
                }
            });

            // Clean up old control buttons & legacy panels
            string[] legacyNames = new string[] {
                "Btn_Left", "Btn_Right", "Btn_AngleUp", "Btn_AngleDown", "Btn_Shot1", "Btn_Shot2", "Btn_SS",
                "ItemPanel", "WeaponPanel", "ShotPanel", "SquareButton", "FloatingPanel", "ResidualButtons",
                "ArrowButtons", "Btn_Fire", "LegacyControls"
            };

            foreach (string legacyName in legacyNames)
            {
                CleanupChild(parentTransform, legacyName);
                CleanupChild(canvasObj.transform, legacyName);
            }

            // 4. Setup Ammo Row (Btn_T1, Btn_T2, Btn_SS) in HorizontalLayoutGroup (70x70)
            GameObject ammoRowObj = GetOrCreateContainer(parentTransform, "AmmoRow", new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(250f, 75f));
            HorizontalLayoutGroup ammoHlg = GetOrAdd<HorizontalLayoutGroup>(ammoRowObj);
            ammoHlg.spacing = 10f;
            ammoHlg.childControlWidth = false;
            ammoHlg.childControlHeight = false;
            ammoHlg.childForceExpandWidth = false;
            ammoHlg.childForceExpandHeight = false;
            ammoHlg.childAlignment = TextAnchor.MiddleRight;

            Button btnT1 = CreateStyledButton(ammoRowObj.transform, "Btn_T1", "T1", defaultFont, new Vector2(70f, 70f), new Color(0.2f, 0.25f, 0.35f, 0.9f));
            Button btnT2 = CreateStyledButton(ammoRowObj.transform, "Btn_T2", "T2", defaultFont, new Vector2(70f, 70f), new Color(0.2f, 0.25f, 0.35f, 0.9f));
            Button btnSS = CreateStyledButton(ammoRowObj.transform, "Btn_SS", "SS", defaultFont, new Vector2(70f, 70f), new Color(0.85f, 0.25f, 0.25f, 0.9f));

            // 5. Setup Item Row (Btn_Dual, Btn_Teleport, Btn_Heal) in HorizontalLayoutGroup (60x60)
            GameObject itemRowObj = GetOrCreateContainer(parentTransform, "ItemRow", new Vector2(1f, 0f), new Vector2(-20f, 100f), new Vector2(220f, 65f));
            HorizontalLayoutGroup itemHlg = GetOrAdd<HorizontalLayoutGroup>(itemRowObj);
            itemHlg.spacing = 10f;
            itemHlg.childControlWidth = false;
            itemHlg.childControlHeight = false;
            itemHlg.childForceExpandWidth = false;
            itemHlg.childForceExpandHeight = false;
            itemHlg.childAlignment = TextAnchor.MiddleRight;

            Button btnDual = CreateStyledButton(itemRowObj.transform, "Btn_Dual", "DUAL", defaultFont, new Vector2(60f, 60f), new Color(0.85f, 0.65f, 0.1f, 0.9f));
            Button btnTeleport = CreateStyledButton(itemRowObj.transform, "Btn_Teleport", "TELEPORT", defaultFont, new Vector2(60f, 60f), new Color(0.2f, 0.65f, 0.85f, 0.9f));
            Button btnHeal = CreateStyledButton(itemRowObj.transform, "Btn_Heal", "HEAL", defaultFont, new Vector2(60f, 60f), new Color(0.2f, 0.85f, 0.4f, 0.9f));

            // Destroy any floating buttons in top right quadrant
            Transform[] children = parentTransform.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child == null || child == parentTransform || child == ammoRowObj.transform || child == itemRowObj.transform) continue;
                if (child.parent == ammoRowObj.transform || child.parent == itemRowObj.transform) continue;

                if (child.name.StartsWith("Move_") || child.name.StartsWith("Angle_") || child.name == "TouchShootArea") continue;

                if (child.GetComponent<Button>() != null)
                {
                    Object.DestroyImmediate(child.gameObject);
                    Debug.Log($"[Phase6HUDFixSetup] Destroyed floating residual button: {child.name}");
                }
            }

            // 6. Connect UIManager Serialized References
            SerializedObject serializedUI = new SerializedObject(uiManager);
            var propShot1 = serializedUI.FindProperty("_btnShot1"); if (propShot1 != null) propShot1.objectReferenceValue = btnT1;
            var propShot2 = serializedUI.FindProperty("_btnShot2"); if (propShot2 != null) propShot2.objectReferenceValue = btnT2;
            var propSS = serializedUI.FindProperty("_btnSS"); if (propSS != null) propSS.objectReferenceValue = btnSS;
            var propDual = serializedUI.FindProperty("_btnDual"); if (propDual != null) propDual.objectReferenceValue = btnDual;
            var propTeleport = serializedUI.FindProperty("_btnTeleport"); if (propTeleport != null) propTeleport.objectReferenceValue = btnTeleport;
            var propHeal = serializedUI.FindProperty("_btnHeal"); if (propHeal != null) propHeal.objectReferenceValue = btnHeal;
            var propShot1Bg = serializedUI.FindProperty("_btnShot1Bg"); if (propShot1Bg != null) propShot1Bg.objectReferenceValue = btnT1 != null ? btnT1.GetComponent<Image>() : null;
            var propShot2Bg = serializedUI.FindProperty("_btnShot2Bg"); if (propShot2Bg != null) propShot2Bg.objectReferenceValue = btnT2 != null ? btnT2.GetComponent<Image>() : null;
            var propSSBg = serializedUI.FindProperty("_btnSSBg"); if (propSSBg != null) propSSBg.objectReferenceValue = btnSS != null ? btnSS.GetComponent<Image>() : null;
            var propDualBg = serializedUI.FindProperty("_btnDualBg"); if (propDualBg != null) propDualBg.objectReferenceValue = btnDual != null ? btnDual.GetComponent<Image>() : null;
            var propTeleportBg = serializedUI.FindProperty("_btnTeleportBg"); if (propTeleportBg != null) propTeleportBg.objectReferenceValue = btnTeleport != null ? btnTeleport.GetComponent<Image>() : null;
            var propHealBg = serializedUI.FindProperty("_btnHealBg"); if (propHealBg != null) propHealBg.objectReferenceValue = btnHeal != null ? btnHeal.GetComponent<Image>() : null;
            serializedUI.ApplyModifiedProperties();

            uiManager.SetupWeaponButtons();
            uiManager.SetupItemButtons();

            // 7. Raycast Target Audit
            Graphic[] graphics = canvasObj.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic g in graphics)
            {
                bool isInteractive = g.GetComponentInParent<Button>() != null ||
                                     g.GetComponentInParent<HoldButtonHandler>() != null ||
                                     g.GetComponentInParent<TouchShootArea>() != null ||
                                     g.gameObject.name == "TouchShootArea";

                g.raycastTarget = isInteractive;
            }

            Debug.Log("[Phase6HUDFixSetup] Raycast target audit & UIManager references successfully updated.");
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private static void ConfigureControlBtn(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size, Font font, System.Action<GameObject> customSetup)
        {
            Transform existing = parent.Find(name);
            GameObject btnObj;
            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);
            }

            Image img = GetOrAdd<Image>(btnObj);
            img.color = new Color(0.2f, 0.25f, 0.35f, 0.9f);
            img.raycastTarget = true;

            Button btn = GetOrAdd<Button>(btnObj);

            RectTransform rt = GetOrAdd<RectTransform>(btnObj);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Transform textTrans = btnObj.transform.Find("Text");
            GameObject textObj;
            if (textTrans != null)
            {
                textObj = textTrans.gameObject;
            }
            else
            {
                textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
            }

            Text txt = GetOrAdd<Text>(textObj);
            txt.font = font;
            txt.text = label;
            txt.fontSize = 20;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform txtRt = GetOrAdd<RectTransform>(textObj);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            customSetup?.Invoke(btnObj);
        }

        private static GameObject GetOrCreateContainer(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject container;
            if (existing != null)
            {
                container = existing.gameObject;
            }
            else
            {
                container = new GameObject(name, typeof(RectTransform));
                container.transform.SetParent(parent, false);
            }

            RectTransform rt = container.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return container;
        }

        private static Button CreateStyledButton(Transform parent, string name, string labelText, Font font, Vector2 size, Color bgColor)
        {
            Transform existing = parent.Find(name);
            GameObject btnObj;
            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject(name);
                btnObj.transform.SetParent(parent, false);
            }

            Image img = GetOrAdd<Image>(btnObj);
            img.color = bgColor;
            img.raycastTarget = true;

            Button btn = GetOrAdd<Button>(btnObj);

            RectTransform rt = GetOrAdd<RectTransform>(btnObj);
            rt.sizeDelta = size;

            Transform textTrans = btnObj.transform.Find("Text");
            GameObject textObj;
            if (textTrans != null)
            {
                textObj = textTrans.gameObject;
            }
            else
            {
                textObj = new GameObject("Text");
                textObj.transform.SetParent(btnObj.transform, false);
            }

            Text txt = GetOrAdd<Text>(textObj);
            txt.font = font;
            txt.text = labelText;
            txt.fontSize = 14;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;

            RectTransform txtRt = GetOrAdd<RectTransform>(textObj);
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            return btn;
        }

        private static void CleanupChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
#endif
