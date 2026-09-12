using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Environment;

namespace Gunbound.UI
{
    /// <summary>
    /// UI Component displaying the active global satellite modifier (Normal, Thor, Force, Hurricane)
    /// in the top-center HUD bar. Animates transitions on satellite state changes.
    /// </summary>
    public class SatelliteUI : MonoBehaviour
    {
        public static SatelliteUI Instance { get; private set; }

        [Header("UI Component References")]
        [SerializeField] private Image _satelliteIcon;
        [SerializeField] private Text _satelliteTitleText;
        [SerializeField] private Text _satelliteDescText;
        [SerializeField] private Image _badgeBackground;

        [Header("Satellite Theme Colors")]
        [SerializeField] private Color _normalColor = new Color(0.7f, 0.75f, 0.8f, 1.0f);
        [SerializeField] private Color _thorColor = new Color(0.1f, 0.9f, 1.0f, 1.0f);
        [SerializeField] private Color _forceColor = new Color(1.0f, 0.5f, 0.1f, 1.0f);
        [SerializeField] private Color _hurricaneColor = new Color(0.75f, 0.3f, 1.0f, 1.0f);

        private RectTransform _rectTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged += UpdateSatelliteDisplay;
                UpdateSatelliteDisplay(SatelliteManager.Instance.CurrentSatellite);
            }
        }

        private void OnDestroy()
        {
            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged -= UpdateSatelliteDisplay;
            }
        }

        /// <summary>
        /// Updates the HUD text, icon, and colors according to the active satellite type.
        /// </summary>
        public void UpdateSatelliteDisplay(SatelliteType satelliteType)
        {
            string title = "SATÉLITE: NORMAL";
            string desc = "Sin modificadores climáticos";
            Color themeColor = _normalColor;

            switch (satelliteType)
            {
                case SatelliteType.Thor:
                    title = "SATÉLITE: THOR";
                    desc = "¡Rayo Celestial Orbital al Impactar!";
                    themeColor = _thorColor;
                    break;
                case SatelliteType.Force:
                    title = "SATÉLITE: FORCE";
                    desc = "Potencia Incremetada (+25% Daño)";
                    themeColor = _forceColor;
                    break;
                case SatelliteType.Hurricane:
                    title = "SATÉLITE: VIENTO EXTREMO";
                    desc = "Aceleración de Viento x2.0";
                    themeColor = _hurricaneColor;
                    break;
                case SatelliteType.Normal:
                default:
                    title = "SATÉLITE: NORMAL";
                    desc = "Condiciones Climáticas Estándar";
                    themeColor = _normalColor;
                    break;
            }

            if (_satelliteTitleText != null)
            {
                _satelliteTitleText.text = title;
                _satelliteTitleText.color = themeColor;
            }

            if (_satelliteDescText != null)
            {
                _satelliteDescText.text = desc;
            }

            if (_badgeBackground != null)
            {
                _badgeBackground.color = new Color(themeColor.r * 0.3f, themeColor.g * 0.3f, themeColor.b * 0.3f, 0.85f);
            }

            if (_satelliteIcon != null)
            {
                _satelliteIcon.color = themeColor;
            }

            Debug.Log($"[SatelliteUI] HUD actualizado a {satelliteType}");
            StartCoroutine(PulseAnimationRoutine());
        }

        private IEnumerator PulseAnimationRoutine()
        {
            if (_rectTransform == null) yield break;

            Vector3 startScale = Vector3.one;
            Vector3 popScale = Vector3.one * 1.18f;
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _rectTransform.localScale = Vector3.Lerp(startScale, popScale, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            _rectTransform.localScale = startScale;
        }
    }
}
