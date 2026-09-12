using System;
using UnityEngine;

namespace Gunbound.Player
{
    /// <summary>
    /// Manages elevation angle and launch vector calculations for turret aiming.
    /// </summary>
    public class TurretAim : MonoBehaviour
    {
        [Header("Aim Boundaries")]
        [SerializeField] private float _minAngle = 0.0f;
        [SerializeField] private float _maxAngle = 90.0f;
        [SerializeField] private float _currentAngle = 45.0f;

        [Header("Transforms")]
        [SerializeField] private Transform _turretTransform;
        [SerializeField] private Transform _firePoint;

        public event Action<float> OnAngleChanged;

        public float CurrentAngle => _currentAngle;
        public float MinAngle => _minAngle;
        public float MaxAngle => _maxAngle;
        public Transform FirePoint => _firePoint;

        private void Awake()
        {
            if (_turretTransform == null)
            {
                _turretTransform = transform;
            }

            UpdateTurretRotation(1);
        }

        /// <summary>
        /// Dynamically reconfigures minimum and maximum elevation angle boundaries.
        /// </summary>
        public void SetAngleBoundaries(float minAngle, float maxAngle)
        {
            _minAngle = minAngle;
            _maxAngle = maxAngle;
            SetAngle(Mathf.Clamp(_currentAngle, _minAngle, _maxAngle));
        }

        /// <summary>
        /// Sets absolute elevation angle in degrees.
        /// </summary>
        public void SetAngle(float angle)
        {
            float clampedAngle = Mathf.Clamp(angle, _minAngle, _maxAngle);
            if (!Mathf.Approximately(_currentAngle, clampedAngle))
            {
                _currentAngle = clampedAngle;
                UpdateTurretRotation(1);
                OnAngleChanged?.Invoke(_currentAngle);
            }
        }

        /// <summary>
        /// Adjusts elevation angle by delta value.
        /// </summary>
        public void AdjustAngle(float deltaAngle)
        {
            SetAngle(_currentAngle + deltaAngle);
        }

        /// <summary>
        /// Returns launch velocity vector based on power, elevation angle, and facing direction.
        /// </summary>
        public Vector3 GetLaunchVelocity(float power, int facingDirection)
        {
            float radians = _currentAngle * Mathf.Deg2Rad;
            float vx = power * Mathf.Cos(radians) * facingDirection;
            float vy = power * Mathf.Sin(radians);
            return new Vector3(vx, vy, 0f);
        }

        /// <summary>
        /// Gets absolute world position of the fire point.
        /// </summary>
        public Vector3 GetFirePointPosition()
        {
            return _firePoint != null ? _firePoint.position : transform.position;
        }

        /// <summary>
        /// Visually rotates the turret transform based on elevation angle.
        /// Parent scale handles horizontal orientation.
        /// </summary>
        public void UpdateTurretRotation(int facingDirection = 1)
        {
            if (_turretTransform == null) return;

            float zRotation = _currentAngle;
            _turretTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }
    }
}
