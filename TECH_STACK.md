# Abyssal Echo — Tech Stack & Foundations

Everything that makes this game tick — the engine, the packages, the rendering pipeline, and why each piece was chosen.

---

## Engine

### Unity 6000.2.5f1 (Unity 6)

Unity 6 is the foundation. It's the latest long-term-supported major release with significant improvements to rendering, performance profiling, and XR support.

**Why Unity 6 over an older version:**
- Native support for OpenXR 1.1+ features out of the box
- Improved GPU Resident Drawer for better draw call batching — critical for VR where we need to maintain 72–90 FPS on mobile hardware
- Render Graph API in URP 17 gives us fine-grained control over the render pipeline without writing custom SRPs
- Improved async shader compilation — fewer hitches during gameplay when new materials are encountered
- C# 12 language support — we use modern features like primary constructors and collection expressions where helpful

---

## Programming Languages

### C# 12 (.NET Standard 2.1)

Every script in the project is written in **C#**, Unity's primary programming language. Unity 6 supports C# 12 features via the Roslyn compiler, targeting .NET Standard 2.1 (not full .NET 8 — Unity has its own runtime).

**Why C# for game development:**
- **Managed memory**: Automatic garbage collection means no manual `malloc`/`free`. Critical for rapid development. The tradeoff is occasional GC spikes — we mitigate this with object pooling and pre-allocation
- **Strong typing**: Compile-time type safety catches bugs before they reach the headset. Enum-based events (`GamePhase`, `PlayerRoleType`) prevent entire categories of string-matching bugs
- **Unity API integration**: MonoBehaviour lifecycle (`Awake`, `Start`, `Update`, `OnDestroy`), coroutines (`IEnumerator`), and serialization (`[SerializeField]`) are all C# features tightly woven into Unity's engine

**C# features we actively use:**

| Feature | Where used | Example |
|---------|-----------|---------|
| **Generics** | Event system, collections | `Action<float, float>` for energy events, `List<PuzzleBase>` |
| **Properties** | All scripts | `public float CurrentEnergy => _currentEnergy;` — read-only public access |
| **Events & Delegates** | GameEvents.cs | `public static event Action<Vector3> OnGhostTap;` |
| **Coroutines** | Manifestation, Possession, EchoSight | `IEnumerator FlickerLightCoroutine()` — time-based effects without blocking |
| **Null-conditional** | All interaction code | `OnInteracted?.Invoke(this);` — safe event invocation |
| **String interpolation** | Debug logging | `$"[Puzzle] Completed: {puzzleName}"` |
| **Pattern matching** | Phase transitions | `switch (currentPhase) { case GamePhase.Calibration: ... }` |
| **Auto-properties** | Singleton accessors | `public static GameManager Instance { get; private set; }` |
| **Lambda expressions** | Button callbacks | `survivorButton.onClick.AddListener(SelectSurvivor);` |
| **Namespaces** | All scripts | `namespace SubmarineCoop.Core` — prevents naming collisions |
| **Abstract classes** | Player, Puzzle | `public abstract class PuzzleBase` — enforces implementation contracts |
| **Attributes** | Serialization, metadata | `[SerializeField]`, `[Header]`, `[RequireComponent]`, `[CreateAssetMenu]` |

**Unity-specific C# patterns:**

```csharp
// MonoBehaviour lifecycle — Unity calls these automatically:
void Awake()    { }  // First, when object is created (before Start)
void Start()    { }  // Once, after all Awake calls
void Update()   { }  // Every frame (~90 times/second in VR)
void OnDestroy(){ }  // When object is destroyed

// Coroutines — async-like time sequencing without threads:
IEnumerator FlickerLight() {
    light.enabled = false;
    yield return new WaitForSeconds(0.1f);  // Pause execution for 0.1s
    light.enabled = true;
    yield return new WaitForSeconds(0.1f);
    // ...Unity resumes this function next frame after the wait
}

// Serialization — expose private fields to Unity Inspector:
[SerializeField] private float maxEnergy = 100f;  // Editable in Inspector, private in code
```

**Compilation pipeline:**
1. C# source (`.cs` files) → Roslyn compiler → IL (Intermediate Language) assemblies
2. IL assemblies → Unity's IL2CPP backend → native C++ code
3. Native C++ → platform compiler (LLVM for Android/Quest, MSVC for Windows)
4. Result: native machine code with C#'s development speed

IL2CPP is mandatory for Quest builds. It converts C# to C++ for 2–3× better performance than Mono, at the cost of longer build times. For PCVR editor testing, we use Mono for fast iteration.

