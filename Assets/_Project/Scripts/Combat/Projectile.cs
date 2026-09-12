using System;
using System.Collections.Generic;
using UnityEngine;
using Gunbound.Environment;
using Gunbound.Gameplay;
using Gunbound.Player;

namespace Gunbound.Combat
{
    /// <summary>
    /// Deterministic projectile simulation with gravity and wind acceleration applied step-by-step.
    /// Does not rely on physics engine forces. Uses continuous Raycasting for impact detection.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Properties")]
        [SerializeField] private float _maxLifetime = 10.0f;
        [SerializeField] private float _raycastRadius = 0.1f;
        [SerializeField] private float _windInfluenceMultiplier = 0.2f;
        [SerializeField] private float _windRampDuration = 0.4f;

        [Header("Explosion Properties")]
        [SerializeField] private float explosionRadius = 2.5f;
        [SerializeField] private float maxDamage = 350f;
        [SerializeField] private GameObject _explosionFXPrefab;

        [Header("Special Projectile Type (RF-5.2.1)")]
        [SerializeField] private SpecialProjectileType _specialType = SpecialProjectileType.Standard;
        [SerializeField] private float _boomerangLiftMultiplier = 2.5f;
        private bool _hasSplitShrapnel = false;

        public SpecialProjectileType SpecialType => _specialType;

        private Vector3 _startPosition;
        private Vector3 _initialVelocity;
        private Vector3 _currentVelocity;
        private Vector3 _gravity;
        private Vector3 _windVector;
        private LayerMask _collisionMask;

        private Collider2D _shooterCollider;
        private Collider2D _projectileCollider;

        private float _elapsedTime;
        private bool _isInitialized;
        private Vector3 _previousPosition;

        private bool _isTeleport = false;
        private PlayerController _shooterPlayer;
        private bool _hasBeenForceBoosted = false;

        public event Action<Vector3, Vector3> OnImpact; // (ImpactPoint, ImpactNormal)
        public event Action OnDestroyed;

        public float ExplosionRadius => explosionRadius;
        public float MaxDamage => maxDamage;

        /// <summary>
        /// Configures runtime damage and explosion crater radius for this projectile.
        /// </summary>
        public void SetStats(float newMaxDamage, float newExplosionRadius)
        {
            maxDamage = newMaxDamage;
            explosionRadius = newExplosionRadius;
        }

        /// <summary>
        /// Configures teleport beacon mode. When active, impact teleports shooter tank instead of exploding/carving terrain.
        /// </summary>
        public void SetTeleportMode(bool isTeleport, PlayerController shooterPlayer)
        {
            _isTeleport = isTeleport;
            _shooterPlayer = shooterPlayer;
        }

        /// <summary>
        /// Initializes projectile kinematic parameters and configures physics collision ignoring for the shooter.
        /// </summary>
        public void Initialize(Vector3 startPos, Vector3 initialVelocity, Vector3 gravity, Vector3 windVector, LayerMask collisionMask, Collider2D shooterCollider = null)
        {
            _startPosition = startPos;
            _initialVelocity = initialVelocity;
            _currentVelocity = initialVelocity;
            _gravity = gravity;
            _windVector = windVector;
            _collisionMask = collisionMask;
            _shooterCollider = shooterCollider;

            transform.position = startPos;
            _previousPosition = startPos;
            _elapsedTime = 0f;

            _projectileCollider = GetComponent<Collider2D>();
            if (_projectileCollider != null && _shooterCollider != null)
            {
                Physics2D.IgnoreCollision(_projectileCollider, _shooterCollider, true);
            }

            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = initialVelocity;
#else
                rb.velocity = initialVelocity;
#endif
            }

