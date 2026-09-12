using UnityEngine;
using UnityEngine.UI;
using Gunbound.Gameplay;

namespace Gunbound.UI
{
    /// <summary>
    /// World Space Overhead Health Bar attached above each player tank.
    /// Smoothly updates HP bar color gradient, player label, current HP text,
    /// and listens to damage events to trigger FloatingDamageText.
    /// </summary>
    public class OverheadHealthBar : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Health _targetHealth;
        [SerializeField] private Transform _targetTransform;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 1.6f, 0f);

        [Header("UI Component References")]
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Slider _healthSlider;
        [SerializeField] private Image _healthFillImage;
        [SerializeField] private Text _healthValueText;

        [Header("Color Gradient")]
        [SerializeField] private Color _highHpColor = new Color(0.2f, 0.9f, 0.3f, 1.0f);
        [SerializeField] private Color _mediumHpColor = new Color(0.95f, 0.8f, 0.15f, 1.0f);
        [SerializeField] private Color _lowHpColor = new Color(0.95f, 0.2f, 0.2f, 1.0f);

        private Canvas _canvas;
        private string _playerName = "Jugador";

        public void Setup(Transform targetTransform, Health health, string playerName)
        {
            _targetTransform = targetTransform;
            _targetHealth = health;
            _playerName = playerName;

            if (_playerNameText != null)
            {
                _playerNameText.text = _playerName;
            }

            if (_targetHealth != null)
            {
                _targetHealth.OnHealthChanged += HandleHealthChanged;
                _targetHealth.OnDamageTakenDetails += HandleDamageTakenDetails;
                UpdateUI(_targetHealth.CurrentHealth / _targetHealth.MaxHealth);
            }
        }

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            if (_targetTransform == null)
            {
                _targetTransform = transform.parent != null ? transform.parent : transform;
            }
            if (_targetHealth == null)
            {
                _targetHealth = _targetTransform.GetComponent<Health>();
                if (_targetHealth == null) _targetHealth = _targetTransform.GetComponentInParent<Health>();
            }
        }

        private void OnEnable()
        {
            if (_targetHealth != null)
            {
                _targetHealth.OnHealthChanged += HandleHealthChanged;
                _targetHealth.OnDamageTakenDetails += HandleDamageTakenDetails;
            }
        }

        private void OnDisable()
        {
            if (_targetHealth != null)
            {
                _targetHealth.OnHealthChanged -= HandleHealthChanged;
                _targetHealth.OnDamageTakenDetails -= HandleDamageTakenDetails;
            }
        }

        private void LateUpdate()
        {
            if (_targetTransform != null)
            {
                transform.position = _targetTransform.position + _offset;
            }
        }

        private void HandleHealthChanged(float pct)
        {
            UpdateUI(pct);
        }

        private void HandleDamageTakenDetails(int damage, Vector3 hitPos, bool isBunge)
        {
            if (damage <= 0) return;
            bool isCritical = damage > 200;
            Vector3 spawnPos = hitPos != Vector3.zero ? hitPos : transform.position;
            FloatingDamageText.Spawn(spawnPos, damage, isCritical, isBunge);
        }

        public void UpdateUI(float pct)
        {
            pct = Mathf.Clamp01(pct);

            if (_healthSlider != null)
            {
                _healthSlider.value = pct;
            }

            if (_healthFillImage != null)
            {
                if (pct > 0.5f)
                {
                    _healthFillImage.color = Color.Lerp(_mediumHpColor, _highHpColor, (pct - 0.5f) * 2f);
                }
                else
                {
                    _healthFillImage.color = Color.Lerp(_lowHpColor, _mediumHpColor, pct * 2f);
                }
            }

            if (_healthValueText != null && _targetHealth != null)
            {
                _healthValueText.text = $"{Mathf.RoundToInt(_targetHealth.CurrentHealth)} / {Mathf.RoundToInt(_targetHealth.MaxHealth)}";
            }
        }

        /// <summary>
        /// Creates a World Space OverheadHealthBar procedurally for a target GameObject.
        /// </summary>
        public static OverheadHealthBar CreateOverheadBar(GameObject targetObj, string playerName, Color nameColor)
        {
            Health health = targetObj.GetComponent<Health>();
            if (health == null) health = targetObj.GetComponentInParent<Health>();

            GameObject barObj = new GameObject("OverheadHealthBar_Canvas");
            barObj.transform.SetParent(targetObj.transform, false);
            barObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);

            Canvas canvas = barObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 40;

            RectTransform rect = barObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160f, 40f);
            rect.localScale = Vector3.one * 0.015f;

            // Player Name Label
            GameObject nameObj = new GameObject("PlayerNameText");
            nameObj.transform.SetParent(barObj.transform, false);
            Text nameText = nameObj.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (nameText.font == null) nameText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            nameText.fontSize = 18;
            nameText.fontStyle = FontStyle.Bold;
            nameText.text = playerName;
            nameText.color = nameColor;
            nameText.alignment = TextAnchor.MiddleCenter;

            Outline nameOutline = nameObj.AddComponent<Outline>();
            nameOutline.effectColor = Color.black;

            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0.55f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // Background Bar
            GameObject bgObj = new GameObject("Bar_Background");
            bgObj.transform.SetParent(barObj.transform, false);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.05f, 0.15f);
            bgRect.anchorMax = new Vector2(0.95f, 0.5f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Slider
            GameObject sliderObj = new GameObject("HealthSlider");
            sliderObj.transform.SetParent(bgObj.transform, false);
            Slider slider = sliderObj.AddComponent<Slider>();

            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);
            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.9f, 0.3f, 1.0f);

            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            slider.targetGraphic = bgImg;
            slider.fillRect = fillRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // Health Value Text
            GameObject valObj = new GameObject("HealthValueText");
            valObj.transform.SetParent(bgObj.transform, false);
            Text valText = valObj.AddComponent<Text>();
            valText.font = nameText.font;
            valText.fontSize = 12;
            valText.fontStyle = FontStyle.Bold;
            valText.color = Color.white;
            valText.alignment = TextAnchor.MiddleCenter;

            Outline valOutline = valObj.AddComponent<Outline>();
            valOutline.effectColor = Color.black;

            RectTransform valRect = valObj.GetComponent<RectTransform>();
            valRect.anchorMin = Vector2.zero;
            valRect.anchorMax = Vector2.one;
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;

            OverheadHealthBar barComp = barObj.AddComponent<OverheadHealthBar>();
            barComp._targetTransform = targetObj.transform;
            barComp._targetHealth = health;
            barComp._playerNameText = nameText;
            barComp._healthSlider = slider;
            barComp._healthFillImage = fillImg;
            barComp._healthValueText = valText;
            barComp.Setup(targetObj.transform, health, playerName);

            return barComp;
        }
    }
}
