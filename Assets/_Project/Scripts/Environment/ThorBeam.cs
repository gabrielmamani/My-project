using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gunbound.Gameplay;

namespace Gunbound.Environment
{
    /// <summary>
    /// Vertical orbital laser beam triggered by THOR satellite after projectile impact.
    /// Delays 0.3s, fires a visual sky beam, inflicts 150 HP radial damage, and carves a 2nd terrain crater.
    /// </summary>
    public class ThorBeam : MonoBehaviour
    {
        [Header("Beam Properties")]
        [SerializeField] private float _delayBeforeStrike = 0.3f;
        [SerializeField] private float _damageAmount = 150f;
        [SerializeField] private float _damageRadius = 2.5f;
        [SerializeField] private float _craterRadius = 2.0f;
        [SerializeField] private float _beamDuration = 0.4f;

        private LineRenderer _lineRenderer;

        /// <summary>
        /// Static factory helper to instantiate and trigger a Thor Beam strike.
        /// </summary>
        public static void StrikeAt(Vector3 targetImpactPoint)
        {
            GameObject beamObj = new GameObject("ThorBeam");
            ThorBeam beamComponent = beamObj.AddComponent<ThorBeam>();
            beamComponent.ExecuteStrike(targetImpactPoint);
        }

        public void ExecuteStrike(Vector3 targetImpactPoint)
        {
            StartCoroutine(StrikeRoutine(targetImpactPoint));
        }

        private IEnumerator StrikeRoutine(Vector3 impactPoint)
        {
            yield return new WaitForSeconds(_delayBeforeStrike);

            // Determine top Y coordinate for beam origin (above top of camera view)
            float topY = impactPoint.y + 20f;
            if (Camera.main != null)
            {
                float camTop = Camera.main.transform.position.y + Camera.main.orthographicSize + 2f;
                topY = Mathf.Max(topY, camTop);
            }

            Vector3 startPos = new Vector3(impactPoint.x, topY, 0f);
            Vector3 endPos = new Vector3(impactPoint.x, impactPoint.y, 0f);

            // Setup LineRenderer visually
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, startPos);
            _lineRenderer.SetPosition(1, endPos);
            _lineRenderer.startWidth = 0.8f;
            _lineRenderer.endWidth = 0.8f;
            _lineRenderer.useWorldSpace = true;

            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                _lineRenderer.material = new Material(defaultShader);
            }

            Color beamColor = new Color(0.2f, 0.85f, 1.0f, 1.0f); // Bright Cyan / Electric Blue
            _lineRenderer.startColor = beamColor;
            _lineRenderer.endColor = Color.white;

            // Play Thor beam SFX
            Gunbound.Core.AudioManager.Instance?.PlayThorBeamSFX(endPos);

            // Apply 150 HP Radial Damage to Health targets
            ApplyRadialDamage(endPos);

            // Carve 2nd crater on terrain
            CarveTerrain(endPos);

            Debug.Log($"[ThorBeam] THOR BEAM STRIKE at X: {endPos.x:F2}, Y: {endPos.y:F2}! Applied {_damageAmount} extra damage & crater carved.");

            // Fade out animation
            float elapsed = 0f;
            while (elapsed < _beamDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1.0f, 0.0f, elapsed / _beamDuration);
                Color fadedColor = new Color(beamColor.r, beamColor.g, beamColor.b, alpha);
                _lineRenderer.startColor = fadedColor;
                _lineRenderer.endColor = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void ApplyRadialDamage(Vector3 center)
        {
            Collider2D[] targets = Physics2D.OverlapCircleAll(center, _damageRadius);
            HashSet<Health> processedTargets = new HashSet<Health>();

            foreach (Collider2D target in targets)
            {
                Health health = target.GetComponent<Health>() ?? target.GetComponentInParent<Health>();
                if (health != null && !processedTargets.Contains(health))
                {
                    processedTargets.Add(health);

                    float distance = Vector2.Distance(center, target.transform.position);
                    float damageFactor = Mathf.Clamp01(1f - (distance / _damageRadius));
                    int damage = Mathf.RoundToInt(_damageAmount * damageFactor);

                    health.TakeDamage(damage);
                    Debug.Log($"[ThorBeam] Thor Beam deal {damage} extra damage to '{health.gameObject.name}'");
                }
            }
        }

        private void CarveTerrain(Vector3 center)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(center, _craterRadius);
            HashSet<DestructibleTerrain> carvedTerrains = new HashSet<DestructibleTerrain>();

            foreach (var col in hitColliders)
            {
                DestructibleTerrain terrain = col.GetComponent<DestructibleTerrain>() ?? col.GetComponentInParent<DestructibleTerrain>();
                if (terrain != null && !carvedTerrains.Contains(terrain))
                {
                    carvedTerrains.Add(terrain);
                    terrain.CarveHole(center, _craterRadius);
                }
            }
        }
    }
}
