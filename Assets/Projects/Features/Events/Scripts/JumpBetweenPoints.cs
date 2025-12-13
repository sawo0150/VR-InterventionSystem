using UnityEngine;
using VRInterventionSystem.Audio;

/// <summary>
/// Makes a GameObject jump in a cycle: A → Waiting → B → Waiting → A (repeat)
/// Uses parabolic arcs for jumps. Waits at Waiting Point, pauses at A and B.
/// Can be triggered automatically on start or manually via method calls.
/// Cycle: Point A → Waiting Point → Point B → Waiting Point → Point A
/// </summary>
public class JumpBetweenPoints : MonoBehaviour
{
    [Header("Jump Points")]
    [Tooltip("First jump position (leave empty to use current position)")]
    public Transform pointA;

    [Tooltip("Second jump position")]
    public Transform pointB;

    [Tooltip("Waiting point (deer waits here before jumping to Point B)")]
    public Transform waitingPoint;

    [Header("Jump Settings")]
    [Tooltip("Duration of each jump in seconds")]
    [Range(0.1f, 5f)]
    public float jumpDuration = 1f;

    [Tooltip("Height of the jump arc")]
    [Range(0.5f, 10f)]
    public float jumpHeight = 2f;

    [Tooltip("Pause duration at each point before jumping again")]
    [Range(0f, 5f)]
    public float pauseDuration = 0.5f;

    [Tooltip("Wait duration at the waiting point before jumping to Point B")]
    [Range(0f, 10f)]
    public float waitingDuration = 2f;

    [Tooltip("Rotate 180 degrees after each jump")]
    public bool rotateAfterJump = true;

    [Header("Auto Start")]
    [Tooltip("Start jumping automatically when the scene starts")]
    public bool autoStart = true;

    [Tooltip("Delay before starting the first jump (in seconds)")]
    [Range(0f, 10f)]
    public float startDelay = 0f;

    [Tooltip("Start from Point A (true) or Point B (false)")]
    public bool startFromPointA = true;

    [Header("Debug")]
    [Tooltip("Show the jump arc in the editor")]
    public bool showGizmos = true;

    [Tooltip("Number of gizmo points to draw the arc")]
    [Range(10, 50)]
    public int gizmoResolution = 20;

    // Private state
    private bool isJumping = false;
    private bool isPaused = false;
    private bool isWaitingAtWaitPoint = false;
    private float jumpProgress = 0f;
    private float pauseTimer = 0f;
    private float waitTimer = 0f;
    private Vector3 currentStart;
    private Vector3 currentEnd;
    // Cycle: AtA → AToWaiting → AtWaiting → WaitingToB → AtB → BToWaiting → AtWaiting → WaitingToA → (repeat)
    private enum JumpPhase { AtA, AToWaiting, AtWaiting_FromA, WaitingToB, AtB, BToWaiting, AtWaiting_FromB, WaitingToA }
    private JumpPhase currentPhase = JumpPhase.AtA;
    private bool isWaitingToStart = false;
    private float startDelayTimer = 0f;
    private Rigidbody rb;
    private Vector3 targetPosition; // Position to move to in FixedUpdate
    private bool needsPositionUpdate = false; // Flag to update position in FixedUpdate
    private AudioSource audioSource; // AudioSource for ambient deer sounds

    void Start()
    {
        // Get Rigidbody component
        rb = GetComponent<Rigidbody>();

        // Get or create AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure AudioSource for 3D spatial audio
        InitializeAudioSource();

        // Validate setup
        if (pointB == null)
        {
            Debug.LogError($"JumpBetweenPoints on {gameObject.name}: Point B is not assigned!", this);
            enabled = false;
            return;
        }

        if (waitingPoint == null)
        {
            Debug.LogError($"JumpBetweenPoints on {gameObject.name}: Waiting Point is not assigned!", this);
            enabled = false;
            return;
        }

        // Use current position as Point A if not assigned
        if (pointA == null)
        {
            GameObject pointAObj = new GameObject($"{gameObject.name}_PointA");
            pointA = pointAObj.transform;
            pointA.position = transform.position;
            pointA.SetParent(transform.parent);
        }

        // Set initial position to Point A
        SetPosition(pointA.position);
        currentPhase = JumpPhase.AtA;

        if (autoStart)
        {
            if (startDelay > 0f)
            {
                isWaitingToStart = true;
                startDelayTimer = 0f;
            }
            else
            {
                StartJumping();
            }
        }
    }

