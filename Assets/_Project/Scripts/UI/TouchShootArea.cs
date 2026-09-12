using UnityEngine;
using UnityEngine.EventSystems;
using Gunbound.Player;
using Gunbound.Managers;

namespace Gunbound.UI
{
    /// <summary>
    /// Transparent touch gesture zone supporting Drag to Power charging and firing.
    /// Implements IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, and IPointerUpHandler.
    /// </summary>
    public class TouchShootArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector2 _startPos;
        private bool _isDragging;

        private PlayerController GetActivePlayer()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null)
            {
                return TurnManager.Instance.ActivePlayer;
            }
            return FindAnyObjectByType<PlayerController>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _startPos = eventData.position;
            _isDragging = false;
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.StartDragPower(eventData.position);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.StartDragPower(eventData.position);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            _isDragging = true;
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.UpdateDragPower(eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.ReleaseDragShot();
            }
            _isDragging = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            PlayerController activePlayer = GetActivePlayer();
            if (activePlayer != null)
            {
                activePlayer.ReleaseDragShot();
            }
            _isDragging = false;
        }
    }
}
