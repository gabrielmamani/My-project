using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Gunbound.Player;
using Gunbound.Network;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 16 Setup Utility:
    /// Creates MobileData ScriptableObject assets (Mage, Armor, Boomer), wires Mobile Selection Garage UI in Canvas Lobby,
    /// and configures NetworkSpawnerManager and NetworkLobbyUI references (RF-5.1.1, RF-5.1.2, RF-5.1.3).
    /// </summary>
    public static class Phase16MobileSelectionSetup
    {
        [MenuItem("Gunbound/Setup Phase 16 (Mobile Selection & Garage)")]
        public static void SetupPhase16()
        {
            Debug.Log("[Phase16Setup] Starting Mobile Selection & Garage setup...");

            // 1. Ensure Data Directory
            string folderPath = "Assets/_Project/Data/Mobiles";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // 2. Create or load MobileData assets
            MobileData mageData = CreateOrLoadMobileAsset(folderPath + "/Mobile_Mage.asset", MobileType.Mage, "Mage Mobile", "Móvil de energía equilibrado con buen ángulo de disparo.", 1000, 0.05f, 20f, 70f, 4.0f, 250);
            MobileData armorData = CreateOrLoadMobileAsset(folderPath + "/Mobile_Armor.asset", MobileType.Armor, "Armor Tank", "Móvil pesado defensivo de alta resistencia y metralla.", 1100, 0.15f, 15f, 60f, 3.2f, 270);
            MobileData boomerData = CreateOrLoadMobileAsset(folderPath + "/Mobile_Boomer.asset", MobileType.Boomer, "Boomer Mobile", "Móvil liviano de madera sensible a trayectorias por viento.", 900, 0.00f, 10f, 75f, 4.5f, 230);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 3. Assign assets to NetworkSpawnerManager
            var spawner = Object.FindFirstObjectByType<NetworkSpawnerManager>();
            if (spawner != null)
            {
                var serializedSpawner = new SerializedObject(spawner);
                serializedSpawner.FindProperty("_mageData").objectReferenceValue = mageData;
                serializedSpawner.FindProperty("_armorData").objectReferenceValue = armorData;
                serializedSpawner.FindProperty("_boomerData").objectReferenceValue = boomerData;
                serializedSpawner.ApplyModifiedProperties();
                Debug.Log("[Phase16Setup] MobileData assets assigned to NetworkSpawnerManager.");
            }

            // 4. Construct Mobile Garage UI Panel inside Canvas -> Panel_NetworkLobby
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[Phase16Setup] Canvas not found in scene!");
                return;
            }

            Transform lobbyPanel = canvasObj.transform.Find("Panel_NetworkLobby");
            if (lobbyPanel == null)
            {
                Debug.LogError("[Phase16Setup] Panel_NetworkLobby not found in Canvas!");
                return;
            }

            Transform garagePanel = lobbyPanel.Find("Panel_MobileGarage");
            if (garagePanel == null)
            {
                GameObject garageObj = new GameObject("Panel_MobileGarage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                garageObj.transform.SetParent(lobbyPanel, false);
                garagePanel = garageObj.transform;

                RectTransform garageRect = garageObj.GetComponent<RectTransform>();
                garageRect.anchorMin = new Vector2(0.5f, 0.5f);
                garageRect.anchorMax = new Vector2(0.5f, 0.5f);
                garageRect.pivot = new Vector2(0.5f, 0.5f);
                garageRect.anchoredPosition = new Vector2(0f, 20f);
                garageRect.sizeDelta = new Vector2(460f, 130f);

                Image bgImg = garageObj.GetComponent<Image>();
                bgImg.color = new Color(0.08f, 0.12f, 0.2f, 0.85f);
                bgImg.raycastTarget = false;
            }

            // Title
            Transform titleObj = garagePanel.Find("Txt_GarageTitle");
            Text titleTxt;
            if (titleObj == null)
            {
                GameObject tObj = new GameObject("Txt_GarageTitle", typeof(RectTransform), typeof(Text));
                tObj.transform.SetParent(garagePanel, false);
                titleObj = tObj.transform;

                RectTransform tRect = tObj.GetComponent<RectTransform>();
                tRect.anchorMin = new Vector2(0.5f, 1.0f);
                tRect.anchorMax = new Vector2(0.5f, 1.0f);
                tRect.pivot = new Vector2(0.5f, 1.0f);
                tRect.anchoredPosition = new Vector2(0f, -5f);
                tRect.sizeDelta = new Vector2(440f, 24f);

                titleTxt = tObj.GetComponent<Text>();
                titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                titleTxt.fontSize = 14;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.alignment = TextAnchor.MiddleCenter;
                titleTxt.color = new Color(1.0f, 0.9f, 0.4f);
                titleTxt.text = "SELECCIÓN DE MÓVIL (GARAJE)";
                titleTxt.raycastTarget = false;
            }

            // Button Bar Container
            Transform buttonContainer = garagePanel.Find("Container_MobileButtons");
            if (buttonContainer == null)
            {
                GameObject bObj = new GameObject("Container_MobileButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                bObj.transform.SetParent(garagePanel, false);
                buttonContainer = bObj.transform;

                RectTransform bRect = bObj.GetComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.5f, 0.5f);
                bRect.anchorMax = new Vector2(0.5f, 0.5f);
                bRect.pivot = new Vector2(0.5f, 0.5f);
                bRect.anchoredPosition = new Vector2(0f, 10f);
                bRect.sizeDelta = new Vector2(440f, 40f);

                HorizontalLayoutGroup hlg = bObj.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 15f;
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            Button btnMage = CreateOrFindGarageButton(buttonContainer, "Btn_SelectMage", "Mage", new Color(0.2f, 0.6f, 0.9f));
            Button btnArmor = CreateOrFindGarageButton(buttonContainer, "Btn_SelectArmor", "Armor", new Color(0.8f, 0.3f, 0.2f));
            Button btnBoomer = CreateOrFindGarageButton(buttonContainer, "Btn_SelectBoomer", "Boomer", new Color(0.3f, 0.8f, 0.4f));

            // Stats Preview Text
            Transform statsObj = garagePanel.Find("Txt_MobileStats");
            Text statsTxt;
            if (statsObj == null)
            {
                GameObject sObj = new GameObject("Txt_MobileStats", typeof(RectTransform), typeof(Text));
                sObj.transform.SetParent(garagePanel, false);
                statsObj = sObj.transform;

                RectTransform sRect = sObj.GetComponent<RectTransform>();
                sRect.anchorMin = new Vector2(0.5f, 0.0f);
                sRect.anchorMax = new Vector2(0.5f, 0.0f);
                sRect.pivot = new Vector2(0.5f, 0.0f);
                sRect.anchoredPosition = new Vector2(0f, 5f);
                sRect.sizeDelta = new Vector2(440f, 40f);

                statsTxt = sObj.GetComponent<Text>();
                statsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                statsTxt.fontSize = 12;
                statsTxt.alignment = TextAnchor.MiddleCenter;
                statsTxt.color = Color.white;
                statsTxt.supportRichText = true;
                statsTxt.text = "<b>Mage Mobile</b> | HP: 1000 | Armadura: 5%\nÁngulo: 20°–70° | Vel: 4.0 | Delay: 250";
                statsTxt.raycastTarget = false;
            }
            else
            {
                statsTxt = statsObj.GetComponent<Text>();
            }

            // Bind to NetworkLobbyUI
            var lobbyUI = lobbyPanel.GetComponent<NetworkLobbyUI>();
            if (lobbyUI != null)
            {
                var serializedUI = new SerializedObject(lobbyUI);
                serializedUI.FindProperty("_btnSelectMage").objectReferenceValue = btnMage;
                serializedUI.FindProperty("_btnSelectArmor").objectReferenceValue = btnArmor;
                serializedUI.FindProperty("_btnSelectBoomer").objectReferenceValue = btnBoomer;
                serializedUI.FindProperty("_mobileStatsText").objectReferenceValue = statsTxt;
                serializedUI.ApplyModifiedProperties();

                lobbyUI.BindMobileButtons(btnMage, btnArmor, btnBoomer, statsTxt);
                Debug.Log("[Phase16Setup] NetworkLobbyUI bound with Mobile Garage buttons successfully.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Phase16Setup] Phase 16 Mobile Selection Setup completed successfully!");
        }

        private static MobileData CreateOrLoadMobileAsset(string assetPath, MobileType type, string name, string desc, int hp, float armor, float minAngle, float maxAngle, float speed, int delay)
        {
            MobileData asset = AssetDatabase.LoadAssetAtPath<MobileData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MobileData>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("_mobileType").enumValueIndex = (int)type;
            so.FindProperty("_mobileName").stringValue = name;
            so.FindProperty("_description").stringValue = desc;
            so.FindProperty("_maxHealth").intValue = hp;
            so.FindProperty("_armorDefense").floatValue = armor;
            so.FindProperty("_minAngle").floatValue = minAngle;
            so.FindProperty("_maxAngle").floatValue = maxAngle;
            so.FindProperty("_moveSpeed").floatValue = speed;
            so.FindProperty("_baseDelay").intValue = delay;
            so.ApplyModifiedProperties();

            return asset;
        }

        private static Button CreateOrFindGarageButton(Transform parent, string objectName, string label, Color color)
        {
            Transform bObj = parent.Find(objectName);
            if (bObj == null)
            {
                GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                bObj = go.transform;

                RectTransform rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(120f, 35f);

                Image img = go.GetComponent<Image>();
                img.color = color;

                Button btn = go.GetComponent<Button>();
                ColorBlock colors = btn.colors;
                colors.normalColor = color;
                colors.highlightedColor = color * 1.2f;
                colors.pressedColor = color * 0.8f;
                btn.colors = colors;

                GameObject tGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                tGo.transform.SetParent(go.transform, false);
                RectTransform tRect = tGo.GetComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.sizeDelta = Vector2.zero;

                Text txt = tGo.GetComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 13;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.text = label;
                txt.raycastTarget = false;

                return btn;
            }
            return bObj.GetComponent<Button>();
        }
    }
}
