# Time Shift Pulse Effect Setup & Technical Guide

**Component:** `TimeShiftPulseEffect.cs`  
**Location:** [`Assets/Scripts/Player/TimeShiftPulseEffect.cs`](../Assets/Scripts/Player/TimeShiftPulseEffect.cs)  
**Related Components:** [`TimeShiftController.cs`](../Assets/Scripts/Player/TimeShiftController.cs)  
**Architecture Reference:** [`time_shift_setup.md`](time_shift_setup.md), [`time_travel_mechanics.md`](time_travel_mechanics.md)  
**Target Engine:** Unity 6 (`6000.6.9f1`), Universal Render Pipeline (URP 2D)

---

## 1. Overview & Visual Behavior

`TimeShiftPulseEffect` generates an expanding circular shockwave pulse originating from the exact center of the player character whenever the player successfully shifts timelines between the **Past** and **Future** (or resolves an obstructed shift via safety nudge).

### Key Visual & Architectural Features

1. **Player-Centered Origin:**
   - Automatically computes origin from the player's `Collider2D.bounds.center` (`playerCollider`), with an optional inspector `centerOffset`.
   - By default (`followPlayer = false`), the pulse expands in world space from where the player was standing when the shift occurred, giving an impactful physical shockwave sensation. An optional toggle (`followPlayer = true`) pins the ring to the moving player.
2. **Concentric Ripple Echoes:**
   - Emits multi-ring ripples (`ringCount`, default `2`) staggered by a small delay (`ringDelay = 0.06s`) and scale factor (`secondaryRingScale = 0.85`), providing a signature temporal reverberation aesthetic.
3. **Era-Responsive & Nudge Styling:**
   - **Past Shift:** Bright electric cyan (`#59E6FF`).
   - **Future Shift:** Radiant amber (`#FFA633`).
   - **Safety Nudge:** Warm golden accent (`#FFE059`) when the safety nudge depenetration system resolves an overlap.
4. **Zero-Allocation High Performance:**
   - Pre-computes 64-segment unit circle trigonometric coordinates in `Awake()`.
   - Uses pre-allocated vertex buffers and an internal `LineRenderer` object pool to achieve zero garbage collection (0 B GC alloc) during gameplay updates.
5. **Universal Render Pipeline 2D Compatibility:**
   - Automatically resolves `Universal Render Pipeline/2D/Sprite-Unlit` (with fallback to `Sprites/Default`) so the shockwave is crisp, vibrant, unlit, and immune to ambient scene dimming without requiring external texture assets.

---

## 2. Quick Setup in Unity Editor

1. **Attach to Player:**
   - Select the `Player` GameObject in the scene hierarchy.
   - Add the `TimeShiftPulseEffect` component (`Assets/Scripts/Player/TimeShiftPulseEffect.cs`).
   - The component automatically detects `TimeShiftController` and `Collider2D` on the player.
2. **Verify `TimeShiftController` Integration:**
   - In `TimeShiftController`, the **Pulse Effect** slot under **Visual Feedback** will automatically link to `TimeShiftPulseEffect`.
   - `TimeShiftPulseEffect` subscribes to `TimeShiftController.OnTimelineShifted` during `OnEnable` and unregisters in `OnDisable`.
3. **Test in Play Mode:**
   - Enter Play Mode in [`Assets/Scenes/MovementTest.unity`](../Assets/Scenes/MovementTest.unity).
   - Press **Left Shift** or **Right Shift** to shift timelines and see the expanding pulse radiate outward from the player's center.
   - You can also right-click `TimeShiftPulseEffect` in the Inspector during Play Mode and select **Emit Test Pulse**.

---

## 3. Inspector Properties Breakdown

### Dependencies
| Property | Default | Description |
| :--- | :--- | :--- |
| **Time Shift Controller** | `None` *(Auto-retrieves)* | Reference to `TimeShiftController`. Subscribes to `OnTimelineShifted`. |
| **Player Collider** | `None` *(Auto-retrieves)* | Collider used to calculate `bounds.center` for the pulse origin. |
| **Center Offset** | `(0, 0)` | Positional offset added to the collider center. |
| **Follow Player** | `false` | If enabled, rings move with the player. If disabled, rings expand in place from the shift origin. |

