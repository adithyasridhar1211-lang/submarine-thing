# Submarine Co-op VR — Complete Script & Systems Documentation

This document explains every script, component, and prefab configuration in the project. Use it as a reference when setting up scenes, building prefabs, and wiring systems together in the Unity Editor.

---

## Table of Contents

1. [Project Architecture Overview](#1-project-architecture-overview)
2. [Core Infrastructure](#2-core-infrastructure)
3. [Player Systems](#3-player-systems)
4. [Communication & Interaction](#4-communication--interaction)
5. [Environment & Submarine](#5-environment--submarine)
6. [Progression & Leveling](#6-progression--leveling)
7. [Puzzle Framework](#7-puzzle-framework)
8. [UI & HUD](#8-ui--hud)
9. [ScriptableObject Setup](#9-scriptableobject-setup)
10. [Prefab Setup Guide](#10-prefab-setup-guide)
11. [Scene Setup Checklist](#11-scene-setup-checklist)

---

## 1. Project Architecture Overview

The game is an **asymmetric 2-player VR co-op** set inside a submarine. One player is the **Survivor** (full physical agency, impaired vision) and the other is the **Ghost/Echo** (full perception, limited physical interaction).

### System Communication

All systems communicate through a **central event bus** (`GameEvents.cs`). No system holds a direct reference to another unless absolutely necessary — this keeps things decoupled and easy to extend.

```
GameManager (top-level state)
├── SessionManager (lobby, roles)
├── NetworkManager (abstraction layer)
├── PhaseManager (progression)
│   └── PuzzleManager (tracks puzzles)
├── SubmarineManager (environment state)
└── Players
    ├── SurvivorController → VisionSystem, HUD
    └── GhostController → EnergySystem, TetherSystem, HUD
        ├── HapticTapSystem
        ├── ManifestationSystem
        ├── SpiritTagSystem
        └── GhostUpgradeSystem
```

### Namespace Structure

All scripts use the `SubmarineCoop` root namespace:

| Namespace | Purpose |
|-----------|---------|
| `SubmarineCoop.Core` | Game state, events, networking, session |
| `SubmarineCoop.Player` | Player controllers, vision, tether, energy |
| `SubmarineCoop.Communication` | Haptic taps, manifestation, spirit tags |
| `SubmarineCoop.Environment` | Interactables, submarine, hazards, glow |
| `SubmarineCoop.Progression` | Phases, mementos, upgrades |
| `SubmarineCoop.Puzzle` | Puzzle framework (base classes) |
| `SubmarineCoop.UI` | HUDs, lobby |
| `SubmarineCoop.Utility` | Constants, helpers |

---

## 2. Core Infrastructure

### GameConstants.cs
**Path:** `Scripts/Utility/GameConstants.cs`  
**Type:** Static class (no MonoBehaviour)

A single source of truth for all magic numbers used across the project. Instead of scattering `10f` and `"Survivor"` throughout scripts, everything is defined here.

**Key sections:**
- **Player tags/layers** — String constants for tags like `"Survivor"`, `"Ghost"` and layer names like `"Interactable"`, `"GhostOnly"`
- **Tether defaults** — Max distance (10m), warning threshold (80%), snap force
- **Energy costs** — Every Ghost ability has a defined energy cost (tap=1, spirit tag=10, flicker light=5, frost glass=15, move object=20, possession=25, spectral anchor=30)
- **Vision values** — Desaturation amount for Survivor (-80), saturation boost for Ghost (+30), bloom intensity
- **Submarine defaults** — Starting hull/oxygen/power values, depletion rates
- **Memento thresholds** — How many mementos unlock each Ghost level (1, 3, 6)
- **Color constants** — `COLOR_DANGER` (red), `COLOR_INTERACTION` (blue), `COLOR_COLLECTIBLE` (gold), `COLOR_SPIRIT_TAG`, `COLOR_TETHER`

**Usage:** Reference like `GameConstants.ENERGY_COST_SPIRIT_TAG` from anywhere.

---

### GameEvents.cs
**Path:** `Scripts/Core/GameEvents.cs`  
**Type:** Static class + Enums

The central **event bus**. Every major game event has a static `event Action` and a corresponding `Fire___()` method. Any script can subscribe to events without needing references to the publishing script.

**Events include:**
| Category | Events |
|----------|--------|
| Game State | `OnGameStateChanged`, `OnRoleAssigned` |
| Phase | `OnPhaseChanged`, `OnPuzzleCompleted` |
| Energy | `OnEnergyChanged(current, max)`, `OnEnergyDepleted` |
| Communication | `OnGhostTap(worldPos)`, `OnSpiritTagApplied`, `OnSpiritTagExpired` |
| Manifestation | `OnLightFlickered`, `OnGlassFrosted`, `OnObjectMoved` |
| Mementos | `OnMementoCollected`, `OnMementoReturned`, `OnGhostLevelUp(level)` |
| Tether | `OnTetherDistanceChanged(normalized)`, `OnTetherWarning`, `OnTetherSnapped` |
| Submarine | `OnOxygenChanged`, `OnHullIntegrityChanged`, `OnPowerLevelChanged`, `OnSubmarineCritical` |
| Hazards | `OnPlayerDamaged(player, amount)`, `OnHazardTriggered` |
| Ghost Abilities | `OnPossessionStarted`, `OnPossessionEnded`, `OnEchoSightActivated` |

**Enums defined here:**
- `GameState` — Lobby, InGame, Paused, GameOver
- `PlayerRoleType` — None, Survivor, Ghost
- `GamePhase` — Calibration, Expansion, Convergence
- `GlowColorType` — Danger, Interaction, Collectible, SpiritTag, Custom
- `PuzzleType` — Solo_Survivor, Solo_Ghost, Collaborative

**How to use:**
```csharp
// Subscribe
GameEvents.OnGhostTap += HandleGhostTap;

// Publish
GameEvents.FireGhostTap(worldPosition);

// Unsubscribe (in OnDestroy)
GameEvents.OnGhostTap -= HandleGhostTap;
```

---

### GameManager.cs
**Path:** `Scripts/Core/GameManager.cs`  
**Type:** Singleton MonoBehaviour, `DontDestroyOnLoad`

The **top-level game controller**. Manages the game lifecycle: Lobby → InGame → Paused → GameOver.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `survivorPrefab` | GameObject | The Survivor player prefab to spawn |
| `ghostPrefab` | GameObject | The Ghost player prefab to spawn |
| `survivorSpawnPoint` | Transform | Where the Survivor appears |
| `ghostSpawnPoint` | Transform | Where the Ghost appears |

**Key public methods:**
- `AssignLocalRole(PlayerRoleType)` — Called by SessionManager/LobbyUI when player picks a role
- `StartGame()` — Transitions to InGame, spawns players
- `PauseGame()` / `ResumeGame()` — State toggles
- `EndGame()` — Transitions to GameOver (called on player death)
- `ReturnToLobby()` — Destroys players, resets to Lobby

**Key properties:**
- `Instance` — Singleton access
- `CurrentState` — Current GameState enum
- `LocalPlayerRole` — Which role the local player chose
- `SurvivorPlayer` / `GhostPlayer` — References to the spawned player GameObjects

**Setup:** Place on an empty GameObject named `GameManager` in your scene. Assign the prefab and spawn point fields.

---

### NetworkManager.cs
**Path:** `Scripts/Core/NetworkManager.cs`  
**Type:** Singleton MonoBehaviour, `DontDestroyOnLoad`

An **abstraction layer** for networking. Currently uses `LocalNetworkService` (both players on same machine). When you choose a networking solution (Netcode, Photon, Mirror), you implement `INetworkService` and swap it in.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `networkMode` | NetworkMode enum | Local, NetcodeForGameObjects, PhotonFusion, Mirror |

**INetworkService interface methods:**
- `Initialize()`, `Host()`, `Join(sessionId)`, `Leave()`, `Shutdown()`
- `SendMessage(channel, data, targetPlayerId)` — Send bytes to all/specific player
- `RegisterHandler(channel, handler)` — Listen for messages on a channel
- `SpawnObject(prefab, position, rotation)` — Instantiate across network

**LocalNetworkService:** The built-in stub. Messages are delivered immediately to local handlers. `SpawnObject()` just calls `Instantiate()`. Perfect for testing.

**Setup:** Place on empty GameObject named `NetworkManager`. Leave mode as Local for now.

---

### SessionManager.cs
**Path:** `Scripts/Core/SessionManager.cs`  
**Type:** Singleton MonoBehaviour

Manages the **lobby flow**: player joining, role selection, readying up, and validation that there is exactly one Survivor and one Ghost before the game can start.

**Key methods:**
- `SetPlayerRole(playerId, role)` — Assigns a role; blocks if role already taken
- `SetPlayerReady(playerId)` — Marks player as ready; auto-starts game if both ready
- `HasValidRoles()` — Returns true if exactly 1 Survivor + 1 Ghost
- `ResetSession()` — Clears all roles/ready states for a new game

**Events:**
- `OnPlayerRoleChanged`, `OnPlayerReady`, `OnAllPlayersReady`
- `OnPlayerJoinedSession`, `OnPlayerLeftSession`

**Setup:** Place on empty GameObject named `SessionManager`.

---

## 3. Player Systems

### PlayerController.cs
**Path:** `Scripts/Player/PlayerController.cs`  
**Type:** Abstract MonoBehaviour (requires `CharacterController`)

The **base class** for both Survivor and Ghost. Handles common VR rig references and basic movement.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `xrOrigin` | Transform | The XR Origin root |
| `headTransform` | Transform | Main Camera / headset |
| `leftHandTransform` | Transform | Left hand anchor |
| `rightHandTransform` | Transform | Right hand anchor |
| `moveSpeed` | float | Movement speed (default 2) |

**Virtual methods subclasses override:**
- `Initialize()` — Called once in `Start()`. Set up role-specific systems.
- `OnUpdate()` — Called every frame for local player only.

**Utility methods:**
- `GetHeadPosition()` / `GetLookDirection()` — Common lookups
- `TeleportTo(position, rotation)` — Moves the XR Origin
- `SetMovementEnabled(bool)` — Toggle movement

---

### SurvivorController.cs
**Path:** `Scripts/Player/SurvivorController.cs`  
**Type:** Extends `PlayerController`

The **Living Player**. Full physical agency, impaired (desaturated) vision.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `visionSystem` | VisionSystem | Applies desaturated post-processing |
| `maxHealth` | float | Starting hp (default 100) |
| `leftGrabInteractor` / `rightGrabInteractor` | XRBaseInteractor | XRI grab references |
| `leftController` / `rightController` | XRBaseController | For sending haptic impulses |

**What it does:**
- On `Initialize()`: applies Survivor vision (desaturated), subscribes to `OnGhostTap`, `OnOxygenChanged`, `OnPlayerDamaged`
- Every frame: depletes oxygen passively. When oxygen hits 0, takes suffocation damage
- `SendDirectionalHaptic(worldPos)`: converts a world position to left/right controller haptic intensity based on which direction the tap came from relative to the player's head
- `TakeDamage(amount)`: reduces health. If 0 → calls `GameManager.EndGame()`
- `EquipTool(GameObject)` / `UseTool()`: placeholder for tool system

**How haptic direction works:**
When the Ghost taps at a world position, this script calculates the direction from the Survivor's head to the tap point, converts to local space, and makes the left controller vibrate more if the tap is to the left, and vice versa. This gives the Survivor spatial awareness of where the Ghost is tapping.

---

### GhostController.cs
**Path:** `Scripts/Player/GhostController.cs`  
**Type:** Extends `PlayerController`

The **Echo Player**. Full spectral perception, limited physical interaction gated by energy.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `visionSystem` | VisionSystem | Applies high-contrast spectral vision |
| `energySystem` | GhostEnergySystem | Energy resource |
| `tetherSystem` | TetherSystem | Distance constraint |
| `ghostBodyRenderers` | Renderer[] | Renderers to make semi-transparent |
| `ghostAlpha` | float | Transparency level (default 0.4) |
| `mistParticleSystem` | ParticleSystem | Level 1 point tracking VFX |
| `pointingLine` | LineRenderer | Level 1 pointing visual |

**Key abilities (all cost energy):**
- `TapSurface(worldPos)` — Costs 1 energy. Fires `OnGhostTap`
- `TagObject(target)` — Costs 10 energy. Fires `OnSpiritTagApplied`
- `TryPossess(target)` — Requires Level 2. Costs 25 energy
- `TryEchoSight()` — Requires Level 3. Costs 25 energy

**Level-up (`LevelUp(int)`):**
- Level 1: Enables point tracking (mist particles + line renderer)
- Level 2: Enables possession
- Level 3: Enables Echo Sight

**Transparency:** On Initialize, iterates all `ghostBodyRenderers` and sets their materials to transparent mode (URP Surface Type = Transparent, alpha = `ghostAlpha`, Z-write off).

---

### VisionSystem.cs
**Path:** `Scripts/Player/VisionSystem.cs`  
**Type:** MonoBehaviour

Manages **URP post-processing** for each role using a runtime Volume Profile.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `postProcessVolume` | Volume | URP Volume component on the camera |
| `survivorSaturation` | float | -80 (heavy desaturation) |
| `survivorColorFilter` | Color | Sepia tint (0.9, 0.85, 0.7) |
| `survivorVignette` | float | 0.35 edge darkening |
| `ghostSaturation` | float | +30 (vivid colors) |
| `ghostBloomIntensity` | float | 1.5 (glow on bright objects) |
| `ghostChromaticAberration` | float | 0.15 (ethereal edge distortion) |

**How it works:**
- Creates a **runtime** VolumeProfile (so the shared asset isn't modified)
- Adds `ColorAdjustments`, `Bloom`, `Vignette`, `ChromaticAberration` overrides
- `ApplyRoleVision(role)`: enables/disables overrides per role
- `TemporaryOverride(saturation, bloom, duration)`: used by Echo Sight to briefly give the Survivor clear vision, then auto-restores

**Survivor vision:** Desaturated (nearly grayscale), sepia color filter, edge vignette, no bloom  
**Ghost vision:** Boosted saturation, bloom on bright objects, subtle chromatic aberration, no vignette

**Setup:** Attach to each player prefab. Ensure the player's camera has a URP Volume component; assign it as the `postProcessVolume`.

---

### TetherSystem.cs
**Path:** `Scripts/Player/TetherSystem.cs`  
**Type:** MonoBehaviour

Constrains the **Ghost to within a configurable distance** of the Survivor (or a Spectral Anchor).

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `maxDistance` | float | Max allowed distance (default 10m) |
| `ghostTransform` / `survivorTransform` | Transform | The two endpoints |
| `tetherLineRenderer` | LineRenderer | Visual line (auto-created if null) |
| `lineSegments` | int | Number of points in the line (default 20) |
| `lineSag` | float | Catenary droop effect (0.5) |
| `normalGradient` / `warningGradient` | Gradient | Line color based on state |

**Behavior:**
- Every frame: calculates distance from Ghost to anchor (Survivor or Spectral Anchor)
- Fires `OnTetherDistanceChanged(normalized)` for the HUD
- When at 80% of max distance: fires `OnTetherWarning` (triggers HUD warning + haptics)
- When exceeding max distance: **snaps the Ghost back** towards the anchor using `snapForce`
- Visual line uses a **catenary sag** algorithm — the line droops more as you get further away

**Spectral Anchor support:**
- `SetTetherAnchor(transform)` — Redirects the tether origin from the Survivor to a placed anchor
- `SetMaxDistance(float)` — Extends range (used by PhaseManager or SpectralAnchors)

---

### GhostEnergySystem.cs
**Path:** `Scripts/Player/GhostEnergySystem.cs`  
**Type:** MonoBehaviour

The Ghost's **energy resource**. All abilities cost energy. It regenerates passively.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `maxEnergy` | float | Maximum capacity (default 100) |
| `regenRate` | float | Energy per second (default 2) |
| `regenDelay` | float | Seconds after spending before regen starts (2s) |
| `infiniteEnergy` | bool | Debug toggle for testing |

**Key methods:**
- `TrySpendEnergy(amount)` → `bool` — Deducts energy if available. Returns false + fires `OnEnergyDepleted` if insufficient. Starts regen cooldown on success.
- `CanAfford(amount)` → `bool` — Check without spending
- `AddEnergy(amount)` — Add from external source
- `FullRestore()` — Refill completely

**Regen behavior:** After any energy spend, regeneration pauses for `regenDelay` seconds, then resumes at `regenRate` per second.

---

## 4. Communication & Interaction

### HapticTapSystem.cs
**Path:** `Scripts/Communication/HapticTapSystem.cs`  
**Type:** MonoBehaviour (attached to Ghost)

The Ghost's primary communication tool. **Tap surfaces to send directional haptic feedback and spatial audio to the Survivor.**

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `tapAction` | InputActionReference | XR button input for tapping |
| `handTransform` | Transform | Which hand is used for tapping |
| `tapDistance` | float | Proximity detection radius (0.1m) |
| `tapCooldown` | float | Prevent spam (0.3s) |
| `tapSoundClip` | AudioClip | The 3D sound played at the tap point |
| `tapRipplePrefab` | GameObject | Visual ripple VFX spawned at tap |

**How it works:**
1. Ghost presses tap input (or hand proximity + button press)
2. Raycasts from hand to find nearest surface
3. Fires `GameEvents.FireGhostTap(worldPosition)`
4. Plays a 3D spatial audio clip at the tap point (AudioSource with spatialBlend=1)
5. Spawns a ripple VFX prefab at the tap point (auto-destroyed after 1s)
6. SurvivorController receives the event and sends directional haptics

**Tap Detection Modes:**
- **Input-based:** Button press triggers raycast from hand forward
- **Proximity-based:** When hand is near surface AND button pressed, uses closest point on collider

---

### ManifestationSystem.cs
**Path:** `Scripts/Communication/ManifestationSystem.cs`  
**Type:** MonoBehaviour (attached to Ghost)

Energy-gated **physical world effects** the Ghost can produce.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `energySystem` | GhostEnergySystem | Energy gate |
| `handTransform` | Transform | Origin for finding nearby objects |
| `interactionRange` | float | How far the Ghost can reach (3m) |
| `flickerFrequency` | float | Hz of light on/off toggling (8) |
| `frostShaderProperty` | string | Shader property name for frost effect ("_FrostAmount") |
| `moveForce` | float | Force applied to moved objects (3 N) |
| `maxObjectMass` | float | Can't move objects heavier than this (5 kg) |

**Three abilities:**

| Ability | Cost | Method | What happens |
|---------|------|--------|-------------|
| Flicker Light | 5 | `TryFlickerLight()` | Finds nearest Light component, rapidly toggles intensity for 2s via coroutine |
| Frost Glass | 15 | `TryFrostGlass()` | Finds nearest glass material (checks for `_FrostAmount` property), animates it to 1 then fades back to 0 over 8s |
| Move Object | 20 | `TryMoveObject(direction)` | Finds nearest Rigidbody under maxObjectMass, applies impulse force |

**Object finding:** Each ability uses `Physics.OverlapSphere()` or iterates scene objects to find the nearest valid target within `interactionRange`.

---

### SpiritTagSystem.cs
**Path:** `Scripts/Communication/SpiritTagSystem.cs`  
**Type:** MonoBehaviour (attached to Ghost)

Ghost **tags objects** to make them briefly glow for the Survivor.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `energySystem` | GhostEnergySystem | Energy gate |
| `pointingHand` | Transform | Which hand to aim with |
| `tagRange` | float | How far the ray goes (10m) |
| `tagDuration` | float | How long the glow lasts (5s) |
| `aimLine` | LineRenderer | Visual aiming line |
| `tagHitIndicator` | GameObject | Reticle shown at aim target |

**How it works:**
1. Every frame: updates the aim line renderer from hand → raycast hit point
2. `TryTagTarget()`: Raycasts to find aimed object, then calls `TagObject()`
3. `TagObject(target)`: Costs 10 energy. Adds a `GlowableObject` component if not present. Sets emission glow with tag color.
4. After 70% of duration: begins fading the glow out over the remaining 30%
5. When glow expires: fires `OnSpiritTagExpired`, removes emission

**Re-tagging:** If an already-tagged object is tagged again, the previous timer is cancelled and a fresh duration starts.

---

## 5. Environment & Submarine

### GlowableObject.cs
**Path:** `Scripts/Environment/GlowableObject.cs`  
**Type:** MonoBehaviour

**Utility component** that gives any object emission-based glow. Used by Spirit Tag, interactables, mementos, and hazards.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `glowType` | GlowColorType | Preset color type (Danger, Interaction, etc.) |
| `glowOnStart` | bool | Start glowing immediately |
| `ghostOnlyVisible` | bool | If true, Survivor doesn't see the glow |
| `pulsate` | bool | Animate glow brightness (default true) |
| `pulsateSpeed` | float | Oscillation speed (2) |

**How it works:**
- Uses `MaterialPropertyBlock` to set `_EmissionColor` — this avoids creating material instances and is GPU-efficient
- `SetGlow(color, intensity)` — Activates emission
- `ClearGlow()` — Restores original emission
- `UpdateVisibilityForRole(role)` — If `ghostOnlyVisible` is true, Survivor sees black emission (no glow)
- Optional pulsation: smoothly oscillates between `pulsateMinIntensity` and full intensity using a sine wave

---

### InteractableBase.cs
**Path:** `Scripts/Environment/InteractableBase.cs`  
**Type:** MonoBehaviour (requires `GlowableObject`)

**Base class for all interactable objects.** Integrates with XR Interaction Toolkit and provides role-based filtering.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `canSurvivorInteract` | bool | Can Survivor use this? (default true) |
| `canGhostInteract` | bool | Can Ghost use this? (default false) |
| `requiredGhostLevel` | int | What Ghost level needed (0 = any) |
| `ghostEnergyCost` | float | Energy cost for Ghost to use |
| `glowType` | GlowColorType | Color type visible to Ghost |
| `isLocked` | bool | Prevent all interaction |
| `interactSound` / `lockedSound` | AudioClip | Feedback sounds |

**XRI Integration:**
- Listens to `selectEntered` → determines interactor's role by checking the root GameObject's tag → calls `TryInteract(role)`
- Listens to `hoverEntered`/`hoverExited` → brightens/dims the glow on hover
- Role determination walks up the hierarchy to find "Survivor" or "Ghost" tags

**Key methods:**
- `TryInteract(role, ghostLevel)` → `bool` — Validates permissions, calls `Interact()` if valid
- `Interact(role)` — Virtual. Toggles `isActivated`, plays sound. Override in subclasses.
- `SetLocked(bool)` / `ResetState()` — State management

---

### Lever.cs
**Path:** `Scripts/Environment/Lever.cs`  
**Type:** Extends `InteractableBase`

A **physical toggle lever** with animated rotation.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `leverHandle` | Transform | The handle child that rotates |
| `onAngle` / `offAngle` | float | Rotation angles (45° / -45°) |
| `rotationSpeed` | float | Animation speed (90°/s) |
| `rotationAxis` | Vector3 | Which axis rotates (default X) |
| `onLeverOn` / `onLeverOff` | UnityEvent | Wire up consequences in Inspector |

**Behavior:** Smoothly rotates the handle to `onAngle` when activated, `offAngle` when deactivated. Fires UnityEvents for each state — use these to trigger doors opening, lights turning on, etc.

---

### Valve.cs
**Path:** `Scripts/Environment/Valve.cs`  
**Type:** Extends `InteractableBase`

A **rotatable valve** with continuous progress tracking (0% to 100%).

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `valveWheel` | Transform | The wheel child that rotates |
| `maxRotation` | float | Total degrees to go from 0% to 100% (720°) |
| `valveColor` | Color | Color Ghost sees (Survivor sees grey) |
| `onProgressChanged` | UnityEvent\<float\> | Fires with 0-1 progress |
| `onFullyOpen` / `onFullyClosed` | UnityEvent | State events |

**Usage:** Can be used with XR knob interaction for continuous rotation, or simple toggle via `Interact()`. `Rotate(deltaDegrees)` allows fine-grained control when grabbed.

---

### WirePanel.cs
**Path:** `Scripts/Environment/WirePanel.cs`  
**Type:** Extends `InteractableBase`

A panel with **multiple color-coded wires**. The Ghost sees wire colors; the Survivor does not.

**Wire struct fields:**
| Field | Purpose |
|-------|---------|
| `wireName` | Display name |
| `wireColor` | Color visible to Ghost |
| `isCorrectToCut` | Whether this wire should be cut |
| `isCut` | Current state |
| `wireRenderer` | Reference to the wire's Renderer |
| `textureType` | WireTexture enum (Smooth, Braided, Corrugated, Ribbed, Coiled) — tactile identification for Survivor |

**Key methods:**
- `TogglePanel()` — Opens/closes the panel cover (with optional animation)
- `CutWire(index)` — Cuts a wire, fires events, checks if all correct wires are cut
- `GetWireInfo(index)` — Returns wire data (Ghost uses this to relay info)

**Events:** `onWireCut(index)`, `onAllCorrectCut`, `onWrongCut`

---

### SpectralAnchor.cs
**Path:** `Scripts/Environment/SpectralAnchor.cs`  
**Type:** MonoBehaviour

Deployable **tether extension point** for the Ghost.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `energyCost` | float | Energy to deploy (30) |
| `additionalTetherRange` | float | Extra range added (5m) |
| `anchorParticles` | ParticleSystem | Ghost-only VFX |

**How it works:**
1. Ghost approaches an unactivated anchor in the scene
2. `TryActivate(energySystem, tetherSystem)` — spends energy, calls `Activate()`
3. `Activate()`: sets this as the new tether origin, extends max distance
4. `Deactivate()`: resets tether back to Survivor, removes extra range

**Visuals:** Uses `GlowableObject` for ghost-only glow + particle system when active.

---

### HazardZone.cs
**Path:** `Scripts/Environment/HazardZone.cs`  
**Type:** MonoBehaviour (requires `Collider`)

Trigger zones that **damage the Survivor** but are **only visible to the Ghost**.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `hazardType` | HazardType enum | Electrical, Flooding, GasLeak, BrokenGlass, etc. |
| `damagePerSecond` | float | DPS to Survivor (10) |
| `blocksPath` | bool | Whether it also blocks movement |
| `hazardParticles` | ParticleSystem | Visual effect |
| `hazardVisualEffect` | GameObject | Ghost-only indicator object |

**Behavior:**
- `OnTriggerStay`: if Survivor is inside, fires `OnPlayerDamaged` every frame
- `OnTriggerEnter`: plays damage sound
- Ghost sees the hazard through `GlowableObject` (red danger glow)
- `SetHazardActive(bool)` — Enable/disable the hazard (e.g., after puzzle completion)

---

### SubmarineManager.cs
**Path:** `Scripts/Environment/SubmarineManager.cs`  
**Type:** Singleton MonoBehaviour

Central manager for the **submarine's environmental state**.

**Inspector fields:**
| Field | Type | Purpose |
|-------|------|---------|
| `powerLevel` / `hullIntegrity` / `oxygenLevel` | float | Main resources (all start at 100) |
| `oxygenDepletionRate` / `powerDepletionRate` | float | Drain rates |
| `criticalThreshold` | float | Below this = critical state (20) |
| `submarineLights` | List\<Light\> | All normal lights (dim with power) |
| `emergencyLights` | List\<Light\> | Turn on during critical state |
| `creakingSounds` | AudioClip[] | Random ambient sounds |
| `alarmAudioSource` | AudioSource | Emergency alarm loop |

**Resource behavior:**
- Oxygen and power deplete each frame while InGame
- Lights dim proportionally to power level; flicker when power is low
- Creaking sounds play at random intervals (15s ± 10s)

**Critical state:** When any resource drops below 20%:
- Emergency lights turn on (red)
- Alarm audio starts
- Fires `OnSubmarineCritical`

**Hull breach:** When hull integrity hits 0, oxygen depletion accelerates 5×.

**Repair methods:** `RestoreOxygen()`, `RestorePower()`, `RepairHull()` — called by puzzle completions or interactables.

---

## 6. Progression & Leveling

### PhaseConfig.cs (ScriptableObject)
**Path:** `Scripts/Progression/PhaseConfig.cs`  
**Menu:** Create → SubmarineCoop → Phase Config

Defines parameters for each game phase. You create 3 instances (one per phase).

**Fields:** `maxTetherDistance`, `ghostMaxEnergy`, `ghostEnergyRegenRate`, `oxygenDepletionRate`, `puzzlesToComplete`, `enableHazards`, `enableSpectralAnchors`, `enableSimultaneousPuzzles`, `enableDarkAreas`

---

### GhostUpgradeConfig.cs (ScriptableObject)
**Path:** `Scripts/Progression/GhostUpgradeConfig.cs`  
**Menu:** Create → SubmarineCoop → Ghost Upgrade Config

Defines memento thresholds and ability parameters for each Ghost level.

**Fields:** Memento counts for levels 1/2/3, possession duration/cost/cooldown, echo sight duration/cost/cooldown, mist alpha.

**Method:** `GetLevelForMementos(count)` → returns the level for a given memento count.

---

### PhaseManager.cs
**Path:** `Scripts/Progression/PhaseManager.cs`  
**Type:** Singleton MonoBehaviour

**Drives game progression** through Calibration → Expansion → Convergence.

**Inspector fields:** Assign the 3 PhaseConfig ScriptableObjects.

**How it works:**
1. When game starts (`OnGameStateChanged` → InGame), calls `StartPhases()`
2. Sets phase to Calibration, applies config (adjusts tether distance, energy, oxygen rates)
3. Listens to `OnPuzzleCompleted` — increments counter
4. When `puzzlesCompletedInPhase >= config.puzzlesToComplete` → auto-advances to next phase
5. Each phase change applies new config values to all relevant systems

---

### MementoSystem.cs + MementoPickup.cs
**Path:** `Scripts/Progression/MementoSystem.cs`  
**Type:** Singleton MonoBehaviour + Component

**MementoSystem** (singleton) tracks collection and return counts, triggers Ghost level-ups.

**MementoPickup** (component on collectible objects):
| Field | Purpose |
|-------|---------|
| `mementoName` | Display name |
| `mementoDescription` | Lore text |
| `mementoGlowColor` | Gold glow (ghost-only) |
| `pickupSound` | Sound on collection |

**Flow:**
1. MementoPickup has a trigger collider. When Survivor enters → `Collect()` → hidden but not destroyed
2. `MementoSystem.CollectMemento()` fires `OnMementoCollected`
3. Survivor brings memento to Ghost → `ReturnToGhost()` → `MementoSystem.ReturnMemento()`
4. `ReturnMemento()` checks if the memento count reaches a level threshold → fires `OnGhostLevelUp`

---

### GhostUpgradeSystem.cs
**Path:** `Scripts/Progression/GhostUpgradeSystem.cs`  
**Type:** MonoBehaviour

Listens to `OnGhostLevelUp` and **activates abilities** with cooldown management.

**Three ability coroutines:**

| Level | Ability | Duration | Cooldown |
|-------|---------|----------|----------|
| 1 | Point/Hand Tracking | Permanent mist | — |
| 2 | Possession | 3s interaction window | 10s |
| 3 | Echo Sight | 5s clear vision for Survivor | 30s |

**Possession coroutine:** Temporarily allows Ghost to interact with a target's `InteractableBase` (calls `TryInteract` with Ghost role), then ends possession and starts cooldown.

**Echo Sight coroutine:** Calls `VisionSystem.TemporaryOverride()` on the Survivor's vision, giving them Ghost-like clear vision for the duration.

---

## 7. Puzzle Framework

### PuzzleBase.cs
**Path:** `Scripts/Puzzle/PuzzleBase.cs`  
**Type:** Abstract MonoBehaviour

**Skeleton** for all future puzzles. You will create concrete subclasses when puzzle details are provided.

**Lifecycle:**
1. `StartPuzzle()` → sets flags, starts timer, calls `OnInitialize()`, fires `OnPuzzleStarted`
2. Each frame: if timer enabled and exceeds `timeLimit` → `Fail()`. Otherwise calls `CheckCompletion()`
3. If `CheckCompletion()` returns true → `Complete()` → fires `OnPuzzleCompleted`
4. `Fail()` → fires `OnPuzzleFailed`

**Abstract methods to implement:**
- `OnInitialize()` — Set up puzzle state (randomize, place objects, etc.)
- `CheckCompletion()` → `bool` — Return true when puzzle is solved

**Properties:** `PuzzleName`, `PuzzleIndex`, `Type` (Solo_Survivor/Solo_Ghost/Collaborative), `IsStarted`, `IsCompleted`, `IsFailed`, `TimeRemaining`

### PuzzleArea.cs
**Path:** `Scripts/Puzzle/PuzzleArea.cs`  
**Type:** MonoBehaviour (requires `Collider`)

Trigger zone that activates a linked puzzle when a player enters. Configure which roles can activate it and whether it auto-starts.

### PuzzleManager.cs
**Path:** `Scripts/Puzzle/PuzzleManager.cs`  
**Type:** Singleton MonoBehaviour

Tracks all puzzles in the scene. Auto-discovers `PuzzleBase` components if the list is empty. Counts completions/failures. Reports to `PhaseManager` via events.

---

## 8. UI & HUD

### SurvivorHUD.cs
**Path:** `Scripts/UI/SurvivorHUD.cs`  
**Type:** MonoBehaviour

World-space canvas HUD for the Survivor (attach to wrist or helmet).

**Panels:**
- **Oxygen bar** — Slider + text, changes color red below 30%
- **Health bar** — Slider + text
- **Tool indicator** — Icon + name of equipped tool
- **Prompt text** — Temporary messages with fade-in/out ("Press A to grab")
- **Warning overlay** — Pulsing red overlay during critical submarine state

### GhostHUD.cs
**Path:** `Scripts/UI/GhostHUD.cs`  
**Type:** MonoBehaviour

World-space canvas HUD for the Ghost.

**Panels:**
- **Energy bar** — Color transitions: blue (full) → orange (low) → dark red (empty)
- **Tether distance** — Inverted bar (reduces as you move further). Color changes at warning threshold
- **Ghost level** — "Level N" text + ability icons (greyed out until unlocked)
- **Ability cooldowns** — Fill images for possession and echo sight cooldown timers
- **Status messages** — Temporary text ("⚠ Tether limit approaching!", "⚡ Energy depleted!")

### LobbyUI.cs
**Path:** `Scripts/UI/LobbyUI.cs`  
**Type:** MonoBehaviour

VR world-space lobby screen for role selection.

**Elements:**
- "Survivor" and "Ghost" buttons with highlight borders
- Role description cards explaining each role's abilities and limitations
- "Ready" button (toggles to "Cancel")
- Player 1/2 role and ready status display
- Status text: "Choose your role" → "Selected: Survivor" → "Waiting for other player..."

**Flow:** Select role → press Ready → when both ready → `SessionManager.SetPlayerReady()` → auto-starts game

---

## 9. ScriptableObject Setup

After scripts compile, create these assets:

1. **Right-click in Project → Create → SubmarineCoop → Phase Config**
   - Create 3 instances: `PhaseConfig_Calibration`, `PhaseConfig_Expansion`, `PhaseConfig_Convergence`
   - Set increasingly challenging values for each (shorter tether, faster oxygen drain, more puzzles needed)

2. **Right-click in Project → Create → SubmarineCoop → Ghost Upgrade Config**
   - Create 1 instance: `GhostUpgradeConfig`
   - Set memento thresholds (1, 3, 6) and ability durations/costs/cooldowns

3. **Assign these** to the PhaseManager and GhostUpgradeSystem/MementoSystem Inspector fields

---

## 10. Prefab Setup Guide

### Survivor Prefab

```
SurvivorRig (tag: "Survivor")
├── XR Origin
│   ├── Main Camera (+ Volume component for VisionSystem)
│   ├── LeftHand Controller (+ XR Controller + Direct/Ray Interactor)
│   └── RightHand Controller (+ XR Controller + Direct/Ray Interactor)
├── [Components on root]
│   ├── CharacterController
│   ├── SurvivorController (assign VR refs, interactors, controllers)
│   ├── VisionSystem (assign Volume)
│   └── SurvivorHUD canvas (world-space, child or wrist-attached)
```

### Ghost Prefab

```
GhostRig (tag: "Ghost")
├── XR Origin
│   ├── Main Camera (+ Volume component for VisionSystem)
│   ├── LeftHand Controller
│   └── RightHand Controller
├── Ghost Body Renderers (semi-transparent mesh)
├── [Components on root]
│   ├── CharacterController
│   ├── GhostController (assign VR refs, energy, tether, renderers)
│   ├── VisionSystem (assign Volume)
│   ├── GhostEnergySystem
│   ├── TetherSystem (assign LineRenderer)
│   ├── HapticTapSystem (assign hand, audio)
│   ├── ManifestationSystem (assign hand, energy)
│   ├── SpiritTagSystem (assign hand, energy, LineRenderer)
│   ├── GhostUpgradeSystem (assign config, controller)
│   └── GhostHUD canvas
```

### Interactable Prefabs

Each interactable needs:
- A Collider (for XR interaction)
- An `XRSimpleInteractable` or `XRGrabInteractable` component
- A `GlowableObject` component (auto-required by InteractableBase)
- The specific script: `Lever`, `Valve`, `WirePanel`, etc.

### Other Prefabs

| Prefab | Components |
|--------|-----------|
| **Memento** | Collider (trigger), MementoPickup, GlowableObject, Renderer |
| **SpectralAnchor** | SpectralAnchor, GlowableObject, ParticleSystem |
| **HazardZone** | Collider (trigger), HazardZone, GlowableObject, ParticleSystem |
| **TapRipple** | ParticleSystem (VFX) — destroyed after 1s |

---

## 11. Scene Setup Checklist

Place these singleton GameObjects in your main scene:

| GameObject Name | Component(s) |
|----------------|-------------|
| `_GameManager` | GameManager |
| `_NetworkManager` | NetworkManager |
| `_SessionManager` | SessionManager |
| `_SubmarineManager` | SubmarineManager |
| `_PhaseManager` | PhaseManager (assign 3 PhaseConfigs) |
| `_PuzzleManager` | PuzzleManager |
| `_MementoSystem` | MementoSystem (assign GhostUpgradeConfig) |

Then set up your submarine environment with:
- Interactable objects (Levers, Valves, WirePanels)
- HazardZones (placed in areas Ghost needs to warn Survivor about)
- SpectralAnchors (placed in strategic locations)
- MementoPickups (hidden collectibles)
- Spawn points for Survivor and Ghost
- LobbyUI canvas (visible until game starts)
