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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_playerController != null)
            {
                _playerController.OnFireButtonDown();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_playerController != null)
            {
                _playerController.OnFireButtonUp();
            }
        }
    }
}
