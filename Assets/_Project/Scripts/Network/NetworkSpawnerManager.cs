using UnityEngine;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.Network
{
    /// <summary>
    /// Server/Host authoritative spawner that places Player 1 at X = -5.0 and Player 2 at X = 5.0,
    /// assigning client authority (OwnerClientId) and initializing network state synchronization (RF-07.2.1).
    /// </summary>
    public class NetworkSpawnerManager : MonoBehaviour
    {
        public static NetworkSpawnerManager Instance { get; private set; }

        [Header("Spawn Points")]
        [SerializeField] private Vector3 _p1SpawnPos = new Vector3(-5.0f, 1.5f, 0.0f);
        [SerializeField] private Vector3 _p2SpawnPos = new Vector3(5.0f, 1.5f, 0.0f);

        [Header("References")]
        [SerializeField] private PlayerController _player1;
        [SerializeField] private PlayerController _player2;

        public Vector3 P1SpawnPos => _p1SpawnPos;
        public Vector3 P2SpawnPos => _p2SpawnPos;

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
            ResolveAndSpawnPlayers();

            if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.OnNetworkRoleChanged += HandleRoleChanged;
            }
        }

        private void OnDestroy()
        {
            if (NetworkLobbyManager.Instance != null)
            {
                NetworkLobbyManager.Instance.OnNetworkRoleChanged -= HandleRoleChanged;
            }
        }

        private void HandleRoleChanged(NetworkRole role)
        {
            ResolveAndSpawnPlayers();
        }

        [Header("Mobile Assets Data (RF-5.1.2)")]
        [SerializeField] private MobileData _mageData;
        [SerializeField] private MobileData _armorData;
        [SerializeField] private MobileData _boomerData;

        public MobileData MageData => _mageData;
        public MobileData ArmorData => _armorData;
        public MobileData BoomerData => _boomerData;

        public MobileData GetMobileData(MobileType type)
        {
            switch (type)
            {
                case MobileType.Armor: return _armorData;
                case MobileType.Boomer: return _boomerData;
                default: return _mageData;
            }
        }

        public void SetMobileDataAssets(MobileData mage, MobileData armor, MobileData boomer)
        {
            _mageData = mage;
            _armorData = armor;
            _boomerData = boomer;
        }

        /// <summary>
        /// Authoritatively spawns / repositions tanks at X = -5 (P1) and X = 5 (P2),
        /// setting facing directions (P1 facing Right +1, P2 facing Left -1) and OwnerClientId authority (RF-07.2.1).
        /// </summary>
        public void ResolveAndSpawnPlayers()
        {
            if (_player1 == null)
            {
                var p1Obj = GameObject.Find("Player_Mage");
                if (p1Obj != null) _player1 = p1Obj.GetComponent<PlayerController>();
            }

            if (_player2 == null)
            {
                var p2Obj = GameObject.Find("Player_2");
                if (p2Obj != null) _player2 = p2Obj.GetComponent<PlayerController>();
            }

            NetworkRole role = NetworkLobbyManager.Instance != null ? NetworkLobbyManager.Instance.CurrentRole : NetworkRole.Offline;

            bool isLocal1 = role == NetworkRole.Offline || role == NetworkRole.Host;
            bool isLocal2 = role == NetworkRole.Offline || role == NetworkRole.Client;

            MobileType p1Type = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.P1SelectedMobile : MobileType.Mage;
            MobileType p2Type = NetworkGameManager.Instance != null ? NetworkGameManager.Instance.P2SelectedMobile : MobileType.Mage;

            MobileData p1Data = GetMobileData(p1Type);
            MobileData p2Data = GetMobileData(p2Type);

            // Configure Player 1 (Host OwnerClientId = 0) at X = -5 facing Right (+1)
            if (_player1 != null)
            {
                _player1.transform.position = _p1SpawnPos;
                _player1.SetFacingDirection(1);

                if (p1Data != null) _player1.ApplyMobileData(p1Data);

                NetworkPlayerSync sync1 = GetOrAddSync(_player1.gameObject);
                sync1.SetupNetworkAuthority(0, 1, isLocal1, _p1SpawnPos, 1);
            }

            // Configure Player 2 (Client OwnerClientId = 1) at X = 5 facing Left (-1)
            if (_player2 != null)
            {
                _player2.transform.position = _p2SpawnPos;
                _player2.SetFacingDirection(-1);

                if (p2Data != null) _player2.ApplyMobileData(p2Data);

                NetworkPlayerSync sync2 = GetOrAddSync(_player2.gameObject);
                sync2.SetupNetworkAuthority(1, 2, isLocal2, _p2SpawnPos, -1);
            }

            if (TurnManager.Instance != null && _player1 != null && _player2 != null)
            {
                TurnManager.Instance.SetPlayers(_player1, _player2);
            }

            Debug.Log($"[NetworkSpawnerManager] Authoritative tank spawn complete (Role={role}): P1 ({p1Type}) at {_p1SpawnPos} (IsLocal={isLocal1}), P2 ({p2Type}) at {_p2SpawnPos} (IsLocal={isLocal2}).");
        }

        private NetworkPlayerSync GetOrAddSync(GameObject obj)
        {
            NetworkPlayerSync sync = obj.GetComponent<NetworkPlayerSync>();
            if (sync == null) sync = obj.AddComponent<NetworkPlayerSync>();
            sync.ResolveComponents();
            return sync;
        }
    }
}
