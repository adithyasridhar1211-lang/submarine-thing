using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;
using SubmarineCoop.Player;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Spectral Anchor that the Ghost can place to extend tether range.
    /// Costs energy to deploy. Visible only to the Ghost.
    /// When activated, becomes the new tether origin instead of the Survivor.
    /// </summary>
    public class SpectralAnchor : MonoBehaviour
    {
        [Header("Anchor Settings")]
        [SerializeField] private float energyCost = GameConstants.ENERGY_COST_SPECTRAL_ANCHOR;
        [SerializeField] private float additionalTetherRange = 5f;
        [SerializeField] private bool isActivated = false;

        [Header("Visuals (Ghost Only)")]
        [SerializeField] private GlowableObject glowable;
        [SerializeField] private ParticleSystem anchorParticles;
        [SerializeField] private Color anchorColor = new Color(0.4f, 0.8f, 1f, 0.8f);
        [SerializeField] private float glowIntensity = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip deploySound;
        [SerializeField] private AudioClip activateSound;

        private AudioSource _audioSource;

        public bool IsActivated => isActivated;
        public float AdditionalRange => additionalTetherRange;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }

            if (glowable == null) glowable = GetComponent<GlowableObject>();
        }

        private void Start()
        {
            // Start deactivated
            SetVisualState(false);
        }

        /// <summary>
        /// Attempt to activate this anchor. Ghost must be nearby and have enough energy.
        /// </summary>
        public bool TryActivate(GhostEnergySystem energySystem, TetherSystem tetherSystem)
        {
            if (isActivated)
            {
                Debug.Log("[SpectralAnchor] Already activated");
                return false;
            }

            if (energySystem != null && !energySystem.TrySpendEnergy(energyCost))
            {
                Debug.Log("[SpectralAnchor] Not enough energy");
                return false;
            }

            Activate(tetherSystem);
            return true;
        }

        /// <summary>
        /// Activate the anchor and set it as the tether origin.
        /// </summary>
        public void Activate(TetherSystem tetherSystem)
        {
            isActivated = true;
            SetVisualState(true);

            if (tetherSystem != null)
            {
                tetherSystem.SetTetherAnchor(transform);
                tetherSystem.SetMaxDistance(tetherSystem.MaxDistance + additionalTetherRange);
            }

            if (_audioSource != null && activateSound != null)
            {
                _audioSource.PlayOneShot(activateSound);
            }

            Debug.Log($"[SpectralAnchor] Activated at {transform.position}");
        }

        /// <summary>
        /// Deactivate the anchor and return tether to Survivor.
        /// </summary>
        public void Deactivate(TetherSystem tetherSystem)
        {
            isActivated = false;
            SetVisualState(false);

            if (tetherSystem != null)
            {
                tetherSystem.SetTetherAnchor(null);
                tetherSystem.SetMaxDistance(tetherSystem.MaxDistance - additionalTetherRange);
            }

            Debug.Log("[SpectralAnchor] Deactivated");
        }

        private void SetVisualState(bool active)
        {
            if (glowable != null)
            {
                if (active)
                    glowable.SetGlow(anchorColor, glowIntensity);
                else
                    glowable.ClearGlow();
            }

            if (anchorParticles != null)
            {
                if (active)
                    anchorParticles.Play();
                else
                    anchorParticles.Stop();
            }
        }
    }
}
