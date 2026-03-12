# How Everything Works — Code Snippets & Explanations

A casual walkthrough of the game's code with snippets showing what each system actually does under the hood. No jargon overload — just "here's the code, here's what it means."

---

## The Event Bus — How Systems Talk To Each Other

None of the scripts hold direct references to each other. Instead, they shout into a room (the event bus) and anyone who cares listens.

```csharp
// ANY script can fire an event:
GameEvents.FireGhostTap(worldPosition);

// ANY other script can listen for it:
void OnEnable() {
    GameEvents.OnGhostTap += HandleGhostTap;
}

void HandleGhostTap(Vector3 tapPosition) {
    // React to the tap — this runs automatically
    Debug.Log("Ghost tapped at " + tapPosition);
}

// Always clean up when the object is destroyed:
void OnDestroy() {
    GameEvents.OnGhostTap -= HandleGhostTap;
}
```

**Why this matters:** The Ghost tapping system doesn't need to know the Survivor exists. It just fires the event. The Survivor's controller independently listens for it and sends haptic feedback. If we add a third system that also reacts to taps (say, a recording system), we just subscribe — zero changes to existing code.

---

## Game State Machine — Lobby to Game Over

The game has four states. `GameManager` controls which one we're in.

```csharp
public enum GameState {
    Lobby,      // Players choosing roles
    InGame,     // Actually playing
    Paused,     // Timeout
    GameOver    // Someone died or mission complete
}
```

Transitioning is one line:

```csharp
GameManager.Instance.SetState(GameState.InGame);
```

When a state changes, every system that cares gets notified:

```csharp
// Inside SetState():
GameEvents.FireGameStateChanged(newState);

// The SubmarineManager listens and only drains oxygen while InGame:
if (GameManager.Instance.CurrentState != GameState.InGame) return;
```

---

## How the Survivor Sees the World

The Survivor has hypoxia — their vision is washed out. This is done through post-processing:

```csharp
// VisionSystem.cs — when role is Survivor:
_colorAdjustments.saturation.Override(-80f);    // Almost greyscale
_colorAdjustments.colorFilter.Override(sepiaColor); // Warm brownish tint
_vignette.intensity.Override(0.35f);            // Dark edges
```

The result: the Survivor can't tell red wires from blue wires. They all look grey-brown. That's the whole point — the Ghost has to tell them which one to cut.

---

## How the Ghost Sees the World

The Ghost gets the opposite — everything is vivid and glowing:

```csharp
// VisionSystem.cs — when role is Ghost:
_colorAdjustments.saturation.Override(30f);      // Extra vivid
_bloom.intensity.Override(1.5f);                  // Bright things glow
_chromaticAberration.intensity.Override(0.15f);   // Subtle ethereal edge effect
```

On top of that, important objects glow in specific colors that only the Ghost can see:

```csharp
// Color coding the Ghost sees:
Red    = Danger (hazards, traps)
Blue   = Interactive (levers, valves, panels)
Gold   = Collectible (mementos)
```

---

## Ghost Tapping — How Communication Works

The Ghost can't speak. Instead, they tap on surfaces. Here's the flow:

**Step 1 — Ghost taps a wall:**
```csharp
// HapticTapSystem.cs
void ExecuteTap(Vector3 worldPosition) {
    GameEvents.FireGhostTap(worldPosition);  // Tell everyone
    PlayTapSound(worldPosition);              // 3D audio at that spot
    SpawnRipple(worldPosition);               // Visual ripple effect
}
```

**Step 2 — Survivor feels it in their hands:**
```csharp
// SurvivorController.cs
void HandleGhostTap(Vector3 tapPosition) {
    SendDirectionalHaptic(tapPosition);
}

void SendDirectionalHaptic(Vector3 worldPosition) {
    // Figure out: is the tap to my left or right?
    Vector3 dirToTap = (worldPosition - head.position).normalized;
    Vector3 localDir = head.InverseTransformDirection(dirToTap);

    // If tap is to the left → left controller buzzes more
    float leftIntensity = (-localDir.x + 1f) / 2f;
    float rightIntensity = (localDir.x + 1f) / 2f;

    leftController.SendHapticImpulse(leftIntensity * 0.6f, 0.15f);
    rightController.SendHapticImpulse(rightIntensity * 0.6f, 0.15f);
}
```

