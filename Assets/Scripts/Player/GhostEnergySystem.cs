using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// Manages the Ghost's energy resource. All Ghost abilities cost energy.
    /// Energy regenerates passively over time.
    /// </summary>
    public class GhostEnergySystem : MonoBehaviour
    {
        [Header("Energy Settings")]
        [SerializeField] private float maxEnergy = GameConstants.DEFAULT_MAX_ENERGY;
        [SerializeField] private float currentEnergy;
        [SerializeField] private float regenRate = GameConstants.DEFAULT_ENERGY_REGEN_RATE;
        [SerializeField] private float regenDelay = 2f; // seconds after spending before regen resumes

        [Header("Debug")]
        [SerializeField] private bool infiniteEnergy = false;

        private float _regenCooldown;

        public float CurrentEnergy => currentEnergy;
        public float MaxEnergy => maxEnergy;
        public float NormalizedEnergy => currentEnergy / maxEnergy;
        public bool HasEnergy => currentEnergy > 0f;

        private void Start()
        {
            currentEnergy = maxEnergy;
            GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
        }

        private void Update()
        {
            if (_regenCooldown > 0f)
            {
                _regenCooldown -= Time.deltaTime;
                return;
            }

            // Passive regeneration
            if (currentEnergy < maxEnergy)
            {
                currentEnergy += regenRate * Time.deltaTime;
                currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
                GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
            }
        }

        /// <summary>
        /// Attempt to spend energy. Returns true if successful.
        /// </summary>
        public bool TrySpendEnergy(float amount)
        {
            if (infiniteEnergy) return true;

            if (currentEnergy < amount)
            {
                Debug.Log($"[Energy] Not enough energy: {currentEnergy:F1}/{amount:F1} required");
                GameEvents.FireEnergyDepleted();
                return false;
            }

            currentEnergy -= amount;
            _regenCooldown = regenDelay;
            GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
            Debug.Log($"[Energy] Spent {amount:F1}. Remaining: {currentEnergy:F1}/{maxEnergy:F1}");
            return true;
        }

        /// <summary>
        /// Add energy (e.g., from a power source or collectible).
        /// </summary>
        public void AddEnergy(float amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
            GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
        }

        /// <summary>
        /// Set the max energy (e.g., from progression upgrades).
        /// </summary>
        public void SetMaxEnergy(float newMax)
        {
            maxEnergy = newMax;
            currentEnergy = Mathf.Min(currentEnergy, maxEnergy);
            GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
        }

        /// <summary>
        /// Set the regen rate (e.g., phase-dependent).
        /// </summary>
        public void SetRegenRate(float rate)
        {
            regenRate = rate;
        }

        /// <summary>
        /// Check if the Ghost has enough energy for an action without spending it.
        /// </summary>
        public bool CanAfford(float amount)
        {
            return infiniteEnergy || currentEnergy >= amount;
        }

        /// <summary>
        /// Fully restore energy.
        /// </summary>
        public void FullRestore()
        {
            currentEnergy = maxEnergy;
            _regenCooldown = 0f;
            GameEvents.FireEnergyChanged(currentEnergy, maxEnergy);
        }
    }
}
