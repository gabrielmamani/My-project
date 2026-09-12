using UnityEngine;
using Gunbound.Gameplay;

namespace Gunbound.Environment
{
    /// <summary>
    /// Trigger volume placed below the map to detect entities falling into the void (Bunge Kill).
    /// Instantly reduces health to zero when triggered, declaring rival victory.
    /// </summary>
    public class DeathZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            Health health = other.GetComponent<Health>();
            if (health == null)
            {
                health = other.GetComponentInParent<Health>();
            }

            if (health != null && health.CurrentHealth > 0)
            {
                Debug.Log($"[DeathZone] Entity '{other.gameObject.name}' fell into the void! Applying lethal Bunge damage.");
                if (Gunbound.UI.MatchResultUI.Instance != null)
                {
                    Gunbound.UI.MatchResultUI.Instance.SetVictoryReason("Victoria por Bunge (Caída al Vacío)");
                }
                int fatalDamage = Mathf.RoundToInt(health.MaxHealth + 1000f);
                health.TakeDamage(fatalDamage, other.transform.position, true);
            }
        }
    }
}
