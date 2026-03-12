using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// Manages per-role post-processing vision effects.
    /// Survivor: desaturated (grayscale/sepia) — impaired perception.
    /// Ghost: high-contrast, saturated, bloom — total perception.
    /// </summary>
    public class VisionSystem : MonoBehaviour
    {
        [Header("Volume Profile")]
        [SerializeField] private Volume postProcessVolume;

        [Header("Survivor Vision")]
        [SerializeField] private float survivorSaturation = -80f;
        [SerializeField] private Color survivorColorFilter = new Color(0.9f, 0.85f, 0.7f, 1f); // Sepia tint
        [SerializeField] private float survivorContrast = -10f;
        [SerializeField] private float survivorVignette = 0.35f;

        [Header("Ghost Vision")]
        [SerializeField] private float ghostSaturation = 30f;
        [SerializeField] private float ghostBloomIntensity = 1.5f;
        [SerializeField] private float ghostBloomThreshold = 0.8f;
        [SerializeField] private float ghostChromaticAberration = 0.15f;

        private ColorAdjustments _colorAdjustments;
        private Bloom _bloom;
        private Vignette _vignette;
        private ChromaticAberration _chromaticAberration;

        private PlayerRoleType _currentRole = PlayerRoleType.None;

        private void Awake()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = GetComponent<Volume>();
            }

            // Create a runtime volume profile so we don't modify the shared asset
            if (postProcessVolume != null)
            {
                postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                InitializeOverrides();
            }
        }

        private void InitializeOverrides()
        {
            var profile = postProcessVolume.profile;

            _colorAdjustments = profile.Add<ColorAdjustments>(true);
            _bloom = profile.Add<Bloom>(true);
            _vignette = profile.Add<Vignette>(true);
            _chromaticAberration = profile.Add<ChromaticAberration>(true);

            // Start with everything disabled
            _colorAdjustments.active = false;
            _bloom.active = false;
            _vignette.active = false;
            _chromaticAberration.active = false;
        }

        /// <summary>
        /// Apply the appropriate vision filter for the given role.
        /// </summary>
        public void ApplyRoleVision(PlayerRoleType role)
        {
            _currentRole = role;

            switch (role)
            {
                case PlayerRoleType.Survivor:
                    ApplySurvivorVision();
                    break;
                case PlayerRoleType.Ghost:
                    ApplyGhostVision();
                    break;
                default:
                    ClearVision();
                    break;
            }

            Debug.Log($"[VisionSystem] Applied {role} vision profile");
        }

        /// <summary>
        /// Temporarily override vision (e.g., for Echo Sight ability).
        /// Call RestoreVision() to revert.
        /// </summary>
        public void TemporaryOverride(float saturation, float bloom, float duration)
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.saturation.Override(saturation);
            }
            if (_bloom != null)
            {
                _bloom.intensity.Override(bloom);
            }

            // Auto-restore after duration
            if (duration > 0f)
            {
                Invoke(nameof(RestoreVision), duration);
            }
        }

        /// <summary>
        /// Restore the vision to the current role's default.
        /// </summary>
        public void RestoreVision()
        {
            ApplyRoleVision(_currentRole);
        }

        // --- Role-specific vision setups ---

        private void ApplySurvivorVision()
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = true;
                _colorAdjustments.saturation.Override(survivorSaturation);
                _colorAdjustments.colorFilter.Override(survivorColorFilter);
                _colorAdjustments.contrast.Override(survivorContrast);
            }

            if (_vignette != null)
            {
                _vignette.active = true;
                _vignette.intensity.Override(survivorVignette);
                _vignette.color.Override(Color.black);
            }

            if (_bloom != null)
            {
                _bloom.active = false;
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = false;
            }
        }

        private void ApplyGhostVision()
        {
            if (_colorAdjustments != null)
            {
                _colorAdjustments.active = true;
                _colorAdjustments.saturation.Override(ghostSaturation);
                _colorAdjustments.colorFilter.Override(Color.white);
                _colorAdjustments.contrast.Override(15f);
            }

            if (_bloom != null)
            {
                _bloom.active = true;
                _bloom.intensity.Override(ghostBloomIntensity);
                _bloom.threshold.Override(ghostBloomThreshold);
            }

            if (_chromaticAberration != null)
            {
                _chromaticAberration.active = true;
                _chromaticAberration.intensity.Override(ghostChromaticAberration);
            }

            if (_vignette != null)
            {
                _vignette.active = false;
            }
        }

        private void ClearVision()
        {
            if (_colorAdjustments != null) _colorAdjustments.active = false;
            if (_bloom != null) _bloom.active = false;
            if (_vignette != null) _vignette.active = false;
            if (_chromaticAberration != null) _chromaticAberration.active = false;
        }
    }
}
