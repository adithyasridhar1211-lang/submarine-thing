# Abyssal Echo — Development Log

**Project:** Abyssal Echo (Asymmetric VR Co-op Submarine Game)  
**Engine:** Unity 6000.2.5f1  
**Platform:** OpenXR (Meta Quest, SteamVR)  
**Team Start Date:** January 2026

---

## v0.1.0 — Project Foundation
**Date:** Jan 12, 2026  
**Milestone:** Skeleton project + basic VR movement

### What was done
- Created Unity project from VR template (URP 17.2)
- Imported XR Interaction Toolkit 3.2.1, XR Hands 1.6.1, OpenXR 1.15.1
- Set up basic VR locomotion (continuous move + snap turn) in SampleScene
- Configured build targets for Android (Quest) and PC (SteamVR)
- Established project folder structure and coding conventions

### Known issues
- No game logic yet — just a VR sandbox
- Default VR template scene with no custom content

---

## v0.2.0 — Core Architecture
**Date:** Jan 28, 2026  
**Milestone:** Event-driven architecture + game state management

### Added
- `GameEvents.cs` — Central static event bus with 30+ events covering all game systems
- `GameManager.cs` — Singleton state machine: Lobby → InGame → Paused → GameOver
- `NetworkManager.cs` — Network abstraction layer with `INetworkService` interface
  - `LocalNetworkService` stub for same-machine testing
  - Designed for future swap to Netcode for GameObjects / Photon Fusion
- `SessionManager.cs` — Lobby management, role selection, player ready-up validation
- `GameConstants.cs` — All magic numbers, tags, layers, and color constants centralized
- Defined core enums: `GameState`, `PlayerRoleType`, `GamePhase`, `GlowColorType`, `PuzzleType`

### Design decisions
- Went with a static event bus over ScriptableObject events for simplicity and performance. All cross-system communication is decoupled — no script needs a reference to any other.
- Network abstraction means we can develop and test entirely offline. The `LocalNetworkService` delivers messages synchronously, making debugging trivial.
- `SessionManager` enforces exactly one Survivor + one Ghost before game start. No duplicate roles allowed.

### Known issues
- No player spawning yet — GameManager has prefab slots but nothing to assign
- Network abstraction is untested beyond local mode

---

## v0.3.0 — Player Systems & Role Asymmetry
**Date:** Feb 10, 2026  
**Milestone:** Both player types playable with distinct vision

### Added
- `PlayerController.cs` — Abstract base for both player types. Shared VR rig references, movement toggle, teleport API
- `SurvivorController.cs` — Living player with:
  - Desaturated (grayscale/sepia) vision via URP post-processing
  - Oxygen depletion system (passive drain, suffocation damage at 0%)
  - Health system with damage and death
  - Directional haptic feedback from Ghost taps (left/right controller intensity based on tap direction relative to head)
  - Tool equip/use stubs
  - XR grab interaction on both hands
- `GhostController.cs` — Echo player with:
  - High-contrast spectral vision (boosted saturation, bloom, chromatic aberration)
  - Semi-transparent body rendering (URP Transparent surface mode)
  - Energy-gated abilities: tap, tag, possess (L2), echo sight (L3)
  - Level-up system with ability unlocks
  - Point tracking line renderer (Level 1 visual)
- `VisionSystem.cs` — Runtime URP Volume Profile with role-specific overrides
  - Survivor: saturation -80, sepia filter, vignette
  - Ghost: saturation +30, bloom 1.5, chromatic aberration 0.15
  - Temporary override API for Echo Sight ability
- `GhostEnergySystem.cs` — Energy resource with:
  - Passive regeneration (2/s)
  - Regen cooldown after spend (2s delay)
  - Debug toggle for infinite energy
  - Full API: TrySpend, CanAfford, AddEnergy, FullRestore

### Changed
- `GameManager` now spawns player prefabs at designated spawn points on game start
- Added `CharacterController` requirement to `PlayerController`

### Design decisions
- Vision is handled through runtime Volume Profiles, not separate profiles per player. This means we don't need to manage profile assets — everything is code-configured and tweakable at runtime.
- Haptic direction is calculated in head-local space. This means the haptic feedback rotates with the player's head, so "left buzz" always means "to your left" regardless of which way you're facing.
- Ghost transparency is set via material property modification at runtime, not separate transparent materials. This means any mesh can be used for the Ghost body without needing special material variants.

