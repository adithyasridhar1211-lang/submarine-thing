using UnityEngine;

namespace SubmarineCoop.Core
{
    /// <summary>
    /// Top-level singleton managing game state, player spawning, and role assignment.
    /// Coordinates between all major systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player Prefabs")]
        [SerializeField] private GameObject survivorPrefab;
        [SerializeField] private GameObject ghostPrefab;

        [Header("Spawn Points")]
        [SerializeField] private Transform survivorSpawnPoint;
        [SerializeField] private Transform ghostSpawnPoint;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Lobby;

        public GameState CurrentState => currentState;
        public PlayerRoleType LocalPlayerRole { get; private set; } = PlayerRoleType.None;
        public GameObject SurvivorPlayer { get; private set; }
        public GameObject GhostPlayer { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            SetState(GameState.Lobby);
            Debug.Log("[GameManager] Initialized in Lobby state");
        }

        /// <summary>
        /// Transition to a new game state.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;
            Debug.Log($"[GameManager] State: {previousState} -> {newState}");

            GameEvents.FireGameStateChanged(newState);

            switch (newState)
            {
                case GameState.Lobby:
                    OnEnterLobby();
                    break;
                case GameState.InGame:
                    OnEnterInGame();
                    break;
                case GameState.Paused:
                    OnEnterPaused();
                    break;
                case GameState.GameOver:
                    OnEnterGameOver();
                    break;
            }
        }

        /// <summary>
        /// Assign the local player's role. Called from lobby UI or session manager.
        /// </summary>
        public void AssignLocalRole(PlayerRoleType role)
        {
            LocalPlayerRole = role;
            Debug.Log($"[GameManager] Local role assigned: {role}");
            GameEvents.FireRoleAssigned(role);
        }

        /// <summary>
        /// Start the game. Both roles must be assigned.
        /// </summary>
        public void StartGame()
        {
            if (LocalPlayerRole == PlayerRoleType.None)
            {
                Debug.LogWarning("[GameManager] Cannot start game: no role assigned");
                return;
            }
            SetState(GameState.InGame);
        }

        public void PauseGame()
        {
            if (currentState == GameState.InGame)
            {
                SetState(GameState.Paused);
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                SetState(GameState.InGame);
            }
        }

        public void EndGame()
        {
            SetState(GameState.GameOver);
        }

        public void ReturnToLobby()
        {
            // Clean up players
            if (SurvivorPlayer != null) Destroy(SurvivorPlayer);
            if (GhostPlayer != null) Destroy(GhostPlayer);

            LocalPlayerRole = PlayerRoleType.None;
            SetState(GameState.Lobby);
        }

        // --- State Handlers ---

        private void OnEnterLobby()
        {
            Time.timeScale = 1f;
        }

        private void OnEnterInGame()
        {
            Time.timeScale = 1f;
            SpawnPlayers();
        }

        private void OnEnterPaused()
        {
            // Note: In VR, pausing with timeScale can cause discomfort.
            // Consider using a game-level pause flag instead.
        }

        private void OnEnterGameOver()
        {
            // Trigger end-game UI / stats
        }

        private void SpawnPlayers()
        {
            // Spawn based on local role
            if (LocalPlayerRole == PlayerRoleType.Survivor || LocalPlayerRole == PlayerRoleType.None)
            {
                if (survivorPrefab != null && survivorSpawnPoint != null)
                {
                    SurvivorPlayer = Instantiate(survivorPrefab, survivorSpawnPoint.position, survivorSpawnPoint.rotation);
                    Debug.Log("[GameManager] Survivor spawned");
                }
            }

            if (LocalPlayerRole == PlayerRoleType.Ghost || LocalPlayerRole == PlayerRoleType.None)
            {
                if (ghostPrefab != null && ghostSpawnPoint != null)
                {
                    GhostPlayer = Instantiate(ghostPrefab, ghostSpawnPoint.position, ghostSpawnPoint.rotation);
                    Debug.Log("[GameManager] Ghost spawned");
                }
            }

            // In local testing mode (role == None), spawn both for debugging
            if (LocalPlayerRole == PlayerRoleType.None)
            {
                Debug.Log("[GameManager] No role assigned - spawned both players for local testing");
            }
        }
    }
}
