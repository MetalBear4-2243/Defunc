# Hazard KillZone & Glow Setup

**Prefab:** [`Hazard_KillZone.prefab`](../Assets/Level%20Prefabs/Hazard_KillZone.prefab)  
**Components:**
- [`KillZone.cs`](../Assets/Scripts/Checkpoints/KillZone.cs)
- [`HazardGlow.cs`](../Assets/Scripts/Checkpoints/HazardGlow.cs)

**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Overview

`Hazard_KillZone` is a fire-red hazard prefab with a pulsing orange glow around its entire perimeter. Touching it triggers `PlayerRespawn.Die()`.

`HazardGlow` generates a shared 9-sliced soft-edge box sprite and unlit material at runtime, automatically compensating for non-uniform parent scaling so wide or tall hazards glow evenly along all four edges. Combined with URP Bloom in [`DefaultVolumeProfile.asset`](../Assets/Settings/DefaultVolumeProfile.asset), the HDR orange aura radiates cleanly around the full hazard.

---

## 2. Inspector Properties (`HazardGlow`)

| Property | Default | Description |
| :--- | :--- | :--- |
| **Glow Color** | HDR Orange `(2.5, 0.9, 0.1, 0.9)` | HDR color applied to the glow sprite; values > `1.0` trigger URP Bloom. |
| **World Glow Padding** | `0.45` | Uniform world-space padding added around all edges of the hazard. |
| **Pulse Padding Amount** | `0.15` | Additional padding added at the peak of each pulse cycle. |
| **Pulse Speed** | `3.2` | Speed of the glow pulse animation. |
| **Point Light** | `None` *(Auto-finds)* | Optional `Light2D` component driven in sync with the pulse. |
| **Base Light Intensity** | `2.5` | Minimum `Light2D` intensity. |
| **Pulse Light Amount** | `0.6` | Extra `Light2D` intensity added at the peak of each pulse. |
