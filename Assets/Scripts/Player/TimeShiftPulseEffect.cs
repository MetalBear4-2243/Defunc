using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns an expanding pulse shockwave effect originating from the center of the player
/// whenever the player shifts timelines. Supports multi-ring ripples, era-specific colors,
/// safety-nudge color variations, and high-performance zero-allocation ring pooling.
/// </summary>
[DisallowMultipleComponent]
public class TimeShiftPulseEffect : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Reference to the player's TimeShiftController. Auto-retrieved from this GameObject if unassigned.")]
    [SerializeField] private TimeShiftController timeShiftController;

    [Tooltip("Collider used to calculate the exact center of the player. Auto-retrieved if unassigned.")]
    [SerializeField] private Collider2D playerCollider;

    [Tooltip("Manual positional offset from the player's collider center.")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;

    [Tooltip("If enabled, active pulse rings follow the player's movement. If disabled, rings expand in place from the shift origin.")]
    [SerializeField] private bool followPlayer = false;

    [Header("Pulse Timing & Dimensions")]
    [Tooltip("Total duration (seconds) of the pulse expansion and fadeout.")]
    [SerializeField] private float pulseDuration = 0.4f;

    [Tooltip("Starting radius (units) of the pulse ring at creation.")]
    [SerializeField] private float startRadius = 0.1f;

    [Tooltip("Peak radius (units) reached by the primary pulse ring before fading out.")]
    [SerializeField] private float targetRadius = 3.2f;

    [Tooltip("Curve governing radius expansion over normalized time (0 to 1).")]
    [SerializeField] private AnimationCurve expansionCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 2.5f, 2.5f),
        new Keyframe(0.45f, 0.75f, 0.8f, 0.8f),
        new Keyframe(1f, 1f, 0.1f, 0.1f)
    );

    [Tooltip("Initial thickness (world units) of the pulse ring.")]
    [SerializeField] private float startWidth = 0.22f;

    [Tooltip("Final thickness (world units) of the pulse ring at max expansion.")]
    [SerializeField] private float endWidth = 0.02f;

    [Tooltip("Multiplier curve for ring width over normalized duration.")]
    [SerializeField] private AnimationCurve widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.25f);

    [Header("Concentric Ripple Rings")]
    [Tooltip("Number of concentric rings emitted per timeline shift.")]
    [Range(1, 4)]
    [SerializeField] private int ringCount = 2;

    [Tooltip("Delay (seconds) between successive concentric rings.")]
    [SerializeField] private float ringDelay = 0.06f;

    [Tooltip("Radius scale multiplier applied to secondary echo rings.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float secondaryRingScale = 0.85f;

    [Header("Colors & Styling")]
    [Tooltip("If true, selects ring color based on the target TimelineEra.")]
    [SerializeField] private bool useEraColors = true;

    [Tooltip("Pulse color when shifting to the Past era (electric cyan).")]
    [SerializeField] private Color pastPulseColor = new Color(0.35f, 0.9f, 1f, 1f);

    [Tooltip("Pulse color when shifting to the Future era (radiant amber).")]
    [SerializeField] private Color futurePulseColor = new Color(1f, 0.65f, 0.2f, 1f);

    [Tooltip("If true, uses a distinct accent color when a safety nudge resolves an overlap.")]
    [SerializeField] private bool useNudgeColor = true;

    [Tooltip("Pulse color when the timeline shift required a safety nudge.")]
    [SerializeField] private Color nudgePulseColor = new Color(1f, 0.88f, 0.35f, 1f);

    [Tooltip("Alpha fade curve over normalized duration.")]
    [SerializeField] private AnimationCurve alphaFadeCurve = new AnimationCurve(
        new Keyframe(0f, 1f, 0f, 0f),
        new Keyframe(0.6f, 0.85f, -0.8f, -0.8f),
        new Keyframe(1f, 0f, -2.5f, -2.5f)
    );

    [Header("Rendering Configuration")]
    [Tooltip("Number of line segments making up each circular pulse ring.")]
    [Range(16, 128)]
    [SerializeField] private int ringSegments = 64;

    [Tooltip("Material used for the pulse ring LineRenderers. If unassigned, automatically creates an unlit 2D material.")]
    [SerializeField] private Material pulseMaterial;

    [Tooltip("Sorting layer for the pulse LineRenderers.")]
    [SerializeField] private string sortingLayerName = "Default";

    [Tooltip("Sorting order within the sorting layer (renders above player by default).")]
    [SerializeField] private int sortingOrder = 5;

    [Tooltip("Z-axis world offset for the pulse rings.")]
    [SerializeField] private float zOffset = 0f;

    [Header("Audio (Optional)")]
    [Tooltip("Optional audio clip played whenever a pulse is emitted.")]
    [SerializeField] private AudioClip pulseSound;

    [Tooltip("AudioSource used to play the pulseSound.")]
    [SerializeField] private AudioSource audioSource;

    // Ring pool and runtime animation
    private class PulseRing
    {
        public GameObject GameObject;
        public LineRenderer Line;
        public Vector3[] PointsBuffer;
        public bool IsActive;
        public float Delay;
        public float ElapsedTime;
        public float Duration;
        public float StartRadius;
        public float TargetRadius;
        public Color BaseColor;
        public Vector2 EmitCenter;
        public bool Follow;
    }

    private readonly List<PulseRing> ringPool = new List<PulseRing>();
    private Transform poolContainer;
    private static Material sharedDefaultMaterial;
    private Vector2[] cachedUnitCircle;
    private int cachedSegments;

    // Public Properties for runtime configuration / scripting
    public Vector2 CenterOffset { get => centerOffset; set => centerOffset = value; }
    public bool FollowPlayer { get => followPlayer; set => followPlayer = value; }
    public float PulseDuration { get => pulseDuration; set => pulseDuration = Mathf.Max(0.01f, value); }
    public float StartRadius { get => startRadius; set => startRadius = Mathf.Max(0f, value); }
    public float TargetRadius { get => targetRadius; set => targetRadius = Mathf.Max(0.01f, value); }
    public int RingCount { get => ringCount; set => ringCount = Mathf.Clamp(value, 1, 4); }
    public Color PastPulseColor { get => pastPulseColor; set => pastPulseColor = value; }
    public Color FuturePulseColor { get => futurePulseColor; set => futurePulseColor = value; }
    public Color NudgePulseColor { get => nudgePulseColor; set => nudgePulseColor = value; }

    /// <summary>
    /// Current center position of the player in world coordinates.
    /// </summary>
    public Vector2 PulseCenter
    {
        get
        {
            if (playerCollider != null)
            {
                return (Vector2)playerCollider.bounds.center + centerOffset;
            }
            return (Vector2)transform.position + centerOffset;
        }
    }

    private void Awake()
    {
        if (timeShiftController == null)
        {
            timeShiftController = GetComponent<TimeShiftController>();
        }

        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider2D>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        RebuildUnitCircleCache();
        InitializePool(Mathf.Max(6, ringCount * 3));
    }

    private void OnEnable()
    {
        if (timeShiftController != null)
        {
            timeShiftController.OnTimelineShifted += HandleTimelineShifted;
        }
    }

    private void OnDisable()
    {
        if (timeShiftController != null)
        {
            timeShiftController.OnTimelineShifted -= HandleTimelineShifted;
        }

        DeactivateAllRings();
    }

    private void OnDestroy()
    {
        if (poolContainer != null)
        {
            Destroy(poolContainer.gameObject);
        }
    }

    private void Update()
    {
        UpdateActiveRings(Time.deltaTime);
    }

    /// <summary>
    /// Listener invoked by TimeShiftController.OnTimelineShifted.
    /// </summary>
    private void HandleTimelineShifted(TimelineEra newEra, bool wasSafetyNudged)
    {
        EmitPulse(newEra, wasSafetyNudged);
    }

    /// <summary>
    /// Emits a pulse effect originating from the player's center for the given timeline era and nudge state.
    /// </summary>
    public void EmitPulse(TimelineEra newEra, bool wasSafetyNudged = false)
    {
        Color ringColor;
        if (wasSafetyNudged && useNudgeColor)
        {
            ringColor = nudgePulseColor;
        }
        else if (useEraColors)
        {
            ringColor = newEra == TimelineEra.Past ? pastPulseColor : futurePulseColor;
        }
        else
        {
            ringColor = pastPulseColor;
        }

        EmitPulseWithColor(ringColor);
    }

    /// <summary>
    /// Emits a pulse effect using the default past era color (or current era if controller is assigned).
    /// </summary>
    [ContextMenu("Emit Test Pulse")]
    public void EmitPulse()
    {
        TimelineEra activeEra = timeShiftController != null ? timeShiftController.CurrentEra : TimelineEra.Past;
        EmitPulse(activeEra, false);
    }

    /// <summary>
    /// Emits one or more concentric pulse rings from the center of the player using the specified base color.
    /// </summary>
    public void EmitPulseWithColor(Color baseColor)
    {
        Vector2 center = PulseCenter;

        if (audioSource != null && pulseSound != null)
        {
            audioSource.PlayOneShot(pulseSound);
        }

        int count = Mathf.Max(1, ringCount);
        for (int i = 0; i < count; i++)
        {
            PulseRing ring = GetAvailableRing();
            float delay = i * ringDelay;
            float scale = i == 0 ? 1f : Mathf.Pow(secondaryRingScale, i);

            ring.IsActive = true;
            ring.Delay = delay;
            ring.ElapsedTime = 0f;
            ring.Duration = pulseDuration;
            ring.StartRadius = startRadius * scale;
            ring.TargetRadius = targetRadius * scale;
            ring.BaseColor = baseColor;
            ring.EmitCenter = center;
            ring.Follow = followPlayer;

            // Hide line renderer until initial delay finishes
            ring.Line.enabled = (delay <= 0f);
            if (delay <= 0f)
            {
                RenderRingGeometry(ring, ring.StartRadius, center, 0f);
            }
        }
    }

    private void UpdateActiveRings(float deltaTime)
    {
        Vector2 currentCenter = PulseCenter;

        for (int i = 0; i < ringPool.Count; i++)
        {
            PulseRing ring = ringPool[i];
            if (!ring.IsActive)
            {
                continue;
            }

            if (ring.Delay > 0f)
            {
                ring.Delay -= deltaTime;
                if (ring.Delay > 0f)
                {
                    continue;
                }

                ring.Line.enabled = true;
            }

            ring.ElapsedTime += deltaTime;
            float normalizedTime = Mathf.Clamp01(ring.ElapsedTime / ring.Duration);

            Vector2 center = ring.Follow ? currentCenter : ring.EmitCenter;
            float progress = expansionCurve.Evaluate(normalizedTime);
            float radius = Mathf.Lerp(ring.StartRadius, ring.TargetRadius, progress);

            RenderRingGeometry(ring, radius, center, normalizedTime);

            if (normalizedTime >= 1f)
            {
                ring.IsActive = false;
                ring.Line.enabled = false;
            }
        }
    }

    private void RenderRingGeometry(PulseRing ring, float radius, Vector2 center, float normalizedTime)
    {
        // Width calculation
        float widthFactor = widthCurve.Evaluate(normalizedTime);
        float width = Mathf.Lerp(startWidth, endWidth, normalizedTime) * widthFactor;
        ring.Line.startWidth = width;
        ring.Line.endWidth = width;

        // Color and alpha calculation
        float alpha = alphaFadeCurve.Evaluate(normalizedTime);
        Color color = ring.BaseColor;
        color.a *= alpha;
        ring.Line.startColor = color;
        ring.Line.endColor = color;

        // Vector positions calculation (zero GC allocation)
        for (int p = 0; p < cachedSegments; p++)
        {
            ring.PointsBuffer[p].x = center.x + cachedUnitCircle[p].x * radius;
            ring.PointsBuffer[p].y = center.y + cachedUnitCircle[p].y * radius;
            ring.PointsBuffer[p].z = zOffset;
        }

        ring.Line.SetPositions(ring.PointsBuffer);
    }

    private PulseRing GetAvailableRing()
    {
        for (int i = 0; i < ringPool.Count; i++)
        {
            if (!ringPool[i].IsActive)
            {
                return ringPool[i];
            }
        }

        // Expand pool dynamically if all rings are active
        PulseRing newRing = CreateNewRingInstance();
        ringPool.Add(newRing);
        return newRing;
    }

    private void InitializePool(int initialCapacity)
    {
        if (poolContainer == null)
        {
            GameObject container = new GameObject($"_PulsePool_{gameObject.name}");
            poolContainer = container.transform;
            poolContainer.SetParent(transform, false);
        }

        for (int i = 0; i < initialCapacity; i++)
        {
            ringPool.Add(CreateNewRingInstance());
        }
    }

    private PulseRing CreateNewRingInstance()
    {
        GameObject ringGo = new GameObject("PulseRing");
        ringGo.transform.SetParent(poolContainer, false);

        LineRenderer line = ringGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = cachedSegments;
        line.sortingLayerName = sortingLayerName;
        line.sortingOrder = sortingOrder;
        line.sharedMaterial = GetOrCreateMaterial();
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.enabled = false;

        PulseRing ring = new PulseRing
        {
            GameObject = ringGo,
            Line = line,
            PointsBuffer = new Vector3[cachedSegments],
            IsActive = false
        };

        return ring;
    }

    private Material GetOrCreateMaterial()
    {
        if (pulseMaterial != null)
        {
            return pulseMaterial;
        }

        if (sharedDefaultMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                sharedDefaultMaterial = new Material(shader)
                {
                    name = "TimeShiftPulse_DefaultUnlitMaterial",
                    hideFlags = HideFlags.DontSave
                };
            }
        }

        return sharedDefaultMaterial;
    }

    private void RebuildUnitCircleCache()
    {
        cachedSegments = Mathf.Clamp(ringSegments, 16, 128);
        cachedUnitCircle = new Vector2[cachedSegments];

        float angleStep = (Mathf.PI * 2f) / cachedSegments;
        for (int i = 0; i < cachedSegments; i++)
        {
            float angle = i * angleStep;
            cachedUnitCircle[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    private void DeactivateAllRings()
    {
        for (int i = 0; i < ringPool.Count; i++)
        {
            if (ringPool[i] != null)
            {
                ringPool[i].IsActive = false;
                if (ringPool[i].Line != null)
                {
                    ringPool[i].Line.enabled = false;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? PulseCenter : (playerCollider != null ? (Vector2)playerCollider.bounds.center + centerOffset : (Vector2)transform.position + centerOffset);

        Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.6f);
        Gizmos.DrawWireSphere(center, targetRadius);

        Gizmos.color = new Color(0.35f, 0.9f, 1f, 0.9f);
        Gizmos.DrawWireSphere(center, startRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.1f);
    }
}
