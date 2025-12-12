using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Project;

/// <summary>
/// Controller for Event 3 - Children Event
/// Manages event lifecycle, robot control, and respawn system
/// </summary>
public class Event3Controller : MonoBehaviour, IEvent
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
    public Event3StartTrigger startTrigger;
    [Tooltip("End trigger zone")]
    public Event3EndTrigger endTrigger;

    [Header("Obstacle References")]
    [Tooltip("Children crossing roads")]
    public ChildrenCrossingRoad[] ChildrenObstacles;

    [Header("Event Boundaries")]
    [Tooltip("Event boundary colliders - robot must stay within ANY of these boundaries")]
    public EventBoundary[] eventBoundaries;

    [Header("Respawn System")]
    [Tooltip("Respawn point for Children collision from first road")]
    public Transform ChildrenRespawnPoint1;
    [Tooltip("Respawn point for Children collision from second road")]
    public Transform ChildrenRespawnPoint2;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotNavMeshController robotController;
    private NavMeshAgent navMeshAgent;

    void Start()
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

    void ValidateSetup()
    {
        if (robot == null)
        {
            Debug.LogError("[Event3Controller] Robot not assigned!");
        }

        if (robotController == null)
        {
            Debug.LogError("[Event3Controller] Robot missing RobotNavMeshController component!");
        }

        if (eventLocation == null)
        {
            Debug.LogError("[Event3Controller] Event location not assigned!");
        }

        if (startTrigger == null)
        {
            Debug.LogWarning("[Event3Controller] Start trigger not assigned!");
        }

        if (endTrigger == null)
        {
            Debug.LogWarning("[Event3Controller] End trigger not assigned!");
        }
    }

    #region IEvent Implementation

    public void InitializeEvent()
    {
        if (currentState != EventState.Idle)
        {
            Debug.LogWarning($"[Event3Controller] Cannot initialize - current state: {currentState}");
            return;
        }

        currentState = EventState.Initializing;

        if (enableDebugLogs)
        {
            Debug.Log("[Event3Controller] Initializing - robot navigating to event location");
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
            Debug.LogWarning($"[Event3Controller] Cannot start - current state: {currentState}");
            return;
        }

        currentState = EventState.Active;

        if (enableDebugLogs)
        {
            Debug.Log("[Event3Controller] Event started - player has control");
        }

        // Disable autonomous navigation
        DisableAutonomousNavigation();

        // Give player control
        if (robotController != null)
        {
            robotController.enabled = true;
        }

        // Start all Children obstacles
        if (ChildrenObstacles != null && ChildrenObstacles.Length > 0)
        {
            foreach (ChildrenCrossingRoad Children in ChildrenObstacles)
            {
                if (Children != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[Event3Controller] Started Children obstacle: {Children.gameObject.name}");
                    }
                }
            }
        }
    }

    public void ResetEvent()
    {
        if (enableDebugLogs)
        {
            Debug.Log("[Event3Controller] Resetting event");
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
                Debug.Log("[Event3Controller] Robot reset to waypoint 0");
            }
        }

        // Reset triggers
        if (startTrigger != null)
        {
            startTrigger.ResetEvent();
        }

        // Stop and reset all Children obstacles
        if (ChildrenObstacles != null && ChildrenObstacles.Length > 0)
        {
            foreach (ChildrenCrossingRoad Children in ChildrenObstacles)
            {
                if (Children != null)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[Event3Controller] Reset Children obstacle: {Children.gameObject.name}");
                    }
                }
            }
        }
    }

    public EventState GetState()
    {
        return currentState;
    }

    public string GetEventName()
    {
        return "Event 3";
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
                Debug.Log($"[Event3Controller] Robot navigating to event location at {eventLocation.position}");
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
                    Debug.LogWarning("[Event3controller] No autonomous navigation assigned - teleported robot to event location");
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
            Debug.Log("[Event3Controller] Robot at event location");
        }
    }

    // TEMPORARY: Simulates robot arrival for testing without autonomous navigation
    void SimulateArrival()
    {
        if (currentState == EventState.Initializing)
        {
            if (enableDebugLogs)
            {
                Debug.Log("[Event3Controller] Robot arrived at event location (simulated)");
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

    /// <summary>
    /// Respawns the robot to the appropriate location based on obstacle type
    /// Shows corresponding panel via PlayerUIManager and auto-hides it
    /// </summary>
public void RespawnRobot3(ObstacleType obstacleType)
{
    Debug.Log($"[Event3Controller] RespawnRobot3 CALLED with {obstacleType}");

    if (currentState != EventState.Active)
    {
        if (enableDebugLogs)
        { 
            Debug.LogWarning($"[Event3Controller] Respawn ignored - event not active (state: {currentState})");
        }
        return;
    }

    Transform respawnPoint = null;
    UIMessageType panelType = UIMessageType.Warning;

    switch (obstacleType)
    {
        case ObstacleType.Children:
            Vector3 botPos = DeliveryBotPostiionChecker.position;
            Debug.Log($"[Event3Controller] botPos.x = {botPos.x}");

            if (botPos.x > -55)
            {
                Debug.Log("[Event3Controller] Using ChildrenRespawnPoint1");
                respawnPoint = ChildrenRespawnPoint1;
            }
            else if (botPos.x < -55)
            {
                Debug.Log("[Event3Controller] Using ChildrenRespawnPoint2");
                respawnPoint = ChildrenRespawnPoint2;
            }
            panelType = UIMessageType.ChildrenRespawn;
            break;
    }

    if (respawnPoint == null)
    {
        Debug.LogError($"[Event3Controller] Respawn point NOT ASSIGNED for {obstacleType}!");
        return;
    }

    if (navMeshAgent != null)
    {
        Debug.Log($"[Event3Controller] Warping to {respawnPoint.position}");
        navMeshAgent.Warp(respawnPoint.position);
        robot.transform.rotation = respawnPoint.rotation;
    }
    else
    {
        Debug.LogError("[Event3Controller] NavMeshAgent not found on robot - cannot warp!");
    }
}


    public Transform DeliveryBotPostiionChecker;

    #endregion
}