### Pulse Timing & Dimensions
| Property | Default | Description |
| :--- | :--- | :--- |
| **Pulse Duration** | `0.4` | Duration (seconds) of the pulse expansion and fadeout. |
| **Start Radius** | `0.1` | Starting radius (world units) of the pulse ring at creation. |
| **Target Radius** | `3.2` | Maximum expansion radius (world units) reached by the primary ring. |
| **Expansion Curve** | `Ease-Out` | Animation curve controlling radial expansion progression over normalized time. |
| **Start Width** | `0.22` | Initial line thickness (world units) of the pulse ring. |
| **End Width** | `0.02` | Final line thickness (world units) at peak expansion. |
| **Width Curve** | `Linear (1 -> 0.25)` | Multiplier curve for ring width over duration. |

### Concentric Ripple Rings
| Property | Default | Description |
| :--- | :--- | :--- |
| **Ring Count** | `2` | Number of concentric rings emitted per shift (range 1–4). |
| **Ring Delay** | `0.06` | Delay (seconds) before secondary echo rings begin expanding. |
| **Secondary Ring Scale** | `0.85` | Radius scale factor applied to secondary echo rings. |

### Colors & Styling
| Property | Default | Description |
| :--- | :--- | :--- |
| **Use Era Colors** | `true` | When true, tints the pulse based on the destination timeline era. |
| **Past Pulse Color** | `RGBA(0.35, 0.9, 1.0, 1.0)` | Electric cyan pulse color when shifting to the Past. |
| **Future Pulse Color** | `RGBA(1.0, 0.65, 0.2, 1.0)` | Radiant amber pulse color when shifting to the Future. |
| **Use Nudge Color** | `true` | When true, uses a distinct accent color when a safety nudge occurred. |
| **Nudge Pulse Color** | `RGBA(1.0, 0.88, 0.35, 1.0)` | Warm golden pulse color when a shift required a safety nudge. |
| **Alpha Fade Curve** | `Ease-In Fade` | Alpha curve controlling the opacity fadeout over normalized time. |

### Rendering Configuration
| Property | Default | Description |
| :--- | :--- | :--- |
| **Ring Segments** | `64` | Number of vertices in the circular LineRenderer (range 16–128). |
| **Pulse Material** | `None` *(Auto-creates unlit)* | Optional custom material. Auto-defaults to URP 2D Sprite-Unlit. |
| **Sorting Layer Name** | `"Default"` | Sorting layer assigned to the pulse LineRenderers. |
| **Sorting Order** | `5` | Sorting order within the layer (renders in front of the player sprite at order 1). |
| **Z Offset** | `0` | World Z-depth offset for pulse rings. |

### Audio (Optional)
| Property | Default | Description |
| :--- | :--- | :--- |
| **Pulse Sound** | `None` | Optional AudioClip played on each pulse emission. |
| **Audio Source** | `None` *(Auto-retrieves)* | AudioSource used to play `pulseSound`. |

---

## 4. Public API & Scripting

```csharp
// Current calculated center position in world space
Vector2 center = pulseEffect.PulseCenter;

// Trigger pulse with timeline context (era & nudge flag)
pulseEffect.EmitPulse(TimelineEra.Future, wasSafetyNudged: false);

// Trigger default pulse
pulseEffect.EmitPulse();

// Trigger pulse with an arbitrary custom color
pulseEffect.EmitPulseWithColor(Color.magenta);

// Runtime properties
pulseEffect.PulseDuration = 0.5f;
pulseEffect.TargetRadius = 4.0f;
pulseEffect.RingCount = 3;
pulseEffect.FollowPlayer = true;
```

---

## 5. Scene View Debugging (Gizmos)

When `Player` (or any GameObject with `TimeShiftPulseEffect`) is selected in the Scene view:
- **Cyan Inner Wire Sphere:** The initial `startRadius` at the player's center.
- **Teal Outer Wire Sphere:** The maximum `targetRadius` reachable by the pulse shockwave.
- **Yellow Small Sphere:** The exact center origin (`PulseCenter`) accounting for `Collider2D.bounds.center` and `centerOffset`.
