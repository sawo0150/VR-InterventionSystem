using UnityEngine;

public class ChildCrossing : MonoBehaviour
{
    [Header("Camera/Player Reference")]
    [SerializeField] private Transform playerCamera;
    [Tooltip("If empty, will automatically find the Main Camera")]
    private Transform cameraTransform;

    [Header("Trigger Settings")]
    [SerializeField] private float triggerDistance = 10f;
    [Tooltip("If true, can only trigger once")]
    [SerializeField] private bool triggerOnce = true;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 2f;
    [SerializeField] private float movementDistance = 5f;
    [SerializeField] private Vector3 movementDirection = Vector3.right;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool hasTriggered = false;
    private float distanceTraveled = 0f;

    void Start()
    {
        // Store starting position
        startPosition = transform.position;

        // Auto-find camera if not assigned
        if (playerCamera == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
            else
            {
                Debug.LogWarning($"ChildCrossing: No camera assigned and no Main Camera found for {gameObject.name}");
            }
        }
        else
        {
            cameraTransform = playerCamera;
        }

        // Calculate target position
        targetPosition = startPosition + (movementDirection.normalized * movementDistance);
    }

    void Update()
    {
        // Check if we should trigger
        if (!hasTriggered && cameraTransform != null)
        {
            float distanceToCamera = Vector3.Distance(transform.position, cameraTransform.position);

            if (distanceToCamera <= triggerDistance)
            {
                TriggerCrossing();
            }
        }

        // Handle movement
        if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    /// <summary>
    /// Starts the crossing movement
    /// </summary>
    public void TriggerCrossing()
    {
        if (triggerOnce && hasTriggered)
            return;

        isMoving = true;
        hasTriggered = true;
        distanceTraveled = 0f;
    }

    private void MoveTowardsTarget()
    {
        // Calculate movement this frame
        float moveStep = movementSpeed * Time.deltaTime;

        // Move towards target
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveStep);
        distanceTraveled += moveStep;

        // Check if reached destination
        if (distanceTraveled >= movementDistance || Vector3.Distance(transform.position, targetPosition) < 0.01f)
        {
            isMoving = false;
            transform.position = targetPosition; // Snap to exact position
        }
    }

    /// <summary>
    /// Resets the child to starting position (useful for testing)
    /// </summary>
    public void ResetPosition()
    {
        transform.position = startPosition;
        isMoving = false;
        hasTriggered = false;
        distanceTraveled = 0f;
    }

    /// <summary>
    /// Manually set a new target position
    /// </summary>
    public void SetTargetPosition(Vector3 newTarget)
    {
        targetPosition = newTarget;
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos)
            return;

        Vector3 pos = Application.isPlaying ? startPosition : transform.position;
        Vector3 target = Application.isPlaying ? targetPosition : pos + (movementDirection.normalized * movementDistance);

        // Draw trigger radius
        Gizmos.color = hasTriggered ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(pos, triggerDistance);

        // Draw movement path
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pos, target);
        Gizmos.DrawWireSphere(target, 0.5f);

        // Draw direction arrow
        Vector3 arrowDir = (target - pos).normalized;
        Vector3 arrowMid = pos + arrowDir * (movementDistance * 0.5f);
        Gizmos.DrawLine(arrowMid, arrowMid + arrowDir * 0.5f);
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos)
            return;

        // Draw current position indicator
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
