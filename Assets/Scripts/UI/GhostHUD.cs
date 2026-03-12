using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.UI
{
    /// <summary>
    /// HUD for the Ghost player (world-space canvas).
    /// Displays energy meter, active ability, tether distance, and Ghost level.
    /// </summary>
    public class GhostHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas hudCanvas;

        [Header("Energy")]
        [SerializeField] private Slider energyBar;
        [SerializeField] private Image energyFill;
        [SerializeField] private TextMeshProUGUI energyText;
        [SerializeField] private Color energyFullColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Color energyLowColor = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private Color energyEmptyColor = new Color(0.5f, 0.2f, 0.2f);
        [SerializeField] private float energyLowThreshold = 0.25f;

        [Header("Tether")]
        [SerializeField] private Slider tetherBar;
        [SerializeField] private Image tetherFill;
        [SerializeField] private TextMeshProUGUI tetherText;
        [SerializeField] private Color tetherNormalColor = new Color(0.5f, 0.7f, 1f);
        [SerializeField] private Color tetherWarningColor = new Color(1f, 0.5f, 0.2f);

        [Header("Level & Abilities")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Image[] abilityIcons; // Index 0=Point, 1=Possess, 2=EchoSight
        [SerializeField] private TextMeshProUGUI activeAbilityText;

        [Header("Ability Cooldowns")]
        [SerializeField] private Image possessionCooldownFill;
        [SerializeField] private Image echoSightCooldownFill;

        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private CanvasGroup statusGroup;

        private float _statusTimer;

        private void OnEnable()
        {
            GameEvents.OnEnergyChanged += UpdateEnergy;
            GameEvents.OnTetherDistanceChanged += UpdateTether;
            GameEvents.OnGhostLevelUp += UpdateLevel;
            GameEvents.OnTetherWarning += OnTetherWarning;
            GameEvents.OnEnergyDepleted += OnEnergyDepleted;
        }

        private void OnDisable()
        {
            GameEvents.OnEnergyChanged -= UpdateEnergy;
            GameEvents.OnTetherDistanceChanged -= UpdateTether;
            GameEvents.OnGhostLevelUp -= UpdateLevel;
            GameEvents.OnTetherWarning -= OnTetherWarning;
            GameEvents.OnEnergyDepleted -= OnEnergyDepleted;
        }

        private void Update()
        {
            // Fade status messages
            if (_statusTimer > 0)
            {
                _statusTimer -= Time.deltaTime;
                if (statusGroup != null)
                {
                    statusGroup.alpha = _statusTimer > 0.5f ? 1f : _statusTimer * 2f;
                }
            }
        }

        /// <summary>
        /// Update energy bar display.
        /// </summary>
        private void UpdateEnergy(float current, float max)
        {
            float normalized = current / max;

            if (energyBar != null) energyBar.value = normalized;
            if (energyText != null) energyText.text = $"{current:F0}/{max:F0}";
            if (energyFill != null)
            {
                if (normalized <= 0f)
                    energyFill.color = energyEmptyColor;
                else if (normalized < energyLowThreshold)
                    energyFill.color = energyLowColor;
                else
                    energyFill.color = Color.Lerp(energyLowColor, energyFullColor, normalized);
            }
        }

        /// <summary>
        /// Update tether distance display.
        /// </summary>
        private void UpdateTether(float normalized)
        {
            if (tetherBar != null) tetherBar.value = 1f - normalized; // Invert so bar reduces as distance grows
            if (tetherText != null) tetherText.text = $"{(1f - normalized) * 100f:F0}%";
            if (tetherFill != null)
            {
                tetherFill.color = normalized > GameConstants.TETHER_WARNING_THRESHOLD
                    ? tetherWarningColor
                    : tetherNormalColor;
            }
        }

        /// <summary>
        /// Update Ghost level display and unlock ability icons.
        /// </summary>
        private void UpdateLevel(int level)
        {
            if (levelText != null) levelText.text = $"Level {level}";

            if (abilityIcons != null)
            {
                for (int i = 0; i < abilityIcons.Length; i++)
                {
                    if (abilityIcons[i] != null)
                    {
                        abilityIcons[i].color = i < level
                            ? Color.white
                            : new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }
            }

            ShowStatus($"Level {level} Unlocked!");
        }

        /// <summary>
        /// Update ability cooldown displays.
        /// </summary>
        public void UpdateCooldowns(float possessionNormalized, float echoSightNormalized)
        {
            if (possessionCooldownFill != null)
                possessionCooldownFill.fillAmount = possessionNormalized;
            if (echoSightCooldownFill != null)
                echoSightCooldownFill.fillAmount = echoSightNormalized;
        }

        /// <summary>
        /// Show the currently active ability name.
        /// </summary>
        public void SetActiveAbility(string abilityName)
        {
            if (activeAbilityText != null)
                activeAbilityText.text = abilityName ?? "";
        }

        /// <summary>
        /// Show a temporary status message.
        /// </summary>
        public void ShowStatus(string message, float duration = 2f)
        {
            if (statusText != null) statusText.text = message;
            _statusTimer = duration;
            if (statusGroup != null) statusGroup.alpha = 1f;
        }

        private void OnTetherWarning()
        {
            ShowStatus("⚠ Tether limit approaching!", 1.5f);
        }

        private void OnEnergyDepleted()
        {
            ShowStatus("⚡ Energy depleted!", 1.5f);
        }
    }
}
