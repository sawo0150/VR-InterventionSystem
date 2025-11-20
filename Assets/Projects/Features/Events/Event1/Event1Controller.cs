using UnityEngine;

/// <summary>
/// Controller for Event 1 - Slope/Tree Fall Event
/// Manages event lifecycle and robot control
/// </summary>
public class Event1Controller : MonoBehaviour, IEvent
{
    [Header("Robot Settings")]
    [Tooltip("The robot GameObject for this event")]
    public GameObject robot;
    [Tooltip("Robot's autonomous navigation component (optional)")]
    public MonoBehaviour autonomousNavigation;
    [Tooltip("Event location where robot should navigate to")]
    public Transform eventLocation;

    [Header("Event Components")]
    [Tooltip("Start trigger zone")]
    public EventStartTrigger startTrigger;
    [Tooltip("End trigger zone")]
    public EventEndTrigger endTrigger;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotWheelController robotController;

    void Start()
    {
        // Get robot controller reference
        if (robot != null)
        {
            robotController = robot.GetComponent<RobotWheelController>();
        }

        // Validate setup
        ValidateSetup();
    }

    void ValidateSetup()
    {
        if (robot == null)
        {
            Debug.LogError("[Event1Controller] Robot not assigned!");
        }

        if (robotController == null)
        {
            Debug.LogError("[Event1Controller] Robot missing RobotWheelController component!");
        }

        if (eventLocation == null)
        {
            Debug.LogError("[Event1Controller] Event location not assigned!");
        }

        if (startTrigger == null)
        {
            Debug.LogWarning("[Event1Controller] Start trigger not assigned!");
        }

        if (endTrigger == null)
        {
            Debug.LogWarning("[Event1Controller] End trigger not assigned!");
        }
    }

    #region IEvent Implementation

    public void InitializeEvent()
    {
        if (currentState != EventState.Idle)
        {
            Debug.LogWarning($"[Event1Controller] Cannot initialize - current state: {currentState}");
            return;
        }

        currentState = EventState.Initializing;

        if (enableDebugLogs)
        {
            Debug.Log("[Event1Controller] Initializing - robot navigating to event location");
        }

        // Disable player control
        if (robotController != null)
        {
            robotController.enabled = false;
        }

        // Enable autonomous navigation to event location
        EnableAutonomousNavigation();
    }

    public void StartEvent()
    {
        if (currentState != EventState.Initializing)
        {
            Debug.LogWarning($"[Event1Controller] Cannot start - current state: {currentState}");
            return;
        }

        currentState = EventState.Active;

        if (enableDebugLogs)
        {
            Debug.Log("[Event1Controller] Event started - player has control");
        }

        // Disable autonomous navigation
        DisableAutonomousNavigation();

        // Give player control
        if (robotController != null)
        {
            robotController.enabled = true;
        }

        // Obstacles are activated by EventStartTrigger
    }

    public void ResetEvent()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[Event1Controller] Resetting event");
        }

        currentState = EventState.Idle;

        // Disable player control
        if (robotController != null)
        {
            robotController.enabled = false;
        }

        // Re-enable autonomous navigation (robot returns to idle state)
        EnableAutonomousNavigation();

        // Reset triggers
        if (startTrigger != null)
        {
            startTrigger.ResetEvent();
        }

        // TODO: Move robot back to spawn point or idle position
        // TODO: Return player camera to monitoring scene
    }

    public EventState GetState()
    {
        return currentState;
    }

    public string GetEventName()
    {
        return "Event 1";
    }

    #endregion

    #region Autonomous Navigation

    void EnableAutonomousNavigation()
    {
        if (autonomousNavigation != null)
        {
            autonomousNavigation.enabled = true;

            // TODO: Tell autonomous navigation to navigate to eventLocation
            // Example: autonomousNavigation.SetDestination(eventLocation.position);

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Autonomous navigation enabled");
            }
        }
        else
        {
            // Fallback: Directly teleport robot to event location for testing
            if (robot != null && eventLocation != null)
            {
                robot.transform.position = eventLocation.position;
                robot.transform.rotation = eventLocation.rotation;

                if (enableDebugLogs)
                {
                    Debug.Log("[Event1Controller] No autonomous navigation - teleported robot to event location");
                }

                // Automatically trigger Start() after teleport for testing
                // Remove this in production when you have real autonomous navigation
                Invoke(nameof(SimulateArrival), 1f);
            }
        }
    }

    void DisableAutonomousNavigation()
    {
        if (autonomousNavigation != null)
        {
            autonomousNavigation.enabled = false;

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Autonomous navigation disabled");
            }
        }
    }

    // TEMPORARY: Simulates robot arrival for testing without autonomous navigation
    void SimulateArrival()
    {
        if (currentState == EventState.Initializing)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Robot arrived at event location (simulated)");
            }

            // Notify SimulationSceneManager
            if (SimulationSceneManager.Instance != null)
            {
                SimulationSceneManager.Instance.OnEventLocationReached();
            }
        }
    }

    #endregion
}
