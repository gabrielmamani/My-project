#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// Phase 8 Shift List Setup:
    /// Constructs Panel_ShiftList UI in top-left quadrant of Canvas with ShiftListUI component.
    /// Connects ShiftListUI reference to UIManager.
    /// </summary>
    [InitializeOnLoad]
    public static class Phase8ShiftListSetup
    {
        static Phase8ShiftListSetup()
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

        [MenuItem("Gunbound/Setup Phase 8 (Dynamic Shift List)")]
        public static void ExecuteBatchSetup()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                var canvasComponent = Object.FindAnyObjectByType<Canvas>();
                if (canvasComponent != null) canvasObj = canvasComponent.gameObject;
            }

            if (canvasObj == null)
            {
                Debug.LogError("[Phase8ShiftListSetup] Canvas object not found in scene!");
                return;
            }

            Transform parentTransform = canvasObj.transform.Find("HUDPanel") ?? canvasObj.transform;
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);

            // 1. Create Panel_ShiftList in Top-Left (240x130)
            GameObject panelObj = GetOrCreateContainer(parentTransform, "Panel_ShiftList", new Vector2(0f, 1f), new Vector2(15f, -15f), new Vector2(240f, 130f));
            RectTransform panelRt = panelObj.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(15f, -15f);

            Image panelBg = GetOrAdd<Image>(panelObj);
            panelBg.color = new Color(0.04f, 0.07f, 0.12f, 0.85f); // Dark semi-transparent blue-gray

            VerticalLayoutGroup vlg = GetOrAdd<VerticalLayoutGroup>(panelObj);
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.spacing = 3f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // 2. Add ShiftListUI component
            ShiftListUI shiftUI = GetOrAdd<ShiftListUI>(panelObj);

            // 3. Create Header Text
            Text headerText = GetOrCreateText(panelObj.transform, "HeaderText", "LÍNEA DE TURNOS (SHIFT)", defaultFont, 12, FontStyle.Bold, new Color(1.0f, 0.85f, 0.2f));

            // 4. Create 4 Turn Entry Text elements
            shiftUI.TurnEntryTexts.Clear();
            for (int i = 1; i <= 4; i++)
            {
                Text entryText = GetOrCreateText(panelObj.transform, $"TurnEntry_{i}", $"Turn {i}...", defaultFont, 12, FontStyle.Bold, Color.white);
                shiftUI.TurnEntryTexts.Add(entryText);
            }

            // 5. Connect ShiftListUI to UIManager
            UIManager uiManager = Object.FindAnyObjectByType<UIManager>();
            if (uiManager != null)
            {
                SerializedObject serializedUI = new SerializedObject(uiManager);
                SerializedProperty shiftProp = serializedUI.FindProperty("_shiftListUI");
                if (shiftProp != null)
                {
                    shiftProp.objectReferenceValue = shiftUI;
                    serializedUI.ApplyModifiedProperties();
                }
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[Phase8ShiftListSetup] Dynamic Shift List UI (Top-Left) setup completed successfully!");
        }

        private static GameObject GetOrCreateContainer(Transform parent, string name, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
                obj.transform.SetParent(parent, false);
            }

            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt == null) rt = obj.AddComponent<RectTransform>();
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return obj;
        }

        private static Text GetOrCreateText(Transform parent, string name, string textStr, Font font, int fontSize, FontStyle style, Color color)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
                obj.transform.SetParent(parent, false);
            }

            RectTransform rt = GetOrAdd<RectTransform>(obj);
            rt.sizeDelta = new Vector2(220f, 20f);

            Text text = GetOrAdd<Text>(obj);
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.text = textStr;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            return text;
        }

        private static T GetOrAdd<T>(GameObject obj) where T : Component
        {
            T comp = obj.GetComponent<T>();
            if (comp == null) comp = obj.AddComponent<T>();
            return comp;
        }
    }
}
#endif
