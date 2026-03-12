using System.Collections.Generic;
using UnityEngine;
using SubmarineCoop.Core;

namespace SubmarineCoop.Environment
{
    /// <summary>
    /// Wire panel interactable with multiple color-coded wires.
    /// Ghost sees wire colors, Survivor sees them all as the same shade.
    /// Used in Color-Coded Repair puzzles.
    /// </summary>
    public class WirePanel : InteractableBase
    {
        [Header("Wire Panel")]
        [SerializeField] private List<Wire> wires = new();
        [SerializeField] private bool panelOpen = false;

        [Header("Panel Visual")]
        [SerializeField] private GameObject panelCover;
        [SerializeField] private Animator panelAnimator;
        [SerializeField] private string openAnimTrigger = "Open";
        [SerializeField] private string closeAnimTrigger = "Close";

        [Header("Connection")]
        [SerializeField] private UnityEngine.Events.UnityEvent<int> onWireCut; // wire index
        [SerializeField] private UnityEngine.Events.UnityEvent onAllCorrectCut;
        [SerializeField] private UnityEngine.Events.UnityEvent onWrongCut;

        public bool IsPanelOpen => panelOpen;
        public List<Wire> Wires => wires;

        protected override void Start()
        {
            base.Start();
            canSurvivorInteract = true;
            canGhostInteract = false; // Ghost can only see, not interact

            if (panelCover != null)
            {
                panelCover.SetActive(!panelOpen);
            }
        }

        /// <summary>
        /// Open/close the panel cover.
        /// </summary>
        public void TogglePanel()
        {
            panelOpen = !panelOpen;

            if (panelCover != null)
            {
                panelCover.SetActive(!panelOpen);
            }

            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(panelOpen ? openAnimTrigger : closeAnimTrigger);
            }

            Debug.Log($"[WirePanel] {name} panel {(panelOpen ? "opened" : "closed")}");
        }

        /// <summary>
        /// Cut a wire by index. Only Survivor can do this.
        /// </summary>
        public void CutWire(int index)
        {
            if (index < 0 || index >= wires.Count) return;

            Wire wire = wires[index];
            if (wire.isCut)
            {
                Debug.Log($"[WirePanel] Wire {index} already cut");
                return;
            }

            wire.isCut = true;
            wires[index] = wire;

            onWireCut?.Invoke(index);
            PlaySound(interactSound);

            if (wire.isCorrectToCut)
            {
                Debug.Log($"[WirePanel] Correct wire cut: {wire.wireColor}");
                CheckCompletion();
            }
            else
            {
                Debug.Log($"[WirePanel] Wrong wire cut: {wire.wireColor}!");
                onWrongCut?.Invoke();
            }
        }

        /// <summary>
        /// Get wire info (for Ghost to relay to Survivor).
        /// </summary>
        public Wire GetWireInfo(int index)
        {
            if (index < 0 || index >= wires.Count) return default;
            return wires[index];
        }

        private void CheckCompletion()
        {
            bool allCorrectCut = true;
            foreach (var wire in wires)
            {
                if (wire.isCorrectToCut && !wire.isCut)
                {
                    allCorrectCut = false;
                    break;
                }
            }

            if (allCorrectCut)
            {
                isActivated = true;
                onAllCorrectCut?.Invoke();
                OnStateChanged?.Invoke(this);
                Debug.Log($"[WirePanel] {name} puzzle completed!");
            }
        }

        protected override void Interact(PlayerRoleType role)
        {
            // Default interaction opens/closes the panel
            TogglePanel();
            OnInteracted?.Invoke(this);
        }

        public override void ResetState()
        {
            base.ResetState();
            for (int i = 0; i < wires.Count; i++)
            {
                Wire w = wires[i];
                w.isCut = false;
                wires[i] = w;
            }
            panelOpen = false;
            if (panelCover != null) panelCover.SetActive(true);
        }
    }

    /// <summary>
    /// Data for a single wire in a WirePanel.
    /// </summary>
    [System.Serializable]
    public struct Wire
    {
        public string wireName;
        public Color wireColor;        // Ghost sees this color
        public bool isCorrectToCut;    // Whether this wire should be cut
        public bool isCut;             // Current state
        public Renderer wireRenderer;  // Reference to the wire's renderer

        [Tooltip("Texture type for tactile identification (Survivor can feel this)")]
        public WireTexture textureType;
    }

    public enum WireTexture
    {
        Smooth,
        Braided,
        Corrugated,
        Ribbed,
        Coiled
    }
}
