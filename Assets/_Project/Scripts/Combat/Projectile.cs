using System;
using UnityEngine;

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

        private Vector3 _startPosition;
        private Vector3 _initialVelocity;
        private Vector3 _currentVelocity;
        private Vector3 _gravity;
        private Vector3 _windVector;
        private LayerMask _collisionMask;

        private float _elapsedTime;
        private bool _isInitialized;
        private Vector3 _previousPosition;

        public event Action<Vector3, Vector3> OnImpact; // (ImpactPoint, ImpactNormal)
        public event Action OnDestroyed;

        /// <summary>
        /// Initializes projectile kinematic parameters.
        /// </summary>
        public void Initialize(Vector3 startPos, Vector3 initialVelocity, Vector3 gravity, Vector3 windVector, LayerMask collisionMask)
        {
            _startPosition = startPos;
            _initialVelocity = initialVelocity;
            _currentVelocity = initialVelocity;
            _gravity = gravity;
            _windVector = windVector;
            _collisionMask = collisionMask;

            transform.position = startPos;
            _previousPosition = startPos;
            _elapsedTime = 0f;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;

            float dt = Time.deltaTime;
            _elapsedTime += dt;

            if (_elapsedTime > _maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Step-by-step mathematical acceleration integration: V_new = V_old + (g + W) * dt
            Vector3 netAcceleration = _gravity + _windVector;
            _currentVelocity += netAcceleration * dt;

            Vector3 currentPosition = transform.position + _currentVelocity * dt;
            Vector3 displacement = currentPosition - _previousPosition;
            float distance = displacement.magnitude;

            if (distance > 0.0001f)
            {
                // Continuous collision detection along current step (Supports 2D and 3D colliders)
                RaycastHit2D hit2D = Physics2D.CircleCast(_previousPosition, _raycastRadius, displacement.normalized, distance, _collisionMask);
                if (hit2D.collider != null)
                {
                    Vector3 hitPoint = hit2D.point;
                    transform.position = hitPoint;
                    OnImpact?.Invoke(hitPoint, hit2D.normal);
                    Debug.Log($"[Projectile] Deterministic 2D Impact at {hitPoint} with object '{hit2D.collider.name}'");
                    Destroy(gameObject);
                    return;
                }

                if (Physics.SphereCast(_previousPosition, _raycastRadius, displacement.normalized, out RaycastHit hit3D, distance, _collisionMask))
                {
                    transform.position = hit3D.point;
                    OnImpact?.Invoke(hit3D.point, hit3D.normal);
                    Debug.Log($"[Projectile] Deterministic 3D Impact at {hit3D.point} with object '{hit3D.collider.name}'");
                    Destroy(gameObject);
                    return;
                }

                // Align projectile orientation with current velocity tangent
                if (_currentVelocity != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.Cross(Vector3.forward, _currentVelocity));
                }
            }

            transform.position = currentPosition;
            _previousPosition = currentPosition;
        }

        private void OnDestroy()
        {
            OnDestroyed?.Invoke();
        }

        /// <summary>
        /// Calculates position vector at time t using analytical trajectory formula:
        /// P(t) = P0 + V0*t + 0.5*(g + W)*t^2
        /// </summary>
        public Vector3 CalculatePositionAtTime(float time)
        {
            Vector3 netAcceleration = _gravity + _windVector;
            return _startPosition + (_initialVelocity * time) + (0.5f * netAcceleration * time * time);
        }

        /// <summary>
        /// Calculates velocity vector at time t using derivative formula:
        /// V(t) = V0 + (g + W)*t
        /// </summary>
        public Vector3 CalculateVelocityAtTime(float time)
        {
            Vector3 netAcceleration = _gravity + _windVector;
            return _initialVelocity + (netAcceleration * time);
        }
    }
}