---

### ShaderLab + HLSL

Shader code uses Unity's **ShaderLab** syntax wrapping **HLSL** (High-Level Shading Language). These programs run directly on the GPU.

**Where shaders matter in our game:**

| Shader Need | Language | Purpose |
|-------------|----------|---------|
| Ghost body transparency | HLSL (URP Lit modified) | Alpha blending with ZWrite off for see-through Ghost body |
| Glass frost effect | HLSL (custom) | `_FrostAmount` property animated 0→1 by ManifestationSystem |
| Glow emission | ShaderLab property blocks | `_EmissionColor` modified at runtime via MaterialPropertyBlock |
| Post-processing | HLSL (URP Volume overrides) | Desaturation, bloom, vignette for role-specific vision |
| Tether line | HLSL (custom unlit) | Catenary line with gradient color and transparency |

**HLSL basics:**

```hlsl
// A simplified fragment shader — runs for every pixel on screen:
half4 frag(Varyings input) : SV_Target {
    half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    
    // Frost effect — blend between clear and frosted
    half frost = _FrostAmount;  // Set by C# code
    half4 frostColor = half4(0.8, 0.85, 0.9, 0.95);
    baseColor = lerp(baseColor, frostColor, frost);
    
    return baseColor;
}
```

**Shader Graph:** For non-programmers, Unity's visual Shader Graph tool creates shaders by connecting nodes. We use it for simpler effects (basic transparency, unlit glow). Complex effects (frost, custom post-processing) use hand-written HLSL for finer control and better performance.

---

### JSON & YAML

Configuration and metadata files use **JSON** and **YAML** — not for game logic, but for project infrastructure:

| File | Format | Purpose |
|------|--------|---------|
| `Packages/manifest.json` | JSON | Unity Package Manager dependencies and versions |
| `ProjectSettings/*.asset` | YAML | Unity project settings (quality, physics, tags, layers, input) |
| `*.prefab` | YAML | Prefab serialization (GameObject hierarchy, component values) |
| `*.unity` | YAML | Scene files (all objects, positions, component references) |
| `*.meta` | YAML | Asset import settings and GUIDs for cross-reference |
| `*.asmdef` | JSON | Assembly definitions for C# compilation units |

Unity scenes and prefabs are serialized as YAML. This is why they work with version control (Git) — you can diff changes to a scene file and see exactly which GameObjects were modified.

---

### Markup: XML Documentation Comments

All C# scripts use **XML documentation comments** for IntelliSense and API documentation:

```csharp
/// <summary>
/// Attempt to interact with this object. Validates role permissions.
/// </summary>
/// <param name="role">The role of the interacting player</param>
/// <param name="ghostLevel">Current Ghost ability level (0 if Survivor)</param>
/// <returns>True if interaction was successful</returns>
public virtual bool TryInteract(PlayerRoleType role, int ghostLevel = 0)
```

These comments appear in IDE tooltips, making the codebase navigable without reading implementation details.

---

## Rendering

### Universal Render Pipeline 17.2.0

URP is the rendering backbone. Every visual system in the game — from the Survivor's desaturated vision to the Ghost's spectral glow — depends on URP features.

**Why URP and not HDRP or Built-in:**
- **Performance on Quest**: HDRP is too heavy for mobile VR hardware. URP gives us the visual quality we need while staying within the Quest's GPU budget (~72 FPS sustained)
- **Single-pass Instanced Rendering**: URP supports VR single-pass instanced stereo rendering, which renders both eyes in one draw call. This roughly halves the CPU overhead of rendering.
- **Post-processing stack**: URP's integrated Volume system lets us apply per-camera post-processing without third-party assets. We use this for the Survivor/Ghost vision asymmetry.

**URP features we use:**

| Feature | What it does for us |
|---------|-------------------|
| **Volume Overrides** | ColorAdjustments (saturation), Bloom, Vignette, ChromaticAberration — all role-specific vision effects |
| **MaterialPropertyBlock** | GlowableObject emission without breaking GPU instancing — critical for performance |
| **Shader Graph** | Custom shaders for glass frost effect, Ghost body transparency, tether line rendering |
| **Decal Renderer** | Ghost tap ripple effects rendered on surfaces without extra geometry |
| **Light Layers** | Emergency lights only affect certain objects, Ghost-only light sources that Survivor can't see |

