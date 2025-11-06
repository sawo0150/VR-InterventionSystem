using UnityEngine;

/// <summary>
/// Makes a GameObject jump back and forth between two points using a parabolic arc.
/// Can be triggered automatically on start or manually via method calls.
/// </summary>
public class JumpBetweenPoints : MonoBehaviour
{
    [Header("Jump Points")]
    [Tooltip("First jump position (leave empty to use current position)")]
    public Transform pointA;

    [Tooltip("Second jump position")]
    public Transform pointB;

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
    private float jumpProgress = 0f;
    private float pauseTimer = 0f;
    private Vector3 currentStart;
    private Vector3 currentEnd;
    private bool jumpingToB = true;
    private bool isWaitingToStart = false;
    private float startDelayTimer = 0f;

    void Start()
    {
        // Validate setup
        if (pointB == null)
        {
            Debug.LogError($"JumpBetweenPoints on {gameObject.name}: Point B is not assigned!", this);
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

        // Set initial position
        transform.position = startFromPointA ? pointA.position : pointB.position;
        jumpingToB = startFromPointA;

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
            transform.position = currentEnd;
            jumpProgress = 0f;

            // Rotate 180 degrees if enabled
            if (rotateAfterJump)
            {
                transform.Rotate(0f, 180f, 0f);
            }

            if (pauseDuration > 0f)
            {
                isPaused = true;
            }
            else
            {
                SetupNextJump();
            }
        }
        else
        {
            // Calculate position along parabolic arc
            transform.position = CalculateJumpPosition(jumpProgress);
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
    /// Sets up the next jump (toggles direction)
    /// </summary>
    private void SetupNextJump()
    {
        jumpingToB = !jumpingToB;

        if (jumpingToB)
        {
            currentStart = pointA.position;
            currentEnd = pointB.position;
        }
        else
        {
            currentStart = pointB.position;
            currentEnd = pointA.position;
        }
    }

    /// <summary>
    /// Starts the jumping behavior
    /// </summary>
    public void StartJumping()
    {
        if (pointB == null || pointA == null)
        {
            Debug.LogWarning($"Cannot start jumping - points not assigned on {gameObject.name}");
            return;
        }

        isJumping = true;
        jumpProgress = 0f;
        SetupNextJump();
    }

    /// <summary>
    /// Stops the jumping behavior
    /// </summary>
    public void StopJumping()
    {
        isJumping = false;
        isPaused = false;
        jumpProgress = 0f;
        pauseTimer = 0f;
    }

    /// <summary>
    /// Resets the object to Point A
    /// </summary>
    public void ResetToPointA()
    {
        StopJumping();
        if (pointA != null)
        {
            transform.position = pointA.position;
            jumpingToB = true;
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
            transform.position = pointB.position;
            jumpingToB = false;
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
        if (!showGizmos || pointA == null || pointB == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pointA.position, 0.3f);
        Gizmos.DrawIcon(pointA.position, "sv_label_1", true);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(pointB.position, 0.3f);
        Gizmos.DrawIcon(pointB.position, "sv_label_2", true);

        // Draw the jump arc
        Gizmos.color = Color.yellow;
        Vector3 previousPoint = pointA.position;

        for (int i = 1; i <= gizmoResolution; i++)
        {
            float t = i / (float)gizmoResolution;
            Vector3 linearPos = Vector3.Lerp(pointA.position, pointB.position, t);
            float heightOffset = jumpHeight * 4f * t * (1f - t);
            Vector3 currentPoint = linearPos + Vector3.up * heightOffset;

            Gizmos.DrawLine(previousPoint, currentPoint);
            previousPoint = currentPoint;
        }
    }
}
