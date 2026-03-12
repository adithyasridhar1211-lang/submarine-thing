using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;
using SubmarineCoop.Player;
using SubmarineCoop.Environment;

namespace SubmarineCoop.Communication
{
    /// <summary>
    /// Spirit Tagging system: Ghost points at an object and tags it.
    /// Tagged objects glow faintly (visible to Survivor) for a configurable duration.
    /// </summary>
    public class SpiritTagSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostEnergySystem energySystem;
        [SerializeField] private Transform pointingHand;

        [Header("Tag Settings")]
        [SerializeField] private float tagRange = 10f;
        [SerializeField] private float tagDuration = GameConstants.SPIRIT_TAG_DURATION;
        [SerializeField] private float glowIntensity = GameConstants.SPIRIT_TAG_GLOW_INTENSITY;
        [SerializeField] private Color tagColor = GameConstants.COLOR_SPIRIT_TAG;
        [SerializeField] private LayerMask taggableLayers = -1;

        [Header("Visual Feedback")]
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private GameObject tagHitIndicator; // Prefab shown at aim target

        private Dictionary<GameObject, Coroutine> _activeTagCoroutines = new();
        private GameObject _currentTarget;

        private void Update()
        {
            UpdateAimVisual();
        }

        /// <summary>
        /// Tag the object the Ghost is currently pointing at.
        /// </summary>
        public bool TryTagTarget()
        {
            GameObject target = GetAimedObject();
            if (target == null)
            {
                Debug.Log("[SpiritTag] No valid target in range");
                return false;
            }

            return TagObject(target);
        }

        /// <summary>
        /// Tag a specific object directly.
        /// </summary>
        public bool TagObject(GameObject target)
        {
            if (target == null) return false;

            // Check energy
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_SPIRIT_TAG))
            {
                Debug.Log("[SpiritTag] Not enough energy");
                return false;
            }

            // If already tagged, refresh duration
            if (_activeTagCoroutines.ContainsKey(target))
            {
                StopCoroutine(_activeTagCoroutines[target]);
                _activeTagCoroutines.Remove(target);
            }

            // Apply glow via GlowableObject component (add if not present)
            GlowableObject glowable = target.GetComponent<GlowableObject>();
            if (glowable == null)
            {
                glowable = target.AddComponent<GlowableObject>();
            }

            glowable.SetGlow(tagColor, glowIntensity);

            // Start fade coroutine
            Coroutine tagCoroutine = StartCoroutine(TagDurationCoroutine(target, glowable));
            _activeTagCoroutines[target] = tagCoroutine;

            GameEvents.FireSpiritTagApplied(target, tagDuration);
            Debug.Log($"[SpiritTag] Tagged: {target.name} for {tagDuration}s");
            return true;
        }

        /// <summary>
        /// Remove tag from an object immediately.
        /// </summary>
        public void RemoveTag(GameObject target)
        {
            if (_activeTagCoroutines.TryGetValue(target, out var coroutine))
            {
                StopCoroutine(coroutine);
                _activeTagCoroutines.Remove(target);
            }

            var glowable = target.GetComponent<GlowableObject>();
            if (glowable != null)
            {
                glowable.ClearGlow();
            }

            GameEvents.FireSpiritTagExpired(target);
        }

        /// <summary>
        /// Get the object the Ghost is currently aiming at.
        /// </summary>
        public GameObject GetAimedObject()
        {
            if (pointingHand == null) return null;

            if (Physics.Raycast(pointingHand.position, pointingHand.forward, out RaycastHit hit, tagRange, taggableLayers))
            {
                _currentTarget = hit.collider.gameObject;
                return _currentTarget;
            }

            _currentTarget = null;
            return null;
        }

        // --- Visuals ---

        private void UpdateAimVisual()
        {
            if (aimLine == null || pointingHand == null) return;

            Vector3 start = pointingHand.position;
            Vector3 end;

            if (Physics.Raycast(start, pointingHand.forward, out RaycastHit hit, tagRange, taggableLayers))
            {
                end = hit.point;

                // Show hit indicator
                if (tagHitIndicator != null)
                {
                    tagHitIndicator.SetActive(true);
                    tagHitIndicator.transform.position = hit.point;
                    tagHitIndicator.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
            else
            {
                end = start + pointingHand.forward * tagRange;
                if (tagHitIndicator != null) tagHitIndicator.SetActive(false);
            }

            aimLine.SetPosition(0, start);
            aimLine.SetPosition(1, end);
        }

        private IEnumerator TagDurationCoroutine(GameObject target, GlowableObject glowable)
        {
            // Full glow for most of the duration
            yield return new WaitForSeconds(tagDuration * 0.7f);

            // Fade out over the remaining 30%
            float fadeDuration = tagDuration * 0.3f;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                float t = 1f - (elapsed / fadeDuration);
                if (glowable != null)
                {
                    glowable.SetGlow(tagColor, glowIntensity * t);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Clean up
            if (glowable != null) glowable.ClearGlow();
            _activeTagCoroutines.Remove(target);
            GameEvents.FireSpiritTagExpired(target);
        }
    }
}
