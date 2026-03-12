using System.Collections.Generic;
using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Puzzle
{
    /// <summary>
    /// Tracks all puzzles in the scene, their states, and completion counts.
    /// Reports to PhaseManager for progression.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        public static PuzzleManager Instance { get; private set; }

        [Header("Puzzles")]
        [SerializeField] private List<PuzzleBase> allPuzzles = new();

        private int _completedCount;
        private int _failedCount;

        public List<PuzzleBase> AllPuzzles => allPuzzles;
        public int CompletedCount => _completedCount;
        public int FailedCount => _failedCount;
        public int TotalCount => allPuzzles.Count;
        public int RemainingCount => TotalCount - _completedCount;

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
            // Auto-discover puzzles if list is empty
            if (allPuzzles.Count == 0)
            {
                allPuzzles.AddRange(FindObjectsByType<PuzzleBase>(FindObjectsSortMode.None));
            }

            // Subscribe to puzzle events
            foreach (var puzzle in allPuzzles)
            {
                puzzle.OnPuzzleComplete += HandlePuzzleComplete;
                puzzle.OnPuzzleFail += HandlePuzzleFail;
            }

            Debug.Log($"[PuzzleManager] Tracking {allPuzzles.Count} puzzles");
        }

        /// <summary>
        /// Register a new puzzle at runtime.
        /// </summary>
        public void RegisterPuzzle(PuzzleBase puzzle)
        {
            if (!allPuzzles.Contains(puzzle))
            {
                allPuzzles.Add(puzzle);
                puzzle.OnPuzzleComplete += HandlePuzzleComplete;
                puzzle.OnPuzzleFail += HandlePuzzleFail;
            }
        }

        /// <summary>
        /// Get all puzzles of a specific type.
        /// </summary>
        public List<PuzzleBase> GetPuzzlesByType(PuzzleType type)
        {
            return allPuzzles.FindAll(p => p.Type == type);
        }

        /// <summary>
        /// Get all incomplete puzzles for the current phase.
        /// </summary>
        public List<PuzzleBase> GetActivePuzzlesForPhase(GamePhase phase)
        {
            return allPuzzles.FindAll(p => !p.IsCompleted && !p.IsFailed);
        }

        /// <summary>
        /// Reset all puzzles.
        /// </summary>
        public void ResetAllPuzzles()
        {
            foreach (var puzzle in allPuzzles)
            {
                puzzle.ResetPuzzle();
            }
            _completedCount = 0;
            _failedCount = 0;
        }

        private void HandlePuzzleComplete(PuzzleBase puzzle)
        {
            _completedCount++;
            Debug.Log($"[PuzzleManager] Puzzle completed: {puzzle.PuzzleName} ({_completedCount}/{TotalCount})");
        }

        private void HandlePuzzleFail(PuzzleBase puzzle)
        {
            _failedCount++;
            Debug.Log($"[PuzzleManager] Puzzle failed: {puzzle.PuzzleName}");
        }

        private void OnDestroy()
        {
            foreach (var puzzle in allPuzzles)
            {
                if (puzzle != null)
                {
                    puzzle.OnPuzzleComplete -= HandlePuzzleComplete;
                    puzzle.OnPuzzleFail -= HandlePuzzleFail;
                }
            }
        }
    }
}