**Key rendering settings for VR:**
- **Render Scale**: 1.0 (native resolution per eye)
- **Anti-aliasing**: MSAA 4x (required for readable text in VR; post-process AA causes ghosting)
- **Shadow Resolution**: 1024 (medium — balances quality vs. performance)
- **Depth Texture**: Enabled (needed for certain effects)
- **HDR**: Enabled (needed for bloom to work properly on bright emissions)

---

## XR Stack

### OpenXR 1.15.1

OpenXR is the cross-platform VR runtime standard. Instead of writing separate code for Oculus, SteamVR, Windows Mixed Reality, etc., we target OpenXR and it works everywhere.

**What it provides:**
- Head tracking (6DOF position + rotation)
- Controller tracking (6DOF per hand)
- Button/trigger/thumbstick input mapping
- Haptic output (vibration) to controllers
- Runtime feature negotiation (the game asks "can you do hand tracking?" and the runtime says yes/no)

**Supported platforms via OpenXR:**
| Platform | Runtime |
|----------|---------|
| Meta Quest 2/3/Pro | Meta OpenXR runtime |
| SteamVR (Valve Index, HTC Vive) | SteamVR OpenXR runtime |
| Windows Mixed Reality | WMR OpenXR runtime |
| Pico 4 | Pico OpenXR runtime |

### XR Interaction Toolkit 3.2.1

XRI is the interaction framework built on top of OpenXR. It handles the "what happens when the player reaches out and grabs something" logic.

**Core concepts:**

| Concept | Role in our game |
|---------|-----------------|
| **XR Origin** | The player's tracked VR rig — head + two hands. Both Survivor and Ghost prefabs have one. |
| **Interactors** | Attached to hands. Direct Interactor (grab nearby), Ray Interactor (point at distant objects). |
| **Interactables** | Any object the player can interact with. Our `InteractableBase` wraps `XRBaseInteractable`. |
| **Locomotion System** | Continuous movement + snap turn. Configured per-player (Ghost can potentially float/fly). |
| **Interaction Groups** | Prevent both hands from grabbing the same object simultaneously. |

**How our role system integrates with XRI:**
When a player grabs something, XRI fires `selectEntered`. Our `InteractableBase` intercepts this, walks up the interactor's hierarchy to find the root tag ("Survivor" or "Ghost"), and applies permission checks before allowing the interaction to proceed. This means XRI handles all the VR physics (grab detection, hover highlighting, snap zones), and we layer game logic on top.

### XR Hands 1.6.1

Hand tracking support — for headsets that support it (Quest 2/3/Pro, Pico 4).

**What it gives us:**
- Full 26-joint hand skeleton tracking (no controllers needed)
- Finger pinch detection (thumb-to-index = grab gesture)
- Hand mesh rendering (so the Ghost can see their transparent hands)
- Integration with XRI — hand tracking gestures map to the same interactor system as controllers

**How we use it:**
- Ghost tapping: hand proximity + pinch gesture triggers a tap on the nearest surface
- Natural pointing: the Ghost's index finger direction drives the spirit tag aiming ray
- Valve turning: hand rotation maps to valve rotation when grabbed

### Android XR OpenXR 1.0.1

Extension for Android-based XR headsets (Quest, Pico). Provides:
- Android-specific OpenXR extensions (passthrough, spatial anchors)
- Performance optimizations for mobile GPU (Adreno/Mali)
- Application Space Warp (ASW) support for frame interpolation when GPU can't hit target framerate

### AR Foundation 6.2.0

Included for potential future mixed reality features:
- Passthrough mode (see the real world through the headset with virtual objects overlaid)
- Plane detection (anchor virtual objects to real surfaces)
- Not actively used currently, but available if we want to add an "AR lobby" where players set up in their real room

### XR Management 4.5.1

Behind-the-scenes loader that initializes the correct XR runtime at startup:
- Detects available XR hardware
- Loads the OpenXR plugin
- Initializes tracking subsystems
- Handles focus loss/gain (removing headset, system overlays)

---

## Input

### Input System 1.14.2

Unity's modern input framework (replacing the legacy `Input.GetButton()` system).

**Why the new Input System:**
- **Action-based input**: We define actions ("Grab", "Tap", "Menu") and bind them to buttons. If the player switches from controllers to hand tracking, the actions still work.
- **Composites**: Thumbstick movement is a 2D composite that works identically across Oculus Touch, Index Controllers, and Vive Wands.
- **XRI integration**: XR Interaction Toolkit 3.x requires the new Input System. It's not optional.