### Known issues
- Ghost abilities fire events but no receiver systems exist yet
- No tether constraint — Ghost can fly anywhere
- Vision system requires Volume component on camera; will error if missing

---

## v0.3.1 — Tether System
**Date:** Feb 14, 2026  
**Milestone:** Ghost leash with visual feedback

### Added
- `TetherSystem.cs` — Distance constraint between Ghost and Survivor:
  - Configurable max distance (default 10m)
  - Warning haptics at 80% distance
  - Hard snap-back when exceeding max
  - Catenary sag effect on visual line (looks like a cable, not a straight line)
  - Spectral Anchor support — tether origin can be relocated
  - Normal (blue) and warning (red/yellow) color gradients
  - Auto-creates LineRenderer if not assigned
  - Auto-finds Survivor from GameManager if not assigned

### Known issues
- Snap-back is instantaneous and harsh — may cause VR discomfort. Will need smoothing or a "rubber band" approach.
- Tether line uses default URP material; will need custom shader for ghostly appearance

---

## v0.4.0 — Communication Mechanics
**Date:** Feb 20, 2026  
**Milestone:** Ghost-to-Survivor non-verbal communication system

### Added
- `HapticTapSystem.cs` — Ghost surface tapping:
  - Input action binding for tap gesture
  - Proximity-based tap detection (hand near surface + button press)
  - Raycast fallback for longer-range taps
  - 3D spatial audio at tap point (AudioSource with spatialBlend=1)
  - Visual ripple VFX prefab spawning (auto-destroyed)
  - Tap cooldown (0.3s) to prevent spam
- `ManifestationSystem.cs` — Physical world manipulation:
  - Flicker lights: finds nearest Light, rapidly toggles intensity for 2s (coroutine)
  - Frost glass: animates shader `_FrostAmount` property 0→1, holds, then fades back
  - Move objects: applies impulse force to nearest Rigidbody under 5kg mass limit
  - All abilities gated by GhostEnergySystem
  - Nearest-object finding via `Physics.OverlapSphere`
- `SpiritTagSystem.cs` — Object tagging:
  - Ghost aims at object with pointing hand
  - Raycast + aim line renderer with hit indicator
  - Tags object by adding/configuring GlowableObject component
  - Glow lasts 5s: full brightness for 70%, then 30% fade-out
  - Re-tagging refreshes the timer
  - Energy cost: 10 per tag

### Changed
- `SurvivorController` now subscribes to `OnGhostTap` and sends directional haptics
- `GhostController` now has public methods calling into HapticTapSystem and SpiritTagSystem

### Design decisions
- Tap sound uses full 3D spatialization — the Survivor hears it from the direction it came, reinforcing the directional haptic feedback. Two senses (touch + hearing) work together.
- Glass frosting uses a shader property rather than a separate material. This means any material with a `_FrostAmount` property will work — custom glass shaders need to implement this.
- Object mass limit (5kg) prevents the Ghost from throwing major objects around. Only small items like cups, books, tools can be moved.

### Known issues
- Frost glass requires materials to have `_FrostAmount` shader property — no standard URP shader has this; custom shader needed
- No visual feedback for Ghost when ability fails (only console log)
- Tap ripple prefab not created — needs VFX

---

## v0.5.0 — Environment & Interactables
**Date:** Mar 1, 2026  
**Milestone:** Submarine environment + interactable objects framework

### Added
- `GlowableObject.cs` — Universal glow utility:
  - Emission-based glow via MaterialPropertyBlock (GPU efficient, no material instances)
  - Preset colors: Danger (red), Interaction (blue), Collectible (gold), SpiritTag
  - Pulsating animation (sine wave oscillation)
  - Ghost-only visibility toggle
  - Role-based visibility switching
- `InteractableBase.cs` — Base class for all interactables:
  - XR Interaction Toolkit integration (selectEntered, hoverEntered/Exited)
  - Role-based permission system (canSurvivorInteract, canGhostInteract, requiredGhostLevel)
  - Lock/unlock state
  - Glow-on-hover feedback (brighter when hand is near)
  - Auto-detect player role from root GameObject tag
  - Audio feedback (interact sound, locked sound)
- `Lever.cs` — Physical toggle lever:
  - Animated handle rotation (on/off angles with smooth interpolation)
  - Configurable rotation axis and speed
  - UnityEvent callbacks for on/off states (wire up in Inspector to trigger doors, lights, etc.)
