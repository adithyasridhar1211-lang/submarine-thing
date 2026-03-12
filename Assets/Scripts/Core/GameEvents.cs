using System;
using UnityEngine;

namespace SubmarineCoop.Core
{
    /// <summary>
    /// Central static event bus for decoupled communication between game systems.
    /// All events are static so any script can subscribe/publish without direct references.
    /// </summary>
    public static class GameEvents
    {
        // --- Game State ---
        public static event Action<GameState> OnGameStateChanged;
        public static event Action<PlayerRoleType> OnRoleAssigned;

        // --- Phase Progression ---
        public static event Action<GamePhase> OnPhaseChanged;
        public static event Action<int> OnPuzzleCompleted; // puzzle index

        // --- Ghost Energy ---
        public static event Action<float, float> OnEnergyChanged; // current, max
        public static event Action OnEnergyDepleted;

        // --- Communication ---
        public static event Action<Vector3> OnGhostTap; // world position of tap
        public static event Action<GameObject, float> OnSpiritTagApplied; // tagged object, duration
        public static event Action<GameObject> OnSpiritTagExpired;

        // --- Manifestation ---
        public static event Action<Light> OnLightFlickered;
        public static event Action<Renderer> OnGlassFrosted;
        public static event Action<Rigidbody, Vector3> OnObjectMoved; // target, force direction

        // --- Mementos & Leveling ---
        public static event Action<GameObject> OnMementoCollected; // memento object
        public static event Action<GameObject> OnMementoReturned;
        public static event Action<int> OnGhostLevelUp; // new level

        // --- Tether ---
        public static event Action<float> OnTetherDistanceChanged; // normalized 0-1
        public static event Action OnTetherWarning;
        public static event Action OnTetherSnapped;

        // --- Submarine ---
        public static event Action<float> OnOxygenChanged;
        public static event Action<float> OnHullIntegrityChanged;
        public static event Action<float> OnPowerLevelChanged;
        public static event Action OnSubmarineCritical;

        // --- Hazards ---
        public static event Action<GameObject, float> OnPlayerDamaged; // player, amount
        public static event Action<GameObject> OnHazardTriggered;

        // --- Puzzle Framework ---
        public static event Action<int> OnPuzzleStarted; // puzzle index
        public static event Action<int> OnPuzzleFailed;

        // --- Ghost Abilities ---
        public static event Action<GameObject> OnPossessionStarted;
        public static event Action OnPossessionEnded;
        public static event Action OnEchoSightActivated;

        // === Fire Methods ===

        public static void FireGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);
        public static void FireRoleAssigned(PlayerRoleType role) => OnRoleAssigned?.Invoke(role);
        public static void FirePhaseChanged(GamePhase phase) => OnPhaseChanged?.Invoke(phase);
        public static void FirePuzzleCompleted(int index) => OnPuzzleCompleted?.Invoke(index);
        public static void FireEnergyChanged(float current, float max) => OnEnergyChanged?.Invoke(current, max);
        public static void FireEnergyDepleted() => OnEnergyDepleted?.Invoke();
        public static void FireGhostTap(Vector3 worldPos) => OnGhostTap?.Invoke(worldPos);
        public static void FireSpiritTagApplied(GameObject obj, float duration) => OnSpiritTagApplied?.Invoke(obj, duration);
        public static void FireSpiritTagExpired(GameObject obj) => OnSpiritTagExpired?.Invoke(obj);
        public static void FireLightFlickered(Light light) => OnLightFlickered?.Invoke(light);
        public static void FireGlassFrosted(Renderer renderer) => OnGlassFrosted?.Invoke(renderer);
        public static void FireObjectMoved(Rigidbody rb, Vector3 force) => OnObjectMoved?.Invoke(rb, force);
        public static void FireMementoCollected(GameObject memento) => OnMementoCollected?.Invoke(memento);
        public static void FireMementoReturned(GameObject memento) => OnMementoReturned?.Invoke(memento);
        public static void FireGhostLevelUp(int level) => OnGhostLevelUp?.Invoke(level);
        public static void FireTetherDistanceChanged(float normalized) => OnTetherDistanceChanged?.Invoke(normalized);
        public static void FireTetherWarning() => OnTetherWarning?.Invoke();
        public static void FireTetherSnapped() => OnTetherSnapped?.Invoke();
        public static void FireOxygenChanged(float value) => OnOxygenChanged?.Invoke(value);
        public static void FireHullIntegrityChanged(float value) => OnHullIntegrityChanged?.Invoke(value);
        public static void FirePowerLevelChanged(float value) => OnPowerLevelChanged?.Invoke(value);
        public static void FireSubmarineCritical() => OnSubmarineCritical?.Invoke();
        public static void FirePlayerDamaged(GameObject player, float amount) => OnPlayerDamaged?.Invoke(player, amount);
        public static void FireHazardTriggered(GameObject hazard) => OnHazardTriggered?.Invoke(hazard);
        public static void FirePuzzleStarted(int index) => OnPuzzleStarted?.Invoke(index);
        public static void FirePuzzleFailed(int index) => OnPuzzleFailed?.Invoke(index);
        public static void FirePossessionStarted(GameObject target) => OnPossessionStarted?.Invoke(target);
        public static void FirePossessionEnded() => OnPossessionEnded?.Invoke();
        public static void FireEchoSightActivated() => OnEchoSightActivated?.Invoke();
    }

    // --- Enums used across the project ---

    public enum GameState
    {
        Lobby,
        InGame,
        Paused,
        GameOver
    }

    public enum PlayerRoleType
    {
        None,
        Survivor,
        Ghost
    }

    public enum GamePhase
    {
        Calibration,
        Expansion,
        Convergence
    }

    public enum GlowColorType
    {
        Danger,
        Interaction,
        Collectible,
        SpiritTag,
        Custom
    }

    public enum PuzzleType
    {
        Solo_Survivor,
        Solo_Ghost,
        Collaborative
    }
}
