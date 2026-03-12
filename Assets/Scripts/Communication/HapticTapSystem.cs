using UnityEngine;
using UnityEngine.InputSystem;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;

namespace SubmarineCoop.Communication
{
    /// <summary>
    /// Handles Ghost surface tapping interaction.
    /// When the Ghost taps a surface, a TapEvent is generated with the world position.
    /// The Survivor receives directional haptic feedback and hears a 3D spatial audio cue.
    /// </summary>
    public class HapticTapSystem : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference tapAction;

        [Header("Tap Detection")]
        [SerializeField] private Transform handTransform;
        [SerializeField] private float tapDistance = 0.1f; // How close hand must be to surface
        [SerializeField] private float tapCooldown = 0.3f;
        [SerializeField] private LayerMask tappableLayers = -1;

        [Header("Audio")]
        [SerializeField] private AudioClip tapSoundClip;
        [SerializeField] private float tapSoundVolume = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject tapRipplePrefab; // Spawned at tap point
        [SerializeField] private float rippleDuration = 1f;

        private float _lastTapTime;
        private AudioSource _audioSource;

        private void Start()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f; // Full 3D
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.maxDistance = 15f;
            _audioSource.minDistance = 0.5f;
            _audioSource.playOnAwake = false;

            if (tapAction != null && tapAction.action != null)
            {
                tapAction.action.Enable();
                tapAction.action.performed += OnTapPerformed;
            }
        }

        private void Update()
        {
            // Alternative: proximity-based tap detection (when hand velocity changes rapidly near surface)
            if (handTransform == null) return;

            DetectProximityTap();
        }

        /// <summary>
        /// Manually trigger a tap at the hand's current position (called by Ghost controller).
        /// </summary>
        public void PerformTap()
        {
            if (Time.time - _lastTapTime < tapCooldown) return;

            if (handTransform == null) return;

            // Raycast from hand to find surface
            if (Physics.Raycast(handTransform.position, handTransform.forward, out RaycastHit hit, tapDistance * 5f, tappableLayers))
            {
                ExecuteTap(hit.point);
            }
        }

        private void OnTapPerformed(InputAction.CallbackContext context)
        {
            PerformTap();
        }

        private void DetectProximityTap()
        {
            // Check if hand is close to any surface
            Collider[] nearby = Physics.OverlapSphere(handTransform.position, tapDistance, tappableLayers);
            if (nearby.Length > 0 && Time.time - _lastTapTime >= tapCooldown)
            {
                // Check for quick deceleration (tap gesture) by checking if input action was just pressed
                if (tapAction != null && tapAction.action != null && tapAction.action.WasPressedThisFrame())
                {
                    Vector3 closestPoint = nearby[0].ClosestPoint(handTransform.position);
                    ExecuteTap(closestPoint);
                }
            }
        }

        private void ExecuteTap(Vector3 worldPosition)
        {
            _lastTapTime = Time.time;

            // Fire global event — Survivor picks this up for haptic response
            GameEvents.FireGhostTap(worldPosition);

            // Play 3D spatial audio at tap point
            PlayTapSound(worldPosition);

            // Spawn visual ripple effect
            SpawnRipple(worldPosition);

            Debug.Log($"[HapticTap] Tap at {worldPosition}");
        }

        private void PlayTapSound(Vector3 position)
        {
            if (tapSoundClip == null || _audioSource == null) return;

            _audioSource.transform.position = position;
            _audioSource.clip = tapSoundClip;
            _audioSource.volume = tapSoundVolume;
            _audioSource.Play();
        }

        private void SpawnRipple(Vector3 position)
        {
            if (tapRipplePrefab == null) return;

            GameObject ripple = Instantiate(tapRipplePrefab, position, Quaternion.identity);
            Destroy(ripple, rippleDuration);
        }

        private void OnDestroy()
        {
            if (tapAction != null && tapAction.action != null)
            {
                tapAction.action.performed -= OnTapPerformed;
            }
        }
    }
}
