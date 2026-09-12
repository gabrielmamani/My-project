using UnityEngine;
using Gunbound.Combat;

namespace Gunbound.Environment
{
    /// <summary>
    /// Vertical Force Zone trigger column. When active (SatelliteType.Force), spawns at a random X coordinate.
    /// When a projectile crosses this trigger column, its maxDamage and explosionRadius are amplified.
    /// </summary>
    public class ForceZone : MonoBehaviour
    {
        public static ForceZone Instance { get; private set; }

        [Header("Force Zone Settings")]
        [SerializeField] private float _minX = -12f;
        [SerializeField] private float _maxX = 12f;
        [SerializeField] private float _zoneWidth = 2.5f;
        [SerializeField] private float _damageMultiplier = 1.5f;
        [SerializeField] private float _radiusMultiplier = 1.4f;

        private BoxCollider2D _triggerCollider;
        private LineRenderer _visualBeam;
        private bool _isActive = false;

        public bool IsActive => _isActive;
        public float MinX => _minX;
        public float MaxX => _maxX;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupCollider();
            SetupVisualBeam();
        }

        private void Start()
        {
            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged += HandleSatelliteChanged;
                HandleSatelliteChanged(SatelliteManager.Instance.CurrentSatellite);
            }
            else
            {
                SetZoneActive(false);
            }
        }

        private void OnDestroy()
        {
            if (SatelliteManager.Instance != null)
            {
                SatelliteManager.Instance.OnSatelliteChanged -= HandleSatelliteChanged;
            }
        }

        private void SetupCollider()
        {
            _triggerCollider = GetComponent<BoxCollider2D>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<BoxCollider2D>();
            }
            _triggerCollider.isTrigger = true;
            _triggerCollider.size = new Vector2(_zoneWidth, 40f);
        }

        private void SetupVisualBeam()
        {
            _visualBeam = GetComponent<LineRenderer>();
            if (_visualBeam == null)
            {
                _visualBeam = gameObject.AddComponent<LineRenderer>();
            }

            _visualBeam.positionCount = 2;
            _visualBeam.startWidth = _zoneWidth;
            _visualBeam.endWidth = _zoneWidth;
            _visualBeam.useWorldSpace = true;

            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                _visualBeam.material = new Material(defaultShader);
            }

            Color forceColor = new Color(0.8f, 0.2f, 1.0f, 0.35f); // Transparent Violet/Magenta Glow
            _visualBeam.startColor = forceColor;
            _visualBeam.endColor = forceColor;
        }

        private void HandleSatelliteChanged(SatelliteType satellite)
        {
            if (satellite == SatelliteType.Force)
            {
                RelocateAndActivate();
            }
            else
            {
                SetZoneActive(false);
            }
        }

        public void RelocateAndActivate()
        {
            float randomX = UnityEngine.Random.Range(_minX, _maxX);
            transform.position = new Vector3(randomX, 0f, 0f);

            Vector3 topPos = new Vector3(randomX, 20f, 0f);
            Vector3 bottomPos = new Vector3(randomX, -20f, 0f);

            _visualBeam.SetPosition(0, topPos);
            _visualBeam.SetPosition(1, bottomPos);

            SetZoneActive(true);
            Debug.Log($"[ForceZone] FORCE ZONE ACTIVATED at X: {randomX:F2}!");
        }

        private void SetZoneActive(bool active)
        {
            _isActive = active;
            if (_triggerCollider != null) _triggerCollider.enabled = active;
            if (_visualBeam != null) _visualBeam.enabled = active;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive) return;

            Projectile projectile = other.GetComponent<Projectile>() ?? other.GetComponentInParent<Projectile>();
            if (projectile != null)
            {
                ApplyForceBoost(projectile);
            }
        }

        public void ApplyForceBoost(Projectile projectile)
        {
            if (projectile == null) return;

            float boostedDamage = projectile.MaxDamage * _damageMultiplier;
            float boostedRadius = projectile.ExplosionRadius * _radiusMultiplier;

            projectile.SetStats(boostedDamage, boostedRadius);
            Debug.Log($"[ForceZone] Projectile entered Force Zone! Boosted Stats -> Damage: {boostedDamage:F0}, Radius: {boostedRadius:F2}m");
        }

        /// <summary>
        /// Checks if world point is horizontally within active Force Zone bounds.
        /// </summary>
        public bool ContainsX(float xPos)
        {
            if (!_isActive) return false;
            float halfWidth = _zoneWidth * 0.5f;
            return (xPos >= transform.position.x - halfWidth && xPos <= transform.position.x + halfWidth);
        }
    }
}
