using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Puzzle
{
    /// <summary>
    /// Trigger zone that activates a puzzle when a player enters.
    /// Links to a specific PuzzleBase implementation.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PuzzleArea : MonoBehaviour
    {
        [Header("Puzzle Reference")]
        [SerializeField] private PuzzleBase linkedPuzzle;

        [Header("Activation Rules")]
        [SerializeField] private bool activateOnSurvivorEnter = true;
        [SerializeField] private bool activateOnGhostEnter = false;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool singleActivation = true;

        [Header("Visual Indicators")]
        [SerializeField] private GameObject areaIndicator; // Boundary glow or marker
        [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 0.4f, 0.3f);
        [SerializeField] private Color completedColor = new Color(0.5f, 0.5f, 0.5f, 0.2f);

        private bool _hasActivated = false;
        private Collider _collider;

        public PuzzleBase LinkedPuzzle => linkedPuzzle;
        public bool HasActivated => _hasActivated;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }

        private void Start()
        {
            UpdateVisual();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (linkedPuzzle == null || linkedPuzzle.IsCompleted) return;
            if (singleActivation && _hasActivated) return;

            bool shouldActivate = false;

            if (activateOnSurvivorEnter &&
                (other.CompareTag("Survivor") || other.transform.root.CompareTag("Survivor")))
            {
                shouldActivate = true;
            }

            if (activateOnGhostEnter &&
                (other.CompareTag("Ghost") || other.transform.root.CompareTag("Ghost")))
            {
                shouldActivate = true;
            }

            if (shouldActivate && autoStart)
            {
                ActivatePuzzle();
            }
        }

        /// <summary>
        /// Manually activate the linked puzzle.
        /// </summary>
        public void ActivatePuzzle()
        {
            if (linkedPuzzle == null) return;
            if (linkedPuzzle.IsCompleted || linkedPuzzle.IsStarted) return;

            _hasActivated = true;
            linkedPuzzle.StartPuzzle();
            UpdateVisual();
            Debug.Log($"[PuzzleArea] Activated: {linkedPuzzle.PuzzleName}");
        }

        /// <summary>
        /// Reset the area for re-activation.
        /// </summary>
        public void ResetArea()
        {
            _hasActivated = false;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (areaIndicator == null) return;

            if (linkedPuzzle != null && linkedPuzzle.IsCompleted)
            {
                var renderer = areaIndicator.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = completedColor;
            }
            else
            {
                var renderer = areaIndicator.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = activeColor;
            }
        }
    }
}
