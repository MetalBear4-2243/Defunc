using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Plays a two-part background music track using sample-accurate DSP scheduling:
/// an optional intro WAV clip followed seamlessly by a looping main WAV clip
/// until the level is completed.
/// </summary>
public class BackgroundMusicPlayer : MonoBehaviour
{
    [Header("Music Clips (WAV)")]
    [Tooltip("Intro clip played once when music starts. Leave empty to start directly with the loop clip.")]
    [SerializeField] private AudioClip introClip;

    [Tooltip("Main background music clip looped seamlessly after the intro finishes until the level is completed.")]
    [SerializeField] private AudioClip loopClip;

    [Header("Playback Settings")]
    [Tooltip("Automatically begin playing music when the scene starts.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Master volume for background music.")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.7f;

    [Tooltip("Small scheduling lead time (seconds) to ensure sample-accurate intro-to-loop handoff.")]
    [Min(0.01f)]
    [SerializeField] private float scheduleLeadTime = 0.05f;

    [Tooltip("Fade-out duration (seconds) when CompleteLevel() is called. Set to 0 for an immediate stop.")]
    [Min(0f)]
    [SerializeField] private float levelCompleteFadeOutDuration = 1f;

    [Tooltip("Optional AudioMixerGroup routed to both music AudioSources.")]
    [SerializeField] private AudioMixerGroup outputMixerGroup;

    [Header("Audio Sources (Optional)")]
    [Tooltip("AudioSource used for the intro clip. Auto-created if left unassigned.")]
    [SerializeField] private AudioSource introSource;

    [Tooltip("AudioSource used for the looping clip. Auto-created if left unassigned.")]
    [SerializeField] private AudioSource loopSource;

    private Coroutine fadeCoroutine;
    private bool isPaused;
    private bool loopPendingResume;
    private double scheduledLoopStartDspTime;
    private double remainingIntroTimeOnPause;

    /// <summary>
    /// True while background music is actively playing or scheduled to transition.
    /// </summary>
    public bool IsPlaying =>
        !isPaused &&
        ((introSource != null && introSource.isPlaying) ||
         (loopSource != null && loopSource.isPlaying) ||
         loopPendingResume);

    /// <summary>
    /// True while music playback is paused.
    /// </summary>
    public bool IsPaused => isPaused;

    /// <summary>
    /// True after <see cref="CompleteLevel"/> has been called for the current playback session.
    /// </summary>
    public bool IsLevelCompleted { get; private set; }

    /// <summary>
    /// Current music volume in the range [0, 1].
    /// </summary>
    public float Volume
    {
        get => volume;
        set => SetVolume(value);
    }

    private void Awake()
    {
        EnsureAudioSources();
        ApplySourceSettings();
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayMusic();
        }
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);
        scheduleLeadTime = Mathf.Max(0.01f, scheduleLeadTime);
        levelCompleteFadeOutDuration = Mathf.Max(0f, levelCompleteFadeOutDuration);