**What the Survivor experiences:** A buzz in their left hand means "look left." Three quick buzzes on the right means "something important is to your right." Players develop their own tap language over time — that's the magic.

---

## The Energy System — Why the Ghost Can't Do Everything

Every Ghost ability costs energy. This prevents the Ghost from just doing everything instantly and creates tension:

```csharp
// GhostEnergySystem.cs
bool TrySpendEnergy(float amount) {
    if (currentEnergy < amount) {
        GameEvents.FireEnergyDepleted();  // HUD shows "out of energy!"
        return false;
    }

    currentEnergy -= amount;
    _regenCooldown = 2f;  // Can't regen for 2 seconds after spending
    return true;
}
```

Energy costs at a glance:
```
Tap a surface:       1 energy   (cheap — do it often)
Spirit tag an object: 10 energy  (moderate)
Flicker a light:      5 energy
Frost glass:         15 energy
Move an object:      20 energy   (expensive)
Possession:          25 energy   (very expensive)
Place anchor:        30 energy   (most expensive)
```

Energy regenerates at 2/second, but only after a 2-second cooldown since the last spend. So if the Ghost spams abilities, they'll run dry fast.

---

## Spirit Tagging — "Look At This Thing!"

When the Ghost tags an object, it briefly glows so the Survivor can see it:

```csharp
// SpiritTagSystem.cs
bool TagObject(GameObject target) {
    // Pay the cost
    if (!energySystem.TrySpendEnergy(10f)) return false;

    // Add glow component if it doesn't have one
    GlowableObject glowable = target.GetComponent<GlowableObject>();
    if (glowable == null)
        glowable = target.AddComponent<GlowableObject>();

    // Make it glow!
    glowable.SetGlow(tagColor, 2f);

    // Start a timer to fade it out after 5 seconds
    StartCoroutine(TagDurationCoroutine(target, glowable));
    return true;
}
```

The glow fades out gracefully — full brightness for 70% of the duration, then fades over the last 30%:

```csharp
// Full glow for 3.5 seconds...
yield return new WaitForSeconds(tagDuration * 0.7f);

// Then fade over 1.5 seconds:
while (elapsed < fadeDuration) {
    float t = 1f - (elapsed / fadeDuration);
    glowable.SetGlow(tagColor, glowIntensity * t);  // Getting dimmer...
    elapsed += Time.deltaTime;
    yield return null;
}
glowable.ClearGlow();  // Gone
```

---

## How Glow Works (On Any Object)

The `GlowableObject` component can be slapped on anything to make it glow. It works by changing the material's emission:

```csharp
// GlowableObject.cs
void ApplyEmission(Color emissionColor) {
    renderer.GetPropertyBlock(propertyBlock);
    propertyBlock.SetColor("_EmissionColor", emissionColor);
    renderer.SetPropertyBlock(propertyBlock);
}
```

It can also pulsate (breathe in and out) for extra visibility:

```csharp
// Pulsation — smooth sine wave between dim and bright:
float pulse = Mathf.Sin(Time.time * pulsateSpeed);  // -1 to 1
pulse = (pulse + 1f) / 2f;                          // 0 to 1
pulse = Mathf.Lerp(0.3f, 1f, pulse);                // 0.3 to 1

ApplyEmission(glowColor * intensity * pulse);
```

**Ghost-only visibility:** Some glows are only visible to the Ghost. When the Survivor looks at them, they see nothing:

```csharp
void UpdateVisibilityForRole(PlayerRoleType viewerRole) {
    if (ghostOnlyVisible && viewerRole == PlayerRoleType.Survivor) {
        ApplyEmission(Color.black);  // Survivor sees no glow
    }
}
```

---

## The Tether — Ghost Can't Wander Too Far

The Ghost is leashed to the Survivor. If they go too far, they get pulled back:

