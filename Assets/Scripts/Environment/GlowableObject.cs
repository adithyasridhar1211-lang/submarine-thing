using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Utility component that gives any object the ability to glow in role-specific colors.
    /// Used by Spirit Tag, interactables, mementos, and hazard markers.
    /// Works with URP Lit/Unlit shaders via emission.
    /// </summary>
    public class GlowableObject : MonoBehaviour
    {
        [Header("Glow Settings")]
        [SerializeField] private GlowColorType glowType = GlowColorType.Interaction;
        [SerializeField] private Color customGlowColor = Color.white;
        [SerializeField] private float glowIntensity = 1f;
        [SerializeField] private bool glowOnStart = false;
        [SerializeField] private bool ghostOnlyVisible = true;

        [Header("Animation")]
        [SerializeField] private bool pulsate = true;
        [SerializeField] private float pulsateSpeed = 2f;
        [SerializeField] private float pulsateMinIntensity = 0.3f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Color _currentGlowColor;
        private float _currentIntensity;
        private bool _isGlowing;
        private Color _originalEmission;

        public bool IsGlowing => _isGlowing;
        public GlowColorType GlowType => glowType;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propertyBlock);
                _originalEmission = _propertyBlock.GetColor("_EmissionColor");
            }
        }

        private void Start()
        {
            if (glowOnStart)
            {
                Color color = GetColorForType(glowType);
                SetGlow(color, glowIntensity);
            }
        }

        private void Update()
        {
            if (!_isGlowing || !pulsate) return;

            // Pulsate the glow
            float pulse = Mathf.Lerp(pulsateMinIntensity, 1f,
                (Mathf.Sin(Time.time * pulsateSpeed) + 1f) / 2f);

            ApplyEmission(_currentGlowColor * _currentIntensity * pulse);
        }

        /// <summary>
        /// Activate glow with specified color and intensity.
        /// </summary>
        public void SetGlow(Color color, float intensity)
        {
            _currentGlowColor = color;
            _currentIntensity = intensity;
            _isGlowing = true;

            if (!pulsate)
            {
                ApplyEmission(color * intensity);
            }
        }

        /// <summary>
        /// Activate glow using a preset color type.
        /// </summary>
        public void SetGlow(GlowColorType type, float intensity)
        {
            glowType = type;
            SetGlow(GetColorForType(type), intensity);
        }

        /// <summary>
        /// Clear the glow and restore original emission.
        /// </summary>
        public void ClearGlow()
        {
            _isGlowing = false;
            ApplyEmission(_originalEmission);
        }

        /// <summary>
        /// Set glow visibility based on the local player's role.
        /// Ghost sees all glows; Survivor only sees tagged/non-ghost-only glows.
        /// </summary>
        public void UpdateVisibilityForRole(PlayerRoleType viewerRole)
        {
            if (!_isGlowing) return;

            if (ghostOnlyVisible && viewerRole == PlayerRoleType.Survivor)
            {
                // Hide the glow from Survivor (they see grey/none)
                ApplyEmission(Color.black);
            }
            else
            {
                ApplyEmission(_currentGlowColor * _currentIntensity);
            }
        }

        // --- Internal ---

        private void ApplyEmission(Color emissionColor)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_EmissionColor", emissionColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// Get the standard color for a glow type.
        /// </summary>
        public static Color GetColorForType(GlowColorType type)
        {
            switch (type)
            {
                case GlowColorType.Danger: return GameConstants.COLOR_DANGER;
                case GlowColorType.Interaction: return GameConstants.COLOR_INTERACTION;
                case GlowColorType.Collectible: return GameConstants.COLOR_COLLECTIBLE;
                case GlowColorType.SpiritTag: return GameConstants.COLOR_SPIRIT_TAG;
                case GlowColorType.Custom:
                default: return Color.white;
            }
        }
    }
}
