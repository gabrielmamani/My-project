using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Gunbound.UI
{
    /// <summary>
    /// Animates world-space floating damage numbers (+ upward motion, scale pop, and fade out).
    /// Supports normal damage, critical SS damage, and Bunge kills.
    /// </summary>
    public class FloatingDamageText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float _floatSpeed = 1.8f;
        [SerializeField] private float _duration = 1.0f;
        [SerializeField] private Vector3 _popScale = new Vector3(1.3f, 1.3f, 1.0f);

        [Header("Colors")]
        [SerializeField] private Color _normalColor = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        [SerializeField] private Color _criticalColor = new Color(1.0f, 0.85f, 0.1f, 1.0f);
        [SerializeField] private Color _bungeColor = new Color(0.7f, 0.2f, 1.0f, 1.0f);

        private Text _textComponent;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Spawns a floating damage text in world space at the given position.
        /// </summary>
        public static FloatingDamageText Spawn(Vector3 worldPos, int damage, bool isCritical = false, bool isBunge = false)
        {
            GameObject container = new GameObject("FloatingDamageText_Canvas");
            container.transform.position = worldPos + Vector3.up * 0.5f;

            Canvas canvas = container.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 50;

            RectTransform rect = container.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(250f, 60f);
            rect.localScale = Vector3.one * 0.015f;

            CanvasGroup group = container.AddComponent<CanvasGroup>();

            GameObject textObj = new GameObject("DamageText");
            textObj.transform.SetParent(container.transform, false);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", 28);

            text.fontSize = isCritical || isBunge ? 36 : 28;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;

            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            FloatingDamageText floatComp = container.AddComponent<FloatingDamageText>();
            floatComp._textComponent = text;
            floatComp._canvasGroup = group;

            floatComp.Initialize(damage, isCritical, isBunge);
            return floatComp;
        }

        public void Initialize(int damage, bool isCritical, bool isBunge)
        {
            if (_textComponent == null) _textComponent = GetComponentInChildren<Text>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            if (isBunge)
            {
                _textComponent.text = "¡BUNGE!";
                _textComponent.color = _bungeColor;
            }
            else if (isCritical)
            {
                _textComponent.text = $"-{damage} CRÍTICO!";
                _textComponent.color = _criticalColor;
            }
            else
            {
                _textComponent.text = $"-{damage}";
                _textComponent.color = _normalColor;
            }

            StartCoroutine(AnimateRoutine());
        }

        private IEnumerator AnimateRoutine()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + Vector3.up * 1.5f;
            Vector3 initialScale = transform.localScale;

            while (elapsed < _duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _duration;

                // Position upward curve
                transform.position = Vector3.Lerp(startPos, targetPos, t);

                // Pop scale curve (scale up first 20%, then settle)
                if (t < 0.2f)
                {
                    float popT = t / 0.2f;
                    transform.localScale = Vector3.Lerp(initialScale, Vector3.Scale(initialScale, _popScale), popT);
                }
                else
                {
                    float returnT = (t - 0.2f) / 0.8f;
                    transform.localScale = Vector3.Lerp(Vector3.Scale(initialScale, _popScale), initialScale, returnT);
                }

                // Fade out in last 40%
                if (t > 0.6f)
                {
                    float fadeT = (t - 0.6f) / 0.4f;
                    if (_canvasGroup != null)
                    {
                        _canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, fadeT);
                    }
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