**Our input actions:**
| Action | Controller Binding | Hand Tracking |
|--------|-------------------|---------------|
| Grab | Grip button | Pinch gesture |
| Trigger/Use | Trigger button | Pinch + hold |
| Tap (Ghost) | A/X button | Index flick |
| Tag (Ghost) | B/Y button | Open palm hold |
| Menu | Menu button | — |
| Move | Left thumbstick | — |
| Turn | Right thumbstick | Head turn |

---

## Audio

### Unity Audio Module 1.0.0

Built-in spatial audio for VR. Every sound in the game uses 3D spatialization.

**Why it matters for this game specifically:**
Audio is a *primary gameplay mechanic*, not just ambiance. The Survivor can't see most dangers, so they rely on spatial audio cues:

| Sound | 3D Position | Purpose |
|-------|-------------|---------|
| Ghost tap | Exact surface point | Directional communication (paired with haptics) |
| Ambient creaking | Random hull points | Atmosphere + "the sub is stressed" |
| Hazard buzz/hiss | Hazard zone center | Survivor's only audible warning of danger |
| Alarm | SubmarineManager | Critical resource warning |
| Wire snip | Wire position | Feedback when cutting |
| Memento chime | Pickup position | Collection reward |

**Configuration:** All audio sources use `spatialBlend = 1.0` (fully 3D), `rolloffMode = Logarithmic`, and `maxDistance` tuned per-sound. The Ghost's taps are audible from up to 15m; the alarm is audible everywhere.

---

## Physics

### Physics Module 1.0.0

Unity's PhysX integration. Used for:

- **Trigger zones**: HazardZones, PuzzleAreas, MementoPickups — all use trigger colliders + `OnTriggerEnter/Stay/Exit`
- **Raycasting**: Ghost tapping (surface detection), Spirit tagging (aiming), interaction (XRI hover/select)
- **Rigidbodies**: ManifestationSystem moves small objects with `AddForce`. Mass limit (5kg) prevents flying submarine walls.
- **Overlap queries**: `Physics.OverlapSphere()` finds the nearest light, glass surface, or movable object within the Ghost's interaction range
- **Character controllers**: Both player prefabs use `CharacterController` for movement (not Rigidbody) to avoid physics-based jitter in VR

---

## UI

### Unity UI (uGUI) + TextMeshPro

World-space canvases for VR HUD elements. All UI is diegetic (exists in the 3D world, not screen-overlay).

**Why world-space and not screen-space:**
In VR, screen-space UI is uncomfortable — it's glued to your face and causes eye strain. World-space canvases are physical panels in the 3D world that you look at naturally. Our HUDs are attached to the player's wrist or float at chest height.

**TextMeshPro** handles all text rendering with SDF (Signed Distance Field) fonts. This gives us crisp, anti-aliased text at any viewing angle and distance — critical in VR where the player's head is always moving.

---

## Timeline

### Timeline 1.8.9

Unity's cinematic sequencing system. Planned use:

- **Echo Sight replays**: Record and play back scripted event sequences showing what happened in a room
- **Cutscenes**: Phase transition moments, game intro/outro
- **Puzzle reveals**: Animated reveals when a puzzle is solved (doors opening, lights turning on)

Not actively implemented yet but included in the project for these future features.

---

## Networking Architecture

### Current: Local Network Service (In-House)

A custom `INetworkService` interface with a `LocalNetworkService` implementation for development.

```
                    ┌─────────────────┐
                    │ INetworkService  │ (interface)
                    │ ─────────────── │
                    │ Host()          │
                    │ Join()          │
                    │ SendMessage()   │
                    │ SpawnObject()   │
                    └──────┬──────────┘
                           │ implements
               ┌───────────┼───────────┐
               ▼           ▼           ▼
    ┌──────────────┐ ┌──────────┐ ┌──────────┐
    │LocalNetwork  │ │ Netcode  │ │ Photon   │
    │Service       │ │ Service  │ │ Service  │
    │(current)     │ │(planned) │ │(planned) │
    └──────────────┘ └──────────┘ └──────────┘
```

**Why an abstraction layer:**
The networking solution hasn't been finalized. By coding against an interface, every system in the game is network-agnostic. When we pick Netcode for GameObjects, Photon Fusion, or Mirror, we write one new class and swap it in. Zero changes to any other script.

### Planned: Multiplayer Center 1.0.0

Unity's built-in tool for evaluating and integrating multiplayer solutions. Included in the project to facilitate the networking decision when we're ready.

---

## Software Design Patterns

These aren't packages — they're architectural decisions baked into the code.

