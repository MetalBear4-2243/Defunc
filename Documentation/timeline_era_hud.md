# Timeline Era HUD Setup

**Component:** [`TimelineEraHUD.cs`](../Assets/Scripts/UI/TimelineEraHUD.cs)  
**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Overview

`TimelineEraHUD` displays a compact badge in the top-left corner of the screen showing whether the player is currently in the **Past** or **Future** timeline.

It listens to `TimeShiftController.OnTimelineShifted` so it updates automatically on normal shifts as well as checkpoint respawns (`SetEraImmediate`).

---

## 2. Quick Setup in Unity Editor

### Option A: Zero-Setup (Auto-Generated UI)
1. Create an empty GameObject in your scene (e.g., `TimelineEraHUD`).
2. Add the **`TimelineEraHUD`** component (`Assets/Scripts/UI/TimelineEraHUD.cs`).
3. Leave `Era Label`, `Badge Background`, and `Indicator Dot` unassigned.
4. Press Play — the script automatically creates a `ScreenSpaceOverlay` Canvas and a top-left badge showing `PAST` or `FUTURE`.

### Option B: Custom Canvas UI
1. Build your own UI elements inside an existing `Canvas` in the top-left corner.
2. Add **`TimelineEraHUD`** to your HUD controller or Canvas.
3. Assign your `TMP_Text` to **Era Label** (and optionally `Image` components to **Badge Background** and **Indicator Dot**).

---

## 3. Inspector Properties

| Property | Default | Description |
| :--- | :--- | :--- |
| **Time Shift Controller** | `None` *(Auto-finds)* | Reference to the player's `TimeShiftController`. |
| **Era Label** | `None` *(Auto-builds)* | Optional custom `TMP_Text` displaying the active era. |
| **Badge Background** | `None` *(Auto-builds)* | Optional background `Image` for the badge. |
| **Indicator Dot** | `None` *(Auto-builds)* | Optional accent `Image` tinted to match the active era. |
| **Past Label** | `"PAST"` | Text shown when `TimelineEra.Past` is active. |
| **Future Label** | `"FUTURE"` | Text shown when `TimelineEra.Future` is active. |
| **Past Accent Color** | Warm Amber | Text and accent color used in the Past era. |
| **Future Accent Color** | Cyan | Text and accent color used in the Future era. |
| **Badge Background Color** | Dark Translucent | Background tint for the badge panel. |
| **Top Left Margin** | `(20, -20)` | Offset from the top-left corner when auto-generating the UI. |
| **Badge Size** | `(140, 40)` | Width and height of the auto-generated badge. |
| **Font Size** | `20` | Font size of the auto-generated `TextMeshProUGUI` label. |
