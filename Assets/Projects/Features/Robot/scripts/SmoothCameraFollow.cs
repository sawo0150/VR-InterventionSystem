using UnityEngine;

/// <summary>
/// Smooth camera follow script to eliminate shakiness when following a physics-based object.
/// Attach this to your camera and assign the robot as the target.
/// </summary>
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform to follow (your robot)")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("How smoothly the camera follows (higher = smoother but more lag)")]
    [Range(0.01f, 1f)]
    public float positionSmoothing = 0.1f;

    [Tooltip("How smoothly the camera rotates (higher = smoother but more lag)")]
    [Range(0.01f, 1f)]
    public float rotationSmoothing = 0.1f;

    [Header("Offset Settings")]
    [Tooltip("Position offset from target (local space)")]
    public Vector3 positionOffset = new Vector3(0f, 2f, -5f);

    [Tooltip("Whether to follow target rotation")]
    public bool followRotation = true;

    [Header("Advanced")]
    [Tooltip("Use LateUpdate for smoother following (recommended)")]
    public bool useLateUpdate = true;

    [Tooltip("Use FixedUpdate instead of LateUpdate (try if still shaky)")]
    public bool useFixedUpdate = false;

    private Vector3 velocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;

    void LateUpdate()
    {
        if (!useLateUpdate || useFixedUpdate) return;
        UpdateCameraPosition();
    }

    void FixedUpdate()
    {
        if (!useFixedUpdate) return;
        UpdateCameraPosition();
    }

    void Update()
    {
        if (useLateUpdate || useFixedUpdate) return;
        UpdateCameraPosition();
    }

    void UpdateCameraPosition()
    {
        if (target == null)
        {
            Debug.LogWarning("[SmoothCameraFollow] No target assigned!");
            return;
        }

        // Calculate target position with offset
        Vector3 targetPosition = target.position + target.TransformDirection(positionOffset);

        // Smooth position using SmoothDamp for natural deceleration
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            positionSmoothing
        );

        // Smooth rotation if enabled
        if (followRotation)
        {
            Quaternion targetRotation = target.rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - rotationSmoothing
            );
        }
    }

    /// <summary>
    /// Instantly snap camera to target position (useful for teleporting)
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        transform.position = target.position + target.TransformDirection(positionOffset);
        if (followRotation)
        {
            transform.rotation = target.rotation;
        }
        velocity = Vector3.zero;
    }

    void OnDrawGizmosSelected()
    {
        if (target == null) return;

        // Draw line from target to camera position
        Gizmos.color = Color.cyan;
        Vector3 targetPos = target.position + target.TransformDirection(positionOffset);
        Gizmos.DrawLine(target.position, targetPos);
        Gizmos.DrawWireSphere(targetPos, 0.3f);
    }
}
