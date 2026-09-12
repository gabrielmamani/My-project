using UnityEngine;
using UnityEngine.UI;
using Gunbound.Core;
using Gunbound.Network;

namespace Gunbound.UI
{
    /// <summary>
    /// Main Hub dashboard UI controller with player profile banner and navigation options (RF-6.2.1, RF-6.2.2).
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private Text _profileBannerText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Button _btnQuickMatch;
        [SerializeField] private Button _btnRoomBrowser;
        [SerializeField] private Button _btnGarage;
        [SerializeField] private Button _btnExit;

        private void Start()
        {
            SetupListeners();

            if (UserDataManager.Instance != null)
            {
                UserDataManager.Instance.OnUserDataChanged += HandleUserDataChanged;
                UpdateProfileUI(UserDataManager.Instance.Nickname, UserDataManager.Instance.AvatarId);
            }
        }

        private void OnDestroy()
        {
            if (UserDataManager.Instance != null)
            {
                UserDataManager.Instance.OnUserDataChanged -= HandleUserDataChanged;
            }
        }

        public void BindFields(Text profileText, Text goldText, Button btnQuick, Button btnBrowser, Button btnGarage, Button btnExit)
        {
            _profileBannerText = profileText;
            _goldText = goldText;
            _btnQuickMatch = btnQuick;
            _btnRoomBrowser = btnBrowser;
            _btnGarage = btnGarage;
            _btnExit = btnExit;

            SetupListeners();
            if (UserDataManager.Instance != null)
            {
                UpdateProfileUI(UserDataManager.Instance.Nickname, UserDataManager.Instance.AvatarId);
            }
        }

        private void SetupListeners()
        {
            if (_btnQuickMatch != null)
            {
                _btnQuickMatch.onClick.RemoveAllListeners();
                _btnQuickMatch.onClick.AddListener(OnQuickMatchClicked);
            }
            if (_btnRoomBrowser != null)
            {
                _btnRoomBrowser.onClick.RemoveAllListeners();
                _btnRoomBrowser.onClick.AddListener(OnRoomBrowserClicked);
            }
            if (_btnGarage != null)
            {
                _btnGarage.onClick.RemoveAllListeners();
                _btnGarage.onClick.AddListener(OnGarageClicked);
            }
            if (_btnExit != null)
            {
                _btnExit.onClick.RemoveAllListeners();
                _btnExit.onClick.AddListener(OnExitClicked);
            }
        }

        private void HandleUserDataChanged(string nickname, int avatarId)
        {
            UpdateProfileUI(nickname, avatarId);
        }

        private void UpdateProfileUI(string nickname, int avatarId)
        {
            if (_profileBannerText != null)
            {
                _profileBannerText.text = $"<b>{nickname}</b>  |  Avatar #{avatarId + 1}";
            }
            if (_goldText != null && UserDataManager.Instance != null)
            {
                _goldText.text = $"<b>Oro:</b> {UserDataManager.Instance.Gold} G";
            }
        }

        public void OnQuickMatchClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[MainMenuUI] Quick Match selected -> Navigating to Room Ready Screen");

            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.StartHostSession();
            }

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomReady();
            }
        }

        public void OnRoomBrowserClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[MainMenuUI] Room Browser selected -> Navigating to Room Browser");

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomBrowser();
            }
        }

        public void OnGarageClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[MainMenuUI] Garage selected -> Navigating to Mobile Garage");

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowRoomReady();
            }
        }

        public void OnExitClicked()
        {
            AudioManager.Instance?.PlayClickSFX();
            Debug.Log("[MainMenuUI] Exit selected -> Navigating back to Login");

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowLogin();
            }
        }
    }
}
