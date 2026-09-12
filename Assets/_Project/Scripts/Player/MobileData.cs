using UnityEngine;

namespace Gunbound.Player
{
    /// <summary>
    /// ScriptableObject data container defining stats, angle limits, movement, base delay,
    /// and visual sprites for a specific mobile tank type (RF-5.1.3).
    /// </summary>
    [CreateAssetMenu(fileName = "MobileData_New", menuName = "Gunbound/Mobile Data", order = 1)]
    public class MobileData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private MobileType _mobileType = MobileType.Mage;
        [SerializeField] private string _mobileName = "Mage";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Balanced energy mobile with flexible aiming angle.";

        [Header("Stats Configuration")]
        [SerializeField] private int _maxHealth = 1000;
        [Range(0f, 0.5f)]
        [SerializeField] private float _armorDefense = 0.05f; // % damage reduction (0.05 = 5%)
        [SerializeField] private float _minAngle = 20.0f;
        [SerializeField] private float _maxAngle = 70.0f;
        [SerializeField] private float _moveSpeed = 4.0f;
        [SerializeField] private int _baseDelay = 250;

        [Header("Visual Assets")]
        [SerializeField] private Sprite _chassisSprite;
        [SerializeField] private Sprite _turretSprite;
        [SerializeField] private Sprite _iconSprite;

        [Header("Projectiles (For Module 5.2)")]
        [SerializeField] private GameObject _shot1Prefab;
        [SerializeField] private GameObject _shot2Prefab;
        [SerializeField] private GameObject _ssPrefab;

        public MobileType MobileType => _mobileType;
        public string MobileName => _mobileName;
        public string Description => _description;
        public int MaxHealth => _maxHealth;
        public float ArmorDefense => _armorDefense;
        public float MinAngle => _minAngle;
        public float MaxAngle => _maxAngle;
        public float MoveSpeed => _moveSpeed;
        public int BaseDelay => _baseDelay;
        public Sprite ChassisSprite => _chassisSprite;
        public Sprite TurretSprite => _turretSprite;
        public Sprite IconSprite => _iconSprite;
        public GameObject Shot1Prefab => _shot1Prefab;
        public GameObject Shot2Prefab => _shot2Prefab;
        public GameObject SSPrefab => _ssPrefab;
    }
}
