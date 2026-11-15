# Checkpoint & Respawn System Setup

**Components:**
- [`PlayerRespawn.cs`](../Assets/Scripts/Checkpoints/PlayerRespawn.cs)
- [`Checkpoint.cs`](../Assets/Scripts/Checkpoints/Checkpoint.cs)
- [`KillZone.cs`](../Assets/Scripts/Checkpoints/KillZone.cs)

**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Architecture Overview

The checkpoint system enforces **strict forward-only progression** using numeric IDs:

1. **`PlayerRespawn` (on `Player`):**
   - Tracks `CurrentCheckpointId` (initialized to `int.MinValue`) and `CurrentSpawnPosition` (initialized to the player's starting position in `Awake()`).
   - When the player enters a `Checkpoint` trigger hitbox, `TryActivateCheckpoint(checkpoint)` checks `checkpoint.CheckpointId > CurrentCheckpointId`.
   - Any checkpoint with a **lower or equal** ID is ignored, guaranteeing that once a later checkpoint is reached, no earlier checkpoint can ever reactivate.
   - When `Die()` is called, the player's `Rigidbody2D` velocity is zeroed, their position is reset to `CurrentSpawnPosition`, and (if `Restore Era On Respawn` is enabled) `TimeShiftController.SetEraImmediate(...)` restores the `TimelineEra` that was active when the checkpoint was reached.
2. **`Checkpoint` (on Checkpoint Hitboxes):**
   - Requires a `Collider2D` configured as a trigger (`isTrigger = true`).
   - Exposes `Checkpoint Id` in the Inspector (`1`, `2`, `3`, ...).
   - Optionally tints an attached `SpriteRenderer` (`Inactive Color` vs `Active Color`) and fires `On Activated` (`UnityEvent`).
3. **`KillZone` (on Hazards / Death Pits):**
   - Requires a `Collider2D` (supports both Trigger and solid Collision).
   - Calls `PlayerRespawn.Die()` when touched by the player.

---

## 2. Quick Setup in Unity Editor

### A. Set Up the Player
1. Select the `Player` GameObject.
2. Add the **`PlayerRespawn`** component (`Assets/Scripts/Checkpoints/PlayerRespawn.cs`).
3. Configure **Fall Death** if desired (`Enable Fall Death = true`, `Fall Death Y = -20`).
4. Leave **Restore Era On Respawn** enabled so respawning at a timeline-specific checkpoint restores the correct era.

### B. Place Checkpoints in the Level
1. Create a GameObject (e.g., `Checkpoint_1`) with a **`BoxCollider2D`** (or `CircleCollider2D`).
   - If the checkpoint exists in both eras, place it under `LevelGeometry`.
   - If it only exists in one era, place it under `World_Past` or `World_Future`.
2. Add the **`Checkpoint`** component (`Assets/Scripts/Checkpoints/Checkpoint.cs`).
3. Set **`Checkpoint Id`** sequentially along your level (`1`, `2`, `3`, ...).
4. *(Optional)* Add a child GameObject (e.g., `SpawnPoint`) and assign it to **`Spawn Point`** if you want the player to respawn slightly offset from the hitbox center.

### C. Add Hazards / Death Pits *(Optional)*
1. Create a GameObject (e.g., `PitKillZone` or `Spikes`) with a `Collider2D`.
2. Add the **`KillZone`** component (`Assets/Scripts/Checkpoints/KillZone.cs`).

---

## 3. Inspector Properties

### `PlayerRespawn`
| Property | Default | Description |
| :--- | :--- | :--- |
| **Restore Era On Respawn** | `true` | Restores the `TimelineEra` active when the checkpoint was reached. |
| **Unlock Shift On First Checkpoint** | `true` | Unlocks `TimeShiftController.CanShift` when the player activates their first checkpoint. |
| **Enable Fall Death** | `true` | Automatically triggers `Die()` when `transform.position.y < fallDeathY`. |
| **Fall Death Y** | `-20` | World Y threshold for fall death. |
| **On Player Died** | *(UnityEvent)* | Invoked when `Die()` is called. |
| **On Player Respawned** | *(UnityEvent)* | Invoked after the player is reset to the checkpoint. |

### `Checkpoint`
| Property | Default | Description |
| :--- | :--- | :--- |
| **Checkpoint Id** | `1` | Numeric ID; only checkpoints strictly greater than the current ID can activate. |
| **Spawn Point** | `None` | Optional child Transform for exact respawn coordinates (defaults to `transform.position`). |
| **Force Trigger Collider** | `true` | Forces `Collider2D.isTrigger = true` in `Awake()`. |
| **Sprite Renderer** | `None` *(Auto-finds)* | Optional `SpriteRenderer` tinted to show active/inactive state. |
| **Inactive Color** | Gray | Color applied before activation or after a higher-ID checkpoint activates. |
| **Active Color** | Green | Color applied while this checkpoint is the active respawn point. |
| **On Activated** | *(UnityEvent)* | Invoked when this checkpoint becomes active. |

---

## 4. Public API

* `PlayerRespawn.Die()` — Triggers death and resets the player to the active checkpoint.
* `PlayerRespawn.Respawn()` — Resets position, velocity, and era without firing `OnPlayerDied`.
* `PlayerRespawn.TryActivateCheckpoint(Checkpoint)` — Attempts to register a checkpoint; returns `false` if its ID is `<= CurrentCheckpointId`.
* `PlayerRespawn.CurrentCheckpointId` — Returns the highest activated checkpoint ID (`int.MinValue` if none).
* `TimeShiftController.SetEraImmediate(TimelineEra)` — Immediately sets the active era without obstruction checks (used on respawn).
