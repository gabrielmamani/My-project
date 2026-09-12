using UnityEngine;
using Gunbound.Managers;

namespace Gunbound.Player
{
    /// <summary>
    /// Visual renderer for elevation angle aim arc showing min/max angle boundaries and current elevation vector (RF-01.3).
    /// Dynamically updates with tank elevation, orientation flip, and turn state.
    /// </summary>
    public class AimArcVisualizer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TurretAim _turretAim;
        [SerializeField] private PlayerController _playerController;

        [Header("Arc Configuration")]
        [SerializeField] private float _arcRadius = 1.8f;
        [SerializeField] private int _arcSegments = 24;
        [SerializeField] private float _minMaxLineLength = 2.0f;
        [SerializeField] private float _currentAimLineLength = 2.4f;

        [Header("Color Styling")]
        [SerializeField] private Color _arcColor = new Color(0.2f, 0.85f, 1.0f, 0.6f);
        [SerializeField] private Color _limitLineColor = new Color(0.4f, 0.7f, 1.0f, 0.5f);
        [SerializeField] private Color _currentAimColor = new Color(1.0f, 0.9f, 0.2f, 0.95f);
        [SerializeField] private float _lineWidth = 0.04f;

        private LineRenderer _arcLineRenderer;
        private LineRenderer _minLimitLineRenderer;
        private LineRenderer _maxLimitLineRenderer;
        private LineRenderer _currentAimLineRenderer;

        private bool _isInitialized = false;

        private void Awake()
        {
            ResolveReferences();
            CreateLineRenderers();
        }

        private void Start()
        {
            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged += HandleAngleChanged;
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnInputStateChanged += HandleInputStateChanged;
                TurnManager.Instance.OnTurnPlayerChanged += HandleTurnPlayerChanged;
            }

