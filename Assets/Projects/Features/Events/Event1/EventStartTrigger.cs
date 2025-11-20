using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Triggers event start when robot enters the zone.
/// Notifies SimulationSceneManager when robot arrives.
/// </summary>
public class EventStartTrigger : MonoBehaviour
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

    void OnTriggerEnter(Collider other)
    {
        // Check if robot entered and event hasn't started yet
        if (other.CompareTag(robotTag) && !eventStarted)
        {
            StartEvent(other.gameObject);
        }
    }

    void StartEvent(GameObject robot)
    {
        eventStarted = true;

        if (enableDebugLogs)
        {
            Debug.Log($"[EventStartTrigger] Robot arrived at event location");
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
            Debug.Log($"[EventStartTrigger] Event reset");
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
