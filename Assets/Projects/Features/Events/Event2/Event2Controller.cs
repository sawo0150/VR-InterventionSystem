using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Project;

/// <summary>
/// Controller for Event 2 - Tree Fall Event
/// Manages event lifecycle, robot control, and respawn system
/// </summary>
public class Event2Controller : MonoBehaviour, IEvent
{
    [Header("Robot Settings")]
    [Tooltip("The robot GameObject for this event")]
    public GameObject robot;
    [Tooltip("Robot's waypoint follower component for autonomous navigation")]
    public RobotWaypointFollower autonomousNavigation;

    [Tooltip("Event location where robot should navigate to")]
    public Transform eventLocation;

    public Transform eventWaypoint1;
    
    [Header("Event Components")]
    [Tooltip("Start trigger zone")]
    public Event2StartTrigger startTrigger;
    [Tooltip("End trigger zone")]
    public Event2EndTrigger endTrigger;
    

    [Header("Event Boundaries")]
    [Tooltip("Event boundary colliders - robot must stay within ANY of these boundaries")]
    public EventBoundary[] eventBoundaries;


    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotNavMeshController robotController;
    private NavMeshAgent navMeshAgent;
    
    private Coroutine initializationCoroutine;

    private void Start()
    {
        // Get robot controller reference
        if (robot != null)
        {
            robotController = robot.GetComponent<RobotNavMeshController>();
            navMeshAgent = robot.GetComponent<NavMeshAgent>();

            // Disable manual control at start (robot will use autonomous navigation)
            if (robotController != null)
            {
                robotController.enabled = false;
            }
        }

        // Validate setup
        ValidateSetup();
    }

    private void ValidateSetup()
    {
        if (robot == null)
        {
            Debug.LogError("[Event2Controller] Robot not assigned!");
        }

        if (robotController == null)
        {
            Debug.LogError("[Event2Controller] Robot missing RobotNavMeshController component!");
        }

        if (eventLocation == null)
        {
            Debug.LogError("[Event2Controller] Event location not assigned!");
        }

        if (startTrigger == null)
        {
            Debug.LogWarning("[Event2Controller] Start trigger not assigned!");
        }

        if (endTrigger == null)
        {
            Debug.LogWarning("[Event2Controller] End trigger not assigned!");
        }
    }

    #region IEvent Implementation

    public void InitializeEvent()
    {
        if (currentState != EventState.Idle)
        {
            Debug.LogWarning($"[Event2Controller] Cannot initialize - current state: {currentState}");
            return;
        }

        currentState = EventState.Initializing;

        if (enableDebugLogs)
        {
            Debug.Log("[Event2Controller] Initializing - robot navigating to event location");
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
            Debug.LogWarning($"[Event2Controller] Cannot start - current state: {currentState}");
            return;
        }

        currentState = EventState.Active;

        if (enableDebugLogs)
        {
            Debug.Log("[Event2Controller] Event started - player has control");
        }

        // Disable autonomous navigation
        DisableAutonomousNavigation();

        // Give player control
        if (robotController != null)
        {
            robotController.enabled = true;
        }
    }

    public void ResetEvent()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[Event2Controller] Resetting event");
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
                Debug.Log("[Event2Controller] Robot reset to waypoint 0");
            }
        }

        // Reset triggers
        if (startTrigger != null)
        {
            startTrigger.ResetEvent();
        }
    }

    public EventState GetState()
    {
        return currentState;
    }

    public string GetEventName()
    {
        return "Event 2";
    }

    public EventBoundary[] GetEventBoundaries()
    {
        return eventBoundaries;
    }

    #endregion

    #region Autonomous Navigation

    private void EnableAutonomousNavigation()
    {
        if (initializationCoroutine != null) StopCoroutine(initializationCoroutine);
        initializationCoroutine = StartCoroutine(MoveToEventSequence());
    }
    
    
    private IEnumerator MoveToEventSequence()
    {
        // 1. Move to intermediate waypoint
        if (autonomousNavigation != null && eventWaypoint1 != null)
        {
            autonomousNavigation.NavigateToEvent(eventWaypoint1.position);
            
            if (enableDebugLogs) Debug.Log($"[Event2Controller] Moving to Intermediate Waypoint: {eventWaypoint1.name}");
            yield return new WaitUntil(() => autonomousNavigation.IsAtEvent());
            
            yield return new WaitForSeconds(0.5f);
        }

        // 2. Move to EventLocation
        if (navMeshAgent != null && eventLocation != null)
        {
            if (enableDebugLogs) Debug.Log($"[Event2Controller] Moving to Final Location: {eventLocation.name}");
            
            navMeshAgent.SetDestination(eventLocation.position);
            
            navMeshAgent.isStopped = false; 
            
            while (navMeshAgent.pathPending || navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
            {
                yield return null;
            }
        }

        if (enableDebugLogs) Debug.Log("[Event2Controller] Arrived at Event Location.");

        SimulateArrival();
        
        initializationCoroutine = null;
    }
    
    void DisableAutonomousNavigation()
    {
        // Robot has arrived at event, no need to disable anything
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.isStopped = true;
        }

        if (enableDebugLogs)
        {
            Debug.Log("[Event2Controller] Autonomous navigation finished/disabled");
        }
    }

    // TEMPORARY: Simulates robot arrival for testing without autonomous navigation
    void SimulateArrival()
    {
        if (currentState == EventState.Initializing)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Event2Controller] Robot arrived at event location (simulated)");
            }

            // Notify SimulationSceneManager
            if (SimulationSceneManager.Instance != null)
            {
                SimulationSceneManager.Instance.OnEventLocationReached();
            }
        }
    }

    #endregion
    
    #region Respawn System
    public void RespawnRobot()
    {
        if (currentState != EventState.Active) return;
        
        var respawnTarget = eventLocation; 

        if (respawnTarget != null && navMeshAgent != null)
        {
            navMeshAgent.Warp(respawnTarget.position);
            robot.transform.rotation = respawnTarget.rotation;

            var rb = robot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[Event2Controller] Robot respawned at {respawnTarget.name}");
            }
        }
        else
        {
            Debug.LogWarning("[Event2Controller] Cannot respawn - NavMeshAgent or StartPoint2 is missing.");
        }
    }

    #endregion
}
