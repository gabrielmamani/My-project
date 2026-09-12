using System;
using UnityEngine;

namespace Gunbound.Gameplay
{
    /// <summary>
    /// Manages entity health, damage processing, and UI event notification.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Health Configuration")]
        [SerializeField] private float maxHealth = 1000f;
        [SerializeField] private float currentHealth;

        [SerializeField] private float armorDefense = 0f;
        [SerializeField] private float shieldHp = 0f;
        [SerializeField] private float maxShieldHp = 200f;

        /// <summary>
        /// Event invoked when health changes. Sends normalized health percentage (0.0 to 1.0).
        /// </summary>
        public event Action<float> OnHealthChanged;

        /// <summary>
        /// Event invoked when shield HP changes (currentShield, maxShield).
        /// </summary>
        public event Action<float, float> OnShieldHpChanged;

        /// <summary>
        /// Event invoked when damage is taken with hit details: amount, hitPosition in World Space, and isBunge flag.
        /// </summary>
        public event Action<int, Vector3, bool> OnDamageTakenDetails;

        /// <summary>
        /// Event invoked when entity health reaches zero.
        /// </summary>
        public event Action OnDeath;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float ArmorDefense { get => armorDefense; set => armorDefense = Mathf.Clamp01(value); }
        public float ShieldHp => shieldHp;
        public float MaxShieldHp => maxShieldHp;
        public bool IsShieldActive => shieldHp > 0f;

        private void Start()
        {
            if (currentHealth <= 0f) currentHealth = maxHealth;
            OnHealthChanged?.Invoke(GetHealthPercentage());
            OnShieldHpChanged?.Invoke(shieldHp, maxShieldHp);
        }

        /// <summary>
        /// Reconfigures maximum health and optionally resets current health.
        /// </summary>
        public void SetMaxHealth(float newMaxHealth, bool resetCurrent = true)
        {
            maxHealth = newMaxHealth;
            if (resetCurrent)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }
            OnHealthChanged?.Invoke(GetHealthPercentage());
        }

        /// <summary>
        /// Applies damage to the entity, updates current health, and notifies listeners.
        /// </summary>
        /// <param name="amount">Damage amount to apply.</param>
        public void TakeDamage(int amount)
        {
            TakeDamage(amount, transform.position, false);
        }

        /// <summary>
        /// Applies damage to the entity with hit position and bunge parameters.
        /// Absorbs damage using shieldHp first if active.
        /// </summary>
        public void TakeDamage(int amount, Vector3 hitPosition, bool isBunge = false)
        {
            if (currentHealth <= 0f) return;

            int finalAmount = isBunge ? amount : Mathf.Max(1, Mathf.RoundToInt(amount * (1.0f - armorDefense)));

            if (!isBunge && shieldHp > 0f)
            {
                if (shieldHp >= finalAmount)
                {
                    shieldHp -= finalAmount;
                    Debug.Log($"[Health] Shield absorbed ALL {finalAmount} damage! Remaining Shield: {shieldHp}/{maxShieldHp}");
                    finalAmount = 0;
                }
                else
                {
                    int absorbed = (int)shieldHp;
                    finalAmount -= absorbed;
                    shieldHp = 0f;
                    Debug.Log($"[Health] Shield absorbed {absorbed} damage and broke! Remaining damage to health: {finalAmount}");
                }
                OnShieldHpChanged?.Invoke(shieldHp, maxShieldHp);
            }

            if (finalAmount > 0)
            {
                currentHealth -= finalAmount;
                if (currentHealth < 0f)
                {
                    currentHealth = 0f;
                }
            }

            float normalizedPct = GetHealthPercentage();
            OnHealthChanged?.Invoke(normalizedPct);
            OnDamageTakenDetails?.Invoke(finalAmount, hitPosition, isBunge);

            Debug.Log($"[Health] '{gameObject.name}' took {finalAmount} damage (raw: {amount}, armor: {armorDefense * 100:F0}%) at {hitPosition}. Health: {currentHealth}/{maxHealth} ({normalizedPct * 100:F1}%)");

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Activates or recharges an energy shield barrier on this entity.
        /// </summary>
        public void ActivateShield(float amount = 200f)
        {
            maxShieldHp = amount;
            shieldHp = amount;
            OnShieldHpChanged?.Invoke(shieldHp, maxShieldHp);
            Debug.Log($"[Health] Energy Shield activated on '{gameObject.name}' with {amount} HP barrier!");
        }

        /// <summary>
        /// Restores health to the entity up to maxHealth and notifies listeners.
        /// </summary>
        /// <param name="amount">Amount of health points to restore.</param>
        public void Heal(float amount)
        {
            if (currentHealth <= 0f) return;

            currentHealth += amount;
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            float normalizedPct = GetHealthPercentage();
            OnHealthChanged?.Invoke(normalizedPct);

            Debug.Log($"[Health] '{gameObject.name}' healed {amount} HP. Health: {currentHealth}/{maxHealth} ({normalizedPct * 100:F1}%)");
        }

        private float GetHealthPercentage()
        {
            if (maxHealth <= 0f) return 0f;
            return Mathf.Clamp01(currentHealth / maxHealth);
        }

        private void Die()
        {
            Debug.Log($"[Health] '{gameObject.name}' died (health reached 0). Deactivating target.");
            OnDeath?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
