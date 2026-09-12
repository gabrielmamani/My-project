#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Gunbound.EditorTools
{
    /// <summary>
    /// EventSystem & UI Multi-Input Setup Utility:
    /// Configures EventSystem with StandaloneInputModule for seamless Mouse (PC) and Touch (Mobile) input,
    /// and ensures non-interactive UI elements do not block pointer raycasts.
    /// </summary>
    [InitializeOnLoad]
    public static class EventSystemSetup
    {
        static EventSystemSetup()
        {
            EditorApplication.delayCall += () =>
            {
                var scene = EditorSceneManager.GetActiveScene();
                if (scene.IsValid() && scene.isLoaded && scene.name == "SampleScene")
                {
                    ConfigureEventSystemAndRaycasts();
                }
            };
        }

        [MenuItem("Gunbound/Setup EventSystem & Multi-Input")]
        public static void ConfigureEventSystemAndRaycasts()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                string scenePath = "Assets/Scenes/SampleScene.unity";
                activeScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            if (!activeScene.IsValid())
            {
                Debug.LogError("[EventSystemSetup] Could not open SampleScene.unity");
                return;
            }

            SetupEventSystem();
            CleanCanvasRaycastTargets();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                AssetDatabase.SaveAssets();
            }

            Debug.Log("[EventSystemSetup] EventSystem & Multi-Input (PC Mouse + Mobile Touch) configuration complete!");
        }

        private static void SetupEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
                Debug.Log("[EventSystemSetup] Created new EventSystem GameObject.");
            }

            // Remove legacy or conflicting modules if present
            var newModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (newModule != null)
            {
                Undo.DestroyObjectImmediate(newModule);
                Debug.Log("[EventSystemSetup] Removed InputSystemUIInputModule.");
            }

            // Ensure StandaloneInputModule for multi-platform PC + Mobile support
            StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone == null)
            {
                standalone = Undo.AddComponent<StandaloneInputModule>(es.gameObject);
                Debug.Log("[EventSystemSetup] Added StandaloneInputModule to EventSystem.");
            }
        }

        private static void CleanCanvasRaycastTargets()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null) return;

            Graphic[] graphics = canvasObj.GetComponentsInChildren<Graphic>(true);
            int cleanedCount = 0;

            foreach (var g in graphics)
            {
                Selectable sel = g.GetComponentInParent<Selectable>();
                IPointerDownHandler pointerHandler = g.GetComponentInParent<IPointerDownHandler>();
                IPointerClickHandler clickHandler = g.GetComponentInParent<IPointerClickHandler>();

                bool isInteractive = (sel != null || pointerHandler != null || clickHandler != null);

                if (!isInteractive && g.raycastTarget)
                {
                    Undo.RecordObject(g, "Disable raycastTarget");
                    g.raycastTarget = false;
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                Debug.Log($"[EventSystemSetup] Disabled raycastTarget on {cleanedCount} non-interactive UI elements.");
            }
        }
    }
}
#endif
