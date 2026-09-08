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

        private void Awake()
        {
            if (_turretTransform == null)
            {
                _turretTransform = transform;
            }

            UpdateTurretRotation(1);
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
        /// Visually rotates the turret transform.
        /// </summary>
        public void UpdateTurretRotation(int facingDirection)
        {
            if (_turretTransform == null) return;

            float zRotation = _currentAngle * facingDirection;
            _turretTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        }
    }
}
