using System.Collections.Generic;
using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Central manager for the submarine's environmental state.
    /// Tracks power, hull integrity, oxygen, and controls ambient effects.
    /// </summary>
    public class SubmarineManager : MonoBehaviour
    {
        public static SubmarineManager Instance { get; private set; }

        [Header("Submarine State")]
        [SerializeField] private float powerLevel = GameConstants.DEFAULT_POWER_LEVEL;
        [SerializeField] private float hullIntegrity = GameConstants.DEFAULT_HULL_INTEGRITY;
        [SerializeField] private float oxygenLevel = GameConstants.DEFAULT_OXYGEN_LEVEL;

        [Header("Depletion Rates")]
        [SerializeField] private float oxygenDepletionRate = GameConstants.OXYGEN_DEPLETION_RATE;
        [SerializeField] private float powerDepletionRate = 0.1f;
        [SerializeField] private bool oxygenDepleting = true;
        [SerializeField] private bool powerDepleting = true;

        [Header("Critical Thresholds")]
        [SerializeField] private float criticalThreshold = 20f;

        [Header("Ambient Effects")]
        [SerializeField] private List<Light> submarineLights = new();
        [SerializeField] private AudioSource creakingAudioSource;
        [SerializeField] private AudioClip[] creakingSounds;
        [SerializeField] private float creakInterval = 15f;
        [SerializeField] private float creakIntervalVariance = 10f;

        [Header("Emergency")]
        [SerializeField] private List<Light> emergencyLights = new();
        [SerializeField] private Color emergencyColor = new Color(1f, 0.2f, 0f, 1f);
        [SerializeField] private AudioSource alarmAudioSource;

        private float _nextCreakTime;
        private bool _isInCriticalState;

        public float PowerLevel => powerLevel;
        public float HullIntegrity => hullIntegrity;
        public float OxygenLevel => oxygenLevel;
        public bool IsCritical => _isInCriticalState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            _nextCreakTime = Time.time + creakInterval;
        }

        private void Update()
        {
            if (GameManager.Instance?.CurrentState != GameState.InGame) return;

            UpdateResources();
            UpdateAmbientEffects();
            CheckCriticalState();
        }

        // --- Resource Management ---

        private void UpdateResources()
        {
            if (oxygenDepleting)
            {
                oxygenLevel -= oxygenDepletionRate * Time.deltaTime;
                oxygenLevel = Mathf.Max(0f, oxygenLevel);
                GameEvents.FireOxygenChanged(oxygenLevel);
            }

            if (powerDepleting)
            {
                powerLevel -= powerDepletionRate * Time.deltaTime;
                powerLevel = Mathf.Max(0f, powerLevel);
                GameEvents.FirePowerLevelChanged(powerLevel);

                // Adjust lights based on power level
                UpdateLightIntensity();
            }
        }

        /// <summary>
        /// Restore oxygen (e.g., oxygen generator repaired).
        /// </summary>
        public void RestoreOxygen(float amount)
        {
            oxygenLevel = Mathf.Min(oxygenLevel + amount, GameConstants.DEFAULT_OXYGEN_LEVEL);
            GameEvents.FireOxygenChanged(oxygenLevel);
        }

        /// <summary>
        /// Restore power (e.g., generator repaired).
        /// </summary>
        public void RestorePower(float amount)
        {
            powerLevel = Mathf.Min(powerLevel + amount, GameConstants.DEFAULT_POWER_LEVEL);
            GameEvents.FirePowerLevelChanged(powerLevel);
        }

        /// <summary>
        /// Damage the hull (e.g., from external pressure, collision).
        /// </summary>
        public void DamageHull(float amount)
        {
            hullIntegrity -= amount;
            hullIntegrity = Mathf.Max(0f, hullIntegrity);
            GameEvents.FireHullIntegrityChanged(hullIntegrity);
            Debug.Log($"[Submarine] Hull integrity: {hullIntegrity:F1}%");

            if (hullIntegrity <= 0f)
            {
                OnHullBreached();
            }
        }

        /// <summary>
        /// Repair the hull.
        /// </summary>
        public void RepairHull(float amount)
        {
            hullIntegrity = Mathf.Min(hullIntegrity + amount, GameConstants.DEFAULT_HULL_INTEGRITY);
            GameEvents.FireHullIntegrityChanged(hullIntegrity);
        }

        /// <summary>
        /// Set the oxygen depletion rate (e.g., phase-dependent).
        /// </summary>
        public void SetOxygenDepletionRate(float rate)
        {
            oxygenDepletionRate = rate;
        }

        // --- Ambient Effects ---

        private void UpdateLightIntensity()
        {
            float lightMultiplier = Mathf.Clamp01(powerLevel / GameConstants.DEFAULT_POWER_LEVEL);

            foreach (var light in submarineLights)
            {
                if (light != null)
                {
                    light.intensity = lightMultiplier;

                    // Add flicker when power is low
                    if (powerLevel < criticalThreshold)
                    {
                        float flicker = Random.Range(0.5f, 1f);
                        light.intensity *= flicker;
                    }
                }
            }
        }

        private void UpdateAmbientEffects()
        {
            // Play creaking sounds at intervals
            if (Time.time >= _nextCreakTime)
            {
                PlayCreakingSound();
                _nextCreakTime = Time.time + creakInterval + Random.Range(-creakIntervalVariance, creakIntervalVariance);
            }
        }

        private void PlayCreakingSound()
        {
            if (creakingAudioSource == null || creakingSounds == null || creakingSounds.Length == 0)
                return;

            AudioClip clip = creakingSounds[Random.Range(0, creakingSounds.Length)];
            creakingAudioSource.PlayOneShot(clip);
        }

        private void CheckCriticalState()
        {
            bool wasCritical = _isInCriticalState;
            _isInCriticalState = oxygenLevel < criticalThreshold ||
                                  powerLevel < criticalThreshold ||
                                  hullIntegrity < criticalThreshold;

            if (_isInCriticalState && !wasCritical)
            {
                OnEnterCriticalState();
            }
            else if (!_isInCriticalState && wasCritical)
            {
                OnExitCriticalState();
            }
        }

        private void OnEnterCriticalState()
        {
            Debug.Log("[Submarine] CRITICAL STATE entered!");
            GameEvents.FireSubmarineCritical();

            // Activate emergency lights
            foreach (var light in emergencyLights)
            {
                if (light != null)
                {
                    light.color = emergencyColor;
                    light.enabled = true;
                }
            }

            // Play alarm
            if (alarmAudioSource != null && !alarmAudioSource.isPlaying)
            {
                alarmAudioSource.Play();
            }
        }

        private void OnExitCriticalState()
        {
            Debug.Log("[Submarine] Critical state resolved");

            foreach (var light in emergencyLights)
            {
                if (light != null) light.enabled = false;
            }

            if (alarmAudioSource != null)
            {
                alarmAudioSource.Stop();
            }
        }

        private void OnHullBreached()
        {
            Debug.Log("[Submarine] HULL BREACHED — game over imminent");
            // Accelerate oxygen depletion
            oxygenDepletionRate *= 5f;
            // TODO: Trigger flooding VFX, screen effects
        }
    }
}
