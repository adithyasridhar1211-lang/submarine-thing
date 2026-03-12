using UnityEngine;

namespace SubmarineCoop.Progression
{
    /// <summary>
    /// ScriptableObject defining Ghost upgrade/ability details per level.
    /// </summary>
    [CreateAssetMenu(fileName = "GhostUpgradeConfig", menuName = "SubmarineCoop/Ghost Upgrade Config")]
    public class GhostUpgradeConfig : ScriptableObject
    {
        [Header("Level Thresholds")]
        public int mementosForLevel1 = 1;
        public int mementosForLevel2 = 3;
        public int mementosForLevel3 = 6;

        [Header("Level 1 — Point/Hand Tracking")]
        public float level1MistAlpha = 0.3f;
        public float level1TrackingRange = 5f;

        [Header("Level 2 — Possession")]
        public float possessionDuration = 3f;
        public float possessionEnergyCost = 25f;
        public float possessionCooldown = 10f;

        [Header("Level 3 — Echo Sight")]
        public float echoSightDuration = 5f;
        public float echoSightEnergyCost = 40f;
        public float echoSightCooldown = 30f;

        /// <summary>
        /// Get the level for a given memento count.
        /// </summary>
        public int GetLevelForMementos(int count)
        {
            if (count >= mementosForLevel3) return 3;
            if (count >= mementosForLevel2) return 2;
            if (count >= mementosForLevel1) return 1;
            return 0;
        }
    }
}