```csharp
// TetherSystem.cs — every frame:
float distance = Vector3.Distance(ghostTransform.position, survivorTransform.position);
float normalized = distance / maxDistance;  // 0 = right next to them, 1 = at the limit

// At 80% distance — warning!
if (normalized >= 0.8f) {
    GameEvents.FireTetherWarning();  // HUD shows "⚠ Tether limit!"
}

// Past 100% — snap back!
if (distance > maxDistance) {
    Vector3 pullDirection = (survivorTransform.position - ghostTransform.position).normalized;
    ghostTransform.position += pullDirection * overshoot;
}
```

The visual tether line sags like a real cable:

```csharp
// Catenary droop effect:
for (int i = 0; i < segments; i++) {
    float t = (float)i / (segments - 1);
    Vector3 point = Vector3.Lerp(ghostPos, survivorPos, t);

    // Add sag — biggest in the middle, zero at the ends
    float sag = Mathf.Sin(t * Mathf.PI) * sagAmount * normalized;
    point.y -= sag;

    lineRenderer.SetPosition(i, point);
}
```

---

## Manifestation — Ghost Affects the Physical World

The Ghost can briefly affect physical things (costs energy):

### Flickering lights:
```csharp
IEnumerator FlickerLightCoroutine(Light light) {
    float originalIntensity = light.intensity;
    float elapsed = 0f;

    while (elapsed < 2f) {  // Flicker for 2 seconds
        // Rapid sine wave → on/off/on/off
        float flicker = Mathf.Sin(elapsed * 8f * Mathf.PI * 2f);
        light.intensity = flicker > 0 ? originalIntensity : originalIntensity * 0.1f;
        elapsed += Time.deltaTime;
        yield return null;
    }

    light.intensity = originalIntensity;  // Back to normal
}
```

### Frosting glass:
```csharp
// Gradually frost a glass surface (shader has a _FrostAmount property):
while (frostValue < 1f) {
    frostValue += Time.deltaTime * 2f;
    material.SetFloat("_FrostAmount", frostValue);  // 0 = clear, 1 = fully frosted
    yield return null;
}
// Hold for 8 seconds, then gradually clear...
```

### Pushing small objects:
```csharp
// Only works on objects under 5kg:
if (rigidbody.mass > 5f) return false;
rigidbody.AddForce(direction * 3f, ForceMode.Impulse);  // Quick shove
```

---

## Role-Based Interaction — Who Can Touch What

Every interactable in the submarine has rules about who can use it:

```csharp
// InteractableBase.cs
bool TryInteract(PlayerRoleType role, int ghostLevel) {
    if (isLocked) return false;          // Locked — nobody can use it

    if (role == PlayerRoleType.Survivor && !canSurvivorInteract)
        return false;                    // "This isn't for you"

    if (role == PlayerRoleType.Ghost) {
        if (!canGhostInteract) return false;
        if (ghostLevel < requiredGhostLevel) return false;  // "Need Level 2"
    }

    Interact(role);  // Actually do the thing
    return true;
}
```

Default rules:
- **Levers:** Survivor ✅, Ghost ❌ (unless Ghost uses Possession at Level 2)
- **Valves:** Survivor ✅, Ghost ❌
- **Wire panels:** Survivor ✅ (cuts wires), Ghost ❌ (but Ghost sees the colors)

When the player's hand hovers over something in VR, the system figures out their role from the tag on the root GameObject:

```csharp
// Walk up to the top of the hierarchy and check the tag:
var root = interactor.gameObject.transform.root.gameObject;
if (root.CompareTag("Survivor")) return PlayerRoleType.Survivor;
if (root.CompareTag("Ghost")) return PlayerRoleType.Ghost;
```

---

## The Lever — Simple On/Off Toggle

```csharp
// Lever.cs — when interacted:
void Interact(PlayerRoleType role) {
    isActivated = !isActivated;  // Toggle

    // Animate the handle
    targetAngle = isActivated ? 45f : -45f;

    // Fire Unity events (wire up in Inspector: "open this door," "turn on that light")
    if (isActivated)
        onLeverOn.Invoke();
    else
        onLeverOff.Invoke();
}

// Every frame — smooth rotation:
void Update() {
    currentAngle = Mathf.MoveTowards(currentAngle, targetAngle, 90f * Time.deltaTime);
    leverHandle.localRotation = Quaternion.AngleAxis(currentAngle, Vector3.right);
}
```

