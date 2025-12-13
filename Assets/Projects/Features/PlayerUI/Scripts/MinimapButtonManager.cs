using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    /// <summary>
    /// Manages minimap event buttons - enables them when events are activated.
    /// Singleton that handles:
    /// - Enabling/disabling buttons based on event activation
    /// - Managing blinking effects via BlinkingButton components
    ///
    /// SETUP GUIDE:
    /// 1. Attach this script to a GameObject in your scene (e.g., "MinimapButtonManager")
    /// 2. Create 3 Button GameObjects on your minimap (one for each event)
    /// 3. Add BlinkingButton component to each button
    /// 4. In Inspector, assign all 3 buttons to the eventButtons array (Event 1, Event 2, Event 3)
    /// 5. Buttons will auto-enable when OnEventActivated is called for their event ID
    /// 6. Buttons automatically call GameManager.BoardRobot() when clicked
    /// </summary>
    public class MinimapButtonManager : MonoBehaviour
    {
        public static MinimapButtonManager Instance;

        [Header("Event Buttons")]
        [Tooltip("Array of event buttons (index 0 = Event 1, index 1 = Event 2, etc.)")]
        [SerializeField] private Button[] eventButtons = new Button[3];
        
        [Header("Event Goals")]
        [Tooltip("Array of event goal icons matching the buttons (index 0 = Event 1 Goal, etc.)")]
        [SerializeField] private GameObject[] eventGoals = new GameObject[3];

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool enableDebugLogs = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Ensure all buttons start disabled
            for (int i = 0; i < eventButtons.Length; i++)
            {
                if (eventButtons[i] != null)
                {
                    eventButtons[i].gameObject.SetActive(false);

                    // Add onClick listener to call BoardRobot and disable button
                    int eventId = i + 1; // Capture event ID for closure (1-based)
                    eventButtons[i].onClick.AddListener(() => OnEventButtonPressed(eventId));

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[MinimapButtonManager] Event button {eventId} initialized (disabled)");
                    }
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning($"[MinimapButtonManager] Event button {i + 1} not assigned!");
                }

                if (i < eventGoals.Length && eventGoals[i] != null)
                {
                    eventGoals[i].SetActive(false);
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning($"[MinimapButtonManager] Event Goal Icon {i + 1} not assigned!");
                }
            }
        }

        /// <summary>
        /// Enable a specific event button (called when OnEventActivated fires)
        /// </summary>
        /// <param name="eventId">Event ID (1, 2, 3, etc.)</param>
        public void EnableEventButton(int eventId)
        {
            int index = eventId - 1; // Convert to 0-based index

            if (index < 0 || index >= eventButtons.Length)
            {
                Debug.LogError($"[MinimapButtonManager] Invalid event ID: {eventId}. Must be between 1 and {eventButtons.Length}");
                return;
            }

            if (eventButtons[index] == null)
            {
                Debug.LogError($"[MinimapButtonManager] Event button {eventId} is not assigned!");
                return;
            }

            // Enable the button (BlinkingButton will auto-start via OnEnable)
            eventButtons[index].gameObject.SetActive(true);
            
            if (index < eventGoals.Length && eventGoals[index] != null)
            {
                eventGoals[index].SetActive(true);
            }
            else if (enableDebugLogs)
            {
                Debug.LogWarning($"[MinimapButtonManager] Event Goal Icon {index + 1} not assigned!");
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MinimapButtonManager] Enabled event button {eventId}");
            }
        }

        /// <summary>
        /// Disable a specific event button (called when button is pressed)
        /// </summary>
        /// <param name="eventId">Event ID (1, 2, 3, etc.)</param>
        public void DisableEventButton(int eventId)
        {
            int index = eventId - 1; // Convert to 0-based index

            if (index < 0 || index >= eventButtons.Length)
            {
                Debug.LogError($"[MinimapButtonManager] Invalid event ID: {eventId}. Must be between 1 and {eventButtons.Length}");
                return;
            }

            if (eventButtons[index] == null)
            {
                Debug.LogError($"[MinimapButtonManager] Event button {eventId} is not assigned!");
                return;
            }

            // Disable the button (BlinkingButton will auto-stop via OnDisable)
            eventButtons[index].gameObject.SetActive(false);
            
            
            if (index < eventGoals.Length && eventGoals[index] != null)
            {
                eventGoals[index].SetActive(false);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MinimapButtonManager] Disabled event button {eventId}");
            }
        }

        /// <summary>
        /// Called when an event button is pressed
        /// Calls GameManager.BoardRobot() and disables the button
        /// </summary>
        private void OnEventButtonPressed(int eventId)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MinimapButtonManager] Event button {eventId} pressed - calling BoardRobot({eventId})");
            }

            // Call GameManager.BoardRobot
            if (GameManager.Instance != null)
            {
                GameManager.Instance.BoardRobot(eventId);
            }
            else
            {
                Debug.LogError("[MinimapButtonManager] GameManager.Instance is null! Cannot board robot.");
            }

            // Disable the button
            DisableEventButton(eventId);
        }

        /// <summary>
        /// Check if a specific event button is currently enabled
        /// </summary>
        public bool IsEventButtonEnabled(int eventId)
        {
            int index = eventId - 1;

            if (index < 0 || index >= eventButtons.Length || eventButtons[index] == null)
            {
                return false;
            }

            return eventButtons[index].gameObject.activeSelf;
        }

        /// <summary>
        /// Disable all event buttons
        /// </summary>
        public void DisableAllEventButtons()
        {
            for (int i = 0; i < eventButtons.Length; i++)
            {
                if (eventButtons[i] != null)
                {
                    eventButtons[i].gameObject.SetActive(false);
                }
            }
            
            for (int i = 0; i < eventGoals.Length; i++)
            {
                if (eventGoals[i] != null) eventGoals[i].SetActive(false);
            }

            if (enableDebugLogs)
            {
                Debug.Log("[MinimapButtonManager] Disabled all event buttons");
            }
        }
    }
}
