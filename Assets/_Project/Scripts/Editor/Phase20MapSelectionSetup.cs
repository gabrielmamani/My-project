using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Environment;
using Gunbound.UI;

namespace Gunbound.Editor
{
    /// <summary>
    /// Editor setup tool to generate MapData ScriptableObjects (Miramo Town, Metamine, Cozy Cave)
    /// and assemble Map Environment selection UI (RF-5.4.1, RF-5.4.2).
    /// </summary>
    public class Phase20MapSelectionSetup
    {
        private const string MAPS_FOLDER = "Assets/_Project/Assets/Maps";

        [MenuItem("Gunbound/Setup Phase 20 (Multiple Maps & Environment Selection)")]
        public static void SetupPhase20MapSelection()
        {
            EnsureDirectoryExists(MAPS_FOLDER);

            // 1. Create / Load MapData Assets
            MapData miramo = GetOrCreateMapData("Map_MiramoTown", "Miramo Town", MapType.MiramoTown, "Pradera verde clásica con clima templado.", new Color(0.4f, 0.75f, 1.0f), 1.0f, 1.0f, Color.green);
            MapData metamine = GetOrCreateMapData("Map_Metamine", "Metamine", MapType.Metamine, "Mina subterránea rocosa con gravedad aumentada.", new Color(0.85f, 0.5f, 0.2f), 0.8f, 1.15f, new Color(0.6f, 0.35f, 0.15f));
            MapData cozy = GetOrCreateMapData("Map_CozyCave", "Cozy Cave", MapType.CozyCave, "Caverna helada de cristales con viento fuerte.", new Color(0.15f, 0.2f, 0.45f), 1.4f, 0.95f, new Color(0.2f, 0.6f, 0.9f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Setup MapEnvironmentLoader in Scene
            MapEnvironmentLoader loader = UnityEngine.Object.FindAnyObjectByType<MapEnvironmentLoader>();
            if (loader == null)
            {
                GameObject loaderObj = new GameObject("[MapEnvironmentLoader]", typeof(MapEnvironmentLoader));
                loader = loaderObj.GetComponent<MapEnvironmentLoader>();
                Undo.RegisterCreatedObjectUndo(loaderObj, "Create MapEnvironmentLoader");
            }

            SerializedObject serializedLoader = new SerializedObject(loader);
            SerializedProperty mapsProp = serializedLoader.FindProperty("_availableMaps");
            if (mapsProp != null)
            {
                mapsProp.ClearArray();
                mapsProp.arraySize = 3;
                mapsProp.GetArrayElementAtIndex(0).objectReferenceValue = miramo;
                mapsProp.GetArrayElementAtIndex(1).objectReferenceValue = metamine;
                mapsProp.GetArrayElementAtIndex(2).objectReferenceValue = cozy;
            }
            serializedLoader.ApplyModifiedProperties();

            // 3. Setup RoomReadyUI Map Selector
            RoomReadyUI readyUI = UnityEngine.Object.FindAnyObjectByType<RoomReadyUI>();
            if (readyUI != null)
            {
                Canvas canvas = readyUI.GetComponentInParent<Canvas>();
                Transform panelReady = readyUI.transform;

                Transform mapPanelTrans = panelReady.Find("Panel_MapSelection");
                if (mapPanelTrans == null && canvas != null)
                {
                    GameObject mapPanelObj = new GameObject("Panel_MapSelection", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                    mapPanelObj.transform.SetParent(panelReady, false);
                    mapPanelTrans = mapPanelObj.transform;

                    RectTransform rect = mapPanelObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.25f);
                    rect.anchorMax = new Vector2(0.5f, 0.25f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 0f);
                    rect.sizeDelta = new Vector2(420f, 50f);

                    Image img = mapPanelObj.GetComponent<Image>();
                    img.color = new Color(0.1f, 0.12f, 0.18f, 0.85f);

                    HorizontalLayoutGroup hlg = mapPanelObj.GetComponent<HorizontalLayoutGroup>();
                    hlg.spacing = 10;
                    hlg.padding = new RectOffset(10, 10, 8, 8);
                    hlg.childAlignment = TextAnchor.MiddleCenter;
                    hlg.childControlWidth = true;
                    hlg.childControlHeight = true;
                }

                if (mapPanelTrans != null)
                {
                    Button btnMiramo = EnsureMapButton(mapPanelTrans, "Btn_MapMiramo", "Miramo Town", new Color(0.2f, 0.7f, 0.3f));
                    Button btnMetamine = EnsureMapButton(mapPanelTrans, "Btn_MapMetamine", "Metamine", new Color(0.8f, 0.45f, 0.15f));
                    Button btnCozy = EnsureMapButton(mapPanelTrans, "Btn_MapCozy", "Cozy Cave", new Color(0.2f, 0.5f, 0.85f));

                    Text selectedMapTxt = panelReady.GetComponentInChildren<Text>();
                    readyUI.BindMapButtons(btnMiramo, btnMetamine, btnCozy, selectedMapTxt);
                }

                Debug.Log("[Phase20Setup] RoomReadyUI Map Selection buttons bound successfully!");
            }

            Debug.Log("[Phase20Setup] Phase 20 Map Data & Environment Selection setup completed!");
        }

        private static MapData GetOrCreateMapData(string assetName, string displayName, MapType type, string desc, Color atmosphere, float windMult, float gravMult, Color mainGroundColor)
        {
            string path = $"{MAPS_FOLDER}/{assetName}.asset";
            MapData map = AssetDatabase.LoadAssetAtPath<MapData>(path);

            if (map == null)
            {
                map = ScriptableObject.CreateInstance<MapData>();
                AssetDatabase.CreateAsset(map, path);
            }

            SerializedObject serialized = new SerializedObject(map);
            serialized.FindProperty("_mapName").stringValue = displayName;
            serialized.FindProperty("_mapType").enumValueIndex = (int)type;
            serialized.FindProperty("_description").stringValue = desc;
            serialized.FindProperty("_atmosphereColor").colorValue = atmosphere;
            serialized.FindProperty("_windMultiplier").floatValue = windMult;
            serialized.FindProperty("_gravityMultiplier").floatValue = gravMult;

            // Generate procedural fallback sprites if empty
            Sprite groundSprite = serialized.FindProperty("_groundSprite").objectReferenceValue as Sprite;
            if (groundSprite == null)
            {
                groundSprite = CreateColorSprite(mainGroundColor, 512, 128);
                serialized.FindProperty("_groundSprite").objectReferenceValue = groundSprite;
            }

            Sprite bgSprite = serialized.FindProperty("_backgroundSprite").objectReferenceValue as Sprite;
            if (bgSprite == null)
            {
                bgSprite = CreateColorSprite(atmosphere, 512, 256);
                serialized.FindProperty("_backgroundSprite").objectReferenceValue = bgSprite;
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(map);

            return map;
        }

        private static Sprite CreateColorSprite(Color col, int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Button EnsureMapButton(Transform parent, string buttonName, string labelText, Color color)
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
                if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 11);
                txt.fontSize = 11;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
            }

            return btnObj.GetComponent<Button>();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }
    }
}
#endif
