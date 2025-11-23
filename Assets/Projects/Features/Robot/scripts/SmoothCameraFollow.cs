using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Smooth camera follow script to eliminate shakiness when following a physics-based object.
/// Attach this to your camera and assign the robot as the target.
/// Activate following with AttachToTarget(), detach with DetachFromTarget().
/// </summary>
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("The transform to follow (your robot)")]
    public Transform target;

    [Tooltip("Start following target on scene start")]
    public bool followOnStart = false;

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

    [Header("Mouse Look Settings")]
    [Tooltip("Enable mouse look to rotate camera independently")]
    public bool enableMouseLook = true;

    [Tooltip("Mouse sensitivity for looking around")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;

    [Tooltip("Clamp vertical rotation (min angle)")]
    [Range(-90f, 0f)]
    public float minVerticalAngle = -60f;

    [Tooltip("Clamp vertical rotation (max angle)")]
    [Range(0f, 90f)]
    public float maxVerticalAngle = 60f;

    [Header("Advanced")]
    [Tooltip("Use LateUpdate for smoother following (recommended)")]
    public bool useLateUpdate = true;

    [Tooltip("Use FixedUpdate instead of LateUpdate (try if still shaky)")]
    public bool useFixedUpdate = false;

    private Vector3 velocity = Vector3.zero;
    private Vector3 angularVelocity = Vector3.zero;
    private bool isFollowing = false;

    // Mouse look state
    private float currentYaw = 0f;
    private float currentPitch = 0f;

    void Start()
    {
        if (followOnStart)
        {
            AttachToTarget();
        }
    }

    void LateUpdate()
    {
        if (!useLateUpdate || useFixedUpdate) return;

        if (isFollowing)
        {
            HandleMouseLook();
            UpdateCameraPosition();
        }
    }

    void FixedUpdate()
    {
        if (!useFixedUpdate) return;

        if (isFollowing)
        {
            HandleMouseLook();
            UpdateCameraPosition();
        }
    }

    void Update()
    {
        if (useLateUpdate || useFixedUpdate) return;

        if (isFollowing)
        {
            HandleMouseLook();
            UpdateCameraPosition();
        }
    }

    void HandleMouseLook()
    {
        if (!enableMouseLook) return;

        // Get mouse input using new Input System
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        // Get mouse delta
        Vector2 mouseDelta = mouse.delta.ReadValue();

        // Update yaw (horizontal) and pitch (vertical)
        currentYaw += mouseDelta.x * mouseSensitivity * 0.1f;
        currentPitch -= mouseDelta.y * mouseSensitivity * 0.1f;

        // Clamp pitch to prevent camera flipping
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
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

        // Calculate rotation
        if (followRotation && !enableMouseLook)
        {
            // Follow target rotation (no mouse look)
            Quaternion targetRotation = target.rotation;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - rotationSmoothing
            );
        }
        else if (followRotation && enableMouseLook)
        {
            // Combine target rotation with mouse look
            Quaternion targetBaseRotation = target.rotation;
            Quaternion mouseLookRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            transform.rotation = targetBaseRotation * mouseLookRotation;
        }
        else if (enableMouseLook)
        {
            // Mouse look only (no target rotation following)
            transform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        }
    }

    /// <summary>
    /// Attach camera to target and start following
    /// </summary>
    public void AttachToTarget()
    {
        if (target == null)
        {
            Debug.LogWarning("[SmoothCameraFollow] Cannot attach - no target assigned!");
            return;
        }

        isFollowing = true;

        // Initialize mouse look angles based on current camera rotation
        Vector3 currentRotation = transform.rotation.eulerAngles;
        currentYaw = currentRotation.y;
        currentPitch = currentRotation.x;

        // Normalize pitch to -180 to 180 range
        if (currentPitch > 180f)
            currentPitch -= 360f;

        // Snap to target position
        SnapToTarget();

        Debug.Log("[SmoothCameraFollow] Camera attached to target");
    }

    /// <summary>
    /// Detach camera from target and stop following
    /// </summary>
    public void DetachFromTarget()
    {
        isFollowing = false;
        velocity = Vector3.zero;

        Debug.Log("[SmoothCameraFollow] Camera detached from target");
    }

    /// <summary>
    /// Check if camera is currently following target
    /// </summary>
    public bool IsFollowing()
    {
        return isFollowing;
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
