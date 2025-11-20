using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobotWaypointFollower : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [Tooltip("Waypoints for patrol route")]
    public Transform[] waypoints;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool enableDebugLogs = false;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;

    // Navigation state
    private enum NavigationState
    {
        PatrollingWaypoints,  // Normal loop behavior
        NavigatingToEvent,    // Moving to event location
        AtEvent              // Reached event, waiting
    }

    private NavigationState currentState = NavigationState.PatrollingWaypoints;
    private Vector3 eventDestination;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

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
    /// Navigate to event location (called by EventController.InitializeEvent)
    /// </summary>
    public void NavigateToEvent(Vector3 eventLocation)
    {
        currentState = NavigationState.NavigatingToEvent;
        eventDestination = eventLocation;
        agent.destination = eventLocation;

        if (enableDebugLogs)
        {
            Debug.Log($"[RobotWaypointFollower] {gameObject.name} navigating to event at {eventLocation}");
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
}
