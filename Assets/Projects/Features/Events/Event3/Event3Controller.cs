using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using Project;
using VRInterventionSystem.Audio;

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

    [Header("Audio")]
    [Tooltip("AudioSource for children ambient sound (assign AudioSource on Robot3)")]
    public AudioSource childrenAmbientAudioSource;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private EventState currentState = EventState.Idle;
    private RobotNavMeshController robotController;
    private NavMeshAgent navMeshAgent;
    private bool wasPlayerSeated = false;

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

        // Initialize children ambient audio
        InitializeChildrenAudio();

        // Validate setup
        ValidateSetup();
    }

    /// <summary>
    /// Initialize AudioSource with settings from AudioConfig for children ambient sound
    /// Creates a dedicated AudioSource for children sound to avoid conflicts with engine sound
    /// </summary>
    void InitializeChildrenAudio()
    {
        if (SoundManager.Instance == null)
        {
            Debug.LogWarning("[Event3Controller] SoundManager.Instance is null - cannot initialize!");
            return;
        }

        var config = SoundManager.Instance.GetAudioConfig();
        if (config == null)
        {
            Debug.LogWarning("[Event3Controller] AudioConfig is null - cannot initialize!");
            return;
        }

        // ALWAYS create a new dedicated AudioSource for children ambient sound
        // This ensures it never conflicts with the engine sound AudioSource
        if (robot != null)
        {
            childrenAmbientAudioSource = robot.AddComponent<AudioSource>();
            Debug.Log("[Event3Controller] Created dedicated AudioSource for children ambient sound");
        }

        if (childrenAmbientAudioSource == null)
        {
            Debug.LogWarning("[Event3Controller] childrenAmbientAudioSource is null - cannot initialize!");
            return;
        }

        // Configure the AudioSource for children ambient sound
        childrenAmbientAudioSource.clip = config.childrenAmbientLoop;
        childrenAmbientAudioSource.loop = true;
        childrenAmbientAudioSource.playOnAwake = false;
        childrenAmbientAudioSource.volume = config.childrenAmbientVolume;
        childrenAmbientAudioSource.spatialBlend = config.childrenSpatialBlend;

        Debug.Log($"[Event3Controller] Children audio initialized - clip: {config.childrenAmbientLoop != null}, clip name: {config.childrenAmbientLoop?.name}, volume: {config.childrenAmbientVolume}, spatialBlend: {config.childrenSpatialBlend}");
    }

    void Update()
    {
        // Only manage audio when event is active
        if (currentState == EventState.Active)
        {
            UpdateChildrenAmbientSound();
        }
    }

    void UpdateChildrenAmbientSound()
    {
        if (childrenAmbientAudioSource == null || GameManager.Instance == null) return;

        bool isPlayerControlling = GameManager.Instance.GetPlayerState() == PlayerState.ControllingMode;

        // Check if the player is controlling THIS robot (Event 3's robot), not another robot
        bool isControllingThisRobot = false;
        if (isPlayerControlling)
        {
            Transform currentRobot = GameManager.Instance.GetCurrentRobotTransform();
            if (currentRobot != null && robot != null && currentRobot.gameObject == robot)
            {
                isControllingThisRobot = true;
            }
        }

        // Debug logging every 60 frames
        if (enableDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[Event3Controller] State check - isControllingThisRobot: {isControllingThisRobot}, wasPlayerSeated: {wasPlayerSeated}, audioPlaying: {childrenAmbientAudioSource.isPlaying}");
        }

        // Player just boarded THIS robot - start sound
        if (isControllingThisRobot && !wasPlayerSeated)
        {
            if (childrenAmbientAudioSource.clip != null && !childrenAmbientAudioSource.isPlaying)
            {
                childrenAmbientAudioSource.Play();
                Debug.Log("[Event3Controller] Player boarded Event 3 robot - started children ambient sound");
            }
        }
        // Player just left THIS robot - stop sound
        else if (!isControllingThisRobot && wasPlayerSeated)
        {
            if (childrenAmbientAudioSource.isPlaying)
            {
                childrenAmbientAudioSource.Stop();
                Debug.Log("[Event3Controller] Player left Event 3 robot - stopped children ambient sound");
            }
            else
            {
                Debug.Log("[Event3Controller] Player left Event 3 robot - but sound was not playing");
            }
        }
        // Safety check: If sound is playing but player is not controlling this robot, stop it
        else if (!isControllingThisRobot && childrenAmbientAudioSource.isPlaying)
        {
            childrenAmbientAudioSource.Stop();
            Debug.Log("[Event3Controller] Safety stop - sound playing but not controlling Event 3 robot");
        }

        wasPlayerSeated = isControllingThisRobot;
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

        if (childrenAmbientAudioSource == null)
        {
            Debug.LogWarning("[Event3Controller] Children ambient AudioSource not assigned - please assign AudioSource on Robot3!");
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

        // Show event goal icon on minimap
        if (MinimapButtonManager.Instance != null)
        {
            MinimapButtonManager.Instance.ShowEventGoal(3);
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

        // Initialize wasPlayerSeated and handle sound if already controlling THIS robot
        if (GameManager.Instance != null)
        {
            bool isCurrentlyControlling = GameManager.Instance.GetPlayerState() == PlayerState.ControllingMode;

            // Check if player is controlling THIS robot (Event 3's robot)
            bool isControllingThisRobot = false;
            if (isCurrentlyControlling)
            {
                Transform currentRobot = GameManager.Instance.GetCurrentRobotTransform();
                if (currentRobot != null && robot != null && currentRobot.gameObject == robot)
                {
                    isControllingThisRobot = true;
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[Event3Controller] Event started - isControllingThisRobot: {isControllingThisRobot}");
            }

            // If player is already controlling THIS robot when event starts, start the sound
            if (isControllingThisRobot && childrenAmbientAudioSource != null &&
                childrenAmbientAudioSource.clip != null && !childrenAmbientAudioSource.isPlaying)
            {
                childrenAmbientAudioSource.Play();
                if (enableDebugLogs)
                {
                    Debug.Log("[Event3Controller] Player already controlling Event 3 robot - started children ambient sound");
                }
            }

            wasPlayerSeated = isControllingThisRobot;
        }
        else
        {
            wasPlayerSeated = false;
        }

        if (enableDebugLogs)
        {
            Debug.Log("[Event3Controller] Event started - player has control");
        }

        // Disable autonomous navigation
        DisableAutonomousNavigation();

        // NOTE: Do NOT enable robotController here - GameManager handles this in OnEventActivated()
        // based on whether player is currently boarded on the robot

        // Sound will start automatically when player sits in robot (handled by Update)

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

        // Hide event goal icon on minimap
        if (MinimapButtonManager.Instance != null)
        {
            MinimapButtonManager.Instance.HideEventGoal(3);
        }

        // Disable player control
        if (robotController != null)
        {
            robotController.enabled = false;
        }

        // Stop children ambient sound
        if (childrenAmbientAudioSource != null && childrenAmbientAudioSource.isPlaying)
        {
            childrenAmbientAudioSource.Stop();

            if (enableDebugLogs)
            {
                Debug.Log("[Event3Controller] Stopped children ambient sound");
            }
        }
        wasPlayerSeated = false;

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
        // Robot has arrived at event, stop autonomous navigation and prepare for manual control
        if (navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
        {
            navMeshAgent.ResetPath(); // Clear the autonomous path
            navMeshAgent.isStopped = false; // Must be false for manual control to work
            navMeshAgent.velocity = Vector3.zero; // Stop current movement
        }

        if (enableDebugLogs)
        {
            Debug.Log("[Event3Controller] Autonomous navigation finished/disabled - ready for manual control");
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