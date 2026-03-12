using UnityEngine;

namespace SubmarineCoop.Utility
{
    /// <summary>
    /// Central repository for all game-wide constants, tags, layers, and magic numbers.
    /// </summary>
    public static class GameConstants
    {
        // --- Player Roles ---
        public const string SURVIVOR_TAG = "Survivor";
        public const string GHOST_TAG = "Ghost";

        // --- Layers ---
        public const string LAYER_INTERACTABLE = "Interactable";
        public const string LAYER_GHOST_ONLY = "GhostOnly";
        public const string LAYER_SURVIVOR_ONLY = "SurvivorOnly";
        public const string LAYER_HAZARD = "Hazard";

        // --- Tether ---
        public const float DEFAULT_TETHER_DISTANCE = 10f;
        public const float TETHER_WARNING_THRESHOLD = 0.8f; // 80% of max distance
        public const float TETHER_SNAP_FORCE = 5f;

        // --- Ghost Energy ---
        public const float DEFAULT_MAX_ENERGY = 100f;
        public const float DEFAULT_ENERGY_REGEN_RATE = 2f; // per second
        public const float ENERGY_COST_TAP = 1f;
        public const float ENERGY_COST_SPIRIT_TAG = 10f;
        public const float ENERGY_COST_FLICKER_LIGHT = 5f;
        public const float ENERGY_COST_FROST_GLASS = 15f;
        public const float ENERGY_COST_MOVE_OBJECT = 20f;
        public const float ENERGY_COST_SPECTRAL_ANCHOR = 30f;
        public const float ENERGY_COST_POSSESSION = 25f;

        // --- Spirit Tag ---
        public const float SPIRIT_TAG_DURATION = 5f; // seconds visible to Survivor
        public const float SPIRIT_TAG_GLOW_INTENSITY = 2f;

        // --- Haptics ---
        public const float HAPTIC_TAP_AMPLITUDE = 0.6f;
        public const float HAPTIC_TAP_DURATION = 0.15f;
        public const float HAPTIC_WARNING_AMPLITUDE = 0.3f;
        public const float HAPTIC_WARNING_DURATION = 0.5f;

        // --- Vision ---
        public const float SURVIVOR_DESATURATION = -80f; // Color Adjustments saturation
        public const float GHOST_SATURATION_BOOST = 30f;
        public const float GHOST_BLOOM_INTENSITY = 1.5f;
        public const float GHOST_SURFACE_TRANSPARENCY = 0.3f;

        // --- Submarine ---
        public const float DEFAULT_HULL_INTEGRITY = 100f;
        public const float DEFAULT_OXYGEN_LEVEL = 100f;
        public const float DEFAULT_POWER_LEVEL = 100f;
        public const float OXYGEN_DEPLETION_RATE = 0.5f; // per second

        // --- Mementos & Leveling ---
        public const int MEMENTOS_FOR_LEVEL_1 = 1;
        public const int MEMENTOS_FOR_LEVEL_2 = 3;
        public const int MEMENTOS_FOR_LEVEL_3 = 6;
        public const int MAX_GHOST_LEVEL = 3;

        // --- Manifestation ---
        public const float LIGHT_FLICKER_DURATION = 2f;
        public const float FROST_GLASS_DURATION = 8f;
        public const float OBJECT_MOVE_FORCE = 3f;
        public const float OBJECT_MOVE_DURATION = 1f;

        // --- Colors (for Ghost vision object coding) ---
        public static readonly Color COLOR_DANGER = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color COLOR_INTERACTION = new Color(0.2f, 0.5f, 1f, 1f);
        public static readonly Color COLOR_COLLECTIBLE = new Color(1f, 0.85f, 0.2f, 1f);
        public static readonly Color COLOR_SPIRIT_TAG = new Color(0.6f, 0.8f, 1f, 0.5f);
        public static readonly Color COLOR_TETHER = new Color(0.5f, 0.7f, 1f, 0.6f);
    }
}
