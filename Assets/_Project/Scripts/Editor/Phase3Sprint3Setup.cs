#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Environment;
using Gunbound.Managers;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class Phase3Sprint3Setup
    {
        static Phase3Sprint3Setup()
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

        [MenuItem("Gunbound/Setup Phase 3 Sprint 3 (Satellites & Thor/Force Modifiers)")]
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
                Debug.LogError("[Phase3Sprint3Setup] Could not open SampleScene.unity");
                return;
            }

            SetupSatelliteManager();
            SetupForceZone();
            SetupSatelliteUI();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[Phase3Sprint3Setup] Completed Phase 3 Satellite System & Environmental Modifiers Setup!");
        }

        private static void SetupSatelliteManager()
        {
            SatelliteManager manager = Object.FindAnyObjectByType<SatelliteManager>();
            if (manager == null)
            {
                GameObject managerObj = GameObject.Find("Managers") ?? GameObject.Find("TurnManager") ?? new GameObject("SatelliteManager");
                manager = Undo.AddComponent<SatelliteManager>(managerObj);
            }
            Debug.Log("[Phase3Sprint3Setup] SatelliteManager verified.");
        }

        private static void SetupForceZone()
        {
            ForceZone forceZone = Object.FindAnyObjectByType<ForceZone>();
            if (forceZone == null)
            {
                GameObject forceObj = GameObject.Find("ForceZone");
                if (forceObj == null)
                {
                    forceObj = new GameObject("ForceZone");
                    Undo.RegisterCreatedObjectUndo(forceObj, "Create ForceZone");
                }
                forceZone = forceObj.GetComponent<ForceZone>();
                if (forceZone == null)
                {
                    forceZone = forceObj.AddComponent<ForceZone>();
                }
            }
            Debug.Log("[Phase3Sprint3Setup] ForceZone verified.");
        }

        private static void SetupSatelliteUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogWarning("[Phase3Sprint3Setup] Canvas not found in active scene.");
                return;
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null)
            {
                uiManager = canvasObj.AddComponent<UIManager>();
            }

            Transform satTrans = canvasObj.transform.Find("Text_Satellite");
            GameObject satObj;
            if (satTrans != null)
            {
                satObj = satTrans.gameObject;
            }
            else
            {
                satObj = new GameObject("Text_Satellite");
                Undo.RegisterCreatedObjectUndo(satObj, "Create Text_Satellite");
                satObj.transform.SetParent(canvasObj.transform, false);
            }

            Text txt = satObj.GetComponent<Text>();
            if (txt == null) txt = satObj.AddComponent<Text>();
            txt.font = defaultFont;
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(0.2f, 0.9f, 1.0f); // Bright Cyan / Thor Accent
            txt.text = "[ Satélite: NORMAL ]";

            RectTransform rect = satObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -45f);
            rect.sizeDelta = new Vector2(300f, 30f);

            SerializedObject serializedUI = new SerializedObject(uiManager);
            SerializedProperty satTextProp = serializedUI.FindProperty("_satelliteText");
            if (satTextProp != null)
            {
                satTextProp.objectReferenceValue = txt;
                serializedUI.ApplyModifiedProperties();
            }

            Debug.Log("[Phase3Sprint3Setup] Text_Satellite created and assigned in UIManager.");
        }
    }
}
#endif
