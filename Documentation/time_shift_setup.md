# Time Shift Setup & Technical Guide

**Component:** `TimeShiftController.cs`  
**Location:** [`Assets/Scripts/Player/TimeShiftController.cs`](../Assets/Scripts/Player/TimeShiftController.cs)  
**Architecture Reference:** [`time_travel_mechanics.md`](time_travel_mechanics.md) (Method 1: Spatially Overlapping Hierarchies)  
**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Architecture Overview (Method 1)

`TimeShiftController` implements **Method 1: Spatially Overlapping Hierarchies** from [`time_travel_mechanics.md`](time_travel_mechanics.md). Both the **Past** and **Future** versions of a level occupy the exact same $(X, Y)$ world coordinates under two root GameObjects (`World_Past` and `World_Future`), while shared permanent geometry (outer walls, permanent floors) lives outside both roots (e.g., under `LevelGeometry`).

When the player presses **Left Shift** or **Right Shift**, `TimeShiftController.TryShiftTimeline()` performs the following steps within a single frame:

1. **Pre-Validation in Target Timeline:** Temporarily toggles `pastWorldRoot` and `futureWorldRoot` active states to the target era and calls `Physics2D.SyncTransforms()` so `Physics2D` evaluates the target timeline's colliders.
2. **Clear Space Check:** Runs an overlap query matching the player's collider (`OverlapCapsule` for `CapsuleCollider2D`, `OverlapBox` otherwise), inset by `skinMargin` so resting flush on ground or against walls does not count as an obstruction.
3. **Direct Shift:** If unobstructed, commits the shift, updates the camera background color (if enabled), flashes the sprite `shiftFlashColor`, and invokes `OnTimelineShifted(newEra, false)`.
4. **Safety Nudge Fallback:** If obstructed at the current position, `TimeShiftController` attempts to find the nearest valid unobstructed position within `maxNudgeDistance`:
   - **Depenetration Pass:** Queries `Physics2D.Distance(col, hit)` against each overlapping collider in the target era and tests candidate offsets along the separation vector padded by `skinMargin`.
   - **Directional Ring Search:** Searches outward in `nudgeStepSize` increments up to `maxNudgeDistance` across 8 directions (`Up`, `Left`, `Right`, `Up-Left`, `Up-Right`, `Down`, `Down-Left`, `Down-Right`). Candidates at or above the player's current height receive a small scoring bias (`0.05` units) so nudging upward onto platforms is favored over pushing downward.
   - If a valid clear position is found, the player is repositioned cleanly, downward `linearVelocity.y` is zeroed when nudged upward onto a surface, the sprite flashes `nudgeFlashColor`, and `OnTimelineShifted(newEra, true)` fires.
5. **Shift Denial Fallback:** If no clear position exists within `maxNudgeDistance` (e.g., deep inside a solid wall or bunker), the world hierarchy immediately reverts to the original era and syncs transforms in the same frame with zero visual flicker, flashes `deniedFlashColor`, and invokes `OnShiftDenied`.

---

## 2. Quick Setup in Unity Editor

1. **Organize Level Hierarchies:**
   - Keep permanent shared level geometry (outer boundaries, shared ground) under a neutral parent such as `LevelGeometry`.
   - Create two empty root GameObjects at `(0, 0, 0)`:
     - `World_Past` (active by default) — place Past-only platforms, obstacles, and visuals inside.
     - `World_Future` (inactive by default) — place Future-only platforms, obstacles, and visuals inside.
   - Assign solid terrain colliders in both timelines to the `Ground` layer (Layer 8).
2. **Attach `TimeShiftController` to `Player`:**
   - Select the `Player` GameObject (which already has `Rigidbody2D` and `Collider2D`).
   - Add `TimeShiftController` (`Assets/Scripts/Player/TimeShiftController.cs`).
   - Assign **Past World Root** to `World_Past` and **Future World Root** to `World_Future` (if left unassigned, `Awake()` searches for GameObjects named `World_Past` and `World_Future`).
   - Set **Obstacle Layer** to `Ground` (if left unset, `Awake()` defaults to `"Ground"` or falls back to `"Default"`).
   - Optionally assign **Target Camera** (defaults to `Camera.main` in `Awake()`).

---

## 3. Inspector Properties Breakdown

