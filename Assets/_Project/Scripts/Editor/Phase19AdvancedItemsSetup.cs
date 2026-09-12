using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Managers;
using Gunbound.UI;
using Gunbound.Core;

namespace Gunbound.Editor
{
    /// <summary>
    /// Editor setup tool to generate and bind Advanced Tactical Items UI (RF-5.3.1, RF-5.3.2)
    /// into the main Canvas, including ChangeWind, Shield, and Dual+ buttons.
    /// </summary>
    public class Phase19AdvancedItemsSetup
    {
        [MenuItem("Gunbound/Setup Phase 19 (Advanced Tactical Items & Inventory)")]
        public static void SetupPhase19AdvancedItems()
        {
            Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[Phase19Setup] No Canvas found in scene! Please open a scene with Canvas.");
                return;
            }

            // Find or create Panel_ItemRow in HUD
            Transform itemRowTrans = canvas.transform.Find("HUD_InGame/Panel_ItemRow");
            if (itemRowTrans == null)
            {
                itemRowTrans = canvas.transform.Find("Panel_ItemRow");
            }

            if (itemRowTrans == null)
            {
                GameObject itemRowObj = new GameObject("Panel_ItemRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                itemRowObj.transform.SetParent(canvas.transform, false);
                itemRowTrans = itemRowObj.transform;

                RectTransform rect = itemRowObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(-20f, 100f);
                rect.sizeDelta = new Vector2(400f, 60f);

                HorizontalLayoutGroup hlg = itemRowObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 8;
                hlg.childAlignment = TextAnchor.MiddleRight;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
            }

            // Create buttons for items if missing: Btn_ChangeWind, Btn_Shield, Btn_DualPlus
            Button btnDual = EnsureItemButton(itemRowTrans, "Btn_Dual", "DUAL", new Color(0.85f, 0.65f, 0.1f, 1.0f));
            Button btnTeleport = EnsureItemButton(itemRowTrans, "Btn_Teleport", "TELEPORT", new Color(0.2f, 0.65f, 0.85f, 1.0f));
            Button btnHeal = EnsureItemButton(itemRowTrans, "Btn_Heal", "HEAL", new Color(0.2f, 0.85f, 0.4f, 1.0f));
            Button btnChangeWind = EnsureItemButton(itemRowTrans, "Btn_ChangeWind", "WIND", new Color(0.7f, 0.4f, 0.9f, 1.0f));
            Button btnShield = EnsureItemButton(itemRowTrans, "Btn_Shield", "SHIELD", new Color(0.2f, 0.8f, 1.0f, 1.0f));
            Button btnDualPlus = EnsureItemButton(itemRowTrans, "Btn_DualPlus", "DUAL+", new Color(1.0f, 0.3f, 0.2f, 1.0f));

            // Ensure ItemManager exists in scene
            ItemManager itemMgr = UnityEngine.Object.FindAnyObjectByType<ItemManager>();
            if (itemMgr == null)
            {
                GameObject mgrObj = new GameObject("[ItemManager]", typeof(ItemManager));
                Undo.RegisterCreatedObjectUndo(mgrObj, "Create ItemManager");
            }

            // Bind to UIManager if present
            UIManager uiMgr = UnityEngine.Object.FindAnyObjectByType<UIManager>();
            if (uiMgr != null)
            {
                SerializedObject serializedUiMgr = new SerializedObject(uiMgr);
                SetSerializedProperty(serializedUiMgr, "_btnDual", btnDual);
                SetSerializedProperty(serializedUiMgr, "_btnTeleport", btnTeleport);
                SetSerializedProperty(serializedUiMgr, "_btnHeal", btnHeal);
                SetSerializedProperty(serializedUiMgr, "_btnChangeWind", btnChangeWind);
                SetSerializedProperty(serializedUiMgr, "_btnShield", btnShield);
                SetSerializedProperty(serializedUiMgr, "_btnDualPlus", btnDualPlus);

                SetSerializedProperty(serializedUiMgr, "_btnDualBg", btnDual != null ? btnDual.GetComponent<Image>() : null);
                SetSerializedProperty(serializedUiMgr, "_btnTeleportBg", btnTeleport != null ? btnTeleport.GetComponent<Image>() : null);
                SetSerializedProperty(serializedUiMgr, "_btnHealBg", btnHeal != null ? btnHeal.GetComponent<Image>() : null);
                SetSerializedProperty(serializedUiMgr, "_btnChangeWindBg", btnChangeWind != null ? btnChangeWind.GetComponent<Image>() : null);
                SetSerializedProperty(serializedUiMgr, "_btnShieldBg", btnShield != null ? btnShield.GetComponent<Image>() : null);
                SetSerializedProperty(serializedUiMgr, "_btnDualPlusBg", btnDualPlus != null ? btnDualPlus.GetComponent<Image>() : null);

                serializedUiMgr.ApplyModifiedProperties();
                uiMgr.SetupItemButtons();
                Debug.Log("[Phase19Setup] All 6 Tactical Item buttons bound to UIManager successfully!");
            }

            EditorUtility.SetDirty(canvas);
            Debug.Log("[Phase19Setup] Phase 19 Advanced Items & Inventory setup completed!");
        }

        private static Button EnsureItemButton(Transform parent, string buttonName, string labelText, Color color)
        {
            Transform existing = parent.Find(buttonName);
            GameObject btnObj;

            if (existing != null)
            {
                btnObj = existing.gameObject;
            }
            else
            {
                btnObj = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(parent, false);

                RectTransform rect = btnObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(58f, 58f);

                Image img = btnObj.GetComponent<Image>();
                img.color = color;

                GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                txtObj.transform.SetParent(btnObj.transform, false);

                RectTransform txtRect = txtObj.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                Text txt = txtObj.GetComponent<Text>();
                txt.text = labelText;
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
                txt.fontSize = 10;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
            }

            return btnObj.GetComponent<Button>();
        }

        private static void SetSerializedProperty(SerializedObject serializedObj, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty prop = serializedObj.FindProperty(propertyName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }
    }
}
#endif
