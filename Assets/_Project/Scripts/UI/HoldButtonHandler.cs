using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Gunbound.UI
{
    /// <summary>
    /// Handles continuous UI touch/mouse button press and hold events.
    /// Fires events while pointer is held down for mobile touch controls.
    /// </summary>
    public class HoldButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Hold Settings")]
        [SerializeField] private UnityEvent _onHeld = new UnityEvent();

        private bool _isPressed = false;

        public event Action OnHoldStateChanged;

        public bool IsPressed => _isPressed;
        public UnityEvent OnHeld => _onHeld;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            OnHoldStateChanged?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            OnHoldStateChanged?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPressed = false;
            OnHoldStateChanged?.Invoke();
        }

        private void Update()
        {
            if (_isPressed)
            {
                _onHeld?.Invoke();
            }
        }
    }
}
