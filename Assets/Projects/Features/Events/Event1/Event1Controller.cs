using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Project;

/// <summary>
/// Controller for Event 1 - Slope/Tree Fall Event
/// Manages event lifecycle, robot control, and respawn system
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

    [Header("Respawn System")]
    [Tooltip("Respawn point for deer collision")]
    public Transform deerRespawnPoint;
    [Tooltip("Respawn point for rolling stone collision")]
    public Transform stoneRespawnPoint;

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

        // Show event goal icon on minimap
        if (MinimapButtonManager.Instance != null)
        {
            MinimapButtonManager.Instance.ShowEventGoal(1);
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

        // NOTE: Do NOT enable robotController here - GameManager handles this in OnEventActivated()
        // based on whether player is currently boarded on the robot

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

        // Hide event goal icon on minimap
        if (MinimapButtonManager.Instance != null)
        {
            MinimapButtonManager.Instance.HideEventGoal(1);
        }

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
                    deer.ResetToPointA();

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
        // Robot has arrived at event, stop autonomous navigation and prepare for manual control
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.ResetPath(); // Clear the autonomous path
            navMeshAgent.isStopped = false; // Must be false for manual control to work
            navMeshAgent.velocity = Vector3.zero; // Stop current movement
        }

        if (enableDebugLogs)
        {
            Debug.Log("[Event1Controller] Autonomous navigation finished/disabled - ready for manual control");
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

    #region Respawn System

    /// <summary>
    /// Respawns the robot to the appropriate location based on obstacle type
    /// Shows corresponding panel via PlayerUIManager and auto-hides it
    /// </summary>
    public void RespawnRobot(ObstacleType obstacleType)
    {
        if (currentState != EventState.Active)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"[Event1Controller] Respawn ignored - event not active (state: {currentState})");
            }
            return;
        }

        Transform respawnPoint = null;
        UIMessageType panelType = UIMessageType.Warning; // Default fallback

        // Determine respawn point and panel type based on obstacle type
        switch (obstacleType)
        {
            case ObstacleType.Deer:
                respawnPoint = deerRespawnPoint;
                panelType = UIMessageType.DeerRespawn;
                break;

            case ObstacleType.RollingStone:
                respawnPoint = stoneRespawnPoint;
                panelType = UIMessageType.StoneRespawn;
                break;
        }

        // Validate respawn point
        if (respawnPoint == null)
        {
            Debug.LogError($"[Event1Controller] Respawn point not assigned for {obstacleType}!");
            return;
        }

        // Warp robot to respawn point
        if (navMeshAgent != null)
        {
            navMeshAgent.Warp(respawnPoint.position);
            robot.transform.rotation = respawnPoint.rotation;

            if (enableDebugLogs)
            {
                Debug.Log($"[Event1Controller] Robot respawned to {obstacleType} respawn point at {respawnPoint.position}");
            }
        }
        else
        {
            Debug.LogError("[Event1Controller] NavMeshAgent not found on robot - cannot warp!");
        }

        // Show respawn panel via PlayerUIManager with auto-hide (3 seconds)
        if (PlayerUIManager.Instance != null)
        {
            PlayerUIManager.Instance.ShowMessage(panelType, "", 3f);

            if (enableDebugLogs)
            {
                Debug.Log($"[Event1Controller] Showing {obstacleType} respawn panel for 3 seconds");
            }
        }
        else
        {
            Debug.LogWarning("[Event1Controller] PlayerUIManager not found - cannot show respawn panel");
        }
    }

    #endregion
}
