using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Hazard zone that damages the Survivor or blocks paths.
    /// Ghost can see hazards clearly with danger glow; Survivor cannot see them.
    /// Examples: electrical arcs, flooded corridors, gas leaks, broken glass.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        [Header("Hazard Settings")]
        [SerializeField] private HazardType hazardType = HazardType.Electrical;
        [SerializeField] private float damagePerSecond = 10f;
        [SerializeField] private bool isActive = true;
        [SerializeField] private bool blocksPath = false;

        [Header("Ghost Visibility")]
        [SerializeField] private GlowableObject glowable;
        [SerializeField] private Color hazardColor = GameConstants.COLOR_DANGER;
        [SerializeField] private float hazardGlowIntensity = 3f;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem hazardParticles;
        [SerializeField] private GameObject hazardVisualEffect; // Only visible to Ghost

        [Header("Audio")]
        [SerializeField] private AudioSource ambientSound;
        [SerializeField] private AudioClip damageSound;

        private Collider _collider;

        public bool IsActive => isActive;
        public HazardType Type => hazardType;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;

            if (glowable == null) glowable = GetComponent<GlowableObject>();
        }

        private void Start()
        {
            SetHazardActive(isActive);
        }

        /// <summary>
        /// Enable or disable the hazard.
        /// </summary>
        public void SetHazardActive(bool active)
        {
            isActive = active;
            _collider.enabled = active;

            if (glowable != null)
            {
                if (active)
                    glowable.SetGlow(hazardColor, hazardGlowIntensity);
                else
                    glowable.ClearGlow();
            }

            if (hazardParticles != null)
            {
                if (active) hazardParticles.Play();
                else hazardParticles.Stop();
            }

            if (hazardVisualEffect != null)
            {
                hazardVisualEffect.SetActive(active);
            }

            if (ambientSound != null)
            {
                if (active) ambientSound.Play();
                else ambientSound.Stop();
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!isActive) return;

            // Only damage the Survivor
            if (other.CompareTag(GameConstants.SURVIVOR_TAG) ||
                other.transform.root.CompareTag(GameConstants.SURVIVOR_TAG))
            {
                float damage = damagePerSecond * Time.deltaTime;
                GameEvents.FirePlayerDamaged(other.transform.root.gameObject, damage);
                GameEvents.FireHazardTriggered(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive) return;

            if (other.CompareTag(GameConstants.SURVIVOR_TAG) ||
                other.transform.root.CompareTag(GameConstants.SURVIVOR_TAG))
            {
                Debug.Log($"[Hazard] Survivor entered {hazardType} hazard: {name}");

                if (damageSound != null && ambientSound != null)
                {
                    ambientSound.PlayOneShot(damageSound);
                }
            }
        }
    }

    public enum HazardType
    {
        Electrical,
        Flooding,
        GasLeak,
        BrokenGlass,
        Pressure,
        Fire,
        Radiation,
        Custom
    }
}
