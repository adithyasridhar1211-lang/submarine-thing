using System.Collections.Generic;
using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Player;
using SubmarineCoop.Environment;

namespace SubmarineCoop.Progression
{
    /// <summary>
    /// Manages game phase progression (Calibration → Expansion → Convergence).
    /// Listens to puzzle completions and applies phase-specific configuration changes.
    /// </summary>
    public class PhaseManager : MonoBehaviour
    {
        public static PhaseManager Instance { get; private set; }

        [Header("Phase Configs")]
        [SerializeField] private PhaseConfig calibrationConfig;
        [SerializeField] private PhaseConfig expansionConfig;
        [SerializeField] private PhaseConfig convergenceConfig;

        [Header("State")]
        [SerializeField] private GamePhase currentPhase = GamePhase.Calibration;
        [SerializeField] private int puzzlesCompletedInPhase = 0;

        private PhaseConfig _activeConfig;
        private Dictionary<GamePhase, PhaseConfig> _configs;

        public GamePhase CurrentPhase => currentPhase;
        public PhaseConfig ActiveConfig => _activeConfig;
        public int PuzzlesCompletedInPhase => puzzlesCompletedInPhase;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _configs = new Dictionary<GamePhase, PhaseConfig>
            {
                { GamePhase.Calibration, calibrationConfig },
                { GamePhase.Expansion, expansionConfig },
                { GamePhase.Convergence, convergenceConfig }
            };
        }

        private void OnEnable()
        {
            GameEvents.OnPuzzleCompleted += HandlePuzzleCompleted;
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPuzzleCompleted -= HandlePuzzleCompleted;
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        }

        /// <summary>
        /// Begin the first phase.
        /// </summary>
        public void StartPhases()
        {
            SetPhase(GamePhase.Calibration);
        }

        /// <summary>
        /// Force-set the current phase.
        /// </summary>
        public void SetPhase(GamePhase phase)
        {
            currentPhase = phase;
            puzzlesCompletedInPhase = 0;

            _activeConfig = _configs.TryGetValue(phase, out var config) ? config : null;

            if (_activeConfig != null)
            {
                ApplyPhaseConfig(_activeConfig);
            }

            GameEvents.FirePhaseChanged(phase);
            Debug.Log($"[PhaseManager] Phase changed to: {phase}");
        }

        /// <summary>
        /// Advance to the next phase if conditions are met.
        /// </summary>
        public bool TryAdvancePhase()
        {
            GamePhase nextPhase;

            switch (currentPhase)
            {
                case GamePhase.Calibration:
                    nextPhase = GamePhase.Expansion;
                    break;
                case GamePhase.Expansion:
                    nextPhase = GamePhase.Convergence;
                    break;
                case GamePhase.Convergence:
                    Debug.Log("[PhaseManager] Already at final phase");
                    return false;
                default:
                    return false;
            }

            SetPhase(nextPhase);
            return true;
        }

        // --- Apply Config ---

        private void ApplyPhaseConfig(PhaseConfig config)
        {
            // Update tether distance
            var ghost = FindAnyObjectByType<GhostController>();
            if (ghost != null && ghost.Tether != null)
            {
                ghost.Tether.SetMaxDistance(config.maxTetherDistance);
            }

            // Update energy system
            if (ghost != null && ghost.Energy != null)
            {
                ghost.Energy.SetMaxEnergy(config.ghostMaxEnergy);
                ghost.Energy.SetRegenRate(config.ghostEnergyRegenRate);
            }

            // Update submarine
            if (SubmarineManager.Instance != null)
            {
                SubmarineManager.Instance.SetOxygenDepletionRate(config.oxygenDepletionRate);
            }

            Debug.Log($"[PhaseManager] Applied config for phase: {config.phaseName}");
        }

        // --- Event Handlers ---

        private void HandlePuzzleCompleted(int puzzleIndex)
        {
            puzzlesCompletedInPhase++;
            Debug.Log($"[PhaseManager] Puzzle completed ({puzzlesCompletedInPhase}/{_activeConfig?.puzzlesToComplete ?? 0})");

            if (_activeConfig != null && puzzlesCompletedInPhase >= _activeConfig.puzzlesToComplete)
            {
                Debug.Log("[PhaseManager] Phase completion threshold reached!");
                TryAdvancePhase();
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.InGame)
            {
                StartPhases();
            }
        }
    }
}
