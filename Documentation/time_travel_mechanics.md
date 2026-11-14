# 2D Time Travel Platformer: Architectural Methods & Design

**Target Engine:** Unity 6 (6000.6.9f1)  
**Context:** Game Jam  
**Core Gimmick:** Single-button jump/swap between a **Past** and **Future** version of the level.

---

## 1. Overview & Architectural Approaches

In a fast-paced game jam setting, your architectural choice dictates not only gameplay fluidity, but also how effectively your team can build levels and avoid Git merge conflicts.

### Method 1: Spatially Overlapping Hierarchies (Layer / Active State Toggling)
Both the **Past** and **Future** versions of the level are constructed at the exact same $(X, Y)$ world coordinates, grouped under separate parent GameObjects (e.g., `World_Past` and `World_Future`).

* **Mechanism:**
  * Pressing the time-shift button toggles active states (`SetActive(true/false)`) or switches 2D physics layers and Camera culling masks.
  * The player and camera remain at their current world coordinates; the surrounding environment swaps instantaneously.
* **Pros:**
  * **Zero Coordinate Math:** No offset calculations or player repositioning required.
  * **Visual Layout Alignment:** In the Scene view, designers can overlay both tilemaps with transparency to verify platform alignment.
* **Cons & Considerations:**
  * Deactivating GameObjects halts internal scripts, timers, and animations. If enemies or hazards in the inactive timeline need to continue patrolling or cycling, layer/collider toggling must be used instead of complete deactivation.
* **Jam Feasibility:** **Highest.** Fastest to implement and easiest to debug.

---

### Method 2: Side-by-Side Offset (Coordinate Teleportation)
The **Past** level is built at the origin $(0, 0)$, while the **Future** level is constructed at a fixed, large spatial offset (e.g., exactly $(1000, 0)$ or $(0, 1000)$).

* **Mechanism:**
  * When shifting, the player and camera instantaneously translate by the offset vector $(+\Delta X)$ or $(-\Delta X)$.
* **Pros:**
  * **Continuous Background Simulation:** Both timelines remain active in the physics and update loops. Enemy patrols, moving hazards, and timers continue running uninterrupted.
  * **Dual-View & Portals:** Enables multi-camera techniques such as a "time peephole" or portal showing a live feed of the other era using RenderTextures.
* **Cons & Considerations:**
  * Requires strict coordinate discipline from level designers to ensure both maps match the offset.
  * Dynamic objects (carried keys, thrown projectiles, moving platforms) must handle delta transformations when passing through a shift.
* **Jam Feasibility:** **High.** Ideal if the non-active era needs active background simulation.

---

### Method 3: Additive Scene Loading (Team Collaboration Approach)
The level structure is separated into distinct Unity scene files:
1. `Core_Scene`: Contains the Player, Camera, UI, and Audio/Game Managers.
2. `Level_Past`: Contains the Past tilemaps, environment, lighting, and hazards.
3. `Level_Future`: Contains the Future tilemaps, environment, lighting, and hazards.

* **Mechanism:**
  * Both era scenes are loaded additively into memory during level initialization. Shifting toggles root object visibility, camera culling, or physics layers.
* **Pros:**
  * **Eliminates Git Merge Conflicts:** Teammates can concurrently build and decorate the Past and Future layouts in separate scene files without locking or conflicting on a single `.unity` asset.
* **Cons & Considerations:**
  * Cross-scene interactions (e.g., a switch in the Past triggering an opening door in the Future) require an event bus or manager references rather than direct Inspector dragging.
* **Jam Feasibility:** **Very High for teams with multiple level designers or artists.**

---

## 2. Critical Gameplay & Technical Challenges

### A. The "Telefrag" (Stuck in Wall) Dilemma
* **The Problem:** The player shifts into an era where their current space is occupied by solid ground (e.g., a wall that existed in the past crumbled in the future, but the player shifts while standing inside where the future wall will be).
* **Solutions:**
  1. **Shift Denial (Recommended for Jams):** Perform an overlap check (box/capsule check) against the target era's collider layer before executing the swap. If obstructed, deny the shift, play an error sound, and flash a warning icon.
  2. **Ghost / Silhouette Preview:** Render a faint outline or silhouette of the other era's terrain near the player so obstacles are intuitively visible before shifting.
  3. **Ejection / Safety Nudge:** Snap or raycast the player to the nearest open, valid standing surface.

### B. Momentum & Trajectory Preservation
* Preserving `Rigidbody2D` velocity across shifts ensures jumping in the past and shifting mid-air into the future feels responsive and continuous.
* Ensure platform heights and ceiling clearances correspond across both layouts so players aren't unexpectedly squashed or blocked mid-jump.

### C. Causality & Persistence (Puzzle Mechanics)
* **The Concept:** Actions taken in the Past alter conditions in the Future (e.g., planting a seed in the Past yields a climbable vine in the Future; breaking a support pillar drops a bridge).
* **Implementation Pattern:** Maintain a centralized state table or flag system. When a past trigger is activated, the corresponding future entity queries the flag state and updates its sprite, collider, or animation.

---

## 3. Audio & Visual Polish ("Jam Juice")

* **Synchronized Dual-Track Music (Horizontal Re-orchestration):**
  * Compose two parallel tracks with the same tempo (BPM) and bar structure (e.g., warm acoustic/8-bit for Past; distorted synth/industrial for Future).
  * Run both tracks concurrently in lockstep, crossfading Audio Mixer group volumes on shift.
* **Visual Transition Effects:**
  * Screen-space ripple, glitch shader, or chromatic aberration spike at the shift frame.
  * A micro freeze-frame (hit-stop for 2–3 frames) during shift creates tactile feedback.
  * Distinct color grading profiles using Unity URP Volume post-processing (e.g., warm, saturated hues for Past; desaturated, cold tones for Future).

---

## 4. Decision Matrix

| Method | Best Used When... | Complexity | Collaboration Risk |
| :--- | :--- | :--- | :--- |
| **Method 1: Spatially Overlapping** | Solo dev or 1 level designer; quick prototyping | Low | High (single scene file) |
| **Method 2: Side-by-Side Offset** | Simultaneous background simulation or live portals needed | Medium | High (single scene file) |
| **Method 3: Additive Scenes** | Multi-person team dividing art and level design tasks | Medium | Low (independent scene files) |