### Singleton Managers

Core systems use the singleton pattern (`Instance` static property):

| Singleton | Responsibility |
|-----------|---------------|
| `GameManager` | Game state, player lifecycle |
| `NetworkManager` | Network abstraction |
| `SessionManager` | Lobby, roles, readiness |
| `SubmarineManager` | Environment state |
| `PhaseManager` | Progression |
| `PuzzleManager` | Puzzle tracking |
| `MementoSystem` | Collectible tracking |

All use `DontDestroyOnLoad` where appropriate and self-destruct if a duplicate exists.

### Event-Driven Architecture

The `GameEvents` static class acts as a message broker. This pattern eliminates the need for systems to hold references to each other:

```
Ghost taps surface
    → GameEvents.FireGhostTap(position)
        → SurvivorController: sends haptic feedback
        → GhostHUD: shows "tap sent" indicator
        → AudioSystem: plays 3D tap sound
```

No coupling. Any system can subscribe to any event without the publisher knowing or caring.

### ScriptableObject Configuration

Game balance data is stored in ScriptableObjects — data assets that exist outside of scenes:

| ScriptableObject | Configures |
|-----------------|------------|
| `PhaseConfig` | Tether distance, energy, oxygen rates, puzzle counts per phase |
| `GhostUpgradeConfig` | Memento thresholds, ability durations/costs/cooldowns |

**Why ScriptableObjects:** They're editable in the Unity Inspector without touching code. A game designer can tweak difficulty by changing a number in the Inspector, hit Play, and immediately see the result. No recompilation needed.

### Component Composition

Instead of deep inheritance, systems use composition. For example, a lever in the submarine is:

```
LeverObject (GameObject)
├── Lever (script — interaction logic)
├── GlowableObject (script — emission glow)
├── XRSimpleInteractable (script — VR grab detection)
├── Collider (physics — trigger volume)
├── AudioSource (audio — click sound)
└── MeshRenderer (visual — the lever mesh)
```

Each component handles one concern. Swap `GlowableObject` for a different visual system and everything else keeps working.

---

## Performance Considerations

VR has strict performance requirements. If the game drops below 72 FPS, players feel physically nauseous.

| Technique | Where used | Why |
|-----------|-----------|-----|
| **MaterialPropertyBlock** | GlowableObject | Avoids material instancing, preserves GPU instancing for batched draw calls |
| **Object pooling** | Tap ripple VFX | Avoid `Instantiate`/`Destroy` GC spikes during gameplay |
| **Coroutines over Update** | Manifestation, Possession, EchoSight | Time-limited effects use coroutines instead of per-frame Update checks |
| **Lazy initialization** | VisionSystem, TetherSystem | Only create runtime profiles/renderers when first needed |
| **Physics layers** | Hazards, Ghost-only | Reduce unnecessary collision checks between unrelated objects |
| **Event unsubscription** | All scripts OnDisable/OnDestroy | Prevent memory leaks from dangling event references |
| **Single-pass instanced VR** | URP settings | Both eyes render in one pass — ~40% less CPU overhead |

---

## Dependencies at a Glance

| Package | Version | Role |
|---------|---------|------|
| Unity Editor | 6000.2.5f1 | Game engine |
| Universal RP | 17.2.0 | Rendering pipeline |
| Input System | 1.14.2 | Controller/hand input |
| XR Interaction Toolkit | 3.2.1 | VR grab, point, teleport |
| XR Hands | 1.6.1 | Hand tracking skeleton |
| OpenXR | 1.15.1 | Cross-platform VR runtime |
| Android XR OpenXR | 1.0.1 | Quest/Pico support |
| AR Foundation | 6.2.0 | Future MR features |
| XR Management | 4.5.1 | XR subsystem loader |
| Timeline | 1.8.9 | Cinematic sequences |
| Multiplayer Center | 1.0.0 | Networking evaluation |
| TextMeshPro | (built-in) | VR-readable text |
| Physics | (built-in) | Raycasting, triggers, forces |
| Audio | (built-in) | 3D spatial sound |

---

## Target Hardware

### Primary Test Device: Meta Quest 3 Pro

The Quest 3 Pro is our primary development and testing headset. It's a standalone Android-based VR headset with optional PC tethering.

**Hardware specifications:**

