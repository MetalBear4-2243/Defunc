# Background Music System Setup & Guide

**Component:** `BackgroundMusicPlayer.cs`  
**Location:** [`Assets/Scripts/Audio/BackgroundMusicPlayer.cs`](../Assets/Scripts/Audio/BackgroundMusicPlayer.cs)  
**Target Engine:** Unity 6 (6000.6.9f1)

---

## 1. Architecture Overview

`BackgroundMusicPlayer` plays a two-part `.wav` background music track—an **intro** clip followed seamlessly by a **looping** clip—until the level is completed.

Instead of polling `AudioSource.isPlaying` in `Update()` (which introduces frame-rate-dependent gaps between clips), the system uses Unity's sample-accurate DSP scheduling:

1. **Dual `AudioSource` Setup:** Two 2D `AudioSource` components (`introSource` and `loopSource`) are managed on the GameObject (auto-created in `Awake()` if left unassigned).
2. **Exact Sample Duration:** The intro duration is computed in `double` precision using `(double)introClip.samples / introClip.frequency` rather than `AudioClip.length` (float).
3. **DSP Scheduling:**
   - `introSource.PlayScheduled(startTime)` starts the intro after a short `scheduleLeadTime` (`AudioSettings.dspTime + scheduleLeadTime`).
   - `loopSource.PlayScheduled(startTime + introDuration)` with `loop = true` queues the main loop to begin at the exact sample the intro finishes.
4. **Pause-Safe Timing:** Calling `Pause()` during the intro records the remaining DSP time before the loop handoff and reschedules `loopSource` on `Resume()` so the loop never drifts or starts early.
5. **Level Completion:** Calling `CompleteLevel()` marks the level as completed and smoothly fades out (using `Time.unscaledDeltaTime`) or immediately stops both sources.

---

## 2. Recommended `.wav` Import Settings

For each `.wav` music file in the Project window, configure the **AudioImporter** Inspector as follows:

| Setting | Recommended Value | Notes |
| :--- | :--- | :--- |
| **Force to Mono** | Disabled (Stereo) | Preserves full stereo mix for BGM. |
| **Load Type** | `Streaming` | Avoids loading large music files completely into RAM. |
| **Preload Audio Data** | Enabled | Ensures the first buffer is ready so scheduled playback starts cleanly without delay. |
| **Compression Format** | `Vorbis` | Best size-to-quality ratio for background music. |
| **Quality** | `60% – 70%` | Transparent audio quality with a significantly smaller build footprint. |
| **Sample Rate Setting** | `Preserve Sample Rate` | Prevents unintended resampling artifacts at the intro-to-loop seam. |

---

## 3. Quick Setup in Unity Editor

1. Create an empty GameObject in your level scene named `BackgroundMusic` (or attach to a dedicated audio manager object).
2. Add the **`BackgroundMusicPlayer`** component (`Assets/Scripts/Audio/BackgroundMusicPlayer.cs`).
3. Assign your two `.wav` files in the Inspector:
   - **Intro Clip:** Drag in the intro `.wav` file.
   - **Loop Clip:** Drag in the looping main `.wav` file.
4. Leave **Intro Source** and **Loop Source** empty (the script creates and configures both automatically in `Awake()`), or assign your own `AudioSource` components if you need custom Inspector overrides.
5. Trigger **`CompleteLevel()`** when the player finishes the level:
   - Wire it via a `UnityEvent` in the Inspector (e.g., on a goal trigger), **or**
   - Call `backgroundMusicPlayer.CompleteLevel();` from your level completion script.

---

## 4. Inspector Properties

| Property | Default | Description |
| :--- | :--- | :--- |
| **Intro Clip** | `None` | `.wav` clip played once at the start. If omitted, playback starts directly with **Loop Clip**. |
| **Loop Clip** | `None` | `.wav` clip looped seamlessly after the intro until `CompleteLevel()` is called. |
| **Play On Start** | `true` | Automatically starts playback in `Start()`. |
| **Volume** | `0.7` | Master volume `[0, 1]` applied to both `AudioSource`s. |
| **Schedule Lead Time** | `0.05` | Lead time in seconds added to `AudioSettings.dspTime` so the audio thread buffers cleanly. |
| **Level Complete Fade Out Duration** | `1.0` | Fade-out duration in seconds when `CompleteLevel()` is called (`0` stops immediately). |
| **Output Mixer Group** | `None` | Optional `AudioMixerGroup` routed to both `AudioSource`s. |
| **Intro Source** | `None` *(Auto-created)* | Optional pre-existing `AudioSource` for the intro clip. |
| **Loop Source** | `None` *(Auto-created)* | Optional pre-existing `AudioSource` for the looping clip. |

---

## 5. Public API

* `bool IsPlaying` — Returns `true` while music is playing or waiting for the scheduled intro-to-loop transition.
* `bool IsPaused` — Returns `true` while playback is paused.
* `bool IsLevelCompleted` — Returns `true` after `CompleteLevel()` is called for the current session.
* `float Volume` — Gets or sets the music volume (`[0, 1]`).
* `void PlayMusic()` — Starts (or restarts) the configured intro and loop clips.
* `void PlayMusic(AudioClip newIntroClip, AudioClip newLoopClip)` — Swaps clips and starts playback immediately.
* `void CompleteLevel()` — Marks the level complete and fades out / stops the music using `levelCompleteFadeOutDuration`.
* `void StopMusic(float fadeOutDuration = 0f)` — Stops music immediately or over `fadeOutDuration` seconds.
* `void Pause()` / `void Resume()` — Pauses and resumes playback while preserving the intro-to-loop schedule.
* `void SetVolume(float newVolume)` — Updates volume in `[0, 1]`.

### Example Usage

```csharp
using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    [SerializeField] private BackgroundMusicPlayer musicPlayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && musicPlayer != null)
        {
            musicPlayer.CompleteLevel();
        }
    }
}
```
