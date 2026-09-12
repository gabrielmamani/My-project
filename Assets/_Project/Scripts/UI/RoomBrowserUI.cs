using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Core;
using Gunbound.Network;

namespace Gunbound.UI
{
    /// <summary>
    /// Room Browser UI controller for listing active rooms, searching, creating custom rooms, and joining (RF-6.3.1, RF-6.3.2).
    /// </summary>
    public class RoomBrowserUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Transform _roomListContainer;
        [SerializeField] private Button _btnCreateRoom;
        [SerializeField] private Button _btnRefresh;
        [SerializeField] private Button _btnBack;
        [SerializeField] private GameObject _modalCreateRoom;
        [SerializeField] private InputField _roomNameInput;
        [SerializeField] private Button _btnSubmitCreate;
        [SerializeField] private Button _btnCloseModal;
        [SerializeField] private InputField _ipInputField;
        [SerializeField] private InputField _portInputField;
        [SerializeField] private Button _btnConnectIp;

        private List<RoomData> _simulatedRooms = new List<RoomData>();

        private void Start()
        {
            SetupListeners();
            GenerateMockRoomDirectory();
            PopulateRoomListUI();
        }

        public void BindFields(Transform container, Button btnCreate, Button btnRefresh, Button btnBack, GameObject modal, InputField roomNameInput, Button btnSubmitCreate, Button btnCloseModal, InputField ipInput, InputField portInput, Button btnConnectIp)
        {
            _roomListContainer = container;
            _btnCreateRoom = btnCreate;
            _btnRefresh = btnRefresh;
            _btnBack = btnBack;
            _modalCreateRoom = modal;
            _roomNameInput = roomNameInput;
            _btnSubmitCreate = btnSubmitCreate;
            _btnCloseModal = btnCloseModal;
            _ipInputField = ipInput;
            _portInputField = portInput;
            _btnConnectIp = btnConnectIp;

            SetupListeners();
            GenerateMockRoomDirectory();
            PopulateRoomListUI();
        }

        private void SetupListeners()
        {
            if (_btnCreateRoom != null)
            {
                _btnCreateRoom.onClick.RemoveAllListeners();
                _btnCreateRoom.onClick.AddListener(OpenCreateModal);
            }
            if (_btnRefresh != null)
            {
                _btnRefresh.onClick.RemoveAllListeners();
                _btnRefresh.onClick.AddListener(OnRefreshClicked);
            }
            if (_btnBack != null)
            {
                _btnBack.onClick.RemoveAllListeners();
                _btnBack.onClick.AddListener(OnBackClicked);
            }
            if (_btnSubmitCreate != null)
            {
                _btnSubmitCreate.onClick.RemoveAllListeners();
                _btnSubmitCreate.onClick.AddListener(OnSubmitCreateRoom);
            }
            if (_btnCloseModal != null)
            {
                _btnCloseModal.onClick.RemoveAllListeners();
                _btnCloseModal.onClick.AddListener(CloseCreateModal);
            }
            if (_btnConnectIp != null)
            {
                _btnConnectIp.onClick.RemoveAllListeners();
                _btnConnectIp.onClick.AddListener(OnConnectIpClicked);
            }
        }

        private void GenerateMockRoomDirectory()
        {
            _simulatedRooms.Clear();
            _simulatedRooms.Add(new RoomData("R01", "#01 - Sala de Honor 1v1", "Host_Matias", "Miramo Town", 1, 2, "127.0.0.1", 7777));
            _simulatedRooms.Add(new RoomData("R02", "#02 - Batalla Táctica Bunge", "GunnerMaster", "Metamine", 1, 2, "127.0.0.1", 7777));
            _simulatedRooms.Add(new RoomData("R03", "#03 - Duelo de Boomers", "AirStriker", "Cozy Cave", 1, 2, "127.0.0.1", 7777));
        }

        private void PopulateRoomListUI()
        {
            if (_roomListContainer == null) return;

            foreach (Transform child in _roomListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var room in _simulatedRooms)
            {
                CreateRoomListItemUI(room);
            }
        }

        private void CreateRoomListItemUI(RoomData room)
        {
            GameObject itemObj = new GameObject($"RoomItem_{room.roomId}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            itemObj.transform.SetParent(_roomListContainer, false);

            RectTransform rect = itemObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(460f, 40f);

            Image img = itemObj.GetComponent<Image>();
            img.color = new Color(0.12f, 0.18f, 0.28f, 0.9f);

            Button btn = itemObj.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = img.color;
            cb.highlightedColor = new Color(0.2f, 0.4f, 0.6f, 1f);
            cb.pressedColor = new Color(0.1f, 0.3f, 0.5f, 1f);
            btn.colors = cb;

            btn.onClick.AddListener(() => JoinRoom(room));

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(itemObj.transform, false);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text txt = textObj.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 13;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.text = $"<b>{room.roomName}</b> | Mapa: {room.mapName} | Host: {room.hostName} ({room.currentPlayers}/{room.maxPlayers})";
            txt.raycastTarget = false;
        }

        public void JoinRoom(RoomData room)
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log($"[RoomBrowserUI] Joining room '{room.roomName}' at {room.serverIp}:{room.serverPort}");

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SetConnectionInfo(room.serverIp, room.serverPort);
                NetworkGameManager.Instance.StartClientSession();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomReady();
            }
        }

        public void OpenCreateModal()
        {
            AudioManager.Instance?.PlayClickSFX();
            if (_modalCreateRoom != null)
            {
                _modalCreateRoom.SetActive(true);
            }
        }

        public void CloseCreateModal()
        {
            AudioManager.Instance?.PlayClickSFX();
            if (_modalCreateRoom != null)
            {
                _modalCreateRoom.SetActive(false);
            }
        }

        public void OnSubmitCreateRoom()
        {
            AudioManager.Instance?.PlayClickSFX();
            CloseCreateModal();

            string nameStr = _roomNameInput != null ? _roomNameInput.text : "Sala Personalizada";
            if (string.IsNullOrWhiteSpace(nameStr)) nameStr = "Sala de " + (UserDataManager.Instance != null ? UserDataManager.Instance.Nickname : "Host");

            Debug.Log($"[RoomBrowserUI] Room created: '{nameStr}' -> Starting Host session");

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.StartHostSession();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomReady();
            }
        }

        public void OnConnectIpClicked()
        {
            AudioManager.Instance?.PlayClickSFX();

            string ip = _ipInputField != null ? _ipInputField.text : "127.0.0.1";
            ushort port = 7777;
            if (_portInputField != null && ushort.TryParse(_portInputField.text, out ushort p))
            {
                port = p;
            }

            Debug.Log($"[RoomBrowserUI] Connecting directly to IP {ip}:{port}");

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.SetConnectionInfo(ip, port);
                NetworkGameManager.Instance.StartClientSession();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomReady();
            }
        }

        public void OnRefreshClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            GenerateMockRoomDirectory();
            PopulateRoomListUI();
        }

        public void OnBackClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowMainMenu();
            }
        }
    }
}
