using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.UI
{
    /// <summary>
    /// HUD for the Survivor player (world-space canvas, wrist or helmet-mounted).
    /// Displays oxygen, health, equipped tool, and prompt text.
    /// </summary>
    public class SurvivorHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas hudCanvas;

        [Header("Oxygen")]
        [SerializeField] private Slider oxygenBar;
        [SerializeField] private Image oxygenFill;
        [SerializeField] private TextMeshProUGUI oxygenText;
        [SerializeField] private Color oxygenNormalColor = new Color(0.2f, 0.7f, 1f);
        [SerializeField] private Color oxygenLowColor = new Color(1f, 0.3f, 0.2f);
        [SerializeField] private float oxygenLowThreshold = 30f;

        [Header("Health")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Image healthFill;
        [SerializeField] private TextMeshProUGUI healthText;

        [Header("Tool")]
        [SerializeField] private Image toolIcon;
        [SerializeField] private TextMeshProUGUI toolNameText;

        [Header("Prompt")]
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private float promptFadeSpeed = 2f;

        [Header("Warning Effects")]
        [SerializeField] private Image warningOverlay;
        [SerializeField] private float warningPulseSpeed = 3f;

        private float _promptTimer;
        private bool _showingPrompt;

        private void OnEnable()
        {
            GameEvents.OnOxygenChanged += UpdateOxygen;
            GameEvents.OnSubmarineCritical += ShowWarning;
        }

        private void OnDisable()
        {
            GameEvents.OnOxygenChanged -= UpdateOxygen;
            GameEvents.OnSubmarineCritical -= ShowWarning;
        }

        private void Update()
        {
            // Fade prompt
            if (_showingPrompt)
            {
                _promptTimer -= Time.deltaTime;
                if (_promptTimer <= 0)
                {
                    _showingPrompt = false;
                }
            }

            if (promptGroup != null)
            {
                float targetAlpha = _showingPrompt ? 1f : 0f;
                promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, targetAlpha, promptFadeSpeed * Time.deltaTime);
            }

            // Warning pulse
            if (warningOverlay != null && warningOverlay.enabled)
            {
                float pulse = (Mathf.Sin(Time.time * warningPulseSpeed) + 1f) / 2f;
                Color c = warningOverlay.color;
                c.a = pulse * 0.3f;
                warningOverlay.color = c;
            }
        }

        /// <summary>
        /// Update the oxygen display.
        /// </summary>
        public void UpdateOxygen(float oxygenLevel)
        {
            float normalized = oxygenLevel / GameConstants.DEFAULT_OXYGEN_LEVEL;

            if (oxygenBar != null) oxygenBar.value = normalized;
            if (oxygenText != null) oxygenText.text = $"{oxygenLevel:F0}%";
            if (oxygenFill != null)
            {
                oxygenFill.color = oxygenLevel < oxygenLowThreshold ? oxygenLowColor : oxygenNormalColor;
            }
        }

        /// <summary>
        /// Update the health display.
        /// </summary>
        public void UpdateHealth(float health, float maxHealth)
        {
            if (healthBar != null) healthBar.value = health / maxHealth;
            if (healthText != null) healthText.text = $"{health:F0}";
        }

        /// <summary>
        /// Update the equipped tool display.
        /// </summary>
        public void UpdateTool(string toolName, Sprite icon = null)
        {
            if (toolNameText != null) toolNameText.text = toolName ?? "None";
            if (toolIcon != null)
            {
                toolIcon.sprite = icon;
                toolIcon.enabled = icon != null;
            }
        }

        /// <summary>
        /// Show a temporary prompt message (e.g., "Press A to grab").
        /// </summary>
        public void ShowPrompt(string message, float duration = 3f)
        {
            if (promptText != null) promptText.text = message;
            _promptTimer = duration;
            _showingPrompt = true;
        }

        /// <summary>
        /// Show the critical warning overlay.
        /// </summary>
        public void ShowWarning()
        {
            if (warningOverlay != null) warningOverlay.enabled = true;
        }

        /// <summary>
        /// Hide the warning overlay.
        /// </summary>
        public void HideWarning()
        {
            if (warningOverlay != null)
            {
                warningOverlay.enabled = false;
            }
        }
    }
}
