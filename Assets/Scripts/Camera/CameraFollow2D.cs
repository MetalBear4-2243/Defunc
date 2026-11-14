using UnityEngine;

/// <summary>
/// Smoothly follows a target Transform in 2D space while preserving the camera's Z offset.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothTime = 0.12f;

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        desiredPosition.z = offset.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime
        );
    }
}
