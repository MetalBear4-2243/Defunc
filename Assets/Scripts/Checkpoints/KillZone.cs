using UnityEngine;

/// <summary>
/// Hazard hitbox that kills the player on contact, triggering <see cref="PlayerRespawn.Die"/>.
/// Works with both trigger colliders (e.g., death pits) and solid colliders (e.g., spikes).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class KillZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject target)
    {
        PlayerRespawn playerRespawn = target.GetComponentInParent<PlayerRespawn>();
        if (playerRespawn != null)
        {
            playerRespawn.Die();
        }
    }
}
