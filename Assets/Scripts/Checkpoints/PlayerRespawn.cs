using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the player's active checkpoint, death handling, and respawning.
/// Only checkpoints with an ID strictly greater than <see cref="CurrentCheckpointId"/>
/// can become active, preventing earlier checkpoints from overwriting progress.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("If enabled, restores the timeline era that was active when the checkpoint was reached.")]
    [SerializeField] private bool restoreEraOnRespawn = true;

    [Tooltip("If enabled, unlocks timeline shifting when the player activates their first checkpoint.")]
    [SerializeField] private bool unlockShiftOnFirstCheckpoint = true;

    [Header("Fall Death (Optional)")]
    [Tooltip("Automatically triggers Die() if the player's Y position drops below fallDeathY.")]
    [SerializeField] private bool enableFallDeath = true;

    [Tooltip("World Y coordinate below which the player dies and respawns.")]
    [SerializeField] private float fallDeathY = -20f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerDied;
    [SerializeField] private UnityEvent onPlayerRespawned;

    private Rigidbody2D rb;
    private TimeShiftController timeShiftController;

    private Checkpoint currentCheckpoint;
    private int currentCheckpointId = int.MinValue;
    private Vector3 currentSpawnPosition;
    private TimelineEra currentSpawnEra = TimelineEra.Future;

    /// <summary>
    /// Fired immediately when <see cref="Die"/> is called, before respawning.
    /// </summary>
    public event Action OnPlayerDied;

    /// <summary>
    /// Fired after the player has been reset to the active checkpoint.
    /// </summary>
    public event Action OnPlayerRespawned;

    /// <summary>
    /// ID of the highest-numbered checkpoint activated so far (<see cref="int.MinValue"/> if none).
    /// </summary>
    public int CurrentCheckpointId => currentCheckpointId;

    /// <summary>
    /// Currently active checkpoint, or null if no checkpoint has been reached yet.
    /// </summary>
    public Checkpoint CurrentCheckpoint => currentCheckpoint;

    /// <summary>
    /// World position the player will respawn at on death.
    /// </summary>
    public Vector3 CurrentSpawnPosition => currentSpawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        timeShiftController = GetComponent<TimeShiftController>();
        currentSpawnPosition = transform.position;
    }

    private void Start()
    {
        if (timeShiftController != null)
        {
            currentSpawnEra = timeShiftController.CurrentEra;

            if (unlockShiftOnFirstCheckpoint && currentCheckpoint == null)
            {
                timeShiftController.LockShift();
            }
        }
    }

    private void Update()
    {
        if (enableFallDeath && transform.position.y < fallDeathY)
        {
            Die();
        }
    }

    /// <summary>
    /// Attempts to activate <paramref name="checkpoint"/>.
    /// Succeeds only if its ID is strictly greater than the currently active checkpoint ID.
    /// </summary>
    /// <returns>True if the checkpoint became active; false if ignored.</returns>
    public bool TryActivateCheckpoint(Checkpoint checkpoint)
    {
        if (checkpoint == null || checkpoint.CheckpointId <= currentCheckpointId)
        {
            return false;
        }

        if (currentCheckpoint != null)
        {
            currentCheckpoint.SetVisualActive(false);
        }

        currentCheckpoint = checkpoint;
        currentCheckpointId = checkpoint.CheckpointId;
        currentSpawnPosition = checkpoint.SpawnPosition;

        if (timeShiftController != null)
        {
            currentSpawnEra = timeShiftController.CurrentEra;

            if (unlockShiftOnFirstCheckpoint && !timeShiftController.CanShift)
            {
                timeShiftController.UnlockShift();
            }
        }

        checkpoint.NotifyActivated();
        return true;
    }

    /// <summary>
    /// Kills the player and immediately resets them to the active checkpoint (or level start).
    /// </summary>
    public void Die()
    {
        OnPlayerDied?.Invoke();
        onPlayerDied?.Invoke();
        Respawn();
    }

    /// <summary>
    /// Resets the player's position, velocity, and (optionally) timeline era to the active checkpoint.
    /// </summary>
    public void Respawn()
    {
        transform.position = currentSpawnPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        Physics2D.SyncTransforms();

        if (restoreEraOnRespawn && timeShiftController != null)
        {
            timeShiftController.SetEraImmediate(currentSpawnEra);
        }

        OnPlayerRespawned?.Invoke();
        onPlayerRespawned?.Invoke();
    }

    /// <summary>
    /// Overrides the initial fallback spawn position (useful if spawning dynamically before hitting any checkpoint).
    /// </summary>
    public void SetSpawnPosition(Vector3 spawnPosition)
    {
        currentSpawnPosition = spawnPosition;
    }
}
