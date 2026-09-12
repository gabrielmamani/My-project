using UnityEngine;
using UnityEngine.UI;
using Gunbound.Gameplay;

namespace Gunbound.UI
{
    /// <summary>
    /// Lightweight UI component that subscribes to Health.OnHealthChanged to update a UI Slider.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Health targetHealth;
        [SerializeField] private Slider healthSlider;

        private void Awake()
        {
            if (healthSlider == null)
            {
                healthSlider = GetComponent<Slider>();
                if (healthSlider == null)
                {
                    healthSlider = GetComponentInChildren<Slider>();
                }
            }

            if (targetHealth == null)
            {
                targetHealth = GetComponentInParent<Health>();
            }
        }

        private void OnEnable()
        {
            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged += UpdateHealthBar;
            }
        }

        private void OnDisable()
        {
            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged -= UpdateHealthBar;
            }
        }

        /// <summary>
        /// Updates health slider value given normalized health percentage (0 to 1).
        /// </summary>
        /// <param name="percentage">Normalized value between 0 and 1.</param>
        public void UpdateHealthBar(float percentage)
        {
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Clamp01(percentage);
            }
        }

        /// <summary>
        /// Allows explicit binding of target Health component.
        /// </summary>
        public void SetTargetHealth(Health health)
        {
            if (targetHealth != null)
            {
                targetHealth.OnHealthChanged -= UpdateHealthBar;
            }

            targetHealth = health;

            if (targetHealth != null && isActiveAndEnabled)
            {
                targetHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(targetHealth.CurrentHealth / targetHealth.MaxHealth);
            }
        }
    }
}