---

## Wire Panel — The Color Puzzle

The panel has multiple wires. Each has a color only the Ghost sees:

```csharp
[System.Serializable]
public struct Wire {
    public Color wireColor;        // Ghost sees: red, blue, green...
    public bool isCorrectToCut;    // Should this one be cut?
    public WireTexture textureType; // Survivor feels: smooth, braided, corrugated...
}
```

When the Survivor cuts a wire:

```csharp
void CutWire(int index) {
    Wire wire = wires[index];
    wire.isCut = true;

    if (wire.isCorrectToCut) {
        // Good! Check if all correct wires are now cut
        CheckCompletion();
    } else {
        // Wrong wire! Trigger penalty
        onWrongCut.Invoke();
    }
}
```

**The asymmetry in action:** The Ghost sees "cut the RED wire!" but the Survivor sees all wires as the same grey. The Ghost has to communicate which wire using taps, position, or the wire's texture type ("the braided one!").

---

## Oxygen and the Submarine Dying

The submarine slowly runs out of oxygen:

```csharp
// SubmarineManager.cs — every frame while InGame:
oxygenLevel -= 0.5f * Time.deltaTime;  // Loses 0.5% per second
powerLevel -= 0.1f * Time.deltaTime;   // Power drains slower
```

When power gets low, lights start flickering:

```csharp
float lightMultiplier = powerLevel / 100f;  // 0 to 1
foreach (var light in submarineLights) {
    light.intensity = lightMultiplier;

    // Below 20% power — random flicker
    if (powerLevel < 20f) {
        light.intensity *= Random.Range(0.5f, 1f);
    }
}
```

When anything drops below 20% — emergency mode:

```csharp
void OnEnterCriticalState() {
    // Red emergency lights turn on
    foreach (var light in emergencyLights) {
        light.color = Color.red;
        light.enabled = true;
    }
    alarmAudioSource.Play();  // BWAAAA
}
```

If the hull reaches 0? Oxygen drains 5x faster. Game over is imminent.

---

## Mementos — How the Ghost Levels Up

Mementos are hidden personal items. The flow:

```csharp
// Step 1: Survivor walks into a memento (trigger collider)
void OnTriggerEnter(Collider other) {
    if (other.CompareTag("Survivor")) {
        isCollected = true;
        MementoSystem.Instance.CollectMemento(gameObject);
        // Hide it — but don't destroy, Survivor needs to bring it to Ghost
        renderer.enabled = false;
        collider.enabled = false;
    }
}

// Step 2: Survivor brings it to the Ghost somehow
void ReturnToGhost() {
    MementoSystem.Instance.ReturnMemento(gameObject);
    Destroy(gameObject);  // Consumed
}

// Step 3: MementoSystem checks for level-up
void CheckLevelUp() {
    int newLevel = upgradeConfig.GetLevelForMementos(mementosReturned);
    // 1 memento = Level 1, 3 mementos = Level 2, 6 mementos = Level 3
    if (newLevel > currentGhostLevel) {
        GameEvents.FireGhostLevelUp(newLevel);  // Everyone reacts
    }
}
```

---

## Ghost Abilities — Level 1, 2, 3

### Level 1: Point Tracking (Faint Mist)
```csharp
// The Ghost's hand becomes slightly visible as a trail of mist
void ActivatePointTracking() {
    mistEffect.Play();  // Particle system on the hand
    // Also enables a line renderer from the hand forward
}
```
This is permanent once unlocked — helps the Survivor see where the Ghost is pointing.

