using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Gunbound.Managers;
using Gunbound.Player;

namespace Gunbound.UI
{
    public struct TurnOrderData
    {
        public int playerNumber;
        public string label;
        public int delay;
        public bool isCurrent;
        public bool isDoubleTurn;
    }

    /// <summary>
    /// Displays dynamic turn order shift list based on accumulated player delays (RF-04.1).
    /// Highlights current turn, double turns, and projected turn sequence.
    /// </summary>
    public class ShiftListUI : MonoBehaviour
    {
        [Header("UI Component References")]
        [SerializeField] private RectTransform _container;
        [SerializeField] private Text _headerText;
        [SerializeField] private List<Text> _turnEntryTexts = new List<Text>();

        [Header("Styling Colors")]
        [SerializeField] private Color _p1Color = new Color(0.2f, 0.85f, 1.0f, 1.0f); // Cyan for P1
        [SerializeField] private Color _p2Color = new Color(1.0f, 0.7f, 0.2f, 1.0f);  // Amber for P2
        [SerializeField] private Color _doubleTurnColor = new Color(1.0f, 0.3f, 0.3f, 1.0f); // Red alert

        [SerializeField] private int _maxProjectedTurns = 4;

        public List<Text> TurnEntryTexts => _turnEntryTexts;

        private void Start()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnDelaysUpdated += HandleDelaysUpdated;
                TurnManager.Instance.OnTurnPlayerChanged += HandleTurnPlayerChanged;
            }
            UpdateShiftList();
        }

        private void OnDestroy()
        {
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnDelaysUpdated -= HandleDelaysUpdated;
                TurnManager.Instance.OnTurnPlayerChanged -= HandleTurnPlayerChanged;
            }
        }

        private void HandleDelaysUpdated(int p1Delay, int p2Delay)
        {
            UpdateShiftList();
        }

        private void HandleTurnPlayerChanged(int turnNumber, int activePlayerNumber)
        {
            UpdateShiftList();
        }

        public void UpdateShiftList()
        {
            if (TurnManager.Instance == null) return;

            int p1Delay = TurnManager.Instance.Player1Delay;
            int p2Delay = TurnManager.Instance.Player2Delay;
            int activePlayerNum = TurnManager.Instance.ActivePlayerNumber;

            List<TurnOrderData> turnList = CalculateTurnOrder(p1Delay, p2Delay, activePlayerNum, _maxProjectedTurns);
            RenderTurnEntries(turnList);
        }

        public List<TurnOrderData> CalculateTurnOrder(int p1Delay, int p2Delay, int activePlayerNum, int maxTurns)
        {
            List<TurnOrderData> list = new List<TurnOrderData>();

            int simP1Delay = p1Delay;
            int simP2Delay = p2Delay;
            int lastPlayerNum = activePlayerNum;

            for (int i = 0; i < maxTurns; i++)
            {
                TurnOrderData data = new TurnOrderData();

                if (i == 0)
                {
                    data.playerNumber = activePlayerNum;
                    data.delay = (activePlayerNum == 1) ? p1Delay : p2Delay;
                    data.isCurrent = true;
                    data.isDoubleTurn = false;
                }
                else
                {
                    int nextPlayerNum;
                    if (simP1Delay < simP2Delay)
                    {
                        nextPlayerNum = 1;
                    }
                    else if (simP2Delay < simP1Delay)
                    {
                        nextPlayerNum = 2;
                    }
                    else
                    {
                        nextPlayerNum = (lastPlayerNum == 1) ? 2 : 1;
                    }

                    data.playerNumber = nextPlayerNum;
                    data.delay = (nextPlayerNum == 1) ? simP1Delay : simP2Delay;
                    data.isCurrent = false;
                    data.isDoubleTurn = (nextPlayerNum == lastPlayerNum);

                    // Add projected base turn delay (+250) for simulation of subsequent turns
                    if (nextPlayerNum == 1) simP1Delay += 250;
                    else simP2Delay += 250;

                    lastPlayerNum = nextPlayerNum;
                }

                data.label = data.playerNumber == 1 ? "P1 (Jugador 1)" : "P2 (Jugador 2)";
                list.Add(data);
            }

            return list;
        }

        private void RenderTurnEntries(List<TurnOrderData> list)
        {
            if (_turnEntryTexts == null || _turnEntryTexts.Count == 0) return;

            for (int i = 0; i < _turnEntryTexts.Count; i++)
            {
                if (_turnEntryTexts[i] == null) continue;

                if (i < list.Count)
                {
                    _turnEntryTexts[i].gameObject.SetActive(true);
                    TurnOrderData data = list[i];

                    string prefix = i == 0 ? "▶ " : $"{i + 1}. ";
                    string doubleTurnTag = data.isDoubleTurn ? " [¡DOBLE!]" : "";
                    string textStr = $"{prefix}{data.label} | Delay: {data.delay}{doubleTurnTag}";

                    _turnEntryTexts[i].text = textStr;
                    _turnEntryTexts[i].color = data.isDoubleTurn ? _doubleTurnColor : (data.playerNumber == 1 ? _p1Color : _p2Color);
                }
                else
                {
                    _turnEntryTexts[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
