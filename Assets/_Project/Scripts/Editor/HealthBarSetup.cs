#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Gameplay;
using Gunbound.UI;

namespace Gunbound.EditorTools
{
    [InitializeOnLoad]
    public static class HealthBarSetup
    {
        static HealthBarSetup()
        {
            EditorApplication.delayCall += SetupPlayerHealthAndHealthBar;
        }

        [MenuItem("Gunbound/Setup Health & Floating HealthBar")]
        public static void SetupPlayerHealthAndHealthBar()
        {
            GameObject player = GameObject.Find("Player_Mage");
            if (player == null)
            {
                // Silence warning if scene is not open or during domain reloads when scene isn't ready
                return;
            }

            // 1. Add Health component to Player_Mage if missing
            Health health = player.GetComponent<Health>();
            if (health == null)
            {
                health = Undo.AddComponent<Health>(player);
                Debug.Log("[HealthBarSetup] Added 'Health' component to Player_Mage.");
            }

            // 2. Check for existing HealthCanvas child
            Transform existingCanvasTransform = player.transform.Find("HealthCanvas");
            GameObject canvasObj;

            if (existingCanvasTransform != null)
            {
                canvasObj = existingCanvasTransform.gameObject;
            }
            else
            {
                canvasObj = new GameObject("HealthCanvas");
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create HealthCanvas");
                canvasObj.transform.SetParent(player.transform, false);
            }

            // Configure Canvas
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            GraphicRaycaster raycaster = canvasObj.GetComponent<GraphicRaycaster>();
            if (raycaster == null) canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.localPosition = new Vector3(0f, 1.5f, 0f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 1.0f);
            canvasRect.sizeDelta = new Vector2(160f, 20f);
            canvasRect.localRotation = Quaternion.identity;

            // 3. Create or find Slider child
            Transform sliderTransform = canvasObj.transform.Find("HealthSlider");
            GameObject sliderObj;
            if (sliderTransform != null)
            {
                sliderObj = sliderTransform.gameObject;
            }
            else
            {
                sliderObj = new GameObject("HealthSlider");
                Undo.RegisterCreatedObjectUndo(sliderObj, "Create HealthSlider");
                sliderObj.transform.SetParent(canvasObj.transform, false);
            }

            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            if (sliderRect == null) sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.sizeDelta = Vector2.zero;
            sliderRect.localPosition = Vector3.zero;

            Slider slider = sliderObj.GetComponent<Slider>();
            if (slider == null) slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            // Background
            Transform bgTransform = sliderObj.transform.Find("Background");
            GameObject bgObj;
            if (bgTransform != null)
            {
                bgObj = bgTransform.gameObject;
            }
            else
            {
                bgObj = new GameObject("Background");
                bgObj.transform.SetParent(sliderObj.transform, false);
            }
            Image bgImage = bgObj.GetComponent<Image>();
            if (bgImage == null) bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.7f, 0.1f, 0.1f, 0.9f); // Dark Red background
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Fill Area
            Transform fillAreaTransform = sliderObj.transform.Find("Fill Area");
            GameObject fillAreaObj;
            if (fillAreaTransform != null)
            {
                fillAreaObj = fillAreaTransform.gameObject;
            }
            else
            {
                fillAreaObj = new GameObject("Fill Area");
                fillAreaObj.transform.SetParent(sliderObj.transform, false);
            }
            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            if (fillAreaRect == null) fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill Image
            Transform fillTransform = fillAreaObj.transform.Find("Fill");
            GameObject fillObj;
            if (fillTransform != null)
            {
                fillObj = fillTransform.gameObject;
            }
            else
            {
                fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(fillAreaObj.transform, false);
            }
            Image fillImage = fillObj.GetComponent<Image>();
            if (fillImage == null) fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.1f, 0.85f, 0.2f, 1.0f); // Vibrant Green fill
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;

            // 4. Attach HealthBar script to canvasObj (or sliderObj)
            HealthBar healthBar = canvasObj.GetComponent<HealthBar>();
            if (healthBar == null) healthBar = canvasObj.AddComponent<HealthBar>();
            healthBar.SetTargetHealth(health);

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            Debug.Log("[HealthBarSetup] Successfully setup Health & World Space HealthBar for 'Player_Mage'.");
        }
    }
}
#endif
