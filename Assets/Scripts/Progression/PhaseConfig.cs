using UnityEngine;

namespace SubmarineCoop.Progression
{
    /// <summary>
    /// ScriptableObject defining parameters for each game phase.
    /// Configures tether distance, energy regen, available mechanics, etc.
    /// </summary>
    [CreateAssetMenu(fileName = "PhaseConfig", menuName = "SubmarineCoop/Phase Config")]
    public class PhaseConfig : ScriptableObject
    {
        [Header("Phase Identity")]
        public Core.GamePhase phase;
        public string phaseName;
        [TextArea] public string phaseDescription;

        [Header("Tether")]
        public float maxTetherDistance = 10f;

        [Header("Ghost Energy")]
        public float ghostMaxEnergy = 100f;
        public float ghostEnergyRegenRate = 2f;

        [Header("Submarine")]
        public float oxygenDepletionRate = 0.5f;
        public float powerDepletionRate = 0.1f;

        [Header("Puzzles")]
        public int puzzlesToComplete = 3;
        public float puzzleTimePressure = 0f; // seconds; 0 = no timer

        [Header("Mechanics Unlocks")]
        public bool enableHazards = false;
        public bool enableSpectralAnchors = false;
        public bool enableSimultaneousPuzzles = false;
        public bool enableDarkAreas = false;
    }
}