            UpdateArcVisuals();
        }

        private void OnDestroy()
        {
            if (_turretAim != null)
            {
                _turretAim.OnAngleChanged -= HandleAngleChanged;
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.OnInputStateChanged -= HandleInputStateChanged;
                TurnManager.Instance.OnTurnPlayerChanged -= HandleTurnPlayerChanged;
            }
        }

        public void ResolveReferences()
        {
            if (_turretAim == null) _turretAim = GetComponentInParent<TurretAim>();
            if (_turretAim == null) _turretAim = GetComponentInChildren<TurretAim>();
            if (_playerController == null) _playerController = GetComponentInParent<PlayerController>();
            if (_playerController == null) _playerController = GetComponent<PlayerController>();
        }

        private void CreateLineRenderers()
        {
            _arcLineRenderer = CreateChildLineRenderer("ArcCurveLine", _arcColor, _lineWidth);
            _minLimitLineRenderer = CreateChildLineRenderer("MinLimitLine", _limitLineColor, _lineWidth * 0.8f);
            _maxLimitLineRenderer = CreateChildLineRenderer("MaxLimitLine", _limitLineColor, _lineWidth * 0.8f);
            _currentAimLineRenderer = CreateChildLineRenderer("CurrentAimLine", _currentAimColor, _lineWidth * 1.3f);
            _isInitialized = true;
        }

        private LineRenderer CreateChildLineRenderer(string name, Color color, float width)
        {
            Transform existing = transform.Find(name);
            GameObject obj = existing != null ? existing.gameObject : new GameObject(name);
            obj.transform.SetParent(transform, false);

            LineRenderer line = obj.GetComponent<LineRenderer>();
            if (line == null) line = obj.AddComponent<LineRenderer>();

            line.useWorldSpace = true;
            line.startWidth = width;
            line.endWidth = width;
            line.positionCount = 0;
            line.sortingLayerName = "Default";
            line.sortingOrder = 10;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                line.material = new Material(spriteShader);
            }

            line.startColor = color;
            line.endColor = color;

            return line;
        }

        private void LateUpdate()
        {
            UpdateVisibilityAndPosition();
        }

        private void UpdateVisibilityAndPosition()
        {
            bool shouldBeVisible = ShouldDrawArc();
            SetVisualsActive(shouldBeVisible);

            if (shouldBeVisible)
            {
                UpdateArcVisuals();
            }
        }

        public bool ShouldDrawArc()
        {
            if (_playerController != null && !_playerController.IsMyTurn) return false;
            if (TurnManager.Instance != null && TurnManager.Instance.IsInputLocked) return false;
            return true;
        }

        private void SetVisualsActive(bool active)
        {
            if (_arcLineRenderer != null) _arcLineRenderer.enabled = active;
            if (_minLimitLineRenderer != null) _minLimitLineRenderer.enabled = active;
            if (_maxLimitLineRenderer != null) _maxLimitLineRenderer.enabled = active;
            if (_currentAimLineRenderer != null) _currentAimLineRenderer.enabled = active;
        }

        private void HandleAngleChanged(float angle)
        {
            if (ShouldDrawArc())
            {
                UpdateArcVisuals();
            }
        }

        private void HandleInputStateChanged(bool isLocked)
        {
            UpdateVisibilityAndPosition();
        }

        private void HandleTurnPlayerChanged(int turnNum, int activePlayerNum)
        {
            UpdateVisibilityAndPosition();
        }

        public void UpdateArcVisuals()
        {
            if (!_isInitialized)
            {
                CreateLineRenderers();
            }

            if (_turretAim == null) ResolveReferences();
            if (_turretAim == null) return;

            Vector3 pivotPos = _turretAim.GetFirePointPosition();
            float minAngle = _turretAim.MinAngle;
            float maxAngle = _turretAim.MaxAngle;
            float currentAngle = _turretAim.CurrentAngle;

            int facingDir = _playerController != null ? _playerController.FacingDirection : 1;

            // Draw Curved Arc connecting MinAngle to MaxAngle
            if (_arcLineRenderer != null)
            {
                _arcLineRenderer.positionCount = _arcSegments + 1;
                for (int i = 0; i <= _arcSegments; i++)
                {
                    float t = (float)i / _arcSegments;
                    float angleDeg = Mathf.Lerp(minAngle, maxAngle, t);
                    float rad = angleDeg * Mathf.Deg2Rad;

                    Vector3 offset = new Vector3(
                        Mathf.Cos(rad) * _arcRadius * facingDir,
                        Mathf.Sin(rad) * _arcRadius,
                        0f
                    );

                    _arcLineRenderer.SetPosition(i, pivotPos + offset);
                }
            }

            // Draw Min Angle Limit Ray
            if (_minLimitLineRenderer != null)
            {
                float minRad = minAngle * Mathf.Deg2Rad;
                Vector3 minDir = new Vector3(Mathf.Cos(minRad) * facingDir, Mathf.Sin(minRad), 0f).normalized;
                _minLimitLineRenderer.positionCount = 2;
                _minLimitLineRenderer.SetPosition(0, pivotPos);
                _minLimitLineRenderer.SetPosition(1, pivotPos + minDir * _minMaxLineLength);
            }

            // Draw Max Angle Limit Ray
            if (_maxLimitLineRenderer != null)
            {
                float maxRad = maxAngle * Mathf.Deg2Rad;
                Vector3 maxDir = new Vector3(Mathf.Cos(maxRad) * facingDir, Mathf.Sin(maxRad), 0f).normalized;
                _maxLimitLineRenderer.positionCount = 2;
                _maxLimitLineRenderer.SetPosition(0, pivotPos);
                _maxLimitLineRenderer.SetPosition(1, pivotPos + maxDir * _minMaxLineLength);
            }

            // Draw Current Aim Ray
            if (_currentAimLineRenderer != null)
            {
                float currentRad = currentAngle * Mathf.Deg2Rad;
                Vector3 currentDir = new Vector3(Mathf.Cos(currentRad) * facingDir, Mathf.Sin(currentRad), 0f).normalized;
                _currentAimLineRenderer.positionCount = 2;
                _currentAimLineRenderer.SetPosition(0, pivotPos);
                _currentAimLineRenderer.SetPosition(1, pivotPos + currentDir * _currentAimLineLength);
            }
        }
    }
}