- `Valve.cs` — Rotatable valve:
  - Continuous progress tracking (0% to 100%)
  - Configurable rotation axis and max rotation (default 720°)
  - Color-coded (Ghost sees color, Survivor doesn't)
  - UnityEvent callbacks for progress, fully open, fully closed
- `WirePanel.cs` — Color-coded wire puzzle framework:
  - List of Wire structs: name, color, texture type, correct-to-cut flag
  - Panel cover open/close with optional animation
  - Wire cutting with right/wrong feedback
  - Completion check when all correct wires are cut
  - Wire textures (Smooth, Braided, Corrugated, Ribbed, Coiled) for Survivor tactile identification
- `SpectralAnchor.cs` — Ghost tether extension:
  - Energy cost to deploy (30)
  - Adds extra tether range when activated
  - Ghost-only glow + particle system visual
  - Activate/deactivate with TetherSystem integration
- `HazardZone.cs` — Damage zones:
  - Trigger-based continuous damage to Survivor on stay
  - Multiple hazard types (Electrical, Flooding, GasLeak, BrokenGlass, Pressure, Fire, Radiation)
  - Ghost-only visibility (red danger glow)
  - Particle system + ambient sound support
  - Enable/disable API (for puzzle-gated hazard removal)
- `SubmarineManager.cs` — Central environment manager:
  - Power, hull integrity, and oxygen tracking
  - Passive resource depletion while InGame
  - Light intensity scales with power level
  - Low-power light flickering effect
  - Random ambient creaking sounds at intervals
  - Critical state system (<20% on any resource triggers emergency mode)
  - Emergency lights (red) + alarm audio
  - Hull breach accelerates oxygen depletion 5x
  - Repair/restore API for puzzle rewards

### Changed
- `InteractableBase` now uses fully qualified `IXRInteractor` namespace to resolve ambiguity with XRI 3.2.1 interactor types

### Design decisions
- `GlowableObject` uses `MaterialPropertyBlock` instead of material instances. This is critical for VR performance — material instances break GPU instancing and increase draw calls. Property blocks keep instancing intact.
- `InteractableBase` determines player role by walking up the hierarchy to the root and checking the tag. This is simple and works regardless of how the XR rig is structured. No need for a custom interactor or tracker component.
- Wire textures were added alongside colors because the Survivor needs some way to identify wires. Since they can't see color, physical texture gives them information to work with ("the braided one, third from left").
- SubmarineManager creaking sounds use random intervals with variance to feel organic. Fixed intervals feel mechanical and predictable.

### Known issues
- No custom glass frost shader provided — will need to be created for ManifestationSystem frost ability
- Hazard VFX prefabs not created — need particle systems for each hazard type
- SubmarineManager hull breach has no visual flooding effect

---

## v0.5.1 — Progression & Leveling
**Date:** Mar 2, 2026  
**Milestone:** Phase system + Ghost ability unlocks

### Added
- `PhaseConfig.cs` — ScriptableObject for per-phase parameters:
  - Tether distance, energy caps, regen rate
  - Oxygen/power depletion rates
  - Puzzle count required to advance
  - Mechanic unlock flags (hazards, anchors, simultaneous puzzles, dark areas)
- `GhostUpgradeConfig.cs` — ScriptableObject for Ghost abilities:
  - Memento thresholds per level (1, 3, 6)
  - Per-ability duration, energy cost, cooldown
  - `GetLevelForMementos()` helper method
- `PhaseManager.cs` — Phase progression controller:
  - Calibration → Expansion → Convergence
  - Applies PhaseConfig to all relevant systems on transition
  - Puzzle completion counter with auto-advance when threshold met
  - Listens to OnGameStateChanged to auto-start phases on InGame
- `MementoSystem.cs` — Collectible tracking singleton:
  - Tracks collected and returned memento counts
  - Level-up check on each return
- `MementoPickup.cs` — Component for collectible objects:
  - Trigger-based pickup by Survivor
  - Gold glow via GlowableObject (ghost-only)
  - Collection hides object but keeps it alive for return
  - ReturnToGhost() triggers MementoSystem and destroys object
- `GhostUpgradeSystem.cs` — Ability activation manager:
  - Level 1: activates mist particle system on hand
  - Level 2: possession coroutine — 3s interaction window, 10s cooldown
  - Level 3: echo sight coroutine — 5s clear vision for Survivor, 30s cooldown
  - Cooldown timers prevent spam

### Changed
- `PhaseManager` applies config changes to `TetherSystem`, `GhostEnergySystem`, and `SubmarineManager` on phase transition

### Known issues
- Memento return-to-Ghost has no physical proximity check yet — needs trigger zone around Ghost
- Echo Sight only changes post-processing; no actual "replay" of past events (left as future feature)

---

## v0.6.0 — UI, HUD & Puzzle Framework
**Date:** Mar 3, 2026  
**Milestone:** Player HUDs + puzzle skeleton + lobby screen

### Added
- `SurvivorHUD.cs` — World-space canvas:
  - Oxygen bar with color shift (blue → red below 30%)
  - Health bar
  - Equipped tool icon + name
  - Temporary prompt text with fade animation
  - Warning overlay with red pulsation during critical submarine state
- `GhostHUD.cs` — World-space canvas:
  - Energy bar with 3-color transition (blue → orange → dark red)
  - Tether distance bar (inverted — shrinks as you move away)
  - Ghost level display with ability icon unlock states
  - Possession + Echo Sight cooldown fill indicators
  - Status messages with auto-fade ("⚠ Tether limit!", "⚡ Energy depleted!")
- `LobbyUI.cs` — Role selection screen:
  - Survivor / Ghost selection buttons with highlight borders
  - Role description cards with formatted text
  - Ready/Cancel toggle button
  - Player 1 & 2 role + ready state display
  - Status text progression: "Choose role" → "Selected" → "Waiting..."
  - Auto-hides when game state leaves Lobby
- `PuzzleBase.cs` — Abstract puzzle skeleton:
  - Lifecycle: StartPuzzle → OnInitialize → CheckCompletion loop → Complete/Fail
  - Optional timer with auto-fail
  - Audio hooks for start/complete/fail
  - Events: OnPuzzleComplete, OnPuzzleFail
  - Abstract methods for subclasses: `OnInitialize()`, `CheckCompletion()`
- `PuzzleArea.cs` — Trigger-based puzzle activation:
  - Configurable per-role activation
  - Optional auto-start
  - Single-activation guard
  - Visual indicator with active/completed color states
- `PuzzleManager.cs` — Scene-wide puzzle tracker:
  - Auto-discovers all PuzzleBase components
  - Completion/failure counters
  - Runtime puzzle registration
  - Query by type or phase

### Changed
- All systems now have full event subscriptions wired up
- `GameManager`, `SessionManager`, `NetworkManager`, `SubmarineManager`, `PhaseManager`, `PuzzleManager`, `MementoSystem` are all singletons ready for scene placement
- Full documentation added: `DOCUMENTATION.md` (formal reference) and `HOW_IT_WORKS.md` (code snippets for devs/non-technical)

### Known issues
- HUD canvases need to be built in Unity — script references exist but no prefab yet
- Lobby UI needs XR raycaster support for button interaction in VR
- Puzzle subclasses not implemented — waiting for puzzle design specifications
- No networking integration test beyond LocalNetworkService
- Glass frost shader, VFX prefabs, and audio clips remain placeholder references

---

## Roadmap — What's Next

### v0.7.0 — Puzzle Implementation (Pending Design)
- Concrete puzzle subclasses based on design specs
- Sequence Overload (lever order puzzle)
- Tactile Circuit Board (texture-based wire tracing)
- Ethereal Conduit (Ghost-only 3D maze)
- Spatial Alignment (Spirit Shard positioning)
- Color-Coded Repair (collaborative wire cutting)
- Overwatch Navigation (Ghost guides Survivor through dark maze)

### v0.8.0 — Networking Integration
- Select and integrate networking solution
- Player synchronization (position, rotation, animations)
- State replication (interactable states, puzzle progress, Ghost energy)
- Voice chat integration (or built-in spatial audio)

### v0.9.0 — Polish & VFX
- Custom glass frost shader
- Hazard VFX (sparks, water, gas clouds, fire)
- Ghost body VFX (ethereal particles, transparency shader)
- Tap ripple VFX
- Spectral Anchor deployment VFX
- Sound design pass (ambient, feedback, music)

### v1.0.0 — Release Candidate
- Full QA pass
- Performance optimization for Quest
- Accessibility options
- Tutorial / onboarding flow
- Build and deploy pipeline