    void Update()
    {
        // Handle start delay
        if (isWaitingToStart)
        {
            startDelayTimer += Time.deltaTime;
            if (startDelayTimer >= startDelay)
            {
                isWaitingToStart = false;
                StartJumping();
            }
            return;
        }

        if (!isJumping) return;

        // Handle waiting at waiting point (uses waitingDuration)
        if (isWaitingAtWaitPoint)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitingDuration)
            {
                isWaitingAtWaitPoint = false;
                waitTimer = 0f;
                // Set up next jump based on which phase we're in
                SetupNextJump();
            }
            return;
        }

        // Handle pause at Point A or Point B (uses pauseDuration)
        if (isPaused)
        {
            pauseTimer += Time.deltaTime;
            if (pauseTimer >= pauseDuration)
            {
                isPaused = false;
                pauseTimer = 0f;
                // Set up next jump
                SetupNextJump();
            }
            return;
        }

        // Perform jump animation
        jumpProgress += Time.deltaTime / jumpDuration;

        if (jumpProgress >= 1f)
        {
            // Jump complete - snap to end position
            SetPosition(currentEnd);
            jumpProgress = 0f;

            // Update phase and determine what to do next
            if (currentPhase == JumpPhase.AToWaiting)
            {
                // Arrived at waiting point from A - don't rotate here
                currentPhase = JumpPhase.AtWaiting_FromA;
                // Start waiting (no rotation at waiting point)
                isWaitingAtWaitPoint = true;
                waitTimer = 0f;
            }
            else if (currentPhase == JumpPhase.WaitingToB)
            {
                // Arrived at Point B - rotate here
                currentPhase = JumpPhase.AtB;
                // Rotate if enabled
                if (rotateAfterJump) transform.Rotate(0f, 180f, 0f);
                // Pause before returning
                if (pauseDuration > 0f)
                {
                    isPaused = true;
                }
                else
                {
                    SetupNextJump();
                }
            }
            else if (currentPhase == JumpPhase.BToWaiting)
            {
                // Arrived at waiting point from B - don't rotate here
                currentPhase = JumpPhase.AtWaiting_FromB;
                // Start waiting (no rotation at waiting point)
                isWaitingAtWaitPoint = true;
                waitTimer = 0f;
            }
            else if (currentPhase == JumpPhase.WaitingToA)
            {
                // Arrived back at Point A - rotate here
                currentPhase = JumpPhase.AtA;
                // Rotate if enabled
                if (rotateAfterJump) transform.Rotate(0f, 180f, 0f);
                // Pause before next cycle
                if (pauseDuration > 0f)
                {
                    isPaused = true;
                }
                else
                {
                    SetupNextJump();
                }
            }
        }
        else
        {
            // Calculate position along parabolic arc
            SetPosition(CalculateJumpPosition(jumpProgress));
        }
    }

    void FixedUpdate()
    {
        // Apply position changes in FixedUpdate for proper physics collision detection
        if (needsPositionUpdate && rb != null && rb.isKinematic)
        {
            rb.MovePosition(targetPosition);
            needsPositionUpdate = false;
        }
    }

    /// <summary>
    /// Calculates the position along a parabolic jump arc
    /// </summary>
    private Vector3 CalculateJumpPosition(float t)
    {
        // Linear interpolation between start and end
        Vector3 linearPosition = Vector3.Lerp(currentStart, currentEnd, t);

        // Parabolic height curve (peaks at t=0.5)
        float heightOffset = jumpHeight * 4f * t * (1f - t);

        // Add vertical offset
        return linearPosition + Vector3.up * heightOffset;
    }

    /// <summary>
    /// Sets up the next jump based on current phase
    /// Cycle: AtA → AToWaiting → AtWaiting_FromA → WaitingToB → AtB → BToWaiting → AtWaiting_FromB → WaitingToA → (repeat)
    /// </summary>
    private void SetupNextJump()
    {
        if (currentPhase == JumpPhase.AtA)
        {
            // Jump from Point A to Waiting Point
            currentStart = pointA.position;
            currentEnd = waitingPoint.position;
            currentPhase = JumpPhase.AToWaiting;
        }
        else if (currentPhase == JumpPhase.AtWaiting_FromA)
        {
            // Jump from Waiting Point to Point B
            currentStart = waitingPoint.position;
            currentEnd = pointB.position;
            currentPhase = JumpPhase.WaitingToB;
        }
        else if (currentPhase == JumpPhase.AtB)
        {
            // Jump from Point B back to Waiting Point
            currentStart = pointB.position;
            currentEnd = waitingPoint.position;
            currentPhase = JumpPhase.BToWaiting;
        }
        else if (currentPhase == JumpPhase.AtWaiting_FromB)
        {
            // Jump from Waiting Point back to Point A
            currentStart = waitingPoint.position;
            currentEnd = pointA.position;
            currentPhase = JumpPhase.WaitingToA;
        }
    }

    /// <summary>
    /// Initialize AudioSource with settings from AudioConfig
    /// </summary>
    private void InitializeAudioSource()
    {
        if (audioSource == null || SoundManager.Instance == null) return;

        var config = SoundManager.Instance.GetAudioConfig();
        if (config == null) return;

        audioSource.clip = config.deerAmbientLoop;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = config.deerAmbientVolume;
        audioSource.spatialBlend = config.deerSpatialBlend;
        audioSource.maxDistance = config.deerMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }

    /// <summary>
    /// Starts the jumping behavior and ambient sound
    /// </summary>
    public void StartJumping()
    {
        if (pointB == null || pointA == null || waitingPoint == null)
        {
            Debug.LogWarning($"Cannot start jumping - points not assigned on {gameObject.name}");
            return;
        }

        isJumping = true;
        jumpProgress = 0f;

        // Start ambient deer sound
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }

        // Start from Point A with a pause
        currentPhase = JumpPhase.AtA;
        if (pauseDuration > 0f)
        {
            isPaused = true;
            pauseTimer = 0f;
        }
        else
        {
            SetupNextJump();
        }
    }

    /// <summary>
    /// Stops the jumping behavior and ambient sound
    /// </summary>
    public void StopJumping()
    {
        isJumping = false;
        isPaused = false;
        isWaitingAtWaitPoint = false;
        jumpProgress = 0f;
        pauseTimer = 0f;
        waitTimer = 0f;

        // Stop ambient deer sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Sets the position using Rigidbody.MovePosition if available, otherwise transform.position
    /// This ensures proper collision detection for kinematic Rigidbodies
    /// MovePosition is deferred to FixedUpdate for proper physics processing
    /// </summary>
    private void SetPosition(Vector3 position)
    {
        if (rb != null && rb.isKinematic)
        {
            // Queue the position update for FixedUpdate
            targetPosition = position;
            needsPositionUpdate = true;
        }
        else
        {
            transform.position = position;
        }
    }

    /// <summary>
    /// Resets the object to Point A
    /// </summary>
    public void ResetToPointA()
    {
        StopJumping();
        if (pointA != null)
        {
            SetPosition(pointA.position);
            currentPhase = JumpPhase.AtA;
        }
    }

    /// <summary>
    /// Resets the object to waiting point
    /// </summary>
    public void ResetToWaitingPoint()
    {
        StopJumping();
        if (waitingPoint != null)
        {
            SetPosition(waitingPoint.position);
            currentPhase = JumpPhase.AtWaiting_FromB; // Default to coming from B direction
        }
    }

    /// <summary>
    /// Resets the object to Point B
    /// </summary>
    public void ResetToPointB()
    {
        StopJumping();
        if (pointB != null)
        {
            SetPosition(pointB.position);
            currentPhase = JumpPhase.AtB;
        }
    }

    /// <summary>
    /// Toggles jumping on/off
    /// </summary>
    public void ToggleJumping()
    {
        if (isJumping)
            StopJumping();
        else
            StartJumping();
    }

    // Draw the jump arc in the editor
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw Point A (starting point)
        if (pointA != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawIcon(pointA.position, "sv_label_1", true);
        }

        // Draw Waiting Point (intermediate point where deer waits)
        if (waitingPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(waitingPoint.position, 0.3f);
            Gizmos.DrawIcon(waitingPoint.position, "sv_label_2", true);
        }

        // Draw Point B (far point)
        if (pointB != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
            Gizmos.DrawIcon(pointB.position, "sv_label_3", true);
        }

        // Draw all four jump arcs
        if (pointA != null && waitingPoint != null && pointB != null)
        {
            // 1. Arc from A to Waiting Point (magenta)
            Gizmos.color = Color.magenta;
            DrawArc(pointA.position, waitingPoint.position);

            // 2. Arc from Waiting Point to B (yellow)
            Gizmos.color = Color.yellow;
            DrawArc(waitingPoint.position, pointB.position);

            // 3. Arc from B back to Waiting Point (cyan)
            Gizmos.color = Color.cyan;
            DrawArc(pointB.position, waitingPoint.position);

            // 4. Arc from Waiting Point back to A (white)
            Gizmos.color = Color.white;
            DrawArc(waitingPoint.position, pointA.position);
        }
    }

    /// <summary>
    /// Helper method to draw a parabolic arc between two points
    /// </summary>
    private void DrawArc(Vector3 start, Vector3 end)
    {
        Vector3 previousPoint = start;

        for (int i = 1; i <= gizmoResolution; i++)
        {
            float t = i / (float)gizmoResolution;
            Vector3 linearPos = Vector3.Lerp(start, end, t);
            float heightOffset = jumpHeight * 4f * t * (1f - t);
            Vector3 currentPoint = linearPos + Vector3.up * heightOffset;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}
