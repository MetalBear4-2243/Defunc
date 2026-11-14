# Player Movement Setup & Configuration Guide

**Component:** `PlayerMovement.cs`  
**Location:** [`Assets/Scripts/Player/PlayerMovement.cs`](file:///c:/Users/metal/Documents/ACMGameJame2026/Defunc/Assets/Scripts/Player/PlayerMovement.cs)  
**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Quick Setup in Unity Editor

Follow these steps to set up the player character in your scene:

### Step 1: Create the Player GameObject
1. In the **Hierarchy**, right-click and choose **2D Object > Sprites > Capsule** (or **Square**).
2. Rename the object to `Player`.
3. Set the position to your desired starting spawn point (e.g., `(0, 0, 0)`).

### Step 2: Configure Physics Components
1. Select the `Player` GameObject.
2. In the **Inspector**, click **Add Component** and add:
   * **`Rigidbody2D`**
   * **`CapsuleCollider2D`** (or `BoxCollider2D`)
3. Configure the **`Rigidbody2D`** settings:
   * **Body Type:** `Dynamic`
   * **Collision Detection:** `Continuous` *(prevents tunneling through thin platforms at high speeds)*
   * **Interpolate:** `Interpolate` *(ensures smooth motion without visual camera jitter)*
   * **Constraints:** Check **Freeze Rotation Z** *(the script also enforces this in `Awake`)*
   * **Gravity Scale:** Set to `3` *(matches default `baseGravityScale`)*

### Step 3: Attach the PlayerMovement Script
1. Drag `Assets/Scripts/Player/PlayerMovement.cs` onto the `Player` GameObject (or click **Add Component > Player Movement**).

---

## 2. Inspector Properties Breakdown

| Property | Default | Description |
| :--- | :--- | :--- |
| **Move Speed** | `8` | Horizontal velocity in units/second. |
| **Jump Force** | `13` | Instant vertical velocity applied when jumping. |
| **Ground Check** | `None` *(Optional)* | Child transform placed at the feet. If left empty, the script automatically calculates the ground check from the bottom of your `Collider2D`. |
| **Ground Check Radius** | `0.2` | Radius of the ground overlap sphere check. |
| **Ground Layer** | `Default` | LayerMask specifying which layers count as solid ground. |
| **Base Gravity Scale** | `3` | Base gravity scale when moving normally or grounded. |
| **Fall Multiplier** | `2.5` | Gravity multiplier applied during descent for snappy, responsive falls. |
| **Low Jump Multiplier**| `2.0` | Gravity multiplier applied when releasing the jump key early (short hops). |
| **Coyote Time** | `0.12` | Seconds after walking off a ledge where jumping is still allowed. |
| **Jump Buffer Time** | `0.1` | Seconds before landing where pressing jump is remembered and executed upon touching ground. |

---

## 3. Recommended Ground Layer Setup

To avoid unintended collision checks:
1. In the top-right of the Inspector, click the **Layer** dropdown and select **Add Layer...**
2. Name an empty user layer (e.g., Layer 8) as `Ground`.
3. Select your platform/tilemap GameObjects and assign their layer to `Ground`.
4. Select the `Player` object, go to the `PlayerMovement` component, and set **Ground Layer** to `Ground`.

---

## 4. Controls

| Action | Primary Keys | Secondary Keys |
| :--- | :--- | :--- |
| **Move Left** | `A` | `Left Arrow` |
| **Move Right** | `D` | `Right Arrow` |
| **Jump / Variable Jump** | `Spacebar` (Hold for high jump, tap for short hop) | — |

> **Note on Input System:** The script is configured to use the modern Unity Input System Package (`Keyboard.current`) which is active in this project, while retaining an automatic fallback to the legacy Input Manager.

---

## 5. Scene View Debugging

When the `Player` GameObject is selected in the Hierarchy, an overlap circle gizmo is drawn at the feet:
* **Green Gizmo:** Player is currently detected as **Grounded**.
* **Red Gizmo:** Player is currently **Airborne**.

---

## 6. Common Troubleshooting

* **Player sticks to walls when jumping against them:**
  * Create a **Physics Material 2D** (`Assets > Create > 2D > Physics Material 2D`).
  * Set **Friction** to `0` and **Bounciness** to `0`.
  * Assign this material to the `Player`'s `Collider2D` **Material** slot.
* **Player falls through ground:**
  * Ensure the ground tilemap/collider has a collider component (`TilemapCollider2D` or `BoxCollider2D`).
  * Verify the `Player`'s `Rigidbody2D` **Collision Detection** is set to `Continuous`.
