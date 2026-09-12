using UnityEngine;
using Gunbound.Gameplay;

namespace Gunbound.Environment
{
    /// <summary>
    /// Trigger volume that detects entities falling into the void (Bunge Kill).
    /// Instantly reduces health to zero when triggered, declaring rival victory.
    /// </summary>
    public class DeathTrigger : MonoBehaviour
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
                Debug.Log($"[DeathTrigger] Entity '{other.gameObject.name}' fell into the void! Applying 9999 Bunge damage.");
                health.TakeDamage(9999);
            }
        }
    }
}