        if (Application.isPlaying && introSource != null && loopSource != null)
        {
            ApplySourceSettings();
        }
    }

    /// <summary>
    /// Starts playing the configured intro clip followed seamlessly by the loop clip.
    /// </summary>
    public void PlayMusic()
    {
        EnsureAudioSources();
        StopFadeCoroutine();
        StopSourcesImmediately();

        IsLevelCompleted = false;
        isPaused = false;
        loopPendingResume = false;
        ApplySourceSettings();

        if (introClip == null && loopClip == null)
        {
            Debug.LogWarning($"{nameof(BackgroundMusicPlayer)} on '{name}' has no intro or loop AudioClip assigned.", this);
            return;
        }

        double startTime = AudioSettings.dspTime + scheduleLeadTime;

        if (introClip != null)
        {
            introSource.clip = introClip;
            introSource.loop = false;
            introSource.PlayScheduled(startTime);

            if (loopClip != null)
            {
                double introDuration = (double)introClip.samples / introClip.frequency;
                scheduledLoopStartDspTime = startTime + introDuration;

                loopSource.clip = loopClip;
                loopSource.loop = true;
                loopSource.PlayScheduled(scheduledLoopStartDspTime);
            }
        }
        else
        {
            scheduledLoopStartDspTime = startTime;
            loopSource.clip = loopClip;
            loopSource.loop = true;
            loopSource.PlayScheduled(startTime);
        }
    }

    /// <summary>
    /// Replaces the current clips and starts playing the new intro and loop clips.
    /// </summary>
    public void PlayMusic(AudioClip newIntroClip, AudioClip newLoopClip)
    {
        introClip = newIntroClip;
        loopClip = newLoopClip;
        PlayMusic();
    }

    /// <summary>
    /// Marks the level as completed and stops the background music using <see cref="levelCompleteFadeOutDuration"/>.
    /// </summary>
    public void CompleteLevel()
    {
        if (IsLevelCompleted)
        {
            return;
        }

        IsLevelCompleted = true;
        StopMusic(levelCompleteFadeOutDuration);
    }

    /// <summary>
    /// Stops background music immediately or over the specified fade-out duration in seconds.
    /// </summary>
    public void StopMusic(float fadeOutDuration = 0f)
    {
        StopFadeCoroutine();
        isPaused = false;
        loopPendingResume = false;

        if (fadeOutDuration <= 0f)
        {
            StopSourcesImmediately();
            ApplySourceSettings();
            return;
        }

        fadeCoroutine = StartCoroutine(FadeOutAndStopCoroutine(fadeOutDuration));
    }

    /// <summary>
    /// Pauses music playback while preserving the remaining intro-to-loop timing.
    /// </summary>
    public void Pause()
    {
        if (isPaused || !IsPlaying)
        {
            return;
        }

        isPaused = true;
        double now = AudioSettings.dspTime;

        if (introSource.isPlaying)
        {
            introSource.Pause();
        }

        if (loopClip != null && now < scheduledLoopStartDspTime)
        {
            remainingIntroTimeOnPause = scheduledLoopStartDspTime - now;
            loopSource.Stop();
            loopPendingResume = true;
        }
        else if (loopSource.isPlaying)
        {
            loopSource.Pause();
        }
    }

    /// <summary>
    /// Resumes paused music playback.
    /// </summary>
    public void Resume()
    {
        if (!isPaused)
        {
            return;
        }

        isPaused = false;

        if (introSource.clip != null)
        {
            introSource.UnPause();
        }

        if (loopPendingResume && loopClip != null)
        {
            scheduledLoopStartDspTime = AudioSettings.dspTime + remainingIntroTimeOnPause;
            loopSource.clip = loopClip;
            loopSource.loop = true;
            loopSource.PlayScheduled(scheduledLoopStartDspTime);
            loopPendingResume = false;
        }
        else if (loopSource.clip != null)
        {
            loopSource.UnPause();
        }
    }

    /// <summary>
    /// Updates the background music volume in the range [0, 1].
    /// </summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        if (fadeCoroutine == null && introSource != null && loopSource != null)
        {
            introSource.volume = volume;
            loopSource.volume = volume;
        }
    }

    private void EnsureAudioSources()
    {
        if (introSource == null)
        {
            introSource = gameObject.AddComponent<AudioSource>();
        }

        if (loopSource == null || loopSource == introSource)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ApplySourceSettings()
    {
        ConfigureSource(introSource, loop: false);
        ConfigureSource(loopSource, loop: true);
    }

    private void ConfigureSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = loop;
        source.volume = volume;
        source.outputAudioMixerGroup = outputMixerGroup;
    }

    private void StopFadeCoroutine()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    private void StopSourcesImmediately()
    {
        if (introSource != null)
        {
            introSource.Stop();
        }

        if (loopSource != null)
        {
            loopSource.Stop();
        }
    }

    private IEnumerator FadeOutAndStopCoroutine(float duration)
    {
        float startIntroVolume = introSource != null ? introSource.volume : volume;
        float startLoopVolume = loopSource != null ? loopSource.volume : volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (introSource != null)
            {
                introSource.volume = Mathf.Lerp(startIntroVolume, 0f, t);
            }

            if (loopSource != null)
            {
                loopSource.volume = Mathf.Lerp(startLoopVolume, 0f, t);
            }

            yield return null;
        }

        StopSourcesImmediately();
        ApplySourceSettings();
        fadeCoroutine = null;
    }
}
