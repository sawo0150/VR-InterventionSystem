using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a four-wheeled robot using WheelColliders with VR joystick or WASD keyboard input.
/// VR Controls:
///   - Left thumbstick: Forward/Backward movement
///   - Right thumbstick: Turning left/right
/// Keyboard Controls:
///   - W/S: Forward/Backward movement
///   - A/D: Turning left/right
/// </summary>
public class RobotWheelController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    [Tooltip("Front left wheel collider")]
    public WheelCollider frontLeftWheel;
    [Tooltip("Front right wheel collider")]
    public WheelCollider frontRightWheel;
    [Tooltip("Rear left wheel collider")]
    public WheelCollider rearLeftWheel;
    [Tooltip("Rear right wheel collider")]
    public WheelCollider rearRightWheel;

    [Header("Movement Settings")]
    [Tooltip("Maximum motor torque applied to wheels")]
    public float maxMotorTorque = 500f;
    [Tooltip("Maximum steering angle in degrees")]
    public float maxSteeringAngle = 30f;
    [Tooltip("Brake torque applied when not moving")]
    public float brakeTorque = 1000f;

    [Header("Input Settings")]
    [Tooltip("Deadzone for joystick input to prevent drift")]
    [Range(0f, 0.3f)]
    public float joystickDeadzone = 0.1f;

    [Header("Input Actions")]
    [Tooltip("Left hand XR controller input action reference (optional)")]
    public InputActionReference leftJoystickAction;
    [Tooltip("Right hand XR controller input action reference (optional)")]
    public InputActionReference rightJoystickAction;

    [Header("Keyboard Controls")]
    [Tooltip("Enable keyboard WASD controls")]
    public bool enableKeyboardControls = true;

    [Header("Stability Settings")]
    [Tooltip("Lower the center of mass to make the robot more stable")]
    public bool adjustCenterOfMass = true;
    [Tooltip("Center of mass offset (negative Y = lower, increase for more stability)")]
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private Vector2 leftJoystickInput;
    private Vector2 rightJoystickInput;
    private Keyboard keyboard;
    private Rigidbody rb;

    void Start()
    {
        // Check for Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError($"[RobotWheelController] No Rigidbody found on {gameObject.name}! Robot needs a Rigidbody to move.");
        }
        else
        {
            // Adjust center of mass for stability
            if (adjustCenterOfMass)
            {
                rb.centerOfMass = centerOfMassOffset;
                if (enableDebugLogs)
                {
                    Debug.Log($"[RobotWheelController] Center of mass adjusted to: {rb.centerOfMass}");
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWheelController] Rigidbody found. Mass: {rb.mass}, IsKinematic: {rb.isKinematic}");
            }
        }

        // Validate wheel colliders
        if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogError($"[RobotWheelController] One or more WheelColliders are not assigned on {gameObject.name}!");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[RobotWheelController] All WheelColliders assigned successfully.");
        }

        // Check keyboard
        keyboard = Keyboard.current;
        if (keyboard == null && enableDebugLogs)
        {
            Debug.LogWarning("[RobotWheelController] Keyboard not detected. Keyboard controls will not work.");
        }
    }

    void OnValidate()
    {
        // Update center of mass in editor when values change
        if (Application.isPlaying && rb != null && adjustCenterOfMass)
        {
            rb.centerOfMass = centerOfMassOffset;
            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWheelController] Center of mass updated to: {rb.centerOfMass}");
            }
        }
    }

    void FixedUpdate()
    {
        // Continuously enforce center of mass (in case physics resets it)
        if (rb != null && adjustCenterOfMass)
        {
            rb.centerOfMass = centerOfMassOffset;
        }
    }

    void OnEnable()
    {
        // Enable input actions
        if (leftJoystickAction != null)
        {
            leftJoystickAction.action.Enable();
        }
        if (rightJoystickAction != null)
        {
            rightJoystickAction.action.Enable();
        }

        // Get keyboard reference
        keyboard = Keyboard.current;
    }

    void OnDisable()
    {
        // Disable input actions
        if (leftJoystickAction != null)
        {
            leftJoystickAction.action.Disable();
        }
        if (rightJoystickAction != null)
        {
            rightJoystickAction.action.Disable();
        }
    }

    void Update()
    {
        // Read VR joystick input
        leftJoystickInput = leftJoystickAction != null ? leftJoystickAction.action.ReadValue<Vector2>() : Vector2.zero;
        rightJoystickInput = rightJoystickAction != null ? rightJoystickAction.action.ReadValue<Vector2>() : Vector2.zero;

        // Apply deadzone
        if (leftJoystickInput.magnitude < joystickDeadzone)
            leftJoystickInput = Vector2.zero;
        if (rightJoystickInput.magnitude < joystickDeadzone)
            rightJoystickInput = Vector2.zero;

        // Get throttle and steering from VR controllers
        float throttleInput = leftJoystickInput.y;
        float steeringInput = rightJoystickInput.x;

        // Read keyboard input if enabled
        if (enableKeyboardControls && keyboard != null)
        {
            float keyboardThrottle = 0f;
            float keyboardSteering = 0f;

            // W/S for forward/backward
            if (keyboard.wKey.isPressed)
                keyboardThrottle += 1f;
            if (keyboard.sKey.isPressed)
                keyboardThrottle -= 1f;

            // A/D for turning
            if (keyboard.aKey.isPressed)
                keyboardSteering -= 1f;
            if (keyboard.dKey.isPressed)
                keyboardSteering += 1f;

            // Combine VR and keyboard input (keyboard overrides if pressed)
            if (Mathf.Abs(keyboardThrottle) > 0f)
                throttleInput = keyboardThrottle;
            if (Mathf.Abs(keyboardSteering) > 0f)
                steeringInput = keyboardSteering;
        }

        // Debug input
        if (enableDebugLogs && (Mathf.Abs(throttleInput) > 0f || Mathf.Abs(steeringInput) > 0f))
        {
            Debug.Log($"[RobotWheelController] Throttle: {throttleInput:F2}, Steering: {steeringInput:F2}");
        }

        // Apply movement
        ApplyMotor(throttleInput);
        ApplySteering(steeringInput);
    }

    /// <summary>
    /// Apply motor torque to wheels based on throttle input
    /// </summary>
    void ApplyMotor(float throttleInput)
    {
        float motorTorque = throttleInput * maxMotorTorque;

        // Apply motor torque to all wheels (4-wheel drive)
        frontLeftWheel.motorTorque = motorTorque;
        frontRightWheel.motorTorque = motorTorque;
        rearLeftWheel.motorTorque = motorTorque;
        rearRightWheel.motorTorque = motorTorque;

        // Apply brakes if no input
        float brake = Mathf.Abs(throttleInput) < joystickDeadzone ? brakeTorque : 0f;
        frontLeftWheel.brakeTorque = brake;
        frontRightWheel.brakeTorque = brake;
        rearLeftWheel.brakeTorque = brake;
        rearRightWheel.brakeTorque = brake;
    }

    /// <summary>
    /// Apply steering angle to front wheels based on steering input
    /// </summary>
    void ApplySteering(float steeringInput)
    {
        float steeringAngle = steeringInput * maxSteeringAngle;

        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;
    }

    /// <summary>
    /// Draw gizmos in editor to visualize center of mass
    /// </summary>
    void OnDrawGizmos()
    {
        if (adjustCenterOfMass)
        {
            // Draw center of mass position (works even without Rigidbody in edit mode)
            Vector3 worldCenterOfMass = transform.TransformPoint(centerOfMassOffset);

            // Draw a larger, more visible sphere
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldCenterOfMass, 0.2f);

            // Draw a wire sphere around it for better visibility
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldCenterOfMass, 0.25f);

            // Draw line from robot origin to center of mass
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, worldCenterOfMass);

            // Draw cross at center of mass for better visibility
            float crossSize = 0.3f;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(worldCenterOfMass - Vector3.right * crossSize, worldCenterOfMass + Vector3.right * crossSize);
            Gizmos.DrawLine(worldCenterOfMass - Vector3.up * crossSize, worldCenterOfMass + Vector3.up * crossSize);
            Gizmos.DrawLine(worldCenterOfMass - Vector3.forward * crossSize, worldCenterOfMass + Vector3.forward * crossSize);
        }
    }
}
