using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using VRInterventionSystem.Audio;

/// <summary>
/// NavMesh-based robot controller for VR Intervention System.
/// Provides simplified movement using NavMeshAgent instead of wheel physics.
/// Automatically aligns to slopes for realistic car orientation.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotNavMeshController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Maximum movement speed")]
    [Range(1f, 20f)]
    public float maxSpeed = 5f;

    [Tooltip("How quickly the robot accelerates")]
    [Range(1f, 10f)]
    public float acceleration = 3f;

    [Tooltip("How quickly the robot decelerates (braking)")]
    [Range(1f, 10f)]
    public float deceleration = 5f;

    [Tooltip("Turn speed in degrees per second")]
    [Range(30f, 180f)]
    public float turnSpeed = 90f;

    [Header("Slope Alignment")]
    [Tooltip("Enable automatic slope alignment")]
    public bool alignToSlope = true;

    [Tooltip("Visual mesh to rotate for slope alignment (leave empty to rotate this GameObject)")]
    public Transform visualMesh;

    [Tooltip("Speed of slope alignment (higher = snappier)")]
    [Range(1f, 20f)]
    public float slopeAlignmentSpeed = 8f;

    [Tooltip("Layer mask for ground detection (should exclude Robot layer)")]
    public LayerMask groundLayer = -1; // Default: Everything

    [Tooltip("Max distance to check for ground")]
    [Range(0.5f, 5f)]
    public float groundCheckDistance = 2f;

    [Header("Input Settings")]
    [Tooltip("Enable keyboard input for testing (uses WASD keys)")]
    public bool enableKeyboardInput = true;

    [Tooltip("Enable XR controller input (set by GameManager when player boards robot)")]
    public bool enableXRInput = false;

    [Tooltip("XR controller thumbstick input (Vector2: X=turn, Y=move)")]
    public InputActionReference xrThumbstickAction;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;
    [Tooltip("Show ground check ray in scene view")]
    public bool showGroundCheckGizmo = true;

    private NavMeshAgent agent;
    private float currentSpeed = 0f;
    private RaycastHit lastGroundHit;
    private Transform rotationTarget; // The transform to rotate for slope alignment

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Determine which transform to rotate for slope alignment
        rotationTarget = (visualMesh != null) ? visualMesh : transform;

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotNavMeshController] {gameObject.name} initialized with max speed: {maxSpeed}");
            Debug.Log($"[RobotNavMeshController] Rotation target: {rotationTarget.name}");
        }
    }

    void OnEnable()
    {
        // Configure NavMeshAgent for manual control when this controller is enabled
        if (agent != null)
        {
            agent.updateRotation = false; // We handle rotation manually
            agent.updatePosition = true; // Let NavMesh update position (important!)
            agent.velocity = Vector3.zero;
            agent.speed = maxSpeed;
            agent.ResetPath(); // Clear any existing navigation path

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotNavMeshController] Manual control enabled");
            }
        }

        // Enable XR input action if configured
        if (xrThumbstickAction != null && xrThumbstickAction.action != null)
        {
            xrThumbstickAction.action.Enable();
        }
    }

    void OnDisable()
    {
        // Reset NavMeshAgent to autonomous mode when this controller is disabled
        if (agent != null)
        {
            agent.updateRotation = true; // Let NavMesh handle rotation
            agent.velocity = Vector3.zero;
            currentSpeed = 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotNavMeshController] Manual control disabled");
            }
        }

        // Disable XR input action
        if (xrThumbstickAction != null && xrThumbstickAction.action != null)
        {
            xrThumbstickAction.action.Disable();
        }
    }

    void Update()
    {
        HandleInput();
        UpdateMovement();
        AlignToSlope();
        UpdateEngineSound();
    }

    /// <summary>
    /// Update engine sound based on current speed
    /// </summary>
    void UpdateEngineSound()
    {
        if (SoundManager.Instance != null)
        {
            float speedNormalized = Mathf.Abs(currentSpeed) / maxSpeed;
            bool isMoving = Mathf.Abs(currentSpeed) > 0.01f;
            SoundManager.Instance.UpdateEngineSound(speedNormalized, isMoving);
        }
    }

    /// <summary>
    /// Handle input from keyboard (testing) or XR controllers (VR mode)
    /// W/S or Thumbstick Y = Move forward/backward
    /// A/D or Thumbstick X = Turn left/right
    /// </summary>
    void HandleInput()
    {
        float moveInput = 0f;
        float turnInput = 0f;

        // Priority 1: XR Controller Input (for VR gameplay)
        if (enableXRInput)
        {
            // Read thumbstick input from XR controllers (Vector2)
            if (xrThumbstickAction != null && xrThumbstickAction.action != null)
            {
                Vector2 thumbstick = xrThumbstickAction.action.ReadValue<Vector2>();
                moveInput = thumbstick.y;  // Y-axis for forward/backward
                turnInput = thumbstick.x;  // X-axis for left/right turning
            }

            if (enableDebugLogs && Time.frameCount % 120 == 0 && (Mathf.Abs(moveInput) > 0.01f || Mathf.Abs(turnInput) > 0.01f))
            {
                Debug.Log($"[RobotNavMeshController] XR Input - Move: {moveInput:F2}, Turn: {turnInput:F2}");
            }
        }
        // Priority 2: Keyboard Input (for testing/debugging)
        else if (enableKeyboardInput)
        {
            // Get keyboard reference (new Input System)
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                // W/S: Move forward or backward
                if (keyboard.wKey.isPressed) moveInput = 1f;
                if (keyboard.sKey.isPressed) moveInput = -1f;

                // A/D: Turn left or right
                if (keyboard.aKey.isPressed) turnInput = -1f;
                if (keyboard.dKey.isPressed) turnInput = 1f;
            }
        }

        // Apply movement
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Calculate target speed
            float targetSpeed = moveInput * maxSpeed;
            float accel = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? acceleration : deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
        }
        else
        {
            // Decelerate to stop
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        // Apply turning
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float turnAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0, Space.World);
        }
    }

    /// <summary>
    /// Update movement - apply velocity to NavMeshAgent
    /// </summary>
    void UpdateMovement()
    {
        if (!agent.isOnNavMesh)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[RobotNavMeshController] {gameObject.name} is not on NavMesh!");
            }
            return;
        }

        // Move in the direction the robot is facing
        Vector3 moveDirection = transform.forward * currentSpeed;
        agent.velocity = moveDirection;
    }

    /// <summary>
    /// Align robot rotation to match ground slope
    /// </summary>
    void AlignToSlope()
    {
        if (!alignToSlope) return;

        // Cast from center and slightly above the robot
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Use RaycastAll to get all hits and filter out the robot itself
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);

        // Find the first hit that is NOT part of this robot
        RaycastHit? groundHit = null;
        foreach (RaycastHit hit in hits)
        {
            // Skip if this collider belongs to the robot itself (check if it's a child or the robot itself)
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
            {
                continue;
            }

            // Found a valid ground hit
            groundHit = hit;
            break;
        }

        if (groundHit.HasValue)
        {
            RaycastHit hit = groundHit.Value;
            lastGroundHit = hit;

            // Calculate slope angle from the ground normal
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

            // Calculate the rotation that aligns the robot to the ground normal
            // Keep the current Y rotation (heading direction) from the root transform
            Vector3 forward = transform.forward;
            Vector3 right = Vector3.Cross(hit.normal, forward).normalized;
            forward = Vector3.Cross(right, hit.normal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forward, hit.normal);

            // Smoothly interpolate to target rotation on the rotation target
            rotationTarget.rotation = Quaternion.Slerp(rotationTarget.rotation, targetRotation, slopeAlignmentSpeed * Time.deltaTime);

            if (enableDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[RobotNavMeshController] Slope angle: {slopeAngle:F1}°, Normal: {hit.normal}, Hit: {hit.collider.name}");
                Debug.Log($"[RobotNavMeshController] Rotating {rotationTarget.name}: {rotationTarget.rotation.eulerAngles}");
            }
        }
        else
        {
            if (enableDebugLogs && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"[RobotNavMeshController] No ground detected at {transform.position}");
            }
        }
    }

    /// <summary>
    /// Move the robot forward or backward (for VR controller input)
    /// </summary>
    /// <param name="moveInput">-1 (backward) to 1 (forward)</param>
    public void Move(float moveInput)
    {
        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            float targetSpeed = moveInput * maxSpeed;
            float accel = (Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)) ? acceleration : deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }
    }

    /// <summary>
    /// Turn the robot left or right (for VR controller input)
    /// </summary>
    /// <param name="turnInput">-1 (left) to 1 (right)</param>
    public void Turn(float turnInput)
    {
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);

        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float turnAmount = turnInput * turnSpeed * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0, Space.World);
        }
    }

    /// <summary>
    /// Get current speed
    /// </summary>
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    /// <summary>
    /// Stop the robot immediately
    /// </summary>
    public void Stop()
    {
        currentSpeed = 0f;
        agent.velocity = Vector3.zero;
    }

    void OnDrawGizmos()
    {
        if (!showGroundCheckGizmo || !Application.isPlaying) return;

        // Draw ground check ray
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);

        // Draw ground hit point and normal
        if (lastGroundHit.collider != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(lastGroundHit.point, 0.1f);
            Gizmos.DrawLine(lastGroundHit.point, lastGroundHit.point + lastGroundHit.normal * 1f);
        }
    }
}