| Component | Spec | Why it matters |
|-----------|------|----------------|
| **SoC** | Snapdragon XR2 Gen 2 | The GPU (Adreno 740) determines our rendering budget. URP + single-pass instanced rendering keeps us within its 72–120 FPS capability |
| **RAM** | 12 GB LPDDR5 | Shared between system and GPU. Our game targets ~2 GB usage including textures, meshes, and audio |
| **Display** | Dual LCD, 2064×2208 per eye | High pixel density means our UI text (TextMeshPro SDF) needs to be crisp. Also means more pixels to shade — URP render scale tuning is critical |
| **Refresh rate** | 72 / 90 / 120 Hz | We target 90 Hz. If GPU can't keep up, Quest's Application SpaceWarp (ASW) synthesizes intermediate frames from 45 real FPS |
| **Optics** | Pancake lenses, 106° horizontal FOV | Wider FOV means more geometry visible per frame. Frustum culling and occlusion culling matter |
| **IPD** | Continuous adjustment 55–75mm | Handled by OpenXR runtime — our code doesn't need to manage this |
| **Storage** | 256 GB / 512 GB | Our build target is under 2 GB installed |
| **Battery** | ~2 hours active play | Session length is self-limiting. Games should have natural stopping points (phase completions) |
| **Weight** | 515g | Lightweight enough for extended sessions; relevant since our game targets 30–60 minute runs |

**Tracking system — Inside-out 6DOF:**

The Quest 3 Pro uses **inside-out tracking** with an array of cameras mounted on the headset itself (no external base stations needed):

- **5 tracking cameras** (grayscale, wide-angle, IR-assisted) positioned around the headset
- Uses **Visual-Inertial Odometry (VIO)**: fuses camera data with IMU (accelerometer + gyroscope) readings at 1000 Hz
- The cameras photograph the environment, a SLAM (Simultaneous Localization and Mapping) algorithm builds a 3D map of the room, and the headset's position is calculated relative to that map
- Sub-millimeter tracking accuracy with <10ms latency from physical movement to rendered frame
- **Why this matters for our game**: The Survivor needs to physically reach for levers and valves, and the Ghost needs to precisely tap surfaces. Tracking accuracy directly affects how "physical" the VR interaction feels

**Hand tracking — Direct hand input without controllers:**

The Quest 3 Pro has dedicated **hand tracking cameras** (separate from positional tracking):

- Tracks **26 skeletal joints per hand** (wrist, palm, thumb ×4, fingers ×4 each)
- ~30 Hz update rate for hand pose, interpolated to frame rate
- Machine learning model running on-device classifies hand poses into gestures (pinch, point, open palm, fist)
- **Pinch detection**: measures distance between thumb tip and index tip. Below threshold = "grab." This maps to XRI's grab interactor
- Unity's `XR Hands` package receives the joint data and exposes it as `XRHand.GetJoint(XRHandJointID)` — our `GhostController` reads index finger direction for spirit tag aiming

**Full-color passthrough:**

The Quest 3 Pro has **stereo RGB passthrough** using two 4MP color cameras:

- Reconstructs a 3D view of the real world inside the headset
- Planned use: mixed reality lobby where players set up their play area before diving into the fully virtual submarine
- AR Foundation 6.2.0 in our project supports passthrough via Meta's OpenXR extensions

**Quest 3 Pro controllers — Meta Touch Pro:**

| Feature | Detail | Game use |
|---------|--------|----------|
| **6DOF tracking** | Self-tracked via cameras + IMU (no headset LoS needed) | Hands can go behind back, under desk — still tracked |
| **Thumbstick** | Clickable analog stick | Movement (left), snap turn (right) |
| **Trigger** | Analog 0–1 | Primary interact / use tool |
| **Grip** | Analog 0–1 | Grab objects (levers, valves, mementos) |
| **A/B and X/Y buttons** | Digital | Ghost tap (A/X), Ghost tag (B/Y) |
| **Menu button** | Digital | Pause menu |
| **Haptic motor** | Linear resonant actuator (LRA) | Ghost→Survivor directional haptics, tether warnings, interaction feedback |
| **Rechargeable battery** | USB-C, ~8 hours | Supports full play sessions |

**How haptics work at the hardware level:**

The controllers contain a **Linear Resonant Actuator (LRA)** — a small weight on a spring driven by an electromagnetic coil:

1. Our code calls `SendHapticImpulse(amplitude, duration)` via XRI
2. This goes through OpenXR → Quest runtime → controller firmware
3. The firmware drives the coil with a signal at the LRA's resonant frequency (~150–200 Hz)
4. `amplitude` (0–1) controls the drive voltage → vibration strength
5. `duration` controls how long the pulse lasts
6. Total latency from code call to physical vibration: ~15ms

