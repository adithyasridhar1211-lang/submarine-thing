using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using SubmarineCoop.Core;

namespace SubmarineCoop.Player
{
    /// <summary>
    /// Base class for both Survivor and Ghost player controllers.
    /// Handles common VR rig setup, role identification, and shared logic.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public abstract class PlayerController : MonoBehaviour
    {
        [Header("Player Identity")]
        [SerializeField] protected PlayerRoleType role = PlayerRoleType.None;

        [Header("VR References")]
        [SerializeField] protected Transform xrOrigin;
        [SerializeField] protected Transform headTransform;
        [SerializeField] protected Transform leftHandTransform;
        [SerializeField] protected Transform rightHandTransform;

        [Header("Movement")]
        [SerializeField] protected float moveSpeed = 2f;
        [SerializeField] protected bool canMove = true;

        protected CharacterController characterController;
        protected bool isLocalPlayer = true; // Will be set by networking

        public PlayerRoleType Role => role;
        public Transform Head => headTransform;
        public Transform LeftHand => leftHandTransform;
        public Transform RightHand => rightHandTransform;
        public bool IsLocalPlayer => isLocalPlayer;

        protected virtual void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void Update()
        {
            if (!isLocalPlayer || !canMove) return;
            OnUpdate();
        }

        /// <summary>
        /// Called once during Start. Override to set up role-specific systems.
        /// </summary>
        protected virtual void Initialize()
        {
            Debug.Log($"[PlayerController] Initialized as {role}");
        }

        /// <summary>
        /// Called every frame for the local player. Override for role-specific update logic.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Enable or disable player movement.
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            canMove = enabled;
        }

        /// <summary>
        /// Teleport the player to a position.
        /// </summary>
        public void TeleportTo(Vector3 position, Quaternion rotation)
        {
            if (xrOrigin != null)
            {
                xrOrigin.position = position;
                xrOrigin.rotation = rotation;
            }
            else
            {
                transform.position = position;
                transform.rotation = rotation;
            }
        }

        /// <summary>
        /// Get the world position of the player's head (main camera).
        /// </summary>
        public Vector3 GetHeadPosition()
        {
            return headTransform != null ? headTransform.position : transform.position;
        }

        /// <summary>
        /// Get the forward direction the player is looking.
        /// </summary>
        public Vector3 GetLookDirection()
        {
            return headTransform != null ? headTransform.forward : transform.forward;
        }
    }
}
