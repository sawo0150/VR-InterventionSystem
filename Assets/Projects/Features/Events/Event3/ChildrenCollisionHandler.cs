using UnityEngine;

/// <summary>
/// Handles collisions between obstacles and the robot.
/// Attach this to obstacle GameObjects (deer, rolling stones, etc.).
/// Triggers respawn in Event1Controller when robot collides with the obstacle.
/// </summary>
public class ObstacleCollisionHandler : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Type of obstacle - determines which respawn point is used")]
    public ObstacleType obstacleType = ObstacleType.Children;

    [Header("Detection Settings")]
    [Tooltip("Use trigger detection (Children)")]
    public bool useTriggerDetection = true;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;

    private void Start()
    {
        Debug.Log($"[ObstacleCollisionHandler] Script started on {gameObject.name} - Obstacle Type: {obstacleType}, UseTrigger: {useTriggerDetection}");

        // Verify we have required components
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[ObstacleCollisionHandler] No Collider found on {gameObject.name}!");
        }
        else
        {
            Debug.Log($"[ObstacleCollisionHandler] Collider found: {col.GetType().Name}, IsTrigger: {col.isTrigger}");

            // Warn if detection method doesn't match collider settings
            if (useTriggerDetection && !col.isTrigger)
            {
                Debug.LogWarning($"[ObstacleCollisionHandler] useTriggerDetection is true but collider IsTrigger is false on {gameObject.name}!");
            }
            else if (!useTriggerDetection && col.isTrigger)
            {
                Debug.LogWarning($"[ObstacleCollisionHandler] useTriggerDetection is false but collider IsTrigger is true on {gameObject.name}!");
            }
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"[ObstacleCollisionHandler] No Rigidbody found on {gameObject.name} - detection may not work!");
        }
        else
        {
            Debug.Log($"[ObstacleCollisionHandler] Rigidbody found: IsKinematic: {rb.isKinematic}, CollisionDetection: {rb.collisionDetectionMode}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerDetection) return; // Only process if using trigger detection

        Debug.Log($"[ChildreneCollisionHandler] *** TRIGGER DETECTED *** on {gameObject.name} with {other.gameObject.name}");

        HandleDetection(other.transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTriggerDetection) return; // Only process if using collision detection

        Debug.Log($"[ObstacleCollisionHandler] *** COLLISION DETECTED *** on {gameObject.name} with {collision.gameObject.name}");

        HandleDetection(collision.transform);
    }

    private void HandleDetection(Transform detectedTransform)
    {
        // Check if we collided with the robot by searching up the entire parent hierarchy
        bool isRobot = HasRobotTagInHierarchy(detectedTransform);

        if (isRobot)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[ObstacleCollisionHandler] Robot detected by {obstacleType} obstacle: {gameObject.name}");
            }

            // Notify Event1Controller to respawn the robot
            Event3Controller controller = FindFirstObjectByType<Event3Controller>();
            if (controller != null)
            {
                controller.RespawnRobot3(obstacleType);
            }
            else
            {
                Debug.LogError("[ObstacleCollisionHandler] Event1Controller not found in scene!");
            }
        }
        else if (enableDebugLogs)
        {
            // Only log non-robot detections if debug is enabled
            Debug.Log($"[ChildrenCollisionHandler] Non-robot detection on {gameObject.name} with {detectedTransform.gameObject.name} (tag: {detectedTransform.gameObject.tag})");
        }
    }

    /// <summary>
    /// Check if this transform or any of its parents has the "Robot" tag
    /// Traverses up the entire hierarchy until root
    /// </summary>
    private bool HasRobotTagInHierarchy(Transform transform)
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.CompareTag("Robot"))
            {
                return true;
            }
            current = current.parent;
        }

        return false;
    }
}

/// <summary>
/// Types of obstacles that can trigger respawn
/// </summary>

