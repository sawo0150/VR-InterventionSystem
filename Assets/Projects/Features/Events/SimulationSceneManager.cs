using UnityEngine;

/// <summary>
/// Manages all events in the simulation scene.
/// Ensures only one event runs at a time.
/// Called by MonitoringScene UI to trigger events.
/// </summary>
public class SimulationSceneManager : MonoBehaviour
{
    [Header("Event References")]
    [Tooltip("Reference to Event 1 controller")]
    public MonoBehaviour event1Controller;
    [Tooltip("Reference to Event 2 controller")]
    public MonoBehaviour event2Controller;
    [Tooltip("Reference to Event 3 controller")]
    public MonoBehaviour event3Controller;

    [Header("VR Settings")]
    [Tooltip("(Optional) Legacy camera with SmoothCameraFollow - leave empty if using XR Origin")]
    public SmoothCameraFollow mainCamera;
    [Tooltip("Monitoring scene spawn point (player returns here on reset)")]
    public Transform monitoringSpawnPoint;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private IEvent[] events;
    private IEvent currentEvent;
    private int currentEventIndex = -1;

    // Singleton instance
    private static SimulationSceneManager instance;
    public static SimulationSceneManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SimulationSceneManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Initialize event array
        InitializeEvents();
    }

    void InitializeEvents()
    {
        events = new IEvent[3];

        // Convert MonoBehaviour references to IEvent
        if (event1Controller != null && event1Controller is IEvent)
        {
            events[0] = event1Controller as IEvent;
        }
        else
        {
            Debug.LogWarning("[SimulationSceneManager] Event1Controller not assigned or doesn't implement IEvent!");
        }

        if (event2Controller != null && event2Controller is IEvent)
        {
            events[1] = event2Controller as IEvent;
        }
        else
        {
            Debug.LogWarning("[SimulationSceneManager] Event2Controller not assigned or doesn't implement IEvent!");
        }

        if (event3Controller != null && event3Controller is IEvent)
        {
            events[2] = event3Controller as IEvent;
        }
        else
        {
            Debug.LogWarning("[SimulationSceneManager] Event3Controller not assigned or doesn't implement IEvent!");
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[SimulationSceneManager] Initialized with {events.Length} events");
        }
    }

    /// <summary>
    /// Start an event by index (1, 2, or 3)
    /// Called from MonitoringScene UI buttons
    /// </summary>
    public void StartEvent(int eventNumber)
    {
        // Validate event number (1-based index)
        if (eventNumber < 1 || eventNumber > 3)
        {
            Debug.LogError($"[SimulationSceneManager] Invalid event number: {eventNumber}. Must be 1, 2, or 3.");
            return;
        }

        int eventIndex = eventNumber - 1; // Convert to 0-based index

        // Check if event exists
        if (events[eventIndex] == null)
        {
            Debug.LogError($"[SimulationSceneManager] Event {eventNumber} is not assigned!");
            return;
        }

        // Check if another event is already running
        if (currentEvent != null && currentEvent.GetState() != EventState.Idle)
        {
            Debug.LogWarning($"[SimulationSceneManager] Cannot start Event {eventNumber}. {currentEvent.GetEventName()} is currently {currentEvent.GetState()}");
            return;
        }

        // Start the event
        currentEvent = events[eventIndex];
        currentEventIndex = eventIndex;

        if (enableDebugLogs)
        {
            Debug.Log($"[SimulationSceneManager] Starting {currentEvent.GetEventName()}");
        }

        currentEvent.InitializeEvent();
    }

    /// <summary>
    /// Called by EventStartTrigger when robot reaches event location
    /// </summary>
    public void OnEventLocationReached()
    {
        if (currentEvent == null)
        {
            Debug.LogWarning("[SimulationSceneManager] OnEventLocationReached called but no current event!");
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[SimulationSceneManager] {currentEvent.GetEventName()} location reached. Starting event...");
        }

        currentEvent.StartEvent();
    }

    /// <summary>
    /// Reset the current event
    /// Called when reset button is clicked
    /// </summary>
    public void ResetCurrentEvent()
    {
        if (currentEvent == null)
        {
            Debug.LogWarning("[SimulationSceneManager] No active event to reset!");
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[SimulationSceneManager] Resetting {currentEvent.GetEventName()}");
        }

        currentEvent.ResetEvent();

        // Return player to monitoring scene (using GameManager's system)
        ReturnPlayerToMonitoringScene();

        // Clear current event reference
        currentEvent = null;
        currentEventIndex = -1;
    }

    /// <summary>
    /// Get the current active event (null if none)
    /// </summary>
    public IEvent GetCurrentEvent()
    {
        return currentEvent;
    }

    /// <summary>
    /// Check if any event is currently running
    /// </summary>
    public bool IsEventActive()
    {
        return currentEvent != null && currentEvent.GetState() != EventState.Idle;
    }

    /// <summary>
    /// Get current event state for UI display
    /// </summary>
    public EventState GetCurrentEventState()
    {
        return currentEvent != null ? currentEvent.GetState() : EventState.Idle;
    }

    /// <summary>
    /// Return player (XR Origin) to monitoring scene spawn point
    /// (called during event reset)
    /// Uses GameManager's ReturnToMonitoring system for proper state management
    /// </summary>
    public void ReturnPlayerToMonitoringScene()
    {
        // Option 1: Use GameManager's ReturnToMonitoring (recommended - handles all state properly)
        if (Project.GameManager.Instance != null)
        {
            Project.GameManager.Instance.ReturnToMonitoring(Project.ReturnFlag.Interrupt);

            if (enableDebugLogs)
            {
                Debug.Log($"[SimulationSceneManager] Player returned to monitoring scene via GameManager");
            }
            return;
        }

        // Option 2: Legacy fallback for SmoothCameraFollow (if still using old camera system)
        if (mainCamera != null)
        {
            if (monitoringSpawnPoint == null)
            {
                Debug.LogWarning("[SimulationSceneManager] Cannot return camera - monitoringSpawnPoint not assigned!");
                return;
            }

            // Detach camera from robot
            mainCamera.DetachFromTarget();

            // Move camera to monitoring spawn point
            mainCamera.transform.position = monitoringSpawnPoint.position;
            mainCamera.transform.rotation = monitoringSpawnPoint.rotation;

            if (enableDebugLogs)
            {
                Debug.Log($"[SimulationSceneManager] Legacy camera returned to monitoring scene at {monitoringSpawnPoint.position}");
            }
            return;
        }

        Debug.LogWarning("[SimulationSceneManager] Cannot return player - no GameManager or mainCamera configured!");
    }
}
