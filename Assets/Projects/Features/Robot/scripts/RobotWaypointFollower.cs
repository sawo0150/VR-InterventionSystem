using UnityEngine;
using UnityEngine.AI;
using VRInterventionSystem.Audio;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotWaypointFollower : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [Tooltip("Waypoints for patrol route")]
    public Transform[] waypoints;

    [Header("Slope Alignment")]
    [Tooltip("Enable automatic slope alignment during autonomous navigation")]
    public bool alignToSlope = true;
    [Tooltip("Visual mesh to rotate for slope alignment (leave empty to rotate this GameObject)")]
    public Transform visualMesh;
    [Tooltip("Speed of slope alignment (higher = snappier)")]
    [Range(1f, 20f)]
    public float slopeAlignmentSpeed = 8f;
    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = -1;
    [Tooltip("Max distance to check for ground")]
    [Range(0.5f, 5f)]
    public float groundCheckDistance = 2f;

    [Header("Engine Sound")]
    [Tooltip("Maximum speed for engine sound normalization")]
    public float maxSpeed = 3.5f;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = true;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private Transform rotationTarget;
    private RaycastHit lastGroundHit;
    private AudioSource engineAudioSource;

    // Navigation state
    private enum NavigationState
    {
        PatrollingWaypoints,  // Normal loop behavior
        WaitingToNavigateToEvent, // Waiting to finish loop before going to event
        NavigatingToEvent,    // Moving to event location
        AtEvent              // Reached event, waiting
    }

    private NavigationState currentState = NavigationState.PatrollingWaypoints;
    private Vector3 eventDestination;
    private bool hasReachedWaypoint0 = false; // Track if we've reached waypoint 0 during event initialization

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Configure NavMeshAgent for autonomous navigation
        agent.updateRotation = true;  // Let NavMesh handle rotation
        agent.updatePosition = true;  // Let NavMesh handle position

        // Determine which transform to rotate for slope alignment
        rotationTarget = (visualMesh != null) ? visualMesh : transform;

        // Initialize engine audio source
        InitializeEngineAudioSource();

        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"[RobotWaypointFollower] No waypoints assigned to {gameObject.name}. Robot will not patrol.");
            return;
        }

        // Start patrolling waypoints
        GoToNextWaypoint();

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWaypointFollower] {gameObject.name} started patrolling {waypoints.Length} waypoints");
        }
    }

    void Update()
    {
        if (currentState == NavigationState.PatrollingWaypoints)
        {
            // Normal waypoint patrol behavior
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                GoToNextWaypoint();
            }
        }
        else if (currentState == NavigationState.WaitingToNavigateToEvent)
        {
            // Continue waypoint patrol until we reach waypoint 0, then navigate to event
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                // Check if we just arrived at waypoint 0
                if (currentWaypointIndex == 1 && !hasReachedWaypoint0)
                {
                    // currentWaypointIndex is 1 because GoToNextWaypoint() already incremented it
                    // This means we just arrived at waypoint 0
                    hasReachedWaypoint0 = true;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[RobotWaypointFollower] Reached waypoint 0. Now navigating to event at {eventDestination}");
                    }

                    // Navigate to event
                    currentState = NavigationState.NavigatingToEvent;
                    agent.destination = eventDestination;
                }
                else
                {
                    // Continue to next waypoint
                    GoToNextWaypoint();
                }
            }
        }
        else if (currentState == NavigationState.NavigatingToEvent)
        {
            // Check if reached event location
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                currentState = NavigationState.AtEvent;

                if (enableDebugLogs)
                {
                    Debug.Log($"[RobotWaypointFollower] {gameObject.name} reached event location");
                }
            }
        }
        // If AtEvent, do nothing (wait for ResumeWaypointPatrol)

        // Always align to slope during autonomous navigation
        AlignToSlope();

        // Update engine sound based on velocity
        UpdateEngineSound();
    }
    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.destination = waypoints[currentWaypointIndex].position;

        // Move to next waypoint (loops back to 0 after reaching the end)
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    #region Event Navigation Methods

    /// <summary>
    /// Navigate to event location after completing current waypoint loop
    /// Robot continues patrol until it reaches waypoint 0, then goes to event
    /// (called by EventController.InitializeEvent)
    /// </summary>
    public void NavigateToEvent(Vector3 eventLocation)
    {
        eventDestination = eventLocation;
        hasReachedWaypoint0 = false; // Reset flag

        // If we're currently at or near waypoint 0, go immediately
        if (currentWaypointIndex == 0 && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = NavigationState.NavigatingToEvent;
            agent.destination = eventLocation;

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWaypointFollower] {gameObject.name} navigating to event at {eventLocation} (immediately)");
            }
        }
        else
        {
            // Wait until we complete the loop (reach waypoint 0)
            currentState = NavigationState.WaitingToNavigateToEvent;

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWaypointFollower] {gameObject.name} will navigate to event after reaching waypoint 0 (currently at waypoint {currentWaypointIndex})");
            }
        }
    }

    /// <summary>
    /// Return to waypoint patrol (called by EventController.ResetEvent)
    /// </summary>
    public void ResumeWaypointPatrol()
    {
        currentState = NavigationState.PatrollingWaypoints;

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWaypointFollower] {gameObject.name} resuming waypoint patrol");
        }

        // Immediately go to next waypoint
        if (waypoints.Length > 0)
        {
            GoToNextWaypoint();
        }
    }

    /// <summary>
    /// Reset robot to waypoint 0 and resume waypoint patrol
    /// (called by EventController.ResetEvent)
    /// </summary>
    public void ResetToWaypointZero()
    {
        if (waypoints.Length == 0)
        {
            Debug.LogWarning($"[RobotWaypointFollower] Cannot reset - no waypoints assigned to {gameObject.name}");
            return;
        }

        // Teleport to waypoint 0 (first point in array) using Warp for NavMeshAgent
        if (agent != null)
        {
            agent.Warp(waypoints[0].position);
            AlignToSlope(); // Align immediately after warping
        }
        else
        {
            Debug.LogError($"[RobotWaypointFollower] NavMeshAgent is null on {gameObject.name}");
            return;
        }

        // Reset state and resume patrol
        currentWaypointIndex = 0;
        currentState = NavigationState.PatrollingWaypoints;
        hasReachedWaypoint0 = false; // Reset flag

        // Clear NavMesh path
        agent.ResetPath();

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWaypointFollower] {gameObject.name} reset to waypoint 0 at {waypoints[0].position}");
        }

        // Note: Don't call GoToNextWaypoint() here - robot stays at waypoint 0
        // Normal patrol will resume from Update() when state is PatrollingWaypoints
    }

    /// <summary>
    /// Check if robot has reached event location
    /// </summary>
    public bool HasReachedEventLocation()
    {
        return currentState == NavigationState.NavigatingToEvent &&
               !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance;
    }

    /// <summary>
    /// Get current navigation state
    /// </summary>
    public bool IsAtEvent()
    {
        return currentState == NavigationState.AtEvent;
    }

    #endregion

    #region Slope Alignment

    /// <summary>
    /// Align robot rotation to match ground slope (same logic as RobotNavMeshController)
    /// </summary>
    void AlignToSlope()
    {
        if (!alignToSlope) return;

        // Cast from center and slightly above the robot
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;

        // Use RaycastAll to get all hits and filter out the robot itself
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);

        // Find the first hit that is NOT part of this robot
        RaycastHit? groundHit = null;
        foreach (RaycastHit hit in hits)
        {
            // Skip if this collider belongs to the robot itself
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
            {
                continue;
            }

            // Found a valid ground hit
            groundHit = hit;
            break;
        }

        if (groundHit.HasValue)
        {
            RaycastHit hit = groundHit.Value;
            lastGroundHit = hit;

            // Calculate the rotation that aligns the robot to the ground normal
            // Keep the current Y rotation (heading direction) from the root transform
            Vector3 forward = transform.forward;
            Vector3 right = Vector3.Cross(hit.normal, forward).normalized;
            forward = Vector3.Cross(right, hit.normal).normalized;

            Quaternion targetRotation = Quaternion.LookRotation(forward, hit.normal);

            // Smoothly interpolate to target rotation on the rotation target
            rotationTarget.rotation = Quaternion.Slerp(rotationTarget.rotation, targetRotation, slopeAlignmentSpeed * Time.deltaTime);
        }
    }

    #endregion

    #region Engine Sound

    /// <summary>
    /// Initialize AudioSource for engine sound
    /// </summary>
    void InitializeEngineAudioSource()
    {
        // Get or create AudioSource component
        engineAudioSource = GetComponent<AudioSource>();
        if (engineAudioSource == null)
        {
            engineAudioSource = gameObject.AddComponent<AudioSource>();
        }

        if (SoundManager.Instance == null)
        {
            Debug.LogWarning($"[RobotWaypointFollower] SoundManager.Instance is null on {gameObject.name}");
            return;
        }

        var config = SoundManager.Instance.GetAudioConfig();
        if (config == null)
        {
            Debug.LogWarning($"[RobotWaypointFollower] AudioConfig is null");
            return;
        }

        // Configure engine audio source
        engineAudioSource.clip = config.engineLoop;
        engineAudioSource.loop = true;
        engineAudioSource.playOnAwake = false;
        engineAudioSource.volume = config.engineBaseVolume;
        engineAudioSource.pitch = config.engineMinPitch;
        engineAudioSource.spatialBlend = config.engineSpatialBlend;
        engineAudioSource.maxDistance = config.engineMaxDistance;
        engineAudioSource.rolloffMode = AudioRolloffMode.Linear;

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWaypointFollower] Engine audio initialized on {gameObject.name}");
        }
    }

    /// <summary>
    /// Update engine sound based on NavMeshAgent velocity
    /// Always plays based on robot's movement speed
    /// </summary>
    void UpdateEngineSound()
    {
        if (engineAudioSource == null || agent == null) return;

        if (SoundManager.Instance == null) return;

        var config = SoundManager.Instance.GetAudioConfig();
        if (config == null) return;

        // Calculate speed from NavMeshAgent velocity
        float currentSpeed = agent.velocity.magnitude;
        float speedNormalized = Mathf.Clamp01(currentSpeed / maxSpeed);
        bool isMoving = currentSpeed > 0.01f;

        // Start or stop the engine sound based on movement
        if (isMoving && !engineAudioSource.isPlaying)
        {
            engineAudioSource.Play();

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWaypointFollower] Started engine sound on {gameObject.name}");
            }
        }
        else if (!isMoving && engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();

            if (enableDebugLogs)
            {
                Debug.Log($"[RobotWaypointFollower] Stopped engine sound on {gameObject.name}");
            }
        }

        // Update pitch based on speed
        if (isMoving)
        {
            float targetPitch = Mathf.Lerp(
                config.engineMinPitch,
                config.engineMaxPitch,
                speedNormalized
            );

            engineAudioSource.pitch = Mathf.Lerp(
                engineAudioSource.pitch,
                targetPitch,
                Time.deltaTime / config.enginePitchSmoothTime
            );
        }
    }

    #endregion
}
