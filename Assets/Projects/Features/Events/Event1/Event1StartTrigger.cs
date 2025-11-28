using UnityEngine;
using UnityEngine.Events;
using Project;

/// <summary>
/// Triggers Event 1 start when robot enters the zone.
/// Notifies SimulationSceneManager when robot arrives.
/// </summary>
public class Event1StartTrigger : MonoBehaviour
{
    [Header("Event Settings")]
    [Tooltip("Robot tag to detect (default: Robot)")]
    public string robotTag = "Robot";

    [Header("Event Callbacks")]
    [Tooltip("Called when event starts (notify MonitoringScene)")]
    public UnityEvent onEventStart;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;
    [Tooltip("Show trigger zone gizmo in editor")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.green;

    private bool eventStarted = false;

    void Start()
    {
        // Verify trigger setup at start
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError($"[Event1StartTrigger] No collider found on {gameObject.name}!");
        }
        else if (!triggerCollider.isTrigger)
        {
            Debug.LogError($"[Event1StartTrigger] Collider on {gameObject.name} is NOT set as trigger! Check 'Is Trigger' checkbox.");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"[Event1StartTrigger] Trigger setup verified on {gameObject.name}");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Always log what enters the trigger
        if (enableDebugLogs)
        {
            Debug.Log($"[Event1StartTrigger] Trigger entered by: '{other.gameObject.name}' (Tag: '{other.tag}')");
        }

        // Check if robot entered and event hasn't started yet
        if (other.CompareTag(robotTag) && !eventStarted)
        {
            StartEvent(other.gameObject);
        }
        else if (enableDebugLogs)
        {
            // Explain why event didn't start
            if (!other.CompareTag(robotTag))
            {
                Debug.LogWarning($"[Event1StartTrigger] Tag mismatch! Expected '{robotTag}', got '{other.tag}'");
            }
            if (eventStarted)
            {
                Debug.LogWarning($"[Event1StartTrigger] Event already started, ignoring trigger");
            }
        }
    }

    void StartEvent(GameObject robot)
    {
        eventStarted = true;

        if (enableDebugLogs)
        {
            Debug.Log($"[Event1StartTrigger] Robot arrived at event location");
        }

        // Notify SimulationSceneManager that robot has arrived
        if (SimulationSceneManager.Instance != null)
        {
            SimulationSceneManager.Instance.OnEventLocationReached();
        }

        // Trigger additional callbacks (e.g., minimap notification)
        onEventStart?.Invoke();
    }

    /// <summary>
    /// Reset event state (called from Event1Controller)
    /// </summary>
    public void ResetEvent()
    {
        eventStarted = false;

        if (enableDebugLogs)
        {
            Debug.Log($"[Event1StartTrigger] Event reset");
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // Draw trigger zone
        Gizmos.color = gizmoColor;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Gizmos.DrawWireSphere(transform.position, sphere.radius);
        }
    }
}
