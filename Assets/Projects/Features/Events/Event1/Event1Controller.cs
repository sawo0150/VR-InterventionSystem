using UnityEngine;
using Project;

/// <summary>
/// Controller for Event 1 - Slope/Tree Fall Event
/// Manages event lifecycle and robot control
/// </summary>
public class Event1Controller : MonoBehaviour, IEvent
{
    [Header("Robot Settings")]
    [Tooltip("The robot GameObject for this event")]
    public GameObject robot;
    [Tooltip("Robot's waypoint follower component for autonomous navigation")]
    public RobotWaypointFollower autonomousNavigation;
    [Tooltip("Event location where robot should navigate to")]
    public Transform eventLocation;

    [Header("Event Components")]
    [Tooltip("Start trigger zone")]
    public Event1StartTrigger startTrigger;
    [Tooltip("End trigger zone")]
    public Event1EndTrigger endTrigger;

    [Header("Obstacle References")]
    [Tooltip("Deer jumping obstacles")]
    public JumpBetweenPoints[] deerObstacles;
    [Tooltip("Rolling sphere spawner")]
    public RollingObstacleSpawner rollingObstacle;
    [Tooltip("Clouds particle system")]
    public ParticleSystem cloudsParticleSystem;

    [Header("Event Boundaries")]
    [Tooltip("Event boundary colliders - robot must stay within ANY of these boundaries")]
    public EventBoundary[] eventBoundaries;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotNavMeshController robotController;

    void Start()
    {
        // Get robot controller reference
        if (robot != null)
        {
            robotController = robot.GetComponent<RobotNavMeshController>();

            // Disable manual control at start (robot will use autonomous navigation)
            if (robotController != null)
            {
                robotController.enabled = false;
            }
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
            Debug.LogError("[Event1Controller] Robot missing RobotNavMeshController component!");
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

        // Start all deer obstacles
        if (deerObstacles != null && deerObstacles.Length > 0)
        {
            foreach (JumpBetweenPoints deer in deerObstacles)
            {
                if (deer != null)
                {
                    deer.StartJumping();

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[Event1Controller] Started deer obstacle: {deer.gameObject.name}");
                    }
                }
            }
        }

        // Start rolling obstacle spawner
        if (rollingObstacle != null)
        {
            rollingObstacle.StartSpawning();

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Started rolling obstacle spawner");
            }
        }

        // Start clouds particle system
        if (cloudsParticleSystem != null)
        {
            cloudsParticleSystem.Play();

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Started clouds particle system");
            }
        }
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

        // Move robot back to waypoint 0 and resume patrol
        if (autonomousNavigation != null)
        {
            autonomousNavigation.ResetToWaypointZero();

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Robot reset to waypoint 0");
            }
        }

        // Reset triggers
        if (startTrigger != null)
        {
            startTrigger.ResetEvent();
        }

        // Stop and reset all deer obstacles
        if (deerObstacles != null && deerObstacles.Length > 0)
        {
            foreach (JumpBetweenPoints deer in deerObstacles)
            {
                if (deer != null)
                {
                    deer.StopJumping();
                    deer.ResetToWaitingPoint();

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[Event1Controller] Reset deer obstacle: {deer.gameObject.name}");
                    }
                }
            }
        }

        // Stop rolling obstacle spawner
        if (rollingObstacle != null)
        {
            rollingObstacle.StopSpawning();

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Stopped rolling obstacle spawner");
            }
        }

        // Stop clouds particle system
        if (cloudsParticleSystem != null)
        {
            cloudsParticleSystem.Stop();
            cloudsParticleSystem.Clear(); // Remove existing particles

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Stopped clouds particle system");
            }
        }
    }

    public EventState GetState()
    {
        return currentState;
    }

    public string GetEventName()
    {
        return "Event 1";
    }

    public EventBoundary[] GetEventBoundaries()
    {
        return eventBoundaries;
    }

    #endregion

    #region Autonomous Navigation

    void EnableAutonomousNavigation()
    {
        if (autonomousNavigation != null && eventLocation != null)
        {
            // Tell robot to navigate to event location
            autonomousNavigation.NavigateToEvent(eventLocation.position);

            if (enableDebugLogs)
            {
                Debug.Log($"[Event1Controller] Robot navigating to event location at {eventLocation.position}");
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
                    Debug.LogWarning("[Event1Controller] No autonomous navigation assigned - teleported robot to event location");
                }

                // Automatically trigger Start() after teleport for testing
                Invoke(nameof(SimulateArrival), 1f);
            }
        }
    }

    void DisableAutonomousNavigation()
    {
        // Robot has arrived at event, no need to disable anything
        if (enableDebugLogs)
        {
            Debug.Log("[Event1Controller] Robot at event location");
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
