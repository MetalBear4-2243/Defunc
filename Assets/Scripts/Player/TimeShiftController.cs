using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Represents the active timeline in Approach 1 (Spatially Overlapping Hierarchies).
/// </summary>
public enum TimelineEra
{
    Past,
    Future
}

/// <summary>
/// Controls instantaneous shifting between Past and Future level hierarchies
/// occupying the same world coordinates (Approach 1).
/// Uses a safety nudge fallback when target timeline geometry overlaps the player,
/// and denies the shift if no clear position exists within the nudge radius.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class TimeShiftController : MonoBehaviour
{
    private const float UpwardScoreBias = 0.05f;
    private const float MinCheckExtent = 0.05f;
    private const float MinStepSize = 0.02f;

    private static readonly Vector2[] NudgeDirections =
    {
        Vector2.up,
        Vector2.left,
        Vector2.right,
        new Vector2(-0.70710678f, 0.70710678f),
        new Vector2(0.70710678f, 0.70710678f),
        Vector2.down,
        new Vector2(-0.70710678f, -0.70710678f),
        new Vector2(0.70710678f, -0.70710678f)
    };

    [Header("Timeline Hierarchy (Approach 1)")]
    [Tooltip("Timeline active when the scene starts.")]
    [SerializeField] private TimelineEra startingEra = TimelineEra.Future;

    [Tooltip("Parent GameObject containing Past timeline geometry. Auto-finds 'World_Past' if empty.")]
    [SerializeField] private GameObject pastWorldRoot;

    [Tooltip("Parent GameObject containing Future timeline geometry. Auto-finds 'World_Future' if empty.")]
    [SerializeField] private GameObject futureWorldRoot;

    [Header("Shift Controls & Cooldown")]
    [Tooltip("Whether the player is currently allowed to shift timelines.")]
    [SerializeField] private bool canShift = true;

    [Tooltip("Minimum time (seconds) between successful timeline shifts.")]
    [SerializeField] private float shiftCooldown = 0.15f;

    [Header("Collision & Safety Nudge")]
    [Tooltip("Layers checked for obstructions in the target timeline.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Tooltip("Maximum distance (world units) the safety nudge searches to resolve an obstruction.")]
    [SerializeField] private float maxNudgeDistance = 1.5f;

    [Tooltip("Radial distance increment per search ring when finding a safe nudge position.")]
    [SerializeField] private float nudgeStepSize = 0.1f;

    [Tooltip("Inset applied to the player's collider size during overlap checks so flush floor/wall contact does not block shifting.")]
    [SerializeField] private float skinMargin = 0.04f;

    [Header("Visual Feedback")]
    [Tooltip("Camera whose background color reflects the active era. Auto-finds Camera.main if empty.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("If enabled, updates the camera background color on shift to distinguish Past and Future.")]
    [SerializeField] private bool tintCameraBackground = true;

    [SerializeField] private Color pastBackgroundColor = new Color(0.11f, 0.13f, 0.17f, 0f);
    [SerializeField] private Color futureBackgroundColor = new Color(0.07f, 0.17f, 0.22f, 0f);
    [SerializeField] private Color shiftFlashColor = new Color(0.6f, 0.95f, 1f, 1f);
    [SerializeField] private Color nudgeFlashColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color deniedFlashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Duration (seconds) of the sprite tint flash when shifting or being denied.")]
    [SerializeField] private float feedbackFlashDuration = 0.12f;

    [Tooltip("Optional reference to the pulse visual effect. Auto-retrieves from this GameObject or children if empty.")]
    [SerializeField] private TimeShiftPulseEffect pulseEffect;

    [Header("Level Geometry Timeline Tint")]
    [Tooltip("If enabled, automatically tints platform SpriteRenderers under LevelGeometry to match the active era.")]
    [SerializeField] private bool tintLevelGeometry = true;

    [Tooltip("Parent GameObject containing shared level geometry. Auto-finds 'LevelGeometry' if empty.")]
    [SerializeField] private GameObject levelGeometryRoot;

    [SerializeField] private Color pastPlatformColor = new Color(0.35f, 0.40f, 0.48f, 1f);
    [SerializeField] private Color futurePlatformColor = new Color(0.65f, 0.25f, 0.14f, 1f);

    // Cached components
    private Rigidbody2D rb;
    private Collider2D col;
    private CapsuleCollider2D capsuleCol;
    private SpriteRenderer spriteRenderer;
    private Color baseSpriteColor = Color.white;
    private readonly System.Collections.Generic.List<SpriteRenderer> levelGeometryRenderers = new System.Collections.Generic.List<SpriteRenderer>();

    // State
    private TimelineEra currentEra;
    private float lastShiftTime = float.NegativeInfinity;
    private float feedbackFlashTimer;

    // Pre-allocated physics query buffers
    private readonly Collider2D[] overlapBuffer = new Collider2D[8];
    private readonly Collider2D[] initialHitsBuffer = new Collider2D[8];
    private ContactFilter2D obstacleFilter;

    // Gizmo debugging for the most recent safety nudge
    private bool hasLastNudge;
    private Vector2 lastNudgeOrigin;
    private Vector2 lastNudgeDestination;

    /// <summary>
    /// Currently active timeline era.
    /// </summary>
    public TimelineEra CurrentEra => currentEra;

    /// <summary>
    /// Reference to the TimeShiftPulseEffect component, if attached.
    /// </summary>
    public TimeShiftPulseEffect PulseEffect => pulseEffect;

    /// <summary>
    /// Invoked after a successful timeline shift. Parameters: (newEra, wasSafetyNudged).
    /// </summary>
    public event Action<TimelineEra, bool> OnTimelineShifted;

    /// <summary>
    /// Invoked when a shift is denied because no clear spot exists within maxNudgeDistance.
    /// </summary>
    public event Action OnShiftDenied;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        capsuleCol = col as CapsuleCollider2D;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            baseSpriteColor = spriteRenderer.color;
        }

        if (pulseEffect == null)
        {
            pulseEffect = GetComponentInChildren<TimeShiftPulseEffect>();
        }

        if (pastWorldRoot == null)
        {
            pastWorldRoot = GameObject.Find("World_Past");
        }

        if (futureWorldRoot == null)
        {
            futureWorldRoot = GameObject.Find("World_Future");
        }

        if (levelGeometryRoot == null)
        {
            levelGeometryRoot = GameObject.Find("LevelGeometry");
        }

        CacheLevelGeometryRenderers();

        if (pastWorldRoot == null && futureWorldRoot == null)
        {
            Debug.LogWarning("[TimeShiftController] Neither pastWorldRoot nor futureWorldRoot is assigned or found in the scene.", this);
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (obstacleLayer.value == 0)
        {
            int groundMask = LayerMask.GetMask("Ground");
            obstacleLayer = groundMask != 0 ? groundMask : LayerMask.GetMask("Default");
        }

        obstacleFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = obstacleLayer
        };

        currentEra = startingEra;
        ApplyEraWorldState(currentEra);
        UpdateCameraBackground(currentEra);
    }

    private void Update()
    {
        UpdateFeedbackFlash();

        if (canShift && GatherShiftInput() && Time.time >= lastShiftTime + shiftCooldown)
        {
            TryShiftTimeline();
        }
    }

    /// <summary>
    /// Gets or sets whether the player is currently allowed to shift timelines.
    /// </summary>
    public bool CanShift
    {
        get => canShift;
        set => canShift = value;
    }

    /// <summary>
    /// Allows the player to shift timelines.
    /// </summary>
    public void UnlockShift()
    {
        canShift = true;
    }

    /// <summary>
    /// Prevents the player from shifting timelines.
    /// </summary>
    public void LockShift()
    {
        canShift = false;
    }

    /// <summary>
    /// Attempts to swap to the opposite timeline era, applying a safety nudge if obstructed
    /// or denying the shift if no clear location exists within maxNudgeDistance.
    /// </summary>
    /// <returns>True if the shift succeeded; false if denied.</returns>
    public bool TryShiftTimeline()
    {
        if (!canShift)
        {
            return false;
        }

        TimelineEra targetEra = currentEra == TimelineEra.Past ? TimelineEra.Future : TimelineEra.Past;

        // Temporarily switch active hierarchies so Physics2D evaluates the target timeline's colliders.
        ApplyEraWorldState(targetEra);
        Physics2D.SyncTransforms();

        Vector2 origin = transform.position;
        int originHitCount = GetOverlapCount(origin);

        if (!HasObstruction(originHitCount))
        {
            CommitShift(targetEra, false);
            return true;
        }

        if (TryFindSafeNudgePosition(origin, originHitCount, out Vector2 safePosition))
        {
            ApplySafetyNudge(origin, safePosition);
            CommitShift(targetEra, true);
            return true;
        }

        // No safe position within maxNudgeDistance: revert in the same frame so there is no flicker.
        ApplyEraWorldState(currentEra);
        Physics2D.SyncTransforms();
        TriggerFeedbackFlash(deniedFlashColor);
        OnShiftDenied?.Invoke();
        return false;
    }

    /// <summary>
    /// Immediately switches to the specified timeline era without obstruction checks
    /// (e.g., when respawning at a checkpoint).
    /// </summary>
    public void SetEraImmediate(TimelineEra era)
    {
        if (currentEra == era)
        {
            return;
        }

        currentEra = era;
        ApplyEraWorldState(currentEra);
        Physics2D.SyncTransforms();
        UpdateCameraBackground(currentEra);
        OnTimelineShifted?.Invoke(currentEra, false);
    }

    private bool GatherShiftInput()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            return keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private void ApplyEraWorldState(TimelineEra era)
    {
        if (pastWorldRoot != null)
        {
            pastWorldRoot.SetActive(era == TimelineEra.Past);
        }

        if (futureWorldRoot != null)
        {
            futureWorldRoot.SetActive(era == TimelineEra.Future);
        }

        UpdateLevelGeometryColors(era);
    }

    private void CacheLevelGeometryRenderers()
    {
        levelGeometryRenderers.Clear();
        if (levelGeometryRoot == null)
        {
            return;
        }

        SpriteRenderer[] renderers = levelGeometryRoot.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            if (sr == null || sr.GetComponent<Checkpoint>() != null || sr.GetComponent<KillZone>() != null)
            {
                continue;
            }

            levelGeometryRenderers.Add(sr);
        }
    }

    private void UpdateLevelGeometryColors(TimelineEra era)
    {
        if (!tintLevelGeometry || levelGeometryRenderers.Count == 0)
        {
            return;
        }

        Color targetColor = era == TimelineEra.Past ? pastPlatformColor : futurePlatformColor;
        for (int i = 0; i < levelGeometryRenderers.Count; i++)
        {
            SpriteRenderer sr = levelGeometryRenderers[i];
            if (sr != null)
            {
                sr.color = new Color(targetColor.r, targetColor.g, targetColor.b, sr.color.a);
            }
        }
    }

    private void CommitShift(TimelineEra newEra, bool wasNudged)
    {
        currentEra = newEra;
        lastShiftTime = Time.time;
        UpdateCameraBackground(currentEra);
        TriggerFeedbackFlash(wasNudged ? nudgeFlashColor : shiftFlashColor);
        OnTimelineShifted?.Invoke(currentEra, wasNudged);
    }

    private void ApplySafetyNudge(Vector2 origin, Vector2 safePosition)
    {
        transform.position = new Vector3(safePosition.x, safePosition.y, transform.position.z);
        rb.position = safePosition;
        Physics2D.SyncTransforms();

        // Zero downward velocity when nudged upward onto a surface so the player lands cleanly.
        if (safePosition.y > origin.y + 0.01f)
        {
#if UNITY_6000_0_OR_NEWER
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
#else
            if (rb.velocity.y < 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }
#endif
        }

        hasLastNudge = true;
        lastNudgeOrigin = origin;
        lastNudgeDestination = safePosition;
    }

    private bool TryFindSafeNudgePosition(Vector2 origin, int originHitCount, out Vector2 safePosition)
    {
        safePosition = origin;
        if (maxNudgeDistance <= 0f)
        {
            return false;
        }

        // Snapshot overlapping colliders before IsPositionClear reuses overlapBuffer.
        int snapshotCount = Mathf.Min(originHitCount, initialHitsBuffer.Length);
        for (int i = 0; i < snapshotCount; i++)
        {
            initialHitsBuffer[i] = overlapBuffer[i];
        }

        bool foundBest = false;
        Vector2 bestCandidate = origin;
        float bestScore = float.MaxValue;

        // 1. Check direct Physics2D.Distance depenetration vectors against initial obstructions.
        for (int i = 0; i < snapshotCount; i++)
        {
            Collider2D hit = initialHitsBuffer[i];
            if (hit == null || hit == col)
            {
                continue;
            }

            ColliderDistance2D dist = Physics2D.Distance(col, hit);
            if (!dist.isOverlapped)
            {
                continue;
            }

            Vector2 pointDelta = dist.pointB - dist.pointA;
            Vector2 axis;
            if (pointDelta.sqrMagnitude > 1e-6f)
            {
                axis = pointDelta.normalized;
            }
            else if (dist.normal.sqrMagnitude > 1e-6f)
            {
                axis = dist.normal.normalized;
            }
            else
            {
                continue;
            }

            float pushDistance = Mathf.Abs(dist.distance) + Mathf.Max(0f, skinMargin);
            if (pushDistance <= maxNudgeDistance)
            {
                EvaluateCandidate(origin, origin + axis * pushDistance, ref foundBest, ref bestCandidate, ref bestScore);
                EvaluateCandidate(origin, origin - axis * pushDistance, ref foundBest, ref bestCandidate, ref bestScore);
            }
        }

        // 2. Fallback directional ring search outward up to maxNudgeDistance.
        float stepSize = Mathf.Max(MinStepSize, nudgeStepSize);
        int stepCount = Mathf.Max(1, Mathf.CeilToInt(maxNudgeDistance / stepSize));

        for (int step = 1; step <= stepCount; step++)
        {
            float distance = Mathf.Min(maxNudgeDistance, step * stepSize);

            // Stop early once even a maximally upward-biased ring cannot improve on bestScore.
            if (foundBest && distance - UpwardScoreBias > bestScore)
            {
                break;
            }

            for (int d = 0; d < NudgeDirections.Length; d++)
            {
                Vector2 candidate = origin + NudgeDirections[d] * distance;
                EvaluateCandidate(origin, candidate, ref foundBest, ref bestCandidate, ref bestScore);
            }
        }

        if (foundBest)
        {
            safePosition = bestCandidate;
            return true;
        }

        return false;
    }

    private void EvaluateCandidate(
        Vector2 origin,
        Vector2 candidate,
        ref bool foundBest,
        ref Vector2 bestCandidate,
        ref float bestScore)
    {
        float actualDistance = Vector2.Distance(origin, candidate);
        if (actualDistance > maxNudgeDistance)
        {
            return;
        }

        float score = actualDistance - (candidate.y >= origin.y ? UpwardScoreBias : 0f);
        if (foundBest && score >= bestScore)
        {
            return;
        }

        if (IsPositionClear(candidate))
        {
            foundBest = true;
            bestCandidate = candidate;
            bestScore = score;
        }
    }

    private bool IsPositionClear(Vector2 candidatePos)
    {
        int hitCount = GetOverlapCount(candidatePos);
        return !HasObstruction(hitCount);
    }

    private int GetOverlapCount(Vector2 candidatePos)
    {
        GetCheckGeometry(col, candidatePos, out Vector2 queryCenter, out Vector2 checkSize);

        if (capsuleCol != null)
        {
            return Physics2D.OverlapCapsule(queryCenter, checkSize, capsuleCol.direction, 0f, obstacleFilter, overlapBuffer);
        }

        return Physics2D.OverlapBox(queryCenter, checkSize, 0f, obstacleFilter, overlapBuffer);
    }

    private bool HasObstruction(int hitCount)
    {
        int clampedCount = Mathf.Min(hitCount, overlapBuffer.Length);
        for (int i = 0; i < clampedCount; i++)
        {
            if (overlapBuffer[i] != null && overlapBuffer[i] != col)
            {
                return true;
            }
        }

        return false;
    }

    private void GetCheckGeometry(Collider2D targetCol, Vector2 candidatePos, out Vector2 queryCenter, out Vector2 checkSize)
    {
        if (targetCol != null)
        {
            Bounds bounds = targetCol.bounds;
            Vector2 centerOffset = (Vector2)bounds.center - (Vector2)transform.position;
            queryCenter = candidatePos + centerOffset;
            checkSize = new Vector2(
                Mathf.Max(MinCheckExtent, bounds.size.x - skinMargin),
                Mathf.Max(MinCheckExtent, bounds.size.y - skinMargin)
            );
            return;
        }

        queryCenter = candidatePos;
        checkSize = new Vector2(1f - skinMargin, 2f - skinMargin);
    }

    private void UpdateCameraBackground(TimelineEra era)
    {
        if (!tintCameraBackground || targetCamera == null)
        {
            return;
        }

        targetCamera.backgroundColor = era == TimelineEra.Past ? pastBackgroundColor : futureBackgroundColor;
    }

    private void TriggerFeedbackFlash(Color flashColor)
    {
        if (spriteRenderer == null || feedbackFlashDuration <= 0f)
        {
            return;
        }

        spriteRenderer.color = flashColor;
        feedbackFlashTimer = feedbackFlashDuration;
    }

    private void UpdateFeedbackFlash()
    {
        if (feedbackFlashTimer <= 0f)
        {
            return;
        }

        feedbackFlashTimer -= Time.deltaTime;
        if (feedbackFlashTimer <= 0f && spriteRenderer != null)
        {
            spriteRenderer.color = baseSpriteColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D activeCol = col != null ? col : GetComponent<Collider2D>();
        Vector2 origin = transform.position;
        GetCheckGeometry(activeCol, origin, out Vector2 queryCenter, out Vector2 checkSize);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(queryCenter, checkSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, maxNudgeDistance);

        if (hasLastNudge)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(lastNudgeOrigin, lastNudgeDestination);
            Gizmos.DrawWireSphere(lastNudgeDestination, 0.12f);
        }
    }
}
