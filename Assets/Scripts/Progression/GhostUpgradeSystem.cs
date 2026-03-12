using System.Collections;
using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Player;

namespace SubmarineCoop.Progression
{
    /// <summary>
    /// Manages Ghost ability upgrades triggered by Memento returns.
    /// Level 1: Point/Hand Tracking (faint mist)
    /// Level 2: Possession (briefly interact with physical switches)
    /// Level 3: Echo Sight (visual replay of past events)
    /// </summary>
    public class GhostUpgradeSystem : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GhostUpgradeConfig config;

        [Header("References")]
        [SerializeField] private GhostController ghostController;

        [Header("Level 1 — Point Tracking")]
        [SerializeField] private ParticleSystem mistEffect;

        [Header("Level 2 — Possession")]
        [SerializeField] private float possessionCooldownTimer;
        private bool _isPossessing;
        private GameObject _possessedTarget;

        [Header("Level 3 — Echo Sight")]
        [SerializeField] private float echoSightCooldownTimer;

        private int _currentLevel = 0;

        public int CurrentLevel => _currentLevel;

        private void OnEnable()
        {
            GameEvents.OnGhostLevelUp += HandleLevelUp;
        }

        private void OnDisable()
        {
            GameEvents.OnGhostLevelUp -= HandleLevelUp;
        }

        private void Update()
        {
            // Cooldown timers
            if (possessionCooldownTimer > 0f) possessionCooldownTimer -= Time.deltaTime;
            if (echoSightCooldownTimer > 0f) echoSightCooldownTimer -= Time.deltaTime;
        }

        // --- Level Up ---

        private void HandleLevelUp(int newLevel)
        {
            if (newLevel <= _currentLevel) return;
            _currentLevel = newLevel;

            if (ghostController != null)
            {
                ghostController.LevelUp(newLevel);
            }

            Debug.Log($"[GhostUpgrade] Upgraded to Level {newLevel}");

            switch (newLevel)
            {
                case 1:
                    ActivatePointTracking();
                    break;
                case 2:
                    Debug.Log("[GhostUpgrade] Possession now available");
                    break;
                case 3:
                    Debug.Log("[GhostUpgrade] Echo Sight now available");
                    break;
            }
        }

        // --- Level 1: Point Tracking ---

        private void ActivatePointTracking()
        {
            if (mistEffect != null)
            {
                var main = mistEffect.main;
                main.startColor = new Color(0.8f, 0.9f, 1f, config != null ? config.level1MistAlpha : 0.3f);
                mistEffect.Play();
            }
            Debug.Log("[GhostUpgrade] Point tracking mist activated");
        }

        // --- Level 2: Possession ---

        /// <summary>
        /// Attempt to possess a physical interactable.
        /// </summary>
        public bool TryPossess(GameObject target, GhostEnergySystem energySystem)
        {
            if (_currentLevel < 2)
            {
                Debug.Log("[GhostUpgrade] Possession requires Level 2");
                return false;
            }

            if (possessionCooldownTimer > 0f)
            {
                Debug.Log($"[GhostUpgrade] Possession on cooldown: {possessionCooldownTimer:F1}s");
                return false;
            }

            float cost = config != null ? config.possessionEnergyCost : 25f;
            if (energySystem != null && !energySystem.TrySpendEnergy(cost))
            {
                return false;
            }

            StartCoroutine(PossessionCoroutine(target));
            return true;
        }

        private IEnumerator PossessionCoroutine(GameObject target)
        {
            _isPossessing = true;
            _possessedTarget = target;
            GameEvents.FirePossessionStarted(target);

            float duration = config != null ? config.possessionDuration : 3f;
            Debug.Log($"[GhostUpgrade] Possessing {target.name} for {duration}s");

            // During possession, allow Ghost to interact with the target as if physical
            // The target's InteractableBase temporarily allows Ghost interaction
            var interactable = target.GetComponent<Environment.InteractableBase>();
            if (interactable != null)
            {
                interactable.TryInteract(PlayerRoleType.Ghost, _currentLevel);
            }

            yield return new WaitForSeconds(duration);

            _isPossessing = false;
            _possessedTarget = null;
            GameEvents.FirePossessionEnded();

            float cooldown = config != null ? config.possessionCooldown : 10f;
            possessionCooldownTimer = cooldown;

            Debug.Log("[GhostUpgrade] Possession ended");
        }

        // --- Level 3: Echo Sight ---

        /// <summary>
        /// Activate Echo Sight: show the Survivor a brief visual replay of the room.
        /// </summary>
        public bool TryEchoSight(GhostEnergySystem energySystem, VisionSystem survivorVision)
        {
            if (_currentLevel < 3)
            {
                Debug.Log("[GhostUpgrade] Echo Sight requires Level 3");
                return false;
            }

            if (echoSightCooldownTimer > 0f)
            {
                Debug.Log($"[GhostUpgrade] Echo Sight on cooldown: {echoSightCooldownTimer:F1}s");
                return false;
            }

            float cost = config != null ? config.echoSightEnergyCost : 40f;
            if (energySystem != null && !energySystem.TrySpendEnergy(cost))
            {
                return false;
            }

            StartCoroutine(EchoSightCoroutine(survivorVision));
            return true;
        }

        private IEnumerator EchoSightCoroutine(VisionSystem survivorVision)
        {
            GameEvents.FireEchoSightActivated();

            float duration = config != null ? config.echoSightDuration : 5f;
            Debug.Log($"[GhostUpgrade] Echo Sight active for {duration}s");

            // Temporarily give the Survivor clear (Ghost-like) vision
            if (survivorVision != null)
            {
                survivorVision.TemporaryOverride(0f, 1f, duration);
            }

            yield return new WaitForSeconds(duration);

            float cooldown = config != null ? config.echoSightCooldown : 30f;
            echoSightCooldownTimer = cooldown;

            Debug.Log("[GhostUpgrade] Echo Sight ended");
        }
    }
}
