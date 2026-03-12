using System.Collections.Generic;
using UnityEngine;

namespace SubmarineCoop.Core
{
    /// <summary>
    /// Manages the lobby, player join/leave, and role selection/validation.
    /// Ensures exactly one Survivor and one Ghost before game can start.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Session State")]
        [SerializeField] private int maxPlayers = 2;

        private Dictionary<int, PlayerRoleType> _playerRoles = new();
        private HashSet<int> _readyPlayers = new();

        public int PlayerCount => _playerRoles.Count;
        public bool AllPlayersReady => _readyPlayers.Count >= maxPlayers && HasValidRoles();

        public event System.Action<int, PlayerRoleType> OnPlayerRoleChanged;
        public event System.Action<int> OnPlayerReady;
        public event System.Action OnAllPlayersReady;
        public event System.Action<int> OnPlayerJoinedSession;
        public event System.Action<int> OnPlayerLeftSession;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined += HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerLeft += HandlePlayerLeft;
            }
        }

        private void OnDisable()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnPlayerJoined -= HandlePlayerJoined;
                NetworkManager.Instance.OnPlayerLeft -= HandlePlayerLeft;
            }
        }

        /// <summary>
        /// Called when a player selects their role.
        /// </summary>
        public void SetPlayerRole(int playerId, PlayerRoleType role)
        {
            // Check if role is already taken by another player
            foreach (var kvp in _playerRoles)
            {
                if (kvp.Key != playerId && kvp.Value == role && role != PlayerRoleType.None)
                {
                    Debug.LogWarning($"[SessionManager] Role {role} already taken by Player {kvp.Key}");
                    return;
                }
            }

            _playerRoles[playerId] = role;
            Debug.Log($"[SessionManager] Player {playerId} selected role: {role}");
            OnPlayerRoleChanged?.Invoke(playerId, role);

            // If this is the local player, tell GameManager
            if (NetworkManager.Instance != null && playerId == NetworkManager.Instance.LocalPlayerId)
            {
                GameManager.Instance?.AssignLocalRole(role);
            }
        }

        /// <summary>
        /// Mark a player as ready.
        /// </summary>
        public void SetPlayerReady(int playerId)
        {
            if (!_playerRoles.ContainsKey(playerId) || _playerRoles[playerId] == PlayerRoleType.None)
            {
                Debug.LogWarning($"[SessionManager] Player {playerId} must select a role before readying up");
                return;
            }

            _readyPlayers.Add(playerId);
            Debug.Log($"[SessionManager] Player {playerId} is ready ({_readyPlayers.Count}/{maxPlayers})");
            OnPlayerReady?.Invoke(playerId);

            if (AllPlayersReady)
            {
                Debug.Log("[SessionManager] All players ready! Starting game...");
                OnAllPlayersReady?.Invoke();
                GameManager.Instance?.StartGame();
            }
        }

        /// <summary>
        /// Unready a player (e.g., if they change role).
        /// </summary>
        public void SetPlayerNotReady(int playerId)
        {
            _readyPlayers.Remove(playerId);
        }

        /// <summary>
        /// Get the role assigned to a player.
        /// </summary>
        public PlayerRoleType GetPlayerRole(int playerId)
        {
            return _playerRoles.TryGetValue(playerId, out var role) ? role : PlayerRoleType.None;
        }

        /// <summary>
        /// Check if we have exactly one Survivor and one Ghost.
        /// </summary>
        public bool HasValidRoles()
        {
            int survivors = 0, ghosts = 0;
            foreach (var kvp in _playerRoles)
            {
                if (kvp.Value == PlayerRoleType.Survivor) survivors++;
                if (kvp.Value == PlayerRoleType.Ghost) ghosts++;
            }
            return survivors == 1 && ghosts == 1;
        }

        /// <summary>
        /// Reset the session for a new game.
        /// </summary>
        public void ResetSession()
        {
            _playerRoles.Clear();
            _readyPlayers.Clear();
            Debug.Log("[SessionManager] Session reset");
        }

        // --- Network Callbacks ---

        private void HandlePlayerJoined(int playerId)
        {
            _playerRoles[playerId] = PlayerRoleType.None;
            Debug.Log($"[SessionManager] Player {playerId} joined session");
            OnPlayerJoinedSession?.Invoke(playerId);
        }

        private void HandlePlayerLeft(int playerId)
        {
            _playerRoles.Remove(playerId);
            _readyPlayers.Remove(playerId);
            Debug.Log($"[SessionManager] Player {playerId} left session");
            OnPlayerLeftSession?.Invoke(playerId);
        }
    }
}
