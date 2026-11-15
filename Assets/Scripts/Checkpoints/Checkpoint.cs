using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Trigger hitbox representing a numbered level checkpoint.
/// When the player collides with this hitbox, it attempts to register with <see cref="PlayerRespawn"/>.
/// Checkpoints with an ID lower than or equal to the currently active checkpoint are ignored.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint Identity")]
    [Tooltip("Sequential checkpoint number. Higher numbers override lower numbers; lower numbers can never reactivate.")]
    [SerializeField] private int checkpointId = 1;

    [Tooltip("Optional child Transform marking the exact respawn position. Uses this object's position if unassigned.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Ensures the attached Collider2D is configured as a trigger hitbox.")]
    [SerializeField] private bool forceTriggerCollider = true;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Optional SpriteRenderer tinted to show active vs inactive state. Auto-finds on this object if empty.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);
    [SerializeField] private Color activeColor = new Color(0.25f, 1f, 0.55f, 1f);

    [Header("Events")]
    [SerializeField] private UnityEvent onActivated;

    private Collider2D triggerCollider;
    private bool isActiveCheckpoint;

    /// <summary>
    /// Numeric ID used to enforce forward-only checkpoint progression.
    /// </summary>
    public int CheckpointId => checkpointId;

    /// <summary>
    /// World position where the player will respawn when this checkpoint is active.
    /// </summary>
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

    /// <summary>
    /// True if this checkpoint is currently the active respawn point.
    /// </summary>
    public bool IsActiveCheckpoint => isActiveCheckpoint;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
        if (forceTriggerCollider && triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        SetVisualActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRespawn playerRespawn = other.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn != null)
        {
            playerRespawn.TryActivateCheckpoint(this);
        }
    }

    /// <summary>
    /// Called by <see cref="PlayerRespawn"/> when this checkpoint becomes active.
    /// </summary>
    public void NotifyActivated()
    {
        SetVisualActive(true);
        onActivated?.Invoke();
    }

    /// <summary>
    /// Updates the checkpoint's visual active state.
    /// </summary>
    public void SetVisualActive(bool active)
    {
        isActiveCheckpoint = active;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? activeColor : inactiveColor;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActiveCheckpoint ? new Color(0.25f, 1f, 0.55f, 0.85f) : new Color(1f, 0.85f, 0.25f, 0.65f);
        Gizmos.DrawWireSphere(SpawnPosition, 0.35f);
    }
}