| Property | Default | Description |
| :--- | :--- | :--- |
| **Starting Era** | `Past` | Timeline active when the scene starts (`Past` or `Future`). |
| **Past World Root** | `None` *(Auto-finds `World_Past`)* | Parent GameObject containing Past timeline geometry. |
| **Future World Root** | `None` *(Auto-finds `World_Future`)* | Parent GameObject containing Future timeline geometry. |
| **Shift Cooldown** | `0.15` | Minimum seconds between successful timeline shifts. |
| **Obstacle Layer** | `Ground` *(Fallback: `Default`)* | LayerMask checked for obstructions in the target timeline. |
| **Max Nudge Distance** | `1.5` | Maximum search radius (world units) for the safety nudge fallback. |
| **Nudge Step Size** | `0.1` | Radial distance increment per search ring during safety nudge resolution. |
| **Skin Margin** | `0.04` | Inset applied to the player's collider size during overlap queries so flush floor/wall contacts do not block shifting. |
| **Target Camera** | `None` *(Auto-finds `Camera.main`)* | Camera tinted to reflect the active timeline. |
| **Tint Camera Background** | `true` | Enables camera background color switching between eras. |
| **Past Background Color** | `(0.11, 0.13, 0.17, 0)` | Camera background color when in the Past era. |
| **Future Background Color** | `(0.07, 0.17, 0.22, 0)` | Camera background color when in the Future era. |
| **Shift Flash Color** | `(0.6, 0.95, 1, 1)` | Sprite tint flashed on a clean shift. |
| **Nudge Flash Color** | `(1, 0.85, 0.3, 1)` | Sprite tint flashed when a safety nudge resolves an overlap. |
| **Denied Flash Color** | `(1, 0.3, 0.3, 1)` | Sprite tint flashed when a shift is denied. |
| **Feedback Flash Duration** | `0.12` | Duration (seconds) of the sprite tint flash. |

---

## 4. Controls & Public API

### Controls

| Action | Primary Key | Secondary Key |
| :--- | :--- | :--- |
| **Time Shift (Past <-> Future)** | `Left Shift` | `Right Shift` |

> **Note on Input System:** Uses Unity's modern Input System (`Keyboard.current`) when enabled, with automatic fallback to the legacy Input Manager (`Input.GetKeyDown`).

### Public Members & Events

* `TimelineEra CurrentEra` — Returns the currently active `TimelineEra` (`Past` or `Future`).
* `bool TryShiftTimeline()` — Programmatically triggers a shift attempt; returns `true` if shifted, `false` if denied.
* `event Action<TimelineEra, bool> OnTimelineShifted` — Fired after a successful shift with `(newEra, wasSafetyNudged)`. Subscribe audio/VFX systems here.
* `event Action OnShiftDenied` — Fired when a shift is denied because no clear position exists within `maxNudgeDistance`.

---

## 5. Scene View Debugging (Gizmos)

When `Player` is selected in the Scene view:
* **Green Wire Cube:** The inset overlap check area (`bounds.size - skinMargin`).
* **Cyan Wire Sphere:** The `maxNudgeDistance` search radius for the safety nudge.
* **Yellow Line & Sphere:** Appears after a safety nudge occurs, showing the exact origin-to-destination nudge vector.

---

## 6. Test Layout in `MovementTest.unity`

[`Assets/Scenes/MovementTest.unity`](../Assets/Scenes/MovementTest.unity) is configured with `World_Past` and `World_Future` to exercise all shift outcomes:
* **Clean Shift:** At spawn (`x = -8`), pressing `Shift` swaps between the Past and Future unobstructed.
* **Safety Nudge:** Stand on the main floor around `x = -5.5` in the Past and press `Shift` — `Future_NudgeTestPlatform` overlaps the player's torso, triggering a safety nudge onto the top or side of the platform.
* **Mid-Air Platforming:** Use `Future_CenterPlatform` (`x = -0.5`) and `Future_GapBridge` (`x = 15`) to cross gaps by shifting mid-jump.
* **Shift Denial:** Stand near `x = -12` in the Past and press `Shift` — `Future_SolidBunker` (`4.5 x 4`) exceeds `maxNudgeDistance` (`1.5`), denying the shift and flashing red.
