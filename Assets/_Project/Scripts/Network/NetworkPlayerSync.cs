using System;
using UnityEngine;
using Unity.Netcode;
using Gunbound.Player;
using Gunbound.Managers;

namespace Gunbound.Network
{
    /// <summary>
    /// Synchronizes tank chassis horizontal movement, facing direction (flipX), and turret elevation angle
    /// authoritatively using Netcode for GameObjects NetworkVariables (RF-4.2.1, RF-4.2.2, RF-4.2.3).
    /// </summary>
    public class NetworkPlayerSync : NetworkBehaviour
    {
        [Header("Network Identity")]
        [SerializeField] private ulong _ownerClientId = 0;
        [SerializeField] private int _playerNumber = 1;
        [SerializeField] private bool _isLocalPlayer = true;

        [Header("Synchronized State Variables")]
        private readonly NetworkVariable<Vector3> _syncedPosition = new NetworkVariable<Vector3>(
            Vector3.zero,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private readonly NetworkVariable<int> _syncedFacingDirection = new NetworkVariable<int>(
            1,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        private readonly NetworkVariable<float> _syncedTurretAngle = new NetworkVariable<float>(
            45.0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
        );

        [Header("Smoothing Parameters")]
        [SerializeField] private float _positionLerpSpeed = 15.0f;
        [SerializeField] private float _angleLerpSpeed = 20.0f;

        [Header("Component References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private TurretAim _turretAim;

        // Events
        public event Action<int> OnFacingDirectionSynced;
        public event Action<float> OnTurretAngleSynced;

        public new ulong OwnerClientId => IsSpawned ? base.OwnerClientId : _ownerClientId;
        public int PlayerNumber => _playerNumber;
        public bool IsLocalPlayerActive => IsSpawned ? IsOwner : _isLocalPlayer;
        public Vector3 TargetPosition => _syncedPosition.Value;
        public int FacingDirection => _syncedFacingDirection.Value;
        public float TurretAngle => _syncedTurretAngle.Value;

        private void Awake()
        {
            ResolveComponents();
        }

        private void Start()
        {
            if (_playerController != null)
            {
                int facing = _playerController.FacingDirection;
                if (IsSpawned && IsOwner) _syncedFacingDirection.Value = facing;
            }

            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged += HandleLocalAngleChanged;
                if (IsSpawned && IsOwner) _syncedTurretAngle.Value = _turretAim.CurrentAngle;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _syncedPosition.OnValueChanged += (prev, next) => { };
            _syncedFacingDirection.OnValueChanged += HandleNetworkFacingChanged;
            _syncedTurretAngle.OnValueChanged += HandleNetworkAngleChanged;

            if (IsOwner)
            {
                _syncedPosition.Value = transform.position;
                if (_playerController != null) _syncedFacingDirection.Value = _playerController.FacingDirection;
                if (_turretAim != null) _syncedTurretAngle.Value = _turretAim.CurrentAngle;
            }
        }

        public override void OnNetworkDespawn()
        {
            _syncedFacingDirection.OnValueChanged -= HandleNetworkFacingChanged;
            _syncedTurretAngle.OnValueChanged -= HandleNetworkAngleChanged;
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged -= HandleLocalAngleChanged;
            }
            base.OnDestroy();
        }

        public void ResolveComponents()
        {
            if (_playerController == null) _playerController = GetComponent<PlayerController>();
            if (_turretAim == null) _turretAim = GetComponentInChildren<TurretAim>();
        }

        /// <summary>
        /// Configures network ownership authority for this player vehicle (RF-4.2.1).
        /// </summary>
        public void SetupNetworkAuthority(ulong ownerId, int playerNum, bool isLocal, Vector3 spawnPos, int initialFacing)
        {
            _ownerClientId = ownerId;
            _playerNumber = playerNum;
            _isLocalPlayer = isLocal;

            transform.position = spawnPos;

            if (TryGetComponent<Rigidbody2D>(out var rb))
            {
                if (!isLocal)
                {
                    rb.bodyType = RigidbodyType2D.Kinematic;
                    rb.linearVelocity = Vector2.zero;
                }
                else
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }

            if (_playerController != null)
            {
                _playerController.SetFacingDirection(initialFacing);
            }

            if (IsSpawned && IsOwner)
            {
                _syncedPosition.Value = spawnPos;
                _syncedFacingDirection.Value = initialFacing;
            }

            Debug.Log($"[NetworkPlayerSync] Configured Tank {gameObject.name}: Player {playerNum}, OwnerId={ownerId}, SpawnPos={spawnPos}, Facing={initialFacing}, IsLocal={isLocal}");
        }

        private void HandleLocalAngleChanged(float angle)
        {
            if (IsLocalPlayerActive)
            {
                if (IsSpawned && IsOwner)
                {
                    _syncedTurretAngle.Value = angle;
                }
                OnTurretAngleSynced?.Invoke(angle);
            }
        }

        private void HandleNetworkFacingChanged(int prev, int next)
        {
            if (!IsLocalPlayerActive && _playerController != null)
            {
                _playerController.SetFacingDirection(next);
            }
            OnFacingDirectionSynced?.Invoke(next);
        }

        private void HandleNetworkAngleChanged(float prev, float next)
        {
            if (!IsLocalPlayerActive && _turretAim != null)
            {
                _turretAim.SetAngle(next);
            }
            OnTurretAngleSynced?.Invoke(next);
        }

        private void Update()
        {
            if (IsLocalPlayerActive)
            {
                // Local player updates NetworkVariables with current transform & facing
                if (IsSpawned && IsOwner)
                {
                    _syncedPosition.Value = transform.position;
                    if (_playerController != null)
                    {
                        int currentFacing = _playerController.FacingDirection;
                        if (_syncedFacingDirection.Value != currentFacing)
                        {
                            _syncedFacingDirection.Value = currentFacing;
                        }
                    }
                }
            }
            else
            {
                // Remote player interpolates horizontal position and turret angle from NetworkVariables
                Vector3 targetPos = _syncedPosition.Value;
                transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * _positionLerpSpeed);

                if (_playerController != null && _playerController.FacingDirection != _syncedFacingDirection.Value)
                {
                    _playerController.SetFacingDirection(_syncedFacingDirection.Value);
                }

                if (_turretAim != null && !Mathf.Approximately(_turretAim.CurrentAngle, _syncedTurretAngle.Value))
                {
                    _turretAim.SetAngle(Mathf.LerpAngle(_turretAim.CurrentAngle, _syncedTurretAngle.Value, Time.deltaTime * _angleLerpSpeed));
                }
            }
        }

        /// <summary>
        /// Receives authoritative state updates from server / remote network peer.
        /// </summary>
        public void ApplyNetworkState(Vector3 pos, int facing, float angle)
        {
            if (IsSpawned && IsOwner)
            {
                _syncedPosition.Value = pos;
                _syncedFacingDirection.Value = facing;
                _syncedTurretAngle.Value = angle;
            }

            if (!IsLocalPlayerActive)
            {
                if (_playerController != null)
                {
                    _playerController.SetFacingDirection(facing);
                }
                if (_turretAim != null)
                {
                    _turretAim.SetAngle(angle);
                }
            }
        }
    }
}
