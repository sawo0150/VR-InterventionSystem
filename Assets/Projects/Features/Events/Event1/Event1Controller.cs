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
    [Tooltip("Panel prefab shown when hit by deer")]
    public UIMessagePanel deerRespawnPanelPrefab;
    [Tooltip("Panel prefab shown when hit by rolling stone")]
    public UIMessagePanel stoneRespawnPanelPrefab;
    [Tooltip("How long panels stay visible before auto-fading (in seconds)")]
    [Range(1f, 10f)]
    public float panelDisplayDuration = 3f;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotNavMeshController robotController;
    private NavMeshAgent navMeshAgent;
    private UIMessagePanel deerRespawnPanelInstance;
    private UIMessagePanel stoneRespawnPanelInstance;
    private Coroutine autohidePanelCoroutine;

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

        // Instantiate respawn panels
        InstantiateRespawnPanels();

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

    #region Respawn System

    /// <summary>
    /// Instantiate respawn panel prefabs as children of PlayerUIManager's canvas
    /// </summary>
    void InstantiateRespawnPanels()
    {
        if (PlayerUIManager.Instance == null)
        {
            Debug.LogWarning("[Event1Controller] PlayerUIManager not found - respawn panels will not be created");
            return;
        }

        Canvas worldSpaceCanvas = PlayerUIManager.Instance.GetComponentInChildren<Canvas>();
        if (worldSpaceCanvas == null)
        {
            Debug.LogError("[Event1Controller] World Space Canvas not found in PlayerUIManager!");
            return;
        }

        // Instantiate deer respawn panel
        if (deerRespawnPanelPrefab != null)
        {
            deerRespawnPanelInstance = Instantiate(deerRespawnPanelPrefab, worldSpaceCanvas.transform);
            deerRespawnPanelInstance.gameObject.SetActive(false);
            deerRespawnPanelInstance.name = "DeerRespawnPanel";

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Created deer respawn panel");
            }
        }

        // Instantiate stone respawn panel
        if (stoneRespawnPanelPrefab != null)
        {
            stoneRespawnPanelInstance = Instantiate(stoneRespawnPanelPrefab, worldSpaceCanvas.transform);
            stoneRespawnPanelInstance.gameObject.SetActive(false);
            stoneRespawnPanelInstance.name = "StoneRespawnPanel";

            if (enableDebugLogs)
            {
                Debug.Log("[Event1Controller] Created stone respawn panel");
            }
        }
    }

    /// <summary>
    /// Respawns the robot to the appropriate location based on obstacle type
    /// Shows corresponding panel and auto-hides it after duration
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
        UIMessagePanel panelToShow = null;

        // Determine respawn point and panel based on obstacle type
        switch (obstacleType)
        {
            case ObstacleType.Deer:
                respawnPoint = deerRespawnPoint;
                panelToShow = deerRespawnPanelInstance;
                break;

            case ObstacleType.RollingStone:
                respawnPoint = stoneRespawnPoint;
                panelToShow = stoneRespawnPanelInstance;
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

        // Show respawn panel
        if (panelToShow != null)
        {
            // Hide any currently visible panel
            HideAllRespawnPanels();

            // Show the appropriate panel
            panelToShow.Show("");

            // Start auto-hide coroutine
            if (autohidePanelCoroutine != null)
            {
                StopCoroutine(autohidePanelCoroutine);
            }
            autohidePanelCoroutine = StartCoroutine(AutoHidePanelAfterDelay(panelToShow, panelDisplayDuration));

            if (enableDebugLogs)
            {
                Debug.Log($"[Event1Controller] Showing {obstacleType} respawn panel for {panelDisplayDuration} seconds");
            }
        }
        else
        {
            Debug.LogWarning($"[Event1Controller] No panel instance for {obstacleType} respawn!");
        }
    }

    /// <summary>
    /// Coroutine to automatically hide a panel after a delay
    /// </summary>
    IEnumerator AutoHidePanelAfterDelay(UIMessagePanel panel, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (panel != null && panel.IsVisible())
        {
            panel.Hide();

            if (enableDebugLogs)
            {
                Debug.Log($"[Event1Controller] Auto-hiding respawn panel");
            }
        }
    }

    /// <summary>
    /// Hide all respawn panels
    /// </summary>
    void HideAllRespawnPanels()
    {
        if (deerRespawnPanelInstance != null && deerRespawnPanelInstance.IsVisible())
        {
            deerRespawnPanelInstance.Hide();
        }

        if (stoneRespawnPanelInstance != null && stoneRespawnPanelInstance.IsVisible())
        {
            stoneRespawnPanelInstance.Hide();
        }
    }

    #endregion
}
