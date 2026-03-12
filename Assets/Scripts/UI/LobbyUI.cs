using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SubmarineCoop.Core;

namespace SubmarineCoop.UI
{
    /// <summary>
    /// Lobby UI for role selection and game start.
    /// VR world-space canvas where players pick Survivor or Ghost and ready up.
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        [Header("Canvas")]
        [SerializeField] private Canvas lobbyCanvas;

        [Header("Role Selection")]
        [SerializeField] private Button survivorButton;
        [SerializeField] private Button ghostButton;
        [SerializeField] private Image survivorHighlight;
        [SerializeField] private Image ghostHighlight;

        [Header("Role Descriptions")]
        [SerializeField] private TextMeshProUGUI survivorDescription;
        [SerializeField] private TextMeshProUGUI ghostDescription;

        [Header("Ready State")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TextMeshProUGUI readyButtonText;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI player1RoleText;
        [SerializeField] private TextMeshProUGUI player2RoleText;
        [SerializeField] private Image player1ReadyIcon;
        [SerializeField] private Image player2ReadyIcon;

        [Header("Colors")]
        [SerializeField] private Color selectedColor = new Color(0.3f, 0.8f, 1f, 1f);
        [SerializeField] private Color unselectedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color readyColor = new Color(0.3f, 1f, 0.5f, 1f);

        private PlayerRoleType _selectedRole = PlayerRoleType.None;
        private bool _isReady = false;

        private void Start()
        {
            // Set up button listeners
            if (survivorButton != null)
                survivorButton.onClick.AddListener(SelectSurvivor);
            if (ghostButton != null)
                ghostButton.onClick.AddListener(SelectGhost);
            if (readyButton != null)
                readyButton.onClick.AddListener(ToggleReady);

            // Set descriptions
            SetDescriptions();
            UpdateUI();

            // Listen for game state changes
            GameEvents.OnGameStateChanged += HandleGameStateChanged;

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerRoleChanged += HandleRoleChanged;
                SessionManager.Instance.OnPlayerReady += HandlePlayerReady;
            }
        }

        private void SetDescriptions()
        {
            if (survivorDescription != null)
            {
                survivorDescription.text =
                    "<b>THE SURVIVOR</b>\n\n" +
                    "• Full physical agency\n" +
                    "• Impaired vision (grayscale)\n" +
                    "• Can grab, push, and use tools\n" +
                    "• Cannot see color-coded puzzles";
            }

            if (ghostDescription != null)
            {
                ghostDescription.text =
                    "<b>THE ECHO</b>\n\n" +
                    "• Total perception (vivid colors)\n" +
                    "• Cannot touch physical objects\n" +
                    "• Communicates via tapping & tagging\n" +
                    "• Abilities unlock with Mementos";
            }
        }

        /// <summary>
        /// Select the Survivor role.
        /// </summary>
        public void SelectSurvivor()
        {
            _selectedRole = PlayerRoleType.Survivor;
            _isReady = false;

            if (SessionManager.Instance != null)
            {
                int localId = NetworkManager.Instance?.LocalPlayerId ?? 0;
                SessionManager.Instance.SetPlayerRole(localId, PlayerRoleType.Survivor);
                SessionManager.Instance.SetPlayerNotReady(localId);
            }

            UpdateUI();
            Debug.Log("[LobbyUI] Selected role: Survivor");
        }

        /// <summary>
        /// Select the Ghost role.
        /// </summary>
        public void SelectGhost()
        {
            _selectedRole = PlayerRoleType.Ghost;
            _isReady = false;

            if (SessionManager.Instance != null)
            {
                int localId = NetworkManager.Instance?.LocalPlayerId ?? 0;
                SessionManager.Instance.SetPlayerRole(localId, PlayerRoleType.Ghost);
                SessionManager.Instance.SetPlayerNotReady(localId);
            }

            UpdateUI();
            Debug.Log("[LobbyUI] Selected role: Ghost");
        }

        /// <summary>
        /// Toggle ready state.
        /// </summary>
        public void ToggleReady()
        {
            if (_selectedRole == PlayerRoleType.None)
            {
                if (statusText != null)
                    statusText.text = "Select a role first!";
                return;
            }

            _isReady = !_isReady;

            if (SessionManager.Instance != null)
            {
                int localId = NetworkManager.Instance?.LocalPlayerId ?? 0;
                if (_isReady)
                    SessionManager.Instance.SetPlayerReady(localId);
                else
                    SessionManager.Instance.SetPlayerNotReady(localId);
            }

            UpdateUI();
        }

        private void UpdateUI()
        {
            // Role highlights
            if (survivorHighlight != null)
                survivorHighlight.color = _selectedRole == PlayerRoleType.Survivor ? selectedColor : unselectedColor;
            if (ghostHighlight != null)
                ghostHighlight.color = _selectedRole == PlayerRoleType.Ghost ? selectedColor : unselectedColor;

            // Ready button
            if (readyButtonText != null)
                readyButtonText.text = _isReady ? "CANCEL" : "READY";
            if (readyButton != null)
            {
                var colors = readyButton.colors;
                colors.normalColor = _isReady ? readyColor : Color.white;
                readyButton.colors = colors;
            }

            // Status
            if (statusText != null)
            {
                if (_selectedRole == PlayerRoleType.None)
                    statusText.text = "Choose your role";
                else if (!_isReady)
                    statusText.text = $"Selected: {_selectedRole}. Press Ready when prepared.";
                else
                    statusText.text = "Waiting for other player...";
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (lobbyCanvas != null)
            {
                lobbyCanvas.enabled = state == GameState.Lobby;
            }
        }

        private void HandleRoleChanged(int playerId, PlayerRoleType role)
        {
            // Update other player's role display
            int localId = NetworkManager.Instance?.LocalPlayerId ?? 0;
            if (playerId != localId)
            {
                if (player2RoleText != null)
                    player2RoleText.text = role.ToString();
            }
            else
            {
                if (player1RoleText != null)
                    player1RoleText.text = role.ToString();
            }
        }

        private void HandlePlayerReady(int playerId)
        {
            int localId = NetworkManager.Instance?.LocalPlayerId ?? 0;
            if (playerId != localId)
            {
                if (player2ReadyIcon != null)
                    player2ReadyIcon.color = readyColor;
            }
            else
            {
                if (player1ReadyIcon != null)
                    player1ReadyIcon.color = readyColor;
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;

            if (survivorButton != null) survivorButton.onClick.RemoveListener(SelectSurvivor);
            if (ghostButton != null) ghostButton.onClick.RemoveListener(SelectGhost);
            if (readyButton != null) readyButton.onClick.RemoveListener(ToggleReady);

            if (SessionManager.Instance != null)
            {
                SessionManager.Instance.OnPlayerRoleChanged -= HandleRoleChanged;
                SessionManager.Instance.OnPlayerReady -= HandlePlayerReady;
            }
        }
    }
}
