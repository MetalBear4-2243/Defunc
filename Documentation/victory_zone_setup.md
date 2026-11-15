# Victory Zone & Victory Screen Setup

**Prefab:**
- [`VictoryZone.prefab`](../Assets/Level%20Prefabs/VictoryZone.prefab) *(Emerald Green + Pulsing Green Glow)*

**Components:**
- [`VictoryZone.cs`](../Assets/Scripts/Checkpoints/VictoryZone.cs)
- [`VictoryScreen.cs`](../Assets/Scripts/UI/VictoryScreen.cs)
- [`HazardGlow.cs`](../Assets/Scripts/Checkpoints/HazardGlow.cs) *(Reused on `GreenGlow` child)*

**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Overview

`VictoryZone` is a glowing emerald-green goal prefab that displays a **Victory Screen** when touched by the player (`PlayerMovement` or `PlayerRespawn`).

Like the `Hazard_KillZone` prefabs, `VictoryZone` uses `HazardGlow` and a `Light2D` point light on its `GreenGlow` child so it scales cleanly in any direction while maintaining a uniform glowing border.

If no `VictoryScreen` exists in the scene, `VictoryZone` automatically spawns one at runtime and generates a Screen Space Overlay Canvas with a **Restart Level** button (and a **Next Level** button if a subsequent scene is available in Build Settings).

---

## 2. Quick Setup in Unity Editor

1. Drag [`VictoryZone.prefab`](../Assets/Level%20Prefabs/VictoryZone.prefab) into your scene (either under `LevelGeometry` so it is active in both eras, or inside `World_Past` / `World_Future` if the goal only exists in one timeline).
2. Scale and position it at the end of the level.
3. *(Optional)* To customize the Victory Screen text, styling, or UI layout, add [`VictoryScreen.cs`](../Assets/Scripts/UI/VictoryScreen.cs) to a GameObject in the scene and configure its Inspector properties (or assign your own custom `VictoryPanel` Canvas).

---

## 3. Inspector Properties

### `VictoryZone`
| Property | Default | Description |
| :--- | :--- | :--- |
| **Victory Screen** | `None` *(Auto-finds or creates)* | Optional reference to a `VictoryScreen` in the scene. |
| **Force Trigger Collider** | `true` | Ensures the attached `Collider2D` is set to `isTrigger = true` on `Awake()`. |
| **Freeze Player On Victory** | `true` | Disables `PlayerMovement` and `TimeShiftController` and zeroes `Rigidbody2D.linearVelocity` on contact. |
| **On Victory Triggered** | *(Empty)* | `UnityEvent` invoked when the player touches the goal. |

### `VictoryScreen`
| Property | Default | Description |
| :--- | :--- | :--- |
| **Victory Panel** | `None` *(Auto-generated)* | Optional custom UI panel to show on victory. |
| **Victory Title** | `"LEVEL COMPLETE"` | Heading displayed on the victory card. |
| **Victory Subtitle** | `"Timeline stabilized. Press R or click Restart to play again."` | Subtitle displayed below the heading. |
| **Pause Time On Victory** | `true` | Sets `Time.timeScale = 0f` when shown, and restores `1f` on restart/next level. |
| **Next Scene Name** | `""` | Optional scene name for the **Next Level** button; falls back to `buildIndex + 1` if in Build Settings. |
| **Fade In Duration** | `0.25` | Unscaled fade-in duration (seconds) for the overlay. |
