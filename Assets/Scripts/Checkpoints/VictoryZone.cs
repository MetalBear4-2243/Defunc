using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Glowing goal hitbox that displays the <see cref="VictoryScreen"/> when touched by the player.
/// Works with both trigger colliders (default) and solid colliders.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VictoryZone : MonoBehaviour
{
    [Header("Victory Settings")]
    [Tooltip("Optional VictoryScreen reference. Auto-finds in scene or creates a default one if empty.")]
    [SerializeField] private VictoryScreen victoryScreen;

    [Tooltip("Ensures the attached Collider2D is configured as a trigger hitbox.")]
    [SerializeField] private bool forceTriggerCollider = true;

    [Tooltip("Disables PlayerMovement and TimeShiftController when victory is triggered.")]
    [SerializeField] private bool freezePlayerOnVictory = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onVictoryTriggered;

    private Collider2D triggerCollider;
    private bool hasTriggered;

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
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerVictory(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTriggerVictory(collision.gameObject);
    }

    private void TryTriggerVictory(GameObject target)
    {
        if (hasTriggered)
        {
            return;
        }

        PlayerMovement playerMovement = target.GetComponentInParent<PlayerMovement>();
        PlayerRespawn playerRespawn = target.GetComponentInParent<PlayerRespawn>();
        if (playerMovement == null && playerRespawn == null)
        {
            return;
        }

        hasTriggered = true;

        if (freezePlayerOnVictory)
        {
            FreezePlayer(playerMovement, playerRespawn);
        }

        VictoryScreen screen = ResolveVictoryScreen();
        screen.ShowVictory();

        onVictoryTriggered?.Invoke();
    }

    private VictoryScreen ResolveVictoryScreen()
    {
        if (victoryScreen != null)
        {
            return victoryScreen;
        }

        victoryScreen = FindAnyObjectByType<VictoryScreen>(FindObjectsInactive.Include);
        if (victoryScreen != null)
        {
            return victoryScreen;
        }

        GameObject screenObj = new GameObject("VictoryScreen");
        victoryScreen = screenObj.AddComponent<VictoryScreen>();
        return victoryScreen;
    }

    private static void FreezePlayer(PlayerMovement playerMovement, PlayerRespawn playerRespawn)
    {
        GameObject playerObj = playerMovement != null ? playerMovement.gameObject : playerRespawn.gameObject;

        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        TimeShiftController timeShift = playerObj.GetComponent<TimeShiftController>();
        if (timeShift != null)
        {
            timeShift.enabled = false;
        }

        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
