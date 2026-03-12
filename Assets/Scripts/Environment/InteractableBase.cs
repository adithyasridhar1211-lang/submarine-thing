using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SubmarineCoop.Core;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Base class for all interactable objects in the submarine.
    /// Integrates with XR Interaction Toolkit and provides role-based interaction filtering.
    /// </summary>
    [RequireComponent(typeof(GlowableObject))]
    public class InteractableBase : MonoBehaviour
    {
        [Header("Interaction Rules")]
        [SerializeField] protected bool canSurvivorInteract = true;
        [SerializeField] protected bool canGhostInteract = false;
        [SerializeField] protected int requiredGhostLevel = 0; // 0 = no level required
        [SerializeField] protected float ghostEnergyCost = 0f;

        [Header("Color Coding (Ghost Vision)")]
        [SerializeField] protected GlowColorType glowType = GlowColorType.Interaction;
        [SerializeField] protected Color interactableColor = Color.blue; // Ghost sees this color

        [Header("State")]
        [SerializeField] protected bool isActivated = false;
        [SerializeField] protected bool isLocked = false;

        [Header("Audio")]
        [SerializeField] protected AudioClip interactSound;
        [SerializeField] protected AudioClip lockedSound;

        protected XRBaseInteractable xrInteractable;
        protected GlowableObject glowable;
        protected AudioSource audioSource;

        public bool IsActivated => isActivated;
        public bool IsLocked => isLocked;
        public bool CanSurvivorInteract => canSurvivorInteract;
        public bool CanGhostInteract => canGhostInteract;

        public event System.Action<InteractableBase> OnInteracted;
        public event System.Action<InteractableBase> OnStateChanged;

        protected virtual void Awake()
        {
            glowable = GetComponent<GlowableObject>();
            xrInteractable = GetComponent<XRBaseInteractable>();
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        protected virtual void Start()
        {
            // Set up XR interaction events
            if (xrInteractable != null)
            {
                xrInteractable.selectEntered.AddListener(OnSelectEntered);
                xrInteractable.hoverEntered.AddListener(OnHoverEntered);
                xrInteractable.hoverExited.AddListener(OnHoverExited);
            }

            // Set initial glow for Ghost visibility
            if (glowable != null)
            {
                glowable.SetGlow(glowType, 1f);
            }
        }

        /// <summary>
        /// Attempt to interact with this object. Validates role permissions.
        /// </summary>
        public virtual bool TryInteract(PlayerRoleType role, int ghostLevel = 0)
        {
            if (isLocked)
            {
                PlaySound(lockedSound);
                Debug.Log($"[Interactable] {name} is locked");
                return false;
            }

            if (role == PlayerRoleType.Survivor && !canSurvivorInteract)
            {
                Debug.Log($"[Interactable] {name} cannot be used by Survivor");
                return false;
            }

            if (role == PlayerRoleType.Ghost)
            {
                if (!canGhostInteract)
                {
                    Debug.Log($"[Interactable] {name} cannot be used by Ghost");
                    return false;
                }
                if (ghostLevel < requiredGhostLevel)
                {
                    Debug.Log($"[Interactable] {name} requires Ghost level {requiredGhostLevel}");
                    return false;
                }
            }

            Interact(role);
            return true;
        }

        /// <summary>
        /// Perform the interaction. Override in subclasses for specific behavior.
        /// </summary>
        protected virtual void Interact(PlayerRoleType role)
        {
            isActivated = !isActivated;
            PlaySound(interactSound);
            OnInteracted?.Invoke(this);
            OnStateChanged?.Invoke(this);
            Debug.Log($"[Interactable] {name} interacted by {role}. Activated: {isActivated}");
        }

        /// <summary>
        /// Lock/unlock the interactable.
        /// </summary>
        public virtual void SetLocked(bool locked)
        {
            isLocked = locked;
        }

        /// <summary>
        /// Reset to initial state.
        /// </summary>
        public virtual void ResetState()
        {
            isActivated = false;
            isLocked = false;
            OnStateChanged?.Invoke(this);
        }

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // --- XR Interaction Handlers ---

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // Determine the role of the interacting player
            PlayerRoleType role = DetermineInteractorRole(args.interactorObject);
            TryInteract(role);
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // Highlight on hover
            if (glowable != null)
            {
                glowable.SetGlow(glowType, 2f); // Brighter on hover
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            // Remove highlight
            if (glowable != null)
            {
                glowable.SetGlow(glowType, 1f);
            }
        }

        private PlayerRoleType DetermineInteractorRole(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
        {
            if (interactor == null) return PlayerRoleType.None;

            // Walk up the hierarchy to find a PlayerController
            var go = (interactor as MonoBehaviour)?.gameObject;
            if (go == null) return PlayerRoleType.None;

            // Check root for player tag
            var root = go.transform.root.gameObject;
            if (root.CompareTag("Survivor")) return PlayerRoleType.Survivor;
            if (root.CompareTag("Ghost")) return PlayerRoleType.Ghost;

            return PlayerRoleType.None;
        }

        protected virtual void OnDestroy()
        {
            if (xrInteractable != null)
            {
                xrInteractable.selectEntered.RemoveListener(OnSelectEntered);
                xrInteractable.hoverEntered.RemoveListener(OnHoverEntered);
                xrInteractable.hoverExited.RemoveListener(OnHoverExited);
            }
        }
    }
}
