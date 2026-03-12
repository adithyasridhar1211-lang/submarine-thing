using UnityEngine;
using SubmarineCoop.Core;
using SubmarineCoop.Environment;

namespace SubmarineCoop.Progression
{
    /// <summary>
    /// Memento collectible system. Mementos are personal items hidden in the environment.
    /// The Survivor finds them and returns them to the Ghost to level up Ghost abilities.
    /// </summary>
    public class MementoSystem : MonoBehaviour
    {
        public static MementoSystem Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GhostUpgradeConfig upgradeConfig;

        [Header("State")]
        [SerializeField] private int mementosCollected = 0;
        [SerializeField] private int mementosReturned = 0;
        [SerializeField] private int currentGhostLevel = 0;

        public int MementosCollected => mementosCollected;
        public int MementosReturned => mementosReturned;
        public int CurrentGhostLevel => currentGhostLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Called when the Survivor picks up a Memento.
        /// </summary>
        public void CollectMemento(GameObject memento)
        {
            mementosCollected++;
            GameEvents.FireMementoCollected(memento);
            Debug.Log($"[Memento] Collected: {memento.name} (total: {mementosCollected})");
        }

        /// <summary>
        /// Called when the Survivor returns a Memento to the Ghost.
        /// This triggers Ghost leveling.
        /// </summary>
        public void ReturnMemento(GameObject memento)
        {
            mementosReturned++;
            GameEvents.FireMementoReturned(memento);
            Debug.Log($"[Memento] Returned to Ghost: {memento.name} (total returned: {mementosReturned})");

            CheckLevelUp();
        }

        private void CheckLevelUp()
        {
            if (upgradeConfig == null) return;

            int newLevel = upgradeConfig.GetLevelForMementos(mementosReturned);

            if (newLevel > currentGhostLevel)
            {
                currentGhostLevel = newLevel;
                GameEvents.FireGhostLevelUp(newLevel);
                Debug.Log($"[Memento] Ghost leveled up to Level {newLevel}!");
            }
        }
    }

    /// <summary>
    /// Component attached to Memento collectible objects in the environment.
    /// </summary>
    [RequireComponent(typeof(GlowableObject))]
    public class MementoPickup : MonoBehaviour
    {
        [Header("Memento Info")]
        [SerializeField] private string mementoName = "Unknown Memento";
        [TextArea][SerializeField] private string mementoDescription;
        [SerializeField] private Sprite mementoIcon;

        [Header("Visuals")]
        [SerializeField] private bool glowForGhost = true;
        [SerializeField] private Color mementoGlowColor = new Color(1f, 0.85f, 0.2f, 1f); // Gold
        [SerializeField] private float glowIntensity = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip pickupSound;

        private GlowableObject _glowable;
        private bool _isCollected = false;

        public string MementoName => mementoName;
        public string MementoDescription => mementoDescription;
        public bool IsCollected => _isCollected;

        private void Start()
        {
            _glowable = GetComponent<GlowableObject>();

            if (_glowable != null && glowForGhost)
            {
                _glowable.SetGlow(mementoGlowColor, glowIntensity);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            // Only Survivor can pick up Mementos
            if (other.CompareTag("Survivor") || other.transform.root.CompareTag("Survivor"))
            {
                Collect();
            }
        }

        /// <summary>
        /// Collect this memento.
        /// </summary>
        public void Collect()
        {
            if (_isCollected) return;

            _isCollected = true;

            if (MementoSystem.Instance != null)
            {
                MementoSystem.Instance.CollectMemento(gameObject);
            }

            // Play pickup sound
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            // Disable visuals but keep object for returning to Ghost
            if (_glowable != null) _glowable.ClearGlow();

            // Disable renderer but keep the object
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;

            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;

            Debug.Log($"[MementoPickup] {mementoName} collected by Survivor");
        }

        /// <summary>
        /// Return this memento to the Ghost.
        /// </summary>
        public void ReturnToGhost()
        {
            if (MementoSystem.Instance != null)
            {
                MementoSystem.Instance.ReturnMemento(gameObject);
            }

            // Destroy after returning
            Destroy(gameObject, 0.5f);
            Debug.Log($"[MementoPickup] {mementoName} returned to Ghost");
        }
    }
}