In our game, the `SurvivorController.SendDirectionalHaptic()` sends different amplitudes to left and right controllers simultaneously. The player's brain perceives this as a direction, creating spatial awareness of where the Ghost is tapping.

---

### Secondary Test: PCVR Headsets

For development and high-fidelity testing, we also support tethered PCVR headsets connected to a gaming PC.

#### Valve Index

| Component | Spec | Relevance |
|-----------|------|-----------|
| **Display** | Dual LCD, 1440×1600 per eye | Lower per-eye res than Quest 3 Pro, but higher refresh rate |
| **Refresh rate** | 80 / 90 / 120 / 144 Hz | Can push 144 Hz on PC — extremely smooth |
| **FOV** | ~130° (widest consumer headset) | More geometry visible; test worst-case performance |
| **Audio** | Off-ear "BMR" speakers | Built-in spatial audio — critical for our 3D tap sounds |
| **Tracking** | SteamVR Lighthouse 2.0 (outside-in) | Sub-mm accuracy with external base stations |
| **Controllers** | Index Controllers (Knuckles) | Per-finger tracking + pressure-sensitive grip |
| **Connection** | DisplayPort 1.2 + USB 3.0 | Tethered to PC |

**Lighthouse tracking (outside-in):**

Unlike Quest's inside-out tracking, the Index uses **external base stations** that sweep IR laser beams across the room:

- Each base station has two spinning laser planes (horizontal and vertical) and an IR LED sync flash
- Photosensors on the headset/controllers detect the laser sweeps
- Timing between the sync flash and laser detection gives the **precise angle** from the base station to each sensor
- Multiple sensors + multiple base stations = sub-millimeter positional accuracy
- **Advantage**: no occlusion issues, no camera processing, extremely low latency
- **Disadvantage**: requires mounted base stations and line-of-sight

**Index Controllers (Knuckles):**

The Index controllers strap to the hand and track **individual finger curl** using capacitive sensors along the grip:

| Finger | Sensor | Data |
|--------|--------|------|
| Thumb | Capacitive trackpad + buttons | Position + touch/press |
| Index | Capacitive trigger | Analog curl 0–1 |
| Middle | Capacitive grip strip | Analog curl 0–1 |
| Ring | Capacitive grip strip | Analog curl 0–1 |
| Pinky | Capacitive grip strip | Analog curl 0–1 |

- **Squeeze force**: Force sensor in grip measures how tightly the player is holding the controller (0–1 analog)
- Unity's `XR Hands` abstraction maps these finger values to the hand skeleton, so our code works identically whether using Quest hand tracking or Index finger tracking
- **Grip release detection**: Player can physically let go of the controller (it stays strapped) — this triggers a "hand open" state. Natural for VR interactions like dropping objects.

**Haptics on Index:**
- Also uses LRA motors, same API as Quest through OpenXR
- Slightly stronger and more precise vibration due to larger LRA unit
- Our directional haptic system works identically across both platforms

#### HTC Vive Pro 2

| Component | Spec | Relevance |
|-----------|------|-----------|
| **Display** | Dual LCD, 2448×2448 per eye | Highest per-eye resolution in our test matrix |
| **Refresh rate** | 90 / 120 Hz | Standard VR rates |
| **Tracking** | SteamVR Lighthouse 2.0 | Same tracking system as Index |
| **Controllers** | Vive Wand or Index Controllers | Wand has trackpad; either works via OpenXR |
| **Audio** | Built-in headphones | Integrated spatial audio |

Primarily used for testing at high resolution to identify any rendering artifacts or performance cliffs.

#### Generic SteamVR / WMR Headsets

Any OpenXR-compatible PCVR headset should work. Our OpenXR + XRI stack abstracts the hardware differences. Specific models we've verified compatibility intent with:

- HP Reverb G2 (WMR, 2160×2160 per eye — highest res single LCD)
- Bigscreen Beyond (SteamVR, ultra-compact, pancake lenses)
- Pimax Crystal (SteamVR, ultra-wide 140° FOV)

---

### Display Technology Deep-Dive

VR display tech directly affects how our game looks and feels:

**LCD vs. OLED:**

| Aspect | LCD (Quest 3, Index) | OLED (older Quest 1, Vive OG) |
|--------|---------------------|-------------------------------|
| Black levels | Grey-ish blacks (backlight bleed) | True blacks (pixels off) |
| Response time | 3–5ms | <1ms |
| Subpixel layout | Full RGB stripe | PenTile (shared subpixels) |
| Brightness | Higher peak brightness | Lower, but infinite contrast |
| SDE (screen door) | Minimal at modern resolutions | More visible at same resolution |

