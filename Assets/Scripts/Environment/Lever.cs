using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Physical lever interactable with hinge joint rotation.
    /// On/Off state toggled when the Survivor grabs and pulls.
    /// </summary>
    public class Lever : InteractableBase
    {
        [Header("Lever Settings")]
        [SerializeField] private Transform leverHandle;
        [SerializeField] private float onAngle = 45f;
        [SerializeField] private float offAngle = -45f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private Vector3 rotationAxis = Vector3.right;

        [Header("Lever Audio")]
        [SerializeField] private AudioClip leverOnSound;
        [SerializeField] private AudioClip leverOffSound;

        [Header("Connection")]
        [SerializeField] private UnityEngine.Events.UnityEvent onLeverOn;
        [SerializeField] private UnityEngine.Events.UnityEvent onLeverOff;

        private float _currentAngle;
        private float _targetAngle;

        protected override void Start()
        {
            base.Start();
            _currentAngle = offAngle;
            _targetAngle = offAngle;
            canSurvivorInteract = true;
            canGhostInteract = false; // Default: Ghost can't use levers without Possession
        }

        private void Update()
        {
            if (leverHandle == null) return;

            // Smoothly rotate to target angle
            _currentAngle = Mathf.MoveTowards(_currentAngle, _targetAngle, rotationSpeed * Time.deltaTime);
            leverHandle.localRotation = Quaternion.AngleAxis(_currentAngle, rotationAxis);
        }

        protected override void Interact(PlayerRoleType role)
        {
            isActivated = !isActivated;
            _targetAngle = isActivated ? onAngle : offAngle;

            if (isActivated)
            {
                PlaySound(leverOnSound ?? interactSound);
                onLeverOn?.Invoke();
            }
            else
            {
                PlaySound(leverOffSound ?? interactSound);
                onLeverOff?.Invoke();
            }

            OnInteracted?.Invoke(this);
            OnStateChanged?.Invoke(this);
            Debug.Log($"[Lever] {name} toggled: {(isActivated ? "ON" : "OFF")} by {role}");
        }

        /// <summary>
        /// Force the lever to a specific state without requiring interaction.
        /// </summary>
        public void ForceState(bool on)
        {
            isActivated = on;
            _targetAngle = isActivated ? onAngle : offAngle;

            if (isActivated)
                onLeverOn?.Invoke();
            else
                onLeverOff?.Invoke();

            OnStateChanged?.Invoke(this);
        }

        public override void ResetState()
        {
            base.ResetState();
            _targetAngle = offAngle;
            _currentAngle = offAngle;
        }
    }
}