### Level 2: Possession (Briefly Use Physical Objects)
```csharp
IEnumerator PossessionCoroutine(GameObject target) {
    isPossessing = true;
    GameEvents.FirePossessionStarted(target);

    // For 3 seconds, the Ghost can interact with this object
    var interactable = target.GetComponent<InteractableBase>();
    interactable.TryInteract(PlayerRoleType.Ghost, currentLevel);

    yield return new WaitForSeconds(3f);

    isPossessing = false;
    GameEvents.FirePossessionEnded();
    possessionCooldownTimer = 10f;  // Can't possess again for 10 seconds
}
```

### Level 3: Echo Sight (Show Survivor What Happened)
```csharp
IEnumerator EchoSightCoroutine(VisionSystem survivorVision) {
    // Temporarily give the Survivor clear (unsaturated) vision
    survivorVision.TemporaryOverride(
        saturation: 0f,   // Normal colors!
        bloom: 1f,
        duration: 5f      // Only lasts 5 seconds
    );
    yield return new WaitForSeconds(5f);
    echoSightCooldownTimer = 30f;  // 30 second cooldown
}
```

---

## Phase Progression — The Game Gets Harder

The game has three phases. Each phase changes the rules:

```csharp
// PhaseConfig is a ScriptableObject — data files you edit in the Unity Inspector:
// PhaseConfig_Calibration:     tether=15m, energy=120, oxygen drain=0.3/s, 2 puzzles
// PhaseConfig_Expansion:       tether=10m, energy=100, oxygen drain=0.5/s, 3 puzzles
// PhaseConfig_Convergence:     tether=7m,  energy=80,  oxygen drain=0.8/s, 4 puzzles
```

When enough puzzles are solved, the phase advances:

```csharp
void HandlePuzzleCompleted(int puzzleIndex) {
    puzzlesCompletedInPhase++;

    if (puzzlesCompletedInPhase >= activeConfig.puzzlesToComplete) {
        TryAdvancePhase();  // Calibration → Expansion → Convergence
    }
}

void ApplyPhaseConfig(PhaseConfig config) {
    ghost.Tether.SetMaxDistance(config.maxTetherDistance);      // Shorter leash
    ghost.Energy.SetMaxEnergy(config.ghostMaxEnergy);          // Less energy
    submarine.SetOxygenDepletionRate(config.oxygenDepletionRate); // Faster drain
}
```

---

## Puzzle Framework — The Skeleton

Puzzles aren't implemented yet (waiting for specifics), but here's the base class that all puzzles will extend:

```csharp
public abstract class PuzzleBase : MonoBehaviour {

    // You MUST implement these two:
    protected abstract void OnInitialize();       // Set up your puzzle
    protected abstract bool CheckCompletion();     // Return true when solved

    // The base class handles the rest:
    void Update() {
        if (!isStarted) return;

        // Timer check (if puzzle has a time limit)
        if (timeLimit > 0 && elapsedTime >= timeLimit) {
            Fail();
            return;
        }

        // Check if solved
        if (CheckCompletion()) {
            Complete();
        }
    }

    void Complete() {
        isCompleted = true;
        GameEvents.FirePuzzleCompleted(puzzleIndex);  // PhaseManager hears this
    }
}
```

To make a new puzzle, you'd write something like:

```csharp
public class LeverSequencePuzzle : PuzzleBase {
    public Lever[] levers;
    public int[] correctOrder;
    private int currentStep = 0;

    protected override void OnInitialize() {
        currentStep = 0;
        // Wire up lever callbacks...
    }

    protected override bool CheckCompletion() {
        return currentStep >= correctOrder.Length;  // All levers pulled in order
    }
}
```

---

## Hazard Zones — Invisible Danger

The Survivor can't see hazards. The Ghost can (red glow). When the Survivor walks into one:

```csharp
void OnTriggerStay(Collider other) {
    if (other.transform.root.CompareTag("Survivor")) {
        float damage = 10f * Time.deltaTime;  // 10 HP per second
        GameEvents.FirePlayerDamaged(other.gameObject, damage);
    }
}
```

The Ghost's job: see the hazard, tap near it to warn the Survivor, or Spirit Tag it so the Survivor can briefly see it.

---

## Spectral Anchors — Extending the Leash

When the Ghost needs to explore further from the Survivor, they place an anchor:

