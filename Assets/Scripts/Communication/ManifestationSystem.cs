using System.Collections;
using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Utility;
using SubmarineCoop.Player;

namespace SubmarineCoop.Communication
{
    /// <summary>
    /// System for Ghost manifestation abilities that cost energy:
    /// - Flicker lights
    /// - Frost glass (write symbols)
    /// - Move small objects
    /// All abilities are gated by the GhostEnergySystem.
    /// </summary>
    public class ManifestationSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GhostEnergySystem energySystem;
        [SerializeField] private Transform handTransform;

        [Header("Interaction")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableLayers = -1;

        [Header("Light Flicker")]
        [SerializeField] private float flickerDuration = GameConstants.LIGHT_FLICKER_DURATION;
        [SerializeField] private float flickerFrequency = 8f; // Hz

        [Header("Glass Frost")]
        [SerializeField] private string frostShaderProperty = "_FrostAmount";
        [SerializeField] private float frostDuration = GameConstants.FROST_GLASS_DURATION;
        [SerializeField] private float frostFadeSpeed = 0.5f;

        [Header("Object Move")]
        [SerializeField] private float moveForce = GameConstants.OBJECT_MOVE_FORCE;
        [SerializeField] private float maxObjectMass = 5f;

        /// <summary>
        /// Flicker the nearest light to the Ghost's hand.
        /// </summary>
        public bool TryFlickerLight()
        {
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_FLICKER_LIGHT))
                return false;

            Light nearestLight = FindNearestLight();
            if (nearestLight == null)
            {
                Debug.Log("[Manifestation] No light nearby to flicker");
                return false;
            }

            StartCoroutine(FlickerLightCoroutine(nearestLight));
            GameEvents.FireLightFlickered(nearestLight);
            return true;
        }

        /// <summary>
        /// Frost a glass surface near the Ghost's hand.
        /// </summary>
        public bool TryFrostGlass()
        {
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_FROST_GLASS))
                return false;

            Renderer glassRenderer = FindNearestGlass();
            if (glassRenderer == null)
            {
                Debug.Log("[Manifestation] No glass surface nearby");
                return false;
            }

            StartCoroutine(FrostGlassCoroutine(glassRenderer));
            GameEvents.FireGlassFrosted(glassRenderer);
            return true;
        }

        /// <summary>
        /// Move a small object near the Ghost's hand.
        /// </summary>
        public bool TryMoveObject(Vector3 direction)
        {
            if (energySystem != null && !energySystem.TrySpendEnergy(GameConstants.ENERGY_COST_MOVE_OBJECT))
                return false;

            Rigidbody nearestRb = FindNearestMovableObject();
            if (nearestRb == null)
            {
                Debug.Log("[Manifestation] No movable object nearby");
                return false;
            }

            nearestRb.AddForce(direction.normalized * moveForce, ForceMode.Impulse);
            GameEvents.FireObjectMoved(nearestRb, direction);
            Debug.Log($"[Manifestation] Moved {nearestRb.name}");
            return true;
        }

        // --- Search Helpers ---

        private Light FindNearestLight()
        {
            Light nearest = null;
            float nearestDist = interactionRange;

            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                float dist = Vector3.Distance(GetHandPosition(), light.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = light;
                }
            }
            return nearest;
        }

        private Renderer FindNearestGlass()
        {
            Collider[] colliders = Physics.OverlapSphere(GetHandPosition(), interactionRange, interactableLayers);
            Renderer nearest = null;
            float nearestDist = interactionRange;

            foreach (var col in colliders)
            {
                var renderer = col.GetComponent<Renderer>();
                if (renderer == null) continue;

                // Check if material is tagged as glass (using shader keyword or tag)
                bool isGlass = false;
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty(frostShaderProperty) ||
                        mat.shader.name.Contains("Glass") ||
                        mat.shader.name.Contains("Transparent"))
                    {
                        isGlass = true;
                        break;
                    }
                }

                if (isGlass)
                {
                    float dist = Vector3.Distance(GetHandPosition(), col.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearest = renderer;
                    }
                }
            }
            return nearest;
        }

        private Rigidbody FindNearestMovableObject()
        {
            Collider[] colliders = Physics.OverlapSphere(GetHandPosition(), interactionRange, interactableLayers);
            Rigidbody nearest = null;
            float nearestDist = interactionRange;

            foreach (var col in colliders)
            {
                var rb = col.GetComponent<Rigidbody>();
                if (rb == null || rb.isKinematic || rb.mass > maxObjectMass) continue;

                float dist = Vector3.Distance(GetHandPosition(), col.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = rb;
                }
            }
            return nearest;
        }

        private Vector3 GetHandPosition()
        {
            return handTransform != null ? handTransform.position : transform.position;
        }

        // --- Coroutines ---

        private IEnumerator FlickerLightCoroutine(Light light)
        {
            if (light == null) yield break;

            float originalIntensity = light.intensity;
            float elapsed = 0f;

            while (elapsed < flickerDuration)
            {
                // Rapid on/off flicker
                float flicker = Mathf.Sin(elapsed * flickerFrequency * Mathf.PI * 2f);
                light.intensity = flicker > 0 ? originalIntensity : originalIntensity * 0.1f;

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Restore original intensity
            light.intensity = originalIntensity;
        }

        private IEnumerator FrostGlassCoroutine(Renderer renderer)
        {
            if (renderer == null) yield break;

            Material mat = renderer.material; // Instance material

            // Frost on
            float frostValue = 0f;
            while (frostValue < 1f)
            {
                frostValue += Time.deltaTime * 2f;
                if (mat.HasProperty(frostShaderProperty))
                {
                    mat.SetFloat(frostShaderProperty, frostValue);
                }
                yield return null;
            }

            // Hold frost
            yield return new WaitForSeconds(frostDuration);

            // Frost fade
            while (frostValue > 0f)
            {
                frostValue -= Time.deltaTime * frostFadeSpeed;
                if (mat.HasProperty(frostShaderProperty))
                {
                    mat.SetFloat(frostShaderProperty, Mathf.Max(0f, frostValue));
                }
                yield return null;
            }
        }
    }
}
