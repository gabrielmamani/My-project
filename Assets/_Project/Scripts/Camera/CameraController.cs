using System.Collections;
using UnityEngine;
using Gunbound.Combat;
using Gunbound.Managers;

namespace Gunbound.CameraSystem
{
    public enum CameraState
    {
        FocusPlayer,
        FollowProjectile,
        WaitAndSwitch
    }

    /// <summary>
    /// Smooth dynamic camera controller supporting 3 states:
    /// 1. FocusPlayer: Tracks current active player tank.
    /// 2. FollowProjectile: Tracks active projectile in flight.
    /// 3. WaitAndSwitch: Focuses impact location for 1s, then smoothly transitions back to next active player.
    /// Maintains constant Z = -10.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Interpolation Settings")]
        [SerializeField] private float _smoothTime = 0.25f;
        [SerializeField] private float _waitAtImpactDuration = 1.0f;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private float _cameraZ = -10.0f;

        [Header("Camera Bounds (Optional)")]
        [SerializeField] private bool _useBounds = false;
        [SerializeField] private Vector2 _minBounds = new Vector2(-20f, -5f);
        [SerializeField] private Vector2 _maxBounds = new Vector2(20f, 15f);

        private CameraState _currentState = CameraState.FocusPlayer;
        private Transform _targetTransform;
        private Vector3 _customTargetPosition;
        private Vector3 _currentVelocity = Vector3.zero;
        private Coroutine _waitRoutine;

        public CameraState CurrentState => _currentState;
        public Transform TargetTransform => _targetTransform;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SubscribeEvents();
            FocusActivePlayer();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnPlayerChanged += HandleTurnPlayerChanged;
                TurnManager.Instance.OnProjectileSpawned += HandleProjectileSpawned;
                TurnManager.Instance.OnProjectileImpacted += HandleProjectileImpacted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnTurnPlayerChanged -= HandleTurnPlayerChanged;
                TurnManager.Instance.OnProjectileSpawned -= HandleProjectileSpawned;
                TurnManager.Instance.OnProjectileImpacted -= HandleProjectileImpacted;
            }
        }

        private void LateUpdate()
        {
            Vector3 targetPosition = CalculateTargetPosition();
            targetPosition.z = _cameraZ;

            if (_useBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, _minBounds.x, _maxBounds.x);
                targetPosition.y = Mathf.Clamp(targetPosition.y, _minBounds.y, _maxBounds.y);
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _currentVelocity, _smoothTime);
        }

        private Vector3 CalculateTargetPosition()
        {
            switch (_currentState)
            {
                case CameraState.FocusPlayer:
                    if (_targetTransform != null)
                    {
                        return _targetTransform.position + _targetOffset;
                    }
                    if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null)
                    {
                        return TurnManager.Instance.ActivePlayer.transform.position + _targetOffset;
                    }
                    return transform.position;

                case CameraState.FollowProjectile:
                    if (_targetTransform != null)
                    {
                        return _targetTransform.position + _targetOffset;
                    }
                    // If target destroyed early, maintain position until impact event fires
                    return _customTargetPosition + _targetOffset;

                case CameraState.WaitAndSwitch:
                    return _customTargetPosition + _targetOffset;

                default:
                    return transform.position;
            }
        }

        public void FocusActivePlayer()
        {
            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }

            _currentState = CameraState.FocusPlayer;
            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null)
            {
                _targetTransform = TurnManager.Instance.ActivePlayer.transform;
            }
        }

        private void HandleTurnPlayerChanged(int turnNumber, int activePlayerNumber)
        {
            if (_currentState != CameraState.WaitAndSwitch)
            {
                FocusActivePlayer();
            }
        }

        private void HandleProjectileSpawned(Projectile projectile)
        {
            if (projectile == null) return;

            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }

            _currentState = CameraState.FollowProjectile;
            _targetTransform = projectile.transform;
            _customTargetPosition = projectile.transform.position;
            Debug.Log($"[CameraController] Camera state switched to FollowProjectile -> Target: {projectile.name}");
        }

        private void HandleProjectileImpacted(Vector3 impactPoint)
        {
            _customTargetPosition = impactPoint;
            _targetTransform = null;
            _currentState = CameraState.WaitAndSwitch;

            Debug.Log($"[CameraController] Camera state switched to WaitAndSwitch at impact point {impactPoint}. Waiting {_waitAtImpactDuration}s...");

            if (_waitRoutine != null) StopCoroutine(_waitRoutine);
            _waitRoutine = StartCoroutine(WaitAndSwitchRoutine());
        }

        private IEnumerator WaitAndSwitchRoutine()
        {
            yield return new WaitForSeconds(_waitAtImpactDuration);

            Debug.Log("[CameraController] Impact wait complete. Transitioning camera back to active player.");
            FocusActivePlayer();
            _waitRoutine = null;
        }
    }
}
