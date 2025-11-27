using UnityEngine;
using Project;


namespace Proejct
{
    public class Event2Controller : MonoBehaviour, IEvent
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
        public Event2StartTrigger startTrigger;
        [Tooltip("End trigger zone")]
        public Event2EndTrigger endTrigger;

        [Header("Obstacle References")]

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
        }

        public EventState GetState()
        {
            return currentState;
        }

        public string GetEventName()
        {
            return "Event 2";
        }
        
        

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
    }
}