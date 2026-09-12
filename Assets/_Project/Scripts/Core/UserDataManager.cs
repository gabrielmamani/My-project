using System;
using UnityEngine;

namespace Gunbound.Core
{
    /// <summary>
    /// Manages active player user profile, credentials, and local persistence via PlayerPrefs (RF-6.1.1, RF-6.1.2).
    /// </summary>
    public class UserDataManager : MonoBehaviour
    {
        public static UserDataManager Instance { get; private set; }

        private const string KEY_NICKNAME = "Gunbound_Nickname";
        private const string KEY_AVATAR_ID = "Gunbound_AvatarId";
        private const string KEY_EQUIPPED_ITEMS = "Gunbound_EquippedItems";

        [Header("User Profile State")]
        [SerializeField] private string _nickname = "Jugador1";
        [SerializeField] private int _avatarId = 0;
        [SerializeField] private int _level = 1;
        [SerializeField] private int _gold = 5000;
        [SerializeField] private System.Collections.Generic.List<Gunbound.Managers.ItemType> _equippedItems = new System.Collections.Generic.List<Gunbound.Managers.ItemType>();

        public event Action<string, int> OnUserDataChanged; // (nickname, avatarId)

        public string Nickname => _nickname;
        public int AvatarId => _avatarId;
        public int Level => _level;
        public int Gold => _gold;
        public System.Collections.Generic.List<Gunbound.Managers.ItemType> EquippedItems => _equippedItems;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadUserData();
        }

        public void LoadUserData()
        {
            _nickname = PlayerPrefs.GetString(KEY_NICKNAME, "Jugador_" + UnityEngine.Random.Range(1000, 9999));
            _avatarId = PlayerPrefs.GetInt(KEY_AVATAR_ID, 0);

            string itemsString = PlayerPrefs.GetString(KEY_EQUIPPED_ITEMS, "Dual,Teleport,Heal,Shield,ChangeWind,DualPlus");
            _equippedItems.Clear();
            foreach (string itemStr in itemsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<Gunbound.Managers.ItemType>(itemStr.Trim(), out var parsedType) && parsedType != Gunbound.Managers.ItemType.None)
                {
                    if (!_equippedItems.Contains(parsedType) && _equippedItems.Count < 6)
                    {
                        _equippedItems.Add(parsedType);
                    }
                }
            }

            Debug.Log($"[UserDataManager] User profile loaded: Nickname='{_nickname}', AvatarId={_avatarId}, Items={_equippedItems.Count}");
            OnUserDataChanged?.Invoke(_nickname, _avatarId);
        }

        public void SaveUserData(string nickname, int avatarId)
        {
            if (!string.IsNullOrEmpty(nickname))
            {
                _nickname = nickname.Trim();
            }
            _avatarId = Mathf.Max(0, avatarId);

            PlayerPrefs.SetString(KEY_NICKNAME, _nickname);
            PlayerPrefs.SetInt(KEY_AVATAR_ID, _avatarId);
            PlayerPrefs.Save();

            Debug.Log($"[UserDataManager] User profile saved: Nickname='{_nickname}', AvatarId={_avatarId}");
            OnUserDataChanged?.Invoke(_nickname, _avatarId);
        }

        public void SaveEquippedItems(System.Collections.Generic.List<Gunbound.Managers.ItemType> items)
        {
            if (items == null) return;
            _equippedItems = new System.Collections.Generic.List<Gunbound.Managers.ItemType>(items);
            string itemsString = string.Join(",", _equippedItems.ToArray());
            PlayerPrefs.SetString(KEY_EQUIPPED_ITEMS, itemsString);
            PlayerPrefs.Save();
            Debug.Log($"[UserDataManager] Saved equipped items loadout: {itemsString}");
        }
    }
}