**Why this matters for our game:**

Our submarine is often **dark**. LCD panels can't achieve true black in dark environments — the Survivor's desaturated vision makes this less noticeable, but the Ghost's bright spectral vision creates high contrast scenes where LCD backlight bleed can appear. We tune bloom intensity and minimum ambient light to work well on LCD panels specifically.

**Pancake lenses vs. Fresnel lenses:**

| Aspect | Pancake (Quest 3 Pro) | Fresnel (Quest 2, Index) |
|--------|----------------------|--------------------------|
| Weight | Lighter, thinner | Heavier, bulkier |
| God rays | Minimal | Visible streaks from bright objects |
| Sweet spot | Wider — clearer across more of the lens | Narrow center sweet spot |
| Edge clarity | Good | Blurry edges |

Our Ghost's glow effects (spirit tags, interactable highlights) are bright point sources. On Fresnel lenses, these can cause annoying god ray artifacts. We cap max emission intensity to reduce this.

---

### Audio Hardware

VR spatial audio depends on the headset's audio delivery:

| Headset | Audio Type | 3D Capability |
|---------|-----------|---------------|
| Quest 3 Pro | Built-in speakers (open-ear) | Good spatial imaging; some sound leakage |
| Valve Index | Off-ear BMR speakers | Excellent spatialization; minimal ear fatigue |
| HTC Vive Pro 2 | Over-ear headphones | Best isolation; good bass for submarine ambiance |
| Any headset | User's own earbuds/headphones | Variable; 3.5mm or Bluetooth |

**Head-Related Transfer Function (HRTF):**

Our 3D audio works because Unity's audio engine applies **HRTF filtering** — a process that simulates how sound reaches each ear differently based on direction:

- Sound from the right arrives at the right ear ~0.6ms before the left ear (**interaural time difference**)
- Sound from the right is louder in the right ear (**interaural level difference**)
- Sound from above vs. below has different frequency response due to ear shape (**pinna filtering**)
- Unity approximates this with a generic HRTF. Quest's runtime can use personalized HRTF from ear scans for better accuracy

For the Ghost tap system, HRTF is essential — the Survivor hears the tap *from the direction it happened*, reinforcing the haptic directional cue. Two sensory channels (touch + hearing) together create reliable spatial awareness even without vision.

---

### How Hardware Maps to Game Mechanics

Every game mechanic was designed with specific hardware capabilities in mind:

| Game Mechanic | Hardware Feature Used | Fallback |
|--------------|----------------------|----------|
| **Ghost tapping** | Hand tracking (proximity + pinch) | Controller button + raycast |
| **Directional haptics** | LRA motors in controllers (per-hand amplitude) | Audio-only spatial cues |
| **Survivor grab/pull** | 6DOF controller tracking + grip button | Hand tracking pinch |
| **Valve turning** | Controller rotation tracking or hand tracking wrist rotation | Button-based increment |
| **Spirit tag aiming** | Controller pointing direction or hand index finger direction | Head gaze fallback |
| **Survivor vision** | Per-camera post-processing (URP Volume) | Works on all headsets identically |
| **Ghost transparency** | URP shader with alpha blending | Identical rendering on all GPUs |
| **3D tap sound** | HRTF spatial audio via built-in or connected speakers | Stereo panning minimum |
| **Wire texture identification** | Controller vibration patterns (future) | Visual texture overlay |

---

### Minimum PC Specs for PCVR

When running tethered to a PC:

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| **GPU** | NVIDIA GTX 1070 / AMD RX 580 | NVIDIA RTX 3070 / AMD RX 6800 |
| **CPU** | Intel i5-9400 / AMD Ryzen 5 3600 | Intel i7-12700 / AMD Ryzen 7 5800X |
| **RAM** | 16 GB DDR4 | 32 GB DDR4/DDR5 |
| **OS** | Windows 10 (64-bit) | Windows 11 |
| **USB** | USB 3.0 (for Link/tether) | USB-C 3.1 Gen 2 |
| **Storage** | 4 GB free (game install) | SSD recommended for load times |

**Standalone Quest**: No PC required. The game runs natively on the Quest's Snapdragon XR2 Gen 2. Performance is managed via URP quality tiers — standalone uses lower shadow resolution, reduced particle counts, and simplified shaders compared to PCVR.
