using System;
using UnityEngine;

namespace Gunbound.UI
{
    public enum UIScreenState
    {
        Login,
        MainMenu,
        RoomBrowser,
        RoomReady,
        InGameHUD
    }

    /// <summary>
    /// Central UI Navigation Manager that controls active screen panel transitions in Canvas (RF-6.2.1).
    /// </summary>
    public class UINavigationManager : MonoBehaviour
    {
        public static UINavigationManager Instance { get; private set; }

        [Header("UI Screen Panels")]
        [SerializeField] private GameObject _panelLogin;
        [SerializeField] private GameObject _panelMainMenu;
        [SerializeField] private GameObject _panelRoomBrowser;
        [SerializeField] private GameObject _panelRoomReady;
        [SerializeField] private GameObject _panelInGameHUD;

        [Header("Current Navigation State")]
        [SerializeField] private UIScreenState _currentScreen = UIScreenState.Login;

        public event Action<UIScreenState> OnScreenChanged;

        public UIScreenState CurrentScreen => _currentScreen;

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
            // Initial screen state
            ShowLogin();
        }

        public void BindPanels(GameObject login, GameObject mainMenu, GameObject roomBrowser, GameObject roomReady, GameObject inGameHUD)
        {
            _panelLogin = login;
            _panelMainMenu = mainMenu;
            _panelRoomBrowser = roomBrowser;
            _panelRoomReady = roomReady;
            _panelInGameHUD = inGameHUD;

            ShowScreen(_currentScreen);
        }

        public void ShowLogin()
        {
            ShowScreen(UIScreenState.Login);
        }

        public void ShowMainMenu()
        {
            ShowScreen(UIScreenState.MainMenu);
        }

        public void ShowRoomBrowser()
        {
            ShowScreen(UIScreenState.RoomBrowser);
        }

        public void ShowRoomReady()
        {
            ShowScreen(UIScreenState.RoomReady);
        }

        public void ShowInGameHUD()
        {
            ShowScreen(UIScreenState.InGameHUD);
        }

        public void ShowScreen(UIScreenState state)
        {
            _currentScreen = state;

            if (_panelLogin != null) _panelLogin.SetActive(state == UIScreenState.Login);
            if (_panelMainMenu != null) _panelMainMenu.SetActive(state == UIScreenState.MainMenu);
            if (_panelRoomBrowser != null) _panelRoomBrowser.SetActive(state == UIScreenState.RoomBrowser);
            if (_panelRoomReady != null) _panelRoomReady.SetActive(state == UIScreenState.RoomReady);
            if (_panelInGameHUD != null) _panelInGameHUD.SetActive(state == UIScreenState.InGameHUD);

            Debug.Log($"[UINavigationManager] Switched UI Screen to: {state}");
            OnScreenChanged?.Invoke(state);
        }
    }
}
