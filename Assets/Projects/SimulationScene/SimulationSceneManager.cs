using UnityEngine;

namespace Project
{
    /// <summary>
    /// Manages the simulation scene: robot initialization and event management.
    /// Delegates player movement and event initialization to GameManager.
    /// </summary>
    public class SimulationSceneManager : MonoBehaviour
    {
        public static SimulationSceneManager Instance;

        #region Serialized Fields
        [Header("Robot Settings")]
        [Tooltip("Array of robot GameObjects in the simulation scene")]
        [SerializeField] private GameObject[] rawRobots;

        [Tooltip("Array of Transform anchors where the XR Origin should be positioned when boarding each robot")]
        [SerializeField] private Transform[] robotSeatAnchors;

        [Header("Event References")]
        [Tooltip("Reference to Event 1 controller")]
        [SerializeField] private MonoBehaviour event1Controller;
        [Tooltip("Reference to Event 2 controller")]
        [SerializeField] private MonoBehaviour event2Controller;
        [Tooltip("Reference to Event 3 controller")]
        [SerializeField] private MonoBehaviour event3Controller;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool enableDebugLogs = true;
        #endregion

        #region Runtime Data
        // Event management
        private IEvent[] events;
        private IEvent currentEvent;
        private int currentEventIndex = -1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            MyDebug.Log($"[{GetType().Name}] SimulationSceneManager Started");

            // Pass robot and event data to GameManager for initialization
            MonoBehaviour[] eventControllers = new MonoBehaviour[] { event1Controller, event2Controller, event3Controller };
            GameManager.Instance.InitializeSimulationData(rawRobots, robotSeatAnchors);
            // Initialize events (delegate to SimulationSceneManager for actual event setup)
            GameManager.Instance.InitializeEvents(eventControllers);

            // Initialize local event references
            InitializeEventReferences();
        }
        #endregion

        /// <summary>
        /// Initialize local event references (called after GameManager sets up events)
        /// </summary>
        private void InitializeEventReferences()
        {
            events = new IEvent[3];

            // Convert MonoBehaviour references to IEvent
            if (event1Controller != null && event1Controller is IEvent)
            {
                events[0] = event1Controller as IEvent;
            }

            if (event2Controller != null && event2Controller is IEvent)
            {
                events[1] = event2Controller as IEvent;
            }

            if (event3Controller != null && event3Controller is IEvent)
            {
                events[2] = event3Controller as IEvent;
            }

            if (enableDebugLogs)
            {
                int assignedEvents = 0;
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i] != null) assignedEvents++;
                }
                Debug.Log($"[SimulationSceneManager] Initialized with {assignedEvents}/{events.Length} events");
            }
        }

        /// <summary>
        /// Start an event by index (1, 2, or 3)
        /// Called from GameManager when monitoring UI triggers an event
        /// </summary>
        public void StartEvent(int eventNumber)
        {
            // Validate event number (1-based index)
            if (eventNumber < 1 || eventNumber > 3)
            {
                Debug.LogError($"[SimulationSceneManager] Invalid event number: {eventNumber}. Must be 1, 2, or 3.");
                return;
            }

            int eventIndex = eventNumber - 1; // Convert to 0-based index

            // Check if event exists
            if (events[eventIndex] == null)
            {
                Debug.LogError($"[SimulationSceneManager] Event {eventNumber} is not assigned!");
                return;
            }

            // Check if another event is already running
            if (currentEvent != null && currentEvent.GetState() != EventState.Idle)
            {
                Debug.LogWarning($"[SimulationSceneManager] Cannot start Event {eventNumber}. {currentEvent.GetEventName()} is currently {currentEvent.GetState()}");
                return;
            }

            // Start the event
            currentEvent = events[eventIndex];
            currentEventIndex = eventIndex;

            if (enableDebugLogs)
            {
                Debug.Log($"[SimulationSceneManager] Starting {currentEvent.GetEventName()}");
            }

            currentEvent.InitializeEvent();
        }

        /// <summary>
        /// Called by EventStartTrigger when robot reaches event location
        /// </summary>
        public void OnEventLocationReached()
        {
            if (currentEvent == null)
            {
                Debug.LogWarning("[SimulationSceneManager] OnEventLocationReached called but no current event!");
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[SimulationSceneManager] {currentEvent.GetEventName()} location reached. Starting event...");
            }

            // Notify GameManager that event is now active (enables manual control if player is boarded)
            GameManager.Instance.OnEventActivated(currentEventIndex + 1); // Convert 0-based to 1-based

            // Start the event (activates obstacles, etc.)
            currentEvent.StartEvent();
        }

        /// <summary>
        /// Reset the current event
        /// Called when reset button is clicked or event completes
        /// </summary>
        public void ResetCurrentEvent()
        {
            if (currentEvent == null)
            {
                Debug.LogWarning("[SimulationSceneManager] No active event to reset!");
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[SimulationSceneManager] Resetting {currentEvent.GetEventName()}");
            }

            // Show delivery completion message
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.ShowDeliveryMessage("Delivery Complete! Returning to base...", 3f);

                if (enableDebugLogs)
                {
                    Debug.Log("[SimulationSceneManager] Showing delivery completion message");
                }
            }

            currentEvent.ResetEvent();

            // Return player to monitoring scene via GameManager
            GameManager.Instance.ReturnToMonitoring();

            // Clear current event reference
            currentEvent = null;
            currentEventIndex = -1;
        }

        /// <summary>
        /// Get the current active event (null if none)
        /// </summary>
        public IEvent GetCurrentEvent()
        {
            return currentEvent;
        }

        /// <summary>
        /// Check if any event is currently running
        /// </summary>
        public bool IsEventActive()
        {
            return currentEvent != null && currentEvent.GetState() != EventState.Idle;
        }

        /// <summary>
        /// Get current event state for UI display
        /// </summary>
        public EventState GetCurrentEventState()
        {
            return currentEvent != null ? currentEvent.GetState() : EventState.Idle;
        }
    }
}
