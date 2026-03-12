using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// The Living Player (Survivor) controller.
    /// Has full physical agency but impaired (desaturated) vision.
    /// Can move objects, use tools, and operate machinery.
    /// Receives haptic feedback from Ghost interactions.
    /// </summary>
    public class SurvivorController : PlayerController
    {
        [Header("Survivor Specific")]
        [SerializeField] private VisionSystem visionSystem;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentOxygen = 100f;

        [Header("Interaction")]
        [SerializeField] private XRBaseInteractor leftGrabInteractor;
        [SerializeField] private XRBaseInteractor rightGrabInteractor;

        [Header("Tool System")]
        [SerializeField] private GameObject equippedTool;

        [Header("Haptic Feedback")]
        [SerializeField] private XRBaseController leftController;
        [SerializeField] private XRBaseController rightController;

        private float _currentHealth;

        public float CurrentHealth => _currentHealth;
        public float CurrentOxygen => currentOxygen;
        public GameObject EquippedTool => equippedTool;

        protected override void Awake()
        {
            base.Awake();
            role = PlayerRoleType.Survivor;
            _currentHealth = maxHealth;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Apply desaturated vision
            if (visionSystem != null)
            {
                visionSystem.ApplyRoleVision(PlayerRoleType.Survivor);
            }

            // Subscribe to events
            GameEvents.OnGhostTap += HandleGhostTap;
            GameEvents.OnOxygenChanged += HandleOxygenChanged;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;

            gameObject.tag = GameConstants.SURVIVOR_TAG;
        }

        protected override void OnUpdate()
        {
            // Oxygen depletion over time
            currentOxygen -= GameConstants.OXYGEN_DEPLETION_RATE * Time.deltaTime;
            currentOxygen = Mathf.Max(0f, currentOxygen);

            if (currentOxygen <= 0f)
            {
                OnOxygenDepleted();
            }
        }

        /// <summary>
        /// Send haptic feedback to the appropriate controller based on direction.
        /// </summary>
        public void SendDirectionalHaptic(Vector3 worldPosition)
        {
            if (headTransform == null) return;

            Vector3 dirToTap = (worldPosition - headTransform.position).normalized;
            Vector3 localDir = headTransform.InverseTransformDirection(dirToTap);

            // Determine which hand should receive stronger haptic
            float leftIntensity = Mathf.Clamp01((-localDir.x + 1f) / 2f); // More to the left = stronger left
            float rightIntensity = Mathf.Clamp01((localDir.x + 1f) / 2f); // More to the right = stronger right

            if (leftController != null)
            {
                leftController.SendHapticImpulse(
                    leftIntensity * GameConstants.HAPTIC_TAP_AMPLITUDE,
                    GameConstants.HAPTIC_TAP_DURATION);
            }

            if (rightController != null)
            {
                rightController.SendHapticImpulse(
                    rightIntensity * GameConstants.HAPTIC_TAP_AMPLITUDE,
                    GameConstants.HAPTIC_TAP_DURATION);
            }
        }

        /// <summary>
        /// Equip a tool for use.
        /// </summary>
        public void EquipTool(GameObject tool)
        {
            equippedTool = tool;
            Debug.Log($"[Survivor] Equipped tool: {tool.name}");
        }

        /// <summary>
        /// Use the currently equipped tool.
        /// </summary>
        public void UseTool()
        {
            if (equippedTool == null)
            {
                Debug.Log("[Survivor] No tool equipped");
                return;
            }
            // Tool usage will be implemented per-tool
            Debug.Log($"[Survivor] Using tool: {equippedTool.name}");
        }

        /// <summary>
        /// Take damage from a hazard or environmental source.
        /// </summary>
        public void TakeDamage(float amount)
        {
            _currentHealth -= amount;
            _currentHealth = Mathf.Max(0f, _currentHealth);
            Debug.Log($"[Survivor] Took {amount} damage. Health: {_currentHealth}/{maxHealth}");

            if (_currentHealth <= 0f)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Restore oxygen (e.g., from an oxygen station).
        /// </summary>
        public void RestoreOxygen(float amount)
        {
            currentOxygen = Mathf.Min(currentOxygen + amount, GameConstants.DEFAULT_OXYGEN_LEVEL);
        }

        // --- Event Handlers ---

        private void HandleGhostTap(Vector3 tapPosition)
        {
            SendDirectionalHaptic(tapPosition);
        }

        private void HandleOxygenChanged(float newOxygen)
        {
            currentOxygen = newOxygen;
        }

        private void HandlePlayerDamaged(GameObject player, float amount)
        {
            if (player == gameObject)
            {
                TakeDamage(amount);
            }
        }

        private void OnOxygenDepleted()
        {
            Debug.Log("[Survivor] Oxygen depleted!");
            // Trigger suffocation effects — vision blur, damage over time
            TakeDamage(Time.deltaTime * 5f);
        }

        private void OnDeath()
        {
            Debug.Log("[Survivor] Player died!");
            GameManager.Instance?.EndGame();
        }

        private void OnDestroy()
        {
            GameEvents.OnGhostTap -= HandleGhostTap;
            GameEvents.OnOxygenChanged -= HandleOxygenChanged;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
        }
    }
}
