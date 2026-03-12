using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Rotatable valve interactable. The Survivor grabs and rotates to control flow.
    /// Tracks rotation progress from 0% to 100%.
    /// </summary>
    public class Valve : InteractableBase
    {
        [Header("Valve Settings")]
        [SerializeField] private Transform valveWheel;
        [SerializeField] private float maxRotation = 720f; // degrees to fully open
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;

        [Header("State")]
        [SerializeField, Range(0f, 1f)] private float openProgress = 0f;
        [SerializeField] private float rotationSpeed = 180f; // degrees per second when using action

        [Header("Color")]
        [SerializeField] private Color valveColor = Color.red; // Ghost sees this; Survivor sees grey

        [Header("Connection")]
        [SerializeField] private UnityEngine.Events.UnityEvent<float> onProgressChanged;
        [SerializeField] private UnityEngine.Events.UnityEvent onFullyOpen;
        [SerializeField] private UnityEngine.Events.UnityEvent onFullyClosed;

        private float _currentRotation;

        public float OpenProgress => openProgress;

        protected override void Start()
        {
            base.Start();
            canSurvivorInteract = true;
            canGhostInteract = false;
            _currentRotation = openProgress * maxRotation;
        }

        /// <summary>
        /// Rotate the valve by a delta (called by XR grab rotation or manual input).
        /// </summary>
        public void Rotate(float deltaDegrees)
        {
            if (isLocked) return;

            _currentRotation = Mathf.Clamp(_currentRotation + deltaDegrees, 0f, maxRotation);
            openProgress = _currentRotation / maxRotation;

            if (valveWheel != null)
            {
                valveWheel.localRotation = Quaternion.AngleAxis(_currentRotation, rotationAxis);
            }

            onProgressChanged?.Invoke(openProgress);

            if (Mathf.Approximately(openProgress, 1f))
            {
                onFullyOpen?.Invoke();
            }
            else if (Mathf.Approximately(openProgress, 0f))
            {
                onFullyClosed?.Invoke();
            }
        }

        protected override void Interact(PlayerRoleType role)
        {
            // Simple toggle: rotate to open or closed
            float target = isActivated ? 0f : maxRotation;
            isActivated = !isActivated;

            // Animate towards target
            _currentRotation = target;
            openProgress = _currentRotation / maxRotation;

            if (valveWheel != null)
            {
                valveWheel.localRotation = Quaternion.AngleAxis(_currentRotation, rotationAxis);
            }

            PlaySound(interactSound);
            onProgressChanged?.Invoke(openProgress);
            OnInteracted?.Invoke(this);
            OnStateChanged?.Invoke(this);

            Debug.Log($"[Valve] {name} progress: {openProgress:P0} by {role}");
        }

        /// <summary>
        /// Set valve progress directly (0 to 1).
        /// </summary>
        public void SetProgress(float progress)
        {
            openProgress = Mathf.Clamp01(progress);
            _currentRotation = openProgress * maxRotation;

            if (valveWheel != null)
            {
                valveWheel.localRotation = Quaternion.AngleAxis(_currentRotation, rotationAxis);
            }

            onProgressChanged?.Invoke(openProgress);
            OnStateChanged?.Invoke(this);
        }

        /// <summary>
        /// Get the valve color as seen by the Ghost.
        /// </summary>
        public Color GetValveColor()
        {
            return valveColor;
        }

        public override void ResetState()
        {
            base.ResetState();
            openProgress = 0f;
            _currentRotation = 0f;
            if (valveWheel != null)
            {
                valveWheel.localRotation = Quaternion.identity;
            }
        }
    }
}