            EnsureTrailRenderer();
            _isInitialized = true;
        }

        private void EnsureTrailRenderer()
        {
            var trail = GetComponent<TrailRenderer>();
            if (trail == null)
            {
                trail = gameObject.AddComponent<TrailRenderer>();
            }

            if (trail != null)
            {
                trail.time = 0.8f;
                trail.startWidth = 0.25f;
                trail.endWidth = 0.02f;
                trail.numCornerVertices = 4;
                trail.numCapVertices = 4;
                trail.minVertexDistance = 0.1f;

                if (trail.material == null || trail.material.shader == null || trail.material.shader.name == "Hidden/InternalErrorShader")
                {
                    Shader defaultShader = Shader.Find("Sprites/Default");
                    if (defaultShader != null)
                    {
                        trail.material = new Material(defaultShader);
                    }
                }

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.0f), new GradientColorKey(new Color(1f, 0.3f, 0.0f), 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                );
                trail.colorGradient = gradient;
            }
        }

        private void FixedUpdate()
        {
            if (!_isInitialized) return;

            float dt = Time.fixedDeltaTime;
            _elapsedTime += dt;

            if (_elapsedTime > _maxLifetime)
            {
                ExplodeAndApplyRadialDamage();
                Destroy(gameObject);
                return;
            }

            // Progressive inertia integration (RF-02.2): initial exit velocity dominates first section
            float windFactor = _windRampDuration > 0f ? Mathf.Clamp01(_elapsedTime / _windRampDuration) : 1.0f;
            float effectiveWindMult = _windInfluenceMultiplier;

            if (_specialType == SpecialProjectileType.BoomerangWind)
            {
                // Boomer aerodynamically curves aggressively into/against wind direction (RF-5.2.1)
                effectiveWindMult *= _boomerangLiftMultiplier;

                // Extra lift curve when traveling against wind
                if (_windVector.x != 0f && Mathf.Sign(_windVector.x) != Mathf.Sign(_currentVelocity.x))
                {
                    _currentVelocity.y += Mathf.Abs(_windVector.x) * 0.4f * dt;
                }
            }
            else if (_specialType == SpecialProjectileType.PlasmaBeam)
            {
                effectiveWindMult *= 0.5f; // Plasma projectile maintains higher velocity trajectory
            }

            Vector3 netAcceleration = _gravity + (_windVector * (effectiveWindMult * windFactor));
            _currentVelocity += netAcceleration * dt;

            // Handle Armor Shrapnel Split (RF-5.2.1)
            if (_specialType == SpecialProjectileType.ShrapnelSplit && !_hasSplitShrapnel && _elapsedTime >= 0.5f)
            {
                _hasSplitShrapnel = true;
                SpawnShrapnelSubProjectiles();
            }

            Vector3 currentPosition = transform.position + _currentVelocity * dt;
            Vector3 displacement = currentPosition - _previousPosition;
            float distance = displacement.magnitude;

            if (distance > 0.0001f)
            {
                // Continuous collision detection along current step (Supports 2D and 3D colliders)
                RaycastHit2D[] hits2D = Physics2D.CircleCastAll(_previousPosition, _raycastRadius, displacement.normalized, distance, _collisionMask);
                bool hit2DDetected = false;

                foreach (var hit2D in hits2D)
                {
                    if (hit2D.collider == null) continue;

                    // Ignore self collision and shooter collider (including its hierarchy)
                    if (_projectileCollider != null && hit2D.collider == _projectileCollider) continue;
                    if (_shooterCollider != null && (hit2D.collider == _shooterCollider || hit2D.collider.transform.IsChildOf(_shooterCollider.transform))) continue;

                    Vector3 hitPoint = hit2D.point;
                    transform.position = hitPoint;
                    OnImpact?.Invoke(hitPoint, hit2D.normal);
                    Debug.Log($"[Projectile] Deterministic 2D Impact at {hitPoint} with object '{hit2D.collider.name}'");
                    ExplodeAndApplyRadialDamage();
                    Destroy(gameObject);
                    hit2DDetected = true;
                    break;
                }

                if (hit2DDetected) return;

                RaycastHit[] hits3D = Physics.SphereCastAll(_previousPosition, _raycastRadius, displacement.normalized, distance, _collisionMask);
                bool hit3DDetected = false;
                foreach (var hit3D in hits3D)
                {
                    if (hit3D.collider == null) continue;

                    // Ignore shooter collider hierarchy in 3D
                    if (_shooterCollider != null && hit3D.collider.transform.IsChildOf(_shooterCollider.transform)) continue;

                    transform.position = hit3D.point;
                    OnImpact?.Invoke(hit3D.point, hit3D.normal);
                    Debug.Log($"[Projectile] Deterministic 3D Impact at {hit3D.point} with object '{hit3D.collider.name}'");
                    ExplodeAndApplyRadialDamage();
                    Destroy(gameObject);
                    hit3DDetected = true;
                    break;
                }

                if (hit3DDetected) return;

                // Align projectile orientation with current velocity tangent
                if (_currentVelocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, _currentVelocity));
                }
            }

            transform.position = currentPosition;
            _previousPosition = currentPosition;

            // Check if passing through active Force Zone column
            if (!_hasBeenForceBoosted && ForceZone.Instance != null && ForceZone.Instance.IsActive && ForceZone.Instance.ContainsX(currentPosition.x))
            {
                _hasBeenForceBoosted = true;
                ForceZone.Instance.ApplyForceBoost(this);
            }
        }

        /// <summary>
        /// Applies radial damage to all targets with Health component within explosionRadius and carves terrain holes.
        /// If Teleport mode is active, relocates the shooter tank to impact point without carving or dealing damage.
        /// </summary>
        private void ExplodeAndApplyRadialDamage()
        {
            if (_isTeleport)
            {
                if (_shooterPlayer != null)
                {
                    Vector3 teleportTarget = transform.position + Vector3.up * 0.5f;
                    teleportTarget.z = _shooterPlayer.transform.position.z;
                    _shooterPlayer.transform.position = teleportTarget;
                    Debug.Log($"[Projectile] TELEPORT BEACON IMPACT! Shooter '{_shooterPlayer.name}' teleported to {teleportTarget}");
                }
                return;
            }

            // Trigger impact SFX and visual explosion FX
            Gunbound.Core.AudioManager.Instance?.PlayImpactSFX(transform.position);
            TriggerExplosionVFX(transform.position);

            // Trigger satellite THOR beam strike if active
            if (SatelliteManager.Instance != null && SatelliteManager.Instance.CurrentSatellite == SatelliteType.Thor)
            {
                ThorBeam.StrikeAt(transform.position);
            }

            // Carve terrain craters at explosion center
            CarveTerrainAtImpact();

            Collider2D[] targets = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            HashSet<Health> processedTargets = new HashSet<Health>();

            foreach (Collider2D target in targets)
            {
                Health health = target.GetComponent<Health>();
                if (health == null)
                {
                    health = target.GetComponentInParent<Health>();
                }

                if (health != null && !processedTargets.Contains(health))
                {
                    processedTargets.Add(health);

                    float distance = Vector2.Distance(transform.position, target.transform.position);
                    float damageFactor = Mathf.Clamp01(1f - (distance / explosionRadius));
                    float satMultiplier = SatelliteManager.Instance != null ? SatelliteManager.Instance.DamageMultiplier : 1.0f;
                    int damage = Mathf.RoundToInt(maxDamage * damageFactor * satMultiplier);

                    health.TakeDamage(damage, target.transform.position, false);
                    Debug.Log($"[Projectile] Radial Damage {damage} applied to '{health.gameObject.name}' (Distance: {distance:F2}m, Multiplier: {satMultiplier}x)");
                }
            }
        }

        private void CarveTerrainAtImpact()
        {
            if (Gunbound.Network.NetworkCraterManager.Instance != null)
            {
                Gunbound.Network.NetworkCraterManager.Instance.BroadcastCarveHole(transform.position, explosionRadius);
            }
            else
            {
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
                HashSet<DestructibleTerrain> carvedTerrains = new HashSet<DestructibleTerrain>();

                foreach (var col in hitColliders)
                {
                    DestructibleTerrain terrain = col.GetComponent<DestructibleTerrain>();
                    if (terrain == null)
                    {
                        terrain = col.GetComponentInParent<DestructibleTerrain>();
                    }

                    if (terrain != null && !carvedTerrains.Contains(terrain))
                    {
                        carvedTerrains.Add(terrain);
                        terrain.CarveHole(transform.position, explosionRadius);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        private void TriggerExplosionVFX(Vector3 position)
        {
            if (_explosionFXPrefab != null)
            {
                Instantiate(_explosionFXPrefab, position, Quaternion.identity);
                return;
            }

            GameObject explosionObj = new GameObject("ExplosionFX");
            explosionObj.transform.position = position;

            // Main Fireball Particle Burst
            ParticleSystem ps = explosionObj.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = 0.45f;
            main.startSpeed = 9.0f * (explosionRadius / 2.5f);
            main.startSize = 0.9f * (explosionRadius / 2.5f);
            main.startColor = new Color(1.0f, 0.45f, 0.1f, 1.0f);
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 45) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = explosionRadius * 0.35f;

            // Secondary White-Hot Flash Layer
            GameObject flashObj = new GameObject("FlashCore");
            flashObj.transform.SetParent(explosionObj.transform, false);

            ParticleSystem psFlash = flashObj.AddComponent<ParticleSystem>();
            psFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var flashMain = psFlash.main;
            flashMain.duration = 0.2f;
            flashMain.startLifetime = 0.2f;
            flashMain.startSpeed = 3.0f;
            flashMain.startSize = 1.2f * (explosionRadius / 2.5f);
            flashMain.startColor = new Color(1.0f, 0.95f, 0.7f, 1.0f);
            flashMain.loop = false;

            var flashEmission = psFlash.emission;
            flashEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });

            ps.Play();
            psFlash.Play();
        }

        /// <summary>
        /// Calculates position vector at time t using progressive wind inertia matching FixedUpdate trajectory (RF-02.2).
        /// </summary>
        public Vector3 CalculatePositionAtTime(float time)
        {
            Vector3 pos = _startPosition;
            Vector3 vel = _initialVelocity;
            float dt = 0.02f;
            float elapsed = 0f;
            while (elapsed < time)
            {
                float step = Mathf.Min(dt, time - elapsed);
                elapsed += step;
                float windFactor = _windRampDuration > 0f ? Mathf.Clamp01(elapsed / _windRampDuration) : 1f;
                Vector3 netAcc = _gravity + (_windVector * (_windInfluenceMultiplier * windFactor));
                vel += netAcc * step;
                pos += vel * step;
            }
            return pos;
        }

        /// <summary>
        /// Calculates velocity vector at time t using progressive wind inertia matching FixedUpdate trajectory (RF-02.2).
        /// </summary>
        public Vector3 CalculateVelocityAtTime(float time)
        {
            Vector3 vel = _initialVelocity;
            float dt = 0.02f;
            float elapsed = 0f;
            while (elapsed < time)
            {
                float step = Mathf.Min(dt, time - elapsed);
                elapsed += step;
                float windFactor = _windRampDuration > 0f ? Mathf.Clamp01(elapsed / _windRampDuration) : 1f;
                Vector3 netAcc = _gravity + (_windVector * (_windInfluenceMultiplier * windFactor));
                vel += netAcc * step;
            }
            return vel;
        }

        private void SpawnShrapnelSubProjectiles()
        {
            Debug.Log($"[Projectile] Armor Shrapnel Split triggered at {transform.position}");

            float[] angles = new float[] { -15f, 15f };
            float speed = _currentVelocity.magnitude;
            float baseAngle = Mathf.Atan2(_currentVelocity.y, _currentVelocity.x) * Mathf.Rad2Deg;

            foreach (float offset in angles)
            {
                float radians = (baseAngle + offset) * Mathf.Deg2Rad;
                Vector3 subVel = new Vector3(Mathf.Cos(radians) * speed, Mathf.Sin(radians) * speed, 0f);

                GameObject subObj = Instantiate(gameObject, transform.position, Quaternion.identity);
                Projectile subProj = subObj.GetComponent<Projectile>();
                if (subProj != null)
                {
                    subProj._specialType = SpecialProjectileType.Standard;
                    subProj._hasSplitShrapnel = true;
                    subProj.SetStats(maxDamage * 0.45f, explosionRadius * 0.75f);
                    subProj.Initialize(transform.position, subVel, _gravity, _windVector, _collisionMask, _shooterCollider);
                }
            }
        }
    }
}

