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
    [Tooltip("Maximum motor torque applied to wheels (increase for uphill power)")]
    public float maxMotorTorque = 2000f;
    [Tooltip("Maximum speed limit in m/s (0 = unlimited)")]
    [Range(0f, 20f)]
    public float maxSpeed = 5f;
    [Tooltip("Maximum steering angle in degrees")]
    public float maxSteeringAngle = 30f;
    [Tooltip("Brake torque applied when not moving")]
    public float brakeTorque = 1000f;
    [Tooltip("Automatically reduce torque on steep slopes to prevent tipping")]
    public bool slopeSpeedControl = true;
    [Tooltip("Slope angle (degrees) where speed reduction starts")]
    [Range(0f, 45f)]
    public float slopeAngleThreshold = 15f;

    [Header("Gear System")]
    [Tooltip("Enable automatic gear shifting based on slope")]
    public bool enableAutoGear = true;
    [Tooltip("Slope angle (degrees) to shift into low gear")]
    [Range(5f, 30f)]
    public float lowGearSlopeThreshold = 10f;
    [Tooltip("Low gear: High torque multiplier for climbing (slower but powerful)")]
    [Range(1f, 5f)]
    public float lowGearTorqueMultiplier = 2.5f;
    [Tooltip("Low gear: Speed reduction factor (0.5 = half speed)")]
    [Range(0.1f, 1f)]
    public float lowGearSpeedFactor = 0.5f;

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

    [Header("Suspension Tuning (for smoother ride)")]
    [Tooltip("Apply suspension settings to all wheels")]
    public bool adjustSuspension = true;
    [Tooltip("Suspension spring force (higher = stiffer, less bouncy). Default: 35000")]
    public float suspensionSpring = 35000f;
    [Tooltip("Suspension damper (higher = less oscillation). Default: 4500")]
    public float suspensionDamper = 4500f;
    [Tooltip("Target position (0-1, lower = less compression). Default: 0.5")]
    [Range(0f, 1f)]
    public float suspensionTargetPosition = 0.5f;
    [Tooltip("Suspension travel distance. Default: 0.3")]
    public float suspensionDistance = 0.3f;

    [Header("Wheel Friction (for grip on slopes)")]
    [Tooltip("Apply friction settings to all wheels")]
    public bool adjustFriction = true;
    [Tooltip("Forward grip (higher = better uphill, less sliding). Default: 3")]
    public float forwardStiffness = 4f;
    [Tooltip("Sideways grip (higher = better turning, less sliding). Default: 2")]
    public float sidewaysStiffness = 3f;

    [Header("Stability")]
    [Tooltip("Apply downforce to keep wheels on ground")]
    public bool enableDownforce = true;
    [Tooltip("Downforce strength (higher = better ground contact). Default: 50")]
    public float downforceAmount = 50f;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private Vector2 leftJoystickInput;
    private Vector2 rightJoystickInput;
    private Keyboard keyboard;
    private Rigidbody rb;

    // Store input for FixedUpdate
    private float currentThrottleInput;
    private float currentSteeringInput;

    // Track current gear
    private bool isInLowGear = false;

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
            // Enable interpolation for smooth visual movement
            rb.interpolation = RigidbodyInterpolation.Interpolate;

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
                Debug.Log($"[RobotWheelController] Rigidbody found. Mass: {rb.mass}, IsKinematic: {rb.isKinematic}, Interpolation: {rb.interpolation}");
            }
        }

        // Validate wheel colliders
        if (frontLeftWheel == null || frontRightWheel == null || rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogError($"[RobotWheelController] One or more WheelColliders are not assigned on {gameObject.name}!");
        }
        else
        {
            // Apply suspension and friction settings
            ApplySuspensionSettings();
            ApplyFrictionSettings();

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWheelController] All WheelColliders assigned successfully.");
            }
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

        // Apply wheel physics in FixedUpdate for smooth, consistent movement
        ApplyMotor(currentThrottleInput);
        ApplySteering(currentSteeringInput);

        // Apply downforce to keep wheels on ground
        if (enableDownforce && rb != null)
        {
            rb.AddForce(-transform.up * downforceAmount * rb.linearVelocity.magnitude);
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

        // Store input for FixedUpdate to apply physics
        currentThrottleInput = throttleInput;
        currentSteeringInput = steeringInput;

        // Debug input
        if (enableDebugLogs && (Mathf.Abs(throttleInput) > 0f || Mathf.Abs(steeringInput) > 0f))
        {
            Debug.Log($"[RobotWheelController] Throttle: {throttleInput:F2}, Steering: {steeringInput:F2}");
        }
    }

    /// <summary>
    /// Apply motor torque to wheels based on throttle input
    /// </summary>
    void ApplyMotor(float throttleInput)
    {
        float motorTorque = throttleInput * maxMotorTorque;
        float gearRatio = 1f; // Default gear ratio

        // Auto gear system: shift to low gear on slopes
        if (enableAutoGear)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, transform.up);

            // Shift into low gear on steep slopes
            if (slopeAngle > lowGearSlopeThreshold)
            {
                isInLowGear = true;
                gearRatio = lowGearSpeedFactor; // Slower speed
                motorTorque *= lowGearTorqueMultiplier; // But more torque!

                if (enableDebugLogs)
                {
                    Debug.Log($"[RobotWheelController] LOW GEAR - Slope: {slopeAngle:F1}°, Torque: x{lowGearTorqueMultiplier:F1}, Speed: {gearRatio:F2}x");
                }
            }
            else
            {
                // Shift into high gear on flat ground
                if (isInLowGear && enableDebugLogs)
                {
                    Debug.Log($"[RobotWheelController] HIGH GEAR - Slope: {slopeAngle:F1}°, Normal speed");
                }
                isInLowGear = false;
            }
        }

        // Reduce torque ONLY when going downhill to prevent instability
        if (slopeSpeedControl)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, transform.up);
            if (slopeAngle > slopeAngleThreshold)
            {
                // Check if going downhill (negative dot product between forward velocity and slope normal)
                Vector3 slopeDirection = Vector3.ProjectOnPlane(rb.linearVelocity, Vector3.up).normalized;
                float slopeDot = Vector3.Dot(transform.forward, slopeDirection);
                bool isGoingDownhill = rb.linearVelocity.y < -0.5f && throttleInput > 0f; // Moving down and accelerating

                if (isGoingDownhill)
                {
                    // Reduce torque based on slope steepness (only when going down)
                    float reductionFactor = Mathf.Clamp01(1f - ((slopeAngle - slopeAngleThreshold) / 30f));
                    motorTorque *= reductionFactor;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RobotWheelController] Downhill brake - Torque reduction: {reductionFactor:F2}");
                    }
                }
            }
        }

        // Apply gear ratio to limit top speed in low gear
        if (isInLowGear && rb != null)
        {
            float currentSpeed = rb.linearVelocity.magnitude;
            float maxLowGearSpeed = 5f * lowGearSpeedFactor; // Limit speed in low gear

            if (currentSpeed > maxLowGearSpeed)
            {
                motorTorque *= 0.5f; // Reduce torque when at speed limit
            }
        }

        // Apply overall max speed limit
        if (maxSpeed > 0f && rb != null)
        {
            float currentSpeed = rb.linearVelocity.magnitude;

            if (currentSpeed >= maxSpeed)
            {
                // At max speed, cut motor torque
                motorTorque = 0f;

                if (enableDebugLogs && Mathf.Abs(throttleInput) > 0.1f)
                {
                    Debug.Log($"[RobotWheelController] MAX SPEED REACHED: {currentSpeed:F1} m/s (limit: {maxSpeed:F1} m/s)");
                }
            }
            else if (currentSpeed > maxSpeed * 0.9f)
            {
                // Near max speed, reduce torque gradually
                float speedRatio = (currentSpeed - (maxSpeed * 0.9f)) / (maxSpeed * 0.1f);
                motorTorque *= (1f - speedRatio);
            }
        }

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
    /// Apply suspension settings to all wheels
    /// </summary>
    void ApplySuspensionSettings()
    {
        if (!adjustSuspension) return;

        WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };

        foreach (WheelCollider wheel in wheels)
        {
            if (wheel == null) continue;

            JointSpring spring = wheel.suspensionSpring;
            spring.spring = suspensionSpring;
            spring.damper = suspensionDamper;
            spring.targetPosition = suspensionTargetPosition;
            wheel.suspensionSpring = spring;

            wheel.suspensionDistance = suspensionDistance;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWheelController] Suspension applied - Spring: {suspensionSpring}, Damper: {suspensionDamper}");
        }
    }

    /// <summary>
    /// Apply friction settings to all wheels for better grip
    /// </summary>
    void ApplyFrictionSettings()
    {
        if (!adjustFriction) return;

        WheelCollider[] wheels = { frontLeftWheel, frontRightWheel, rearLeftWheel, rearRightWheel };

        foreach (WheelCollider wheel in wheels)
        {
            if (wheel == null) continue;

            // Forward friction (for uphill grip)
            WheelFrictionCurve forwardFriction = wheel.forwardFriction;
            forwardFriction.stiffness = forwardStiffness;
            wheel.forwardFriction = forwardFriction;

            // Sideways friction (for turning stability)
            WheelFrictionCurve sidewaysFriction = wheel.sidewaysFriction;
            sidewaysFriction.stiffness = sidewaysStiffness;
            wheel.sidewaysFriction = sidewaysFriction;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWheelController] Friction applied - Forward: {forwardStiffness}, Sideways: {sidewaysStiffness}");
        }
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
