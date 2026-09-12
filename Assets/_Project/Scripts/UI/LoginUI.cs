using UnityEngine;
using UnityEngine.UI;
using Gunbound.Core;

namespace Gunbound.UI
{
    /// <summary>
    /// Login UI controller for initial authentication, profile nickname input, avatar selection, and PlayerPrefs saving (RF-6.1.1).
    /// </summary>
    public class LoginUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private InputField _nicknameInput;
        [SerializeField] private Button _btnAvatar1;
        [SerializeField] private Button _btnAvatar2;
        [SerializeField] private Button _btnAvatar3;
        [SerializeField] private Button _btnSubmit;
        [SerializeField] private Text _statusFeedbackText;

        private int _selectedAvatarId = 0;

        private void Start()
        {
            SetupListeners();
            LoadProfileToUI();
        }

        public void BindFields(InputField nicknameInput, Button btnAv1, Button btnAv2, Button btnAv3, Button btnSubmit, Text statusText)
        {
            _nicknameInput = nicknameInput;
            _btnAvatar1 = btnAv1;
            _btnAvatar2 = btnAv2;
            _btnAvatar3 = btnAv3;
            _btnSubmit = btnSubmit;
            _statusFeedbackText = statusText;

            SetupListeners();
            LoadProfileToUI();
        }

        private void SetupListeners()
        {
            if (_btnAvatar1 != null)
            {
                _btnAvatar1.onClick.RemoveAllListeners();
                _btnAvatar1.onClick.AddListener(() => SelectAvatar(0));
            }
            if (_btnAvatar2 != null)
            {
                _btnAvatar2.onClick.RemoveAllListeners();
                _btnAvatar2.onClick.AddListener(() => SelectAvatar(1));
            }
            if (_btnAvatar3 != null)
            {
                _btnAvatar3.onClick.RemoveAllListeners();
                _btnAvatar3.onClick.AddListener(() => SelectAvatar(2));
            }
            if (_btnSubmit != null)
            {
                _btnSubmit.onClick.RemoveAllListeners();
                _btnSubmit.onClick.AddListener(OnSubmitClicked);
            }
        }

        private void LoadProfileToUI()
        {
            if (UserDataManager.Instance != null)
            {
                if (_nicknameInput != null)
                {
                    _nicknameInput.text = UserDataManager.Instance.Nickname;
                }
                _selectedAvatarId = UserDataManager.Instance.AvatarId;
            }
            UpdateStatusText($"Sesión lista. Avatar seleccionado: #{_selectedAvatarId + 1}");
        }

        public void SelectAvatar(int avatarId)
        {
            AudioManager.Instance?.PlayClickSFX();
            _selectedAvatarId = avatarId;
            UpdateStatusText($"Avatar #{_selectedAvatarId + 1} seleccionado");
        }

        public void OnSubmitClicked()
        {
            AudioManager.Instance?.PlayClickSFX();

            string nameStr = _nicknameInput != null ? _nicknameInput.text : "Jugador1";
            if (string.IsNullOrWhiteSpace(nameStr))
            {
                nameStr = "Jugador1";
            }

            if (UserDataManager.Instance != null)
            {
                UserDataManager.Instance.SaveUserData(nameStr, _selectedAvatarId);
            }

            Debug.Log($"[LoginUI] User logged in: '{nameStr}' (Avatar #{_selectedAvatarId})");

            if (UINavigationManager.Instance != null)
            {
                UINavigationManager.Instance.ShowMainMenu();
            }
        }

        private void UpdateStatusText(string message)
        {
            if (_statusFeedbackText != null)
            {
                _statusFeedbackText.text = message;
            }
        }
    }
}
