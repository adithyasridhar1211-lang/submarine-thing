using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Puzzle
{
    /// <summary>
    /// Abstract base class for all puzzles.
    /// Provides lifecycle hooks: Initialize, CheckCompletion, OnComplete, OnFail.
    /// Concrete puzzle implementations will extend this when puzzle specifics are provided.
    /// </summary>
    public abstract class PuzzleBase : MonoBehaviour
    {
        [Header("Puzzle Identity")]
        [SerializeField] protected string puzzleName = "Unnamed Puzzle";
        [SerializeField] protected int puzzleIndex;
        [SerializeField] protected PuzzleType puzzleType = PuzzleType.Collaborative;
        [SerializeField] protected GamePhase requiredPhase = GamePhase.Calibration;

        [Header("State")]
        [SerializeField] protected bool isStarted = false;
        [SerializeField] protected bool isCompleted = false;
        [SerializeField] protected bool isFailed = false;

        [Header("Timer (Optional)")]
        [SerializeField] protected float timeLimit = 0f; // 0 = no time limit
        protected float elapsedTime;

        [Header("Audio")]
        [SerializeField] protected AudioClip startSound;
        [SerializeField] protected AudioClip completeSound;
        [SerializeField] protected AudioClip failSound;

        protected AudioSource audioSource;

        public string PuzzleName => puzzleName;
        public int PuzzleIndex => puzzleIndex;
        public PuzzleType Type => puzzleType;
        public bool IsStarted => isStarted;
        public bool IsCompleted => isCompleted;
        public bool IsFailed => isFailed;
        public float TimeRemaining => timeLimit > 0 ? Mathf.Max(0, timeLimit - elapsedTime) : -1;

        public event System.Action<PuzzleBase> OnPuzzleComplete;
        public event System.Action<PuzzleBase> OnPuzzleFail;

        protected virtual void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        protected virtual void Update()
        {
            if (!isStarted || isCompleted || isFailed) return;

            // Timer
            if (timeLimit > 0f)
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime >= timeLimit)
                {
                    Fail();
                    return;
                }
            }

            // Check completion each frame
            if (CheckCompletion())
            {
                Complete();
            }
        }

        /// <summary>
        /// Initialize and start this puzzle.
        /// </summary>
        public virtual void StartPuzzle()
        {
            isStarted = true;
            isCompleted = false;
            isFailed = false;
            elapsedTime = 0f;

            PlaySound(startSound);
            GameEvents.FirePuzzleStarted(puzzleIndex);
            OnInitialize();
            Debug.Log($"[Puzzle] Started: {puzzleName}");
        }

        /// <summary>
        /// Override to set up puzzle-specific state.
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// Override to check if the puzzle completion condition is met.
        /// </summary>
        protected abstract bool CheckCompletion();

        /// <summary>
        /// Called when puzzle is completed successfully.
        /// </summary>
        protected virtual void Complete()
        {
            if (isCompleted) return;

            isCompleted = true;
            isStarted = false;

            PlaySound(completeSound);
            GameEvents.FirePuzzleCompleted(puzzleIndex);
            OnComplete();
            OnPuzzleComplete?.Invoke(this);
            Debug.Log($"[Puzzle] Completed: {puzzleName}");
        }

        /// <summary>
        /// Called when puzzle fails (timeout or wrong action).
        /// </summary>
        protected virtual void Fail()
        {
            if (isFailed) return;

            isFailed = true;
            isStarted = false;

            PlaySound(failSound);
            GameEvents.FirePuzzleFailed(puzzleIndex);
            OnFail();
            OnPuzzleFail?.Invoke(this);
            Debug.Log($"[Puzzle] Failed: {puzzleName}");
        }

        /// <summary>
        /// Override for puzzle-specific completion behavior.
        /// </summary>
        protected virtual void OnComplete() { }

        /// <summary>
        /// Override for puzzle-specific failure behavior.
        /// </summary>
        protected virtual void OnFail() { }

        /// <summary>
        /// Reset the puzzle to its initial state.
        /// </summary>
        public virtual void ResetPuzzle()
        {
            isStarted = false;
            isCompleted = false;
            isFailed = false;
            elapsedTime = 0f;
            OnInitialize();
        }

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