```csharp
void Activate(TetherSystem tetherSystem) {
    isActivated = true;

    // "I'm the new tether point now"
    tetherSystem.SetTetherAnchor(this.transform);

    // Give 5 extra meters of range
    tetherSystem.SetMaxDistance(tetherSystem.MaxDistance + 5f);
}
```

Costs 30 energy (the most expensive ability). Strategic placement matters — you only get a few of these per area.

---

## The Lobby — Picking Roles

Before the game starts, both players pick their role:

```csharp
// LobbyUI.cs
void SelectSurvivor() {
    selectedRole = PlayerRoleType.Survivor;
    SessionManager.Instance.SetPlayerRole(localPlayerId, PlayerRoleType.Survivor);
}

void ToggleReady() {
    if (selectedRole == PlayerRoleType.None) {
        statusText.text = "Select a role first!";
        return;
    }
    SessionManager.Instance.SetPlayerReady(localPlayerId);
}
```

The SessionManager guards against invalid combos:

```csharp
bool HasValidRoles() {
    int survivors = 0, ghosts = 0;
    foreach (var role in playerRoles.Values) {
        if (role == PlayerRoleType.Survivor) survivors++;
        if (role == PlayerRoleType.Ghost) ghosts++;
    }
    return survivors == 1 && ghosts == 1;  // Exactly one of each!
}
```

When both players are ready and roles are valid → game auto-starts.

---

## The HUDs — What Each Player Sees on Screen

### Survivor HUD:
```
┌─────────────────────┐
│  O₂ [██████████] 85% │  ← Turns red below 30%
│  HP [████████░░] 80   │
│  Tool: Wrench  🔧    │
│                       │
│  "Press A to grab"    │  ← Fades in/out
└─────────────────────┘
     + red pulsing warning overlay when submarine is critical
```

### Ghost HUD:
```
┌──────────────────────┐
│  ⚡ [██████░░░░] 62/100 │  ← Blue → Orange → Red as it depletes
│  🔗 [████████░░] 78%    │  ← Tether remaining (turns orange near limit)
│  Level 2  [✓][✓][·]    │  ← Ability icons (unlocked/locked)
│  Cooldown: Possess 4s   │
│                          │
│  "⚠ Tether limit!"      │  ← Status messages
└──────────────────────┘
```

Both HUDs are world-space canvases in VR — either strapped to the player's wrist or floating near their head-level.

---

## Networking — Currently Local, Ready for Online

Right now, everything runs locally on one machine. The `NetworkManager` uses a `LocalNetworkService` that instantly delivers messages:

```csharp
// Local mode — messages go straight to handlers on the same machine:
void SendMessage(string channel, byte[] data, int targetPlayerId) {
    if (handlers.TryGetValue(channel, out var handlerList)) {
        foreach (var handler in handlerList) {
            handler.Invoke(localPlayerId, data);  // Immediate delivery
        }
    }
}

// Spawning is just Instantiate:
GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation) {
    return GameObject.Instantiate(prefab, position, rotation);
}
```

When you pick a networking solution (Netcode, Photon, Mirror), you write a new class that implements `INetworkService` and swap it in. All existing code stays the same.

---

## Constants — The Tuning Knobs

Every tweakable number lives in `GameConstants.cs`. Want to make the game harder? Change these:

```csharp
// Make Ghost energy scarcer:
DEFAULT_MAX_ENERGY = 80f;       // Was 100
DEFAULT_ENERGY_REGEN_RATE = 1f; // Was 2

// Make tether shorter:
DEFAULT_TETHER_DISTANCE = 7f;   // Was 10

// Make oxygen drain faster:
OXYGEN_DEPLETION_RATE = 1f;     // Was 0.5

// Change ability costs:
ENERGY_COST_SPIRIT_TAG = 15f;   // Was 10 — tagging is now more expensive
ENERGY_COST_POSSESSION = 35f;   // Was 25 — possession is a big commitment
```

These are inspector-overridable where applicable — the constants are just defaults. The `PhaseConfig` ScriptableObjects override many of these per-phase.
