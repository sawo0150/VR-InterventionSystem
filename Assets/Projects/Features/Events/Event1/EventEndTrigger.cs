using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Triggers event completion when robot reaches end zone.
/// Shows reset button and handles event completion.
/// </summary>
public class EventEndTrigger : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("Reset button to show when robot reaches end")]
    public GameObject resetButton;

    [Tooltip("Robot tag to detect (default: Robot)")]
    public string robotTag = "Robot";

    [Header("Event Callbacks")]
    [Tooltip("Called when robot reaches end zone")]
    public UnityEvent onEventComplete;

    [Header("Reset Settings")]
    [Tooltip("Return player to monitoring scene spawn point")]
    public Transform monitoringSceneSpawnPoint;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;
    [Tooltip("Show trigger zone gizmo in editor")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;

    private bool eventCompleted = false;
    private GameObject currentRobot;

    void Start()
    {
        // Hide reset button initially
        if (resetButton != null)
        {
            resetButton.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if robot reached the end
        if (other.CompareTag(robotTag) && !eventCompleted)
        {
            OnRobotReachedEnd(other.gameObject);
        }
    }

    void OnRobotReachedEnd(GameObject robot)
    {
        eventCompleted = true;
        currentRobot = robot;

        if (enableDebugLogs)
        {
            Debug.Log($"[EventEndTrigger] Robot reached end zone at {transform.position}");
        }

        // Show reset button UI
        ShowResetButton();

        // Notify event completion
        onEventComplete?.Invoke();
    }

    void ShowResetButton()
    {
        if (resetButton != null)
        {
            resetButton.SetActive(true);

            if (enableDebugLogs)
            {
                Debug.Log($"[EventEndTrigger] Reset button shown");
            }
        }
    }

    /// <summary>
    /// Called when reset button is clicked
    /// </summary>
    public void OnResetButtonClicked()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[EventEndTrigger] Reset button clicked");
        }

        // Hide reset button
        if (resetButton != null)
        {
            resetButton.SetActive(false);
        }

        // Notify SimulationSceneManager to reset the current event
        if (SimulationSceneManager.Instance != null)
        {
            SimulationSceneManager.Instance.ResetCurrentEvent();
        }

        eventCompleted = false;
        currentRobot = null;
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
