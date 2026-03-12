using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// Tether system constraining the Ghost to a maximum distance from the Survivor.
    /// Visual line renderer between the two, with haptic warnings near the limit.
    /// Spectral Anchors can relocate the tether origin.
    /// </summary>
    public class TetherSystem : MonoBehaviour
    {
        [Header("Tether Settings")]
        [SerializeField] private float maxDistance = GameConstants.DEFAULT_TETHER_DISTANCE;
        [SerializeField] private float warningThreshold = GameConstants.TETHER_WARNING_THRESHOLD;
        [SerializeField] private float snapForce = GameConstants.TETHER_SNAP_FORCE;

        [Header("References")]
        [SerializeField] private Transform ghostTransform;
        [SerializeField] private Transform survivorTransform;
        [SerializeField] private LineRenderer tetherLineRenderer;

        [Header("Visuals")]
        [SerializeField] private Gradient normalGradient;
        [SerializeField] private Gradient warningGradient;
        [SerializeField] private int lineSegments = 20;
        [SerializeField] private float lineSag = 0.5f;
        [SerializeField] private float lineWidth = 0.02f;

        [Header("Spectral Anchor")]
        [SerializeField] private Transform activeTetherAnchor; // null = Survivor is anchor

        private float _currentDistance;
        private float _normalizedDistance; // 0-1
        private bool _isWarning;

        public float CurrentDistance => _currentDistance;
        public float NormalizedDistance => _normalizedDistance;
        public float MaxDistance => maxDistance;
        public bool IsWarning => _isWarning;

        private void Start()
        {
            SetupLineRenderer();

            // Try to auto-find players if not assigned
            if (survivorTransform == null && GameManager.Instance?.SurvivorPlayer != null)
            {
                survivorTransform = GameManager.Instance.SurvivorPlayer.transform;
            }
            if (ghostTransform == null)
            {
                ghostTransform = transform;
            }
        }

        private void Update()
        {
            if (ghostTransform == null || GetAnchorPosition() == Vector3.zero) return;

            UpdateTetherDistance();
            UpdateTetherVisuals();
            EnforceTetherConstraint();
        }

        /// <summary>
        /// Set the Survivor transform (called during player spawning).
        /// </summary>
        public void SetSurvivor(Transform survivor)
        {
            survivorTransform = survivor;
        }

        /// <summary>
        /// Set a Spectral Anchor as the new tether origin.
        /// Pass null to reset back to the Survivor.
        /// </summary>
        public void SetTetherAnchor(Transform anchor)
        {
            activeTetherAnchor = anchor;
            Debug.Log(anchor != null
                ? $"[Tether] Anchor set to {anchor.name}"
                : "[Tether] Anchor reset to Survivor");
        }

        /// <summary>
        /// Extend the max tether distance (e.g., phase progression).
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            maxDistance = distance;
            Debug.Log($"[Tether] Max distance set to {maxDistance}");
        }

        // --- Internal ---

        private Vector3 GetAnchorPosition()
        {
            if (activeTetherAnchor != null)
                return activeTetherAnchor.position;
            if (survivorTransform != null)
                return survivorTransform.position;
            return Vector3.zero;
        }

        private void UpdateTetherDistance()
        {
            Vector3 anchorPos = GetAnchorPosition();
            _currentDistance = Vector3.Distance(ghostTransform.position, anchorPos);
            _normalizedDistance = Mathf.Clamp01(_currentDistance / maxDistance);

            bool wasWarning = _isWarning;
            _isWarning = _normalizedDistance >= warningThreshold;

            if (_isWarning && !wasWarning)
            {
                GameEvents.FireTetherWarning();
            }

            GameEvents.FireTetherDistanceChanged(_normalizedDistance);
        }

        private void EnforceTetherConstraint()
        {
            if (_currentDistance > maxDistance)
            {
                Vector3 anchorPos = GetAnchorPosition();
                Vector3 direction = (anchorPos - ghostTransform.position).normalized;
                float overshoot = _currentDistance - maxDistance;

                // Pull the Ghost back towards the anchor
                ghostTransform.position += direction * (overshoot + snapForce * Time.deltaTime);

                GameEvents.FireTetherSnapped();
            }
        }

        private void UpdateTetherVisuals()
        {
            if (tetherLineRenderer == null) return;

            Vector3 start = ghostTransform.position;
            Vector3 end = GetAnchorPosition();

            tetherLineRenderer.positionCount = lineSegments;
            tetherLineRenderer.colorGradient = _isWarning ? warningGradient : normalGradient;

            for (int i = 0; i < lineSegments; i++)
            {
                float t = (float)i / (lineSegments - 1);
                Vector3 point = Vector3.Lerp(start, end, t);

                // Add catenary sag effect
                float sag = Mathf.Sin(t * Mathf.PI) * lineSag * _normalizedDistance;
                point.y -= sag;

                tetherLineRenderer.SetPosition(i, point);
            }
        }

        private void SetupLineRenderer()
        {
            if (tetherLineRenderer == null)
            {
                tetherLineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            tetherLineRenderer.startWidth = lineWidth;
            tetherLineRenderer.endWidth = lineWidth * 0.5f;
            tetherLineRenderer.positionCount = lineSegments;
            tetherLineRenderer.useWorldSpace = true;

            // Default gradients if not set
            if (normalGradient == null || normalGradient.colorKeys.Length == 0)
            {
                normalGradient = new Gradient();
                normalGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(GameConstants.COLOR_TETHER, 0f),
                        new GradientColorKey(GameConstants.COLOR_TETHER * 0.5f, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.6f, 0f),
                        new GradientAlphaKey(0.2f, 1f)
                    }
                );
            }

            if (warningGradient == null || warningGradient.colorKeys.Length == 0)
            {
                warningGradient = new Gradient();
                warningGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(GameConstants.COLOR_DANGER, 0f),
                        new GradientColorKey(Color.yellow, 1f)
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0.8f, 0f),
                        new GradientAlphaKey(0.4f, 1f)
                    }
                );
            }
        }
    }
}
