using UnityEngine;
using UnityEngine.EventSystems;
using Gunbound.Player;

namespace Gunbound.UI
{
    /// <summary>
    /// Handles mobile screen button press and release events for shot power charging.
    /// Implements IPointerDownHandler and IPointerUpHandler to support dual control.
    /// </summary>
    public class FireButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private PlayerController _playerController;

        private void Start()
        {
            if (_playerController == null)
            {
                _playerController = FindAnyObjectByType<PlayerController>();
            }
        }

        private PlayerController GetActivePlayer()
        {
            if (Gunbound.Managers.TurnManager.Instance != null && Gunbound.Managers.TurnManager.Instance.ActivePlayer != null)
            {
                return Gunbound.Managers.TurnManager.Instance.ActivePlayer;
            }
            if (_playerController != null)
            {
                return _playerController;
            }
            return FindAnyObjectByType<PlayerController>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.OnFireButtonDown();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.OnFireButtonUp();
            }
        }
    }
}
