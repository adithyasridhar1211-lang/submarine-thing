using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// The Ghost Player (Echo) controller.
    /// Has total perception with high-contrast vision but cannot physically interact
    /// without spending Energy. Tethered to the Survivor.
    /// </summary>
    public class GhostController : PlayerController
    {
        [Header("Ghost Systems")]
        [SerializeField] private VisionSystem visionSystem;
        [SerializeField] private GhostEnergySystem energySystem;
        [SerializeField] private TetherSystem tetherSystem;

        [Header("Ghost Visuals")]
        [SerializeField] private Renderer[] ghostBodyRenderers;
        [SerializeField] private float ghostAlpha = 0.4f;

        [Header("Abilities")]
        [SerializeField] private int currentLevel = 0;
        [SerializeField] private bool canPointTrack = false;
        [SerializeField] private bool canPossess = false;
        [SerializeField] private bool canEchoSight = false;

        [Header("Point Tracking (Level 1)")]
        [SerializeField] private ParticleSystem mistParticleSystem;
        [SerializeField] private LineRenderer pointingLine;

        public int CurrentLevel => currentLevel;
        public GhostEnergySystem Energy => energySystem;
        public TetherSystem Tether => tetherSystem;

        protected override void Awake()
        {
            base.Awake();
            role = PlayerRoleType.Ghost;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Apply spectral high-contrast vision
            if (visionSystem != null)
            {
                visionSystem.ApplyRoleVision(PlayerRoleType.Ghost);
            }

            // Make Ghost body semi-transparent
            SetGhostTransparency();

            // Subscribe to level-up events
            GameEvents.OnGhostLevelUp += HandleLevelUp;

            gameObject.tag = GameConstants.GHOST_TAG;

            // Disable point tracking visuals initially
            if (mistParticleSystem != null) mistParticleSystem.Stop();
            if (pointingLine != null) pointingLine.enabled = false;
        }

        protected override void OnUpdate()
        {
            // Update point tracking visuals if enabled
            if (canPointTrack)
            {
                UpdatePointTracking();
            }
        }

        /// <summary>
        /// Tap a surface at a world position. Sends haptic/audio event to Survivor.
        /// </summary>
        public void TapSurface(Vector3 worldPosition)
        {
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_TAP))
            {
                Debug.Log("[Ghost] Not enough energy to tap");
                return;
            }

            GameEvents.FireGhostTap(worldPosition);
            Debug.Log($"[Ghost] Tapped surface at {worldPosition}");
        }

        /// <summary>
        /// Tag an object to make it briefly glow for the Survivor.
        /// </summary>
        public void TagObject(GameObject target)
        {
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_SPIRIT_TAG))
            {
                Debug.Log("[Ghost] Not enough energy to tag");
                return;
            }

            GameEvents.FireSpiritTagApplied(target, GameConstants.SPIRIT_TAG_DURATION);
            Debug.Log($"[Ghost] Tagged object: {target.name}");
        }

        /// <summary>
        /// Attempt to possess (briefly interact with) a physical object.
        /// Requires Level 2.
        /// </summary>
        public bool TryPossess(GameObject target)
        {
            if (!canPossess)
            {
                Debug.Log("[Ghost] Possession not unlocked yet (requires Level 2)");
                return false;
            }

            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_POSSESSION))
            {
                Debug.Log("[Ghost] Not enough energy for possession");
                return false;
            }

            GameEvents.FirePossessionStarted(target);
            Debug.Log($"[Ghost] Possessing: {target.name}");
            return true;
        }

        /// <summary>
        /// Activate Echo Sight: show the Survivor a brief replay of past events.
        /// Requires Level 3.
        /// </summary>
        public bool TryEchoSight()
        {
            if (!canEchoSight)
            {
                Debug.Log("[Ghost] Echo Sight not unlocked yet (requires Level 3)");
                return false;
            }

            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_POSSESSION))
            {
                Debug.Log("[Ghost] Not enough energy for Echo Sight");
                return false;
            }

            GameEvents.FireEchoSightActivated();
            Debug.Log("[Ghost] Echo Sight activated");
            return true;
        }

        /// <summary>
        /// Level up the Ghost's abilities.
        /// </summary>
        public void LevelUp(int newLevel)
        {
            currentLevel = newLevel;

            switch (newLevel)
            {
                case 1:
                    canPointTrack = true;
                    if (mistParticleSystem != null) mistParticleSystem.Play();
                    Debug.Log("[Ghost] Level 1 unlocked: Point/Hand Tracking (Faint Mist)");
                    break;
                case 2:
                    canPossess = true;
                    Debug.Log("[Ghost] Level 2 unlocked: Possession");
                    break;
                case 3:
                    canEchoSight = true;
                    Debug.Log("[Ghost] Level 3 unlocked: Echo Sight");
                    break;
            }

            GameEvents.FireGhostLevelUp(newLevel);
        }

        // --- Private Methods ---

        private void SetGhostTransparency()
        {
            if (ghostBodyRenderers == null) return;

            foreach (var renderer in ghostBodyRenderers)
            {
                if (renderer == null) continue;

                foreach (var mat in renderer.materials)
                {
                    // Set to transparent rendering mode
                    mat.SetFloat("_Surface", 1); // URP: 0 = Opaque, 1 = Transparent
                    mat.SetFloat("_Blend", 0);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;

                    Color color = mat.color;
                    color.a = ghostAlpha;
                    mat.color = color;
                }
            }
        }

        private void UpdatePointTracking()
        {
            if (pointingLine == null || rightHandTransform == null) return;

            pointingLine.enabled = true;
            pointingLine.SetPosition(0, rightHandTransform.position);
            pointingLine.SetPosition(1, rightHandTransform.position + rightHandTransform.forward * 5f);
        }

        private void HandleLevelUp(int level)
        {
            // This is called from external systems (MementoSystem)
            // Only handle if this is for us
            if (level > currentLevel)
            {
                LevelUp(level);
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnGhostLevelUp -= HandleLevelUp;
        }
    }
}
