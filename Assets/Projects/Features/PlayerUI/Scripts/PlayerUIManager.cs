using UnityEngine;
using System.Collections.Generic;

namespace Project
{
    /// <summary>
    /// Manages player-facing UI in VR (warnings, status messages, hints, etc.)
    /// Singleton that handles:
    /// - Boundary checking (shows warning when robot exits event area)
    /// - Lazy-follow camera positioning for VR comfort
    /// - Extensible message system with custom panel prefabs
    ///
    /// SETUP GUIDE:
    /// 1. Create a World Space Canvas in your scene
    ///    - Canvas > Render Mode: World Space
    ///    - Canvas Scaler > UI Scale Mode: Constant Physical Size
    /// 2. Create this GameObject and attach this script
    /// 3. In Inspector, assign:
    ///    - worldSpaceCanvas: Your World Space Canvas
    ///    - Panel prefabs for each message type (Warning, Status, etc.)
    /// 4. Create panel prefabs as children of UI folder with UIMessagePanel script
    /// 5. In Event Controllers, assign EventBoundary components to eventBoundaries array
    /// </summary>
    public class PlayerUIManager : MonoBehaviour
    {
        public static PlayerUIManager Instance;

        [Header("Canvas Reference")]
        [Tooltip("Reference to your existing World Space Canvas")]
        [SerializeField] private Canvas worldSpaceCanvas;

        [Header("Panel Prefabs")]
        [Tooltip("Warning panel prefab (shown when robot exits boundary)")]
        [SerializeField] private UIMessagePanel warningPanelPrefab;

        [Tooltip("Status panel prefab (for informational messages)")]
        [SerializeField] private UIMessagePanel statusPanelPrefab;

        [Tooltip("Hint panel prefab (for player guidance)")]
        [SerializeField] private UIMessagePanel hintPanelPrefab;

        [Tooltip("Error panel prefab (for critical errors)")]
        [SerializeField] private UIMessagePanel errorPanelPrefab;

        [Header("Positioning Settings")]
        [Tooltip("How quickly the canvas follows the camera (0 = instant, higher = slower/lazier)")]
        [Range(1f, 20f)]
        [SerializeField] private float followSpeed = 5f;

        [Tooltip("Distance from camera to position the canvas")]
        [Range(1f, 5f)]
        [SerializeField] private float distanceFromCamera = 2.5f;

        [Tooltip("Offset from camera center (x: left/right, y: up/down, z: forward/back)")]
        [SerializeField] private Vector3 offsetFromCenter = new Vector3(0f, -0.75f, 0f);

        [Header("Boundary Checking")]
        [Tooltip("How often to check if robot is within boundaries (in seconds)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float boundaryCheckInterval = 0.2f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        [SerializeField] private bool enableDebugLogs = false;

        // Runtime state
        private Dictionary<UIMessageType, UIMessagePanel> panelInstances;
        private UIMessagePanel currentVisiblePanel;
        private Camera playerCamera;
        private float boundaryCheckTimer = 0f;
        private bool wasOutOfBounds = false;

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

            panelInstances = new Dictionary<UIMessageType, UIMessagePanel>();
        }

        private void Start()
        {
            if (worldSpaceCanvas == null)
            {
                Debug.LogError("[PlayerUIManager] World Space Canvas not assigned!");
                enabled = false;
                return;
            }

            // Get player camera (will be updated each frame to handle VR camera)
            UpdatePlayerCamera();

            // Instantiate panel prefabs as children of the canvas
            InstantiatePanel(UIMessageType.Warning, warningPanelPrefab);
            InstantiatePanel(UIMessageType.Status, statusPanelPrefab);
            InstantiatePanel(UIMessageType.Hint, hintPanelPrefab);
            InstantiatePanel(UIMessageType.Error, errorPanelPrefab);

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerUIManager] Initialized with {panelInstances.Count} panel types");
            }
        }

        private void Update()
        {
            // Only active when player is controlling a robot
            if (GameManager.Instance == null || GameManager.Instance.GetPlayerState() != PlayerState.ControllingMode)
            {
                return;
            }

            // Update camera reference (important for VR where camera might change)
            UpdatePlayerCamera();

            if (playerCamera != null)
            {
                // Lazy follow camera
                UpdateCanvasPosition();
            }

            // Check boundaries periodically
            boundaryCheckTimer += Time.deltaTime;
            if (boundaryCheckTimer >= boundaryCheckInterval)
            {
                boundaryCheckTimer = 0f;
                CheckRobotBoundaries();
            }
        }

        /// <summary>
        /// Update the player camera reference
        /// </summary>
        private void UpdatePlayerCamera()
        {
            if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
            {
                // Get camera from XR Origin
                playerCamera = GameManager.Instance.playerObject.GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        /// <summary>
        /// Update canvas position to lazily follow camera
        /// </summary>
        private void UpdateCanvasPosition()
        {
            if (playerCamera == null || worldSpaceCanvas == null) return;

            // Calculate target position in front of camera
            Vector3 targetPosition = playerCamera.transform.position +
                                    playerCamera.transform.forward * distanceFromCamera +
                                    playerCamera.transform.right * offsetFromCenter.x +
                                    playerCamera.transform.up * offsetFromCenter.y;

            // Lazy follow with lerp
            worldSpaceCanvas.transform.position = Vector3.Lerp(
                worldSpaceCanvas.transform.position,
                targetPosition,
                Time.deltaTime * followSpeed
            );

            // Billboard effect - always face camera
            Vector3 directionToCamera = playerCamera.transform.position - worldSpaceCanvas.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);

            worldSpaceCanvas.transform.rotation = Quaternion.Slerp(
                worldSpaceCanvas.transform.rotation,
                targetRotation,
                Time.deltaTime * followSpeed
            );
        }

        /// <summary>
        /// Check if robot is within event boundaries
        /// </summary>
        private void CheckRobotBoundaries()
        {
            // Get current event
            IEvent currentEvent = SimulationSceneManager.Instance?.GetCurrentEvent();
            if (currentEvent == null) return;

            // Get event boundaries
            EventBoundary[] boundaries = currentEvent.GetEventBoundaries();
            if (boundaries == null || boundaries.Length == 0) return;

            // Get robot position
            Transform robotTransform = GameManager.Instance.GetCurrentRobotTransform();
            if (robotTransform == null) return;

            Vector3 robotPosition = robotTransform.position;

            // Check if robot is inside ANY boundary
            bool isInsideAnyBoundary = false;
            foreach (EventBoundary boundary in boundaries)
            {
                if (boundary != null && boundary.ContainsPoint(robotPosition))
                {
                    isInsideAnyBoundary = true;
                    break;
                }
            }

            // Show/hide warning based on boundary check
            if (!isInsideAnyBoundary && !wasOutOfBounds)
            {
                // Robot just left boundaries - show warning panel (text is already in the panel design)
                ShowWarning();
                wasOutOfBounds = true;

                if (enableDebugLogs)
                {
                    Debug.LogWarning("[PlayerUIManager] Robot exited event boundaries!");
                }
            }
            else if (isInsideAnyBoundary && wasOutOfBounds)
            {
                // Robot returned to boundaries - hide warning
                HideWarning();
                wasOutOfBounds = false;

                if (enableDebugLogs)
                {
                    Debug.Log("[PlayerUIManager] Robot returned to event boundaries");
                }
            }
        }

        /// <summary>
        /// Instantiate a panel prefab
        /// </summary>
        private void InstantiatePanel(UIMessageType type, UIMessagePanel prefab)
        {
            if (prefab == null)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerUIManager] No prefab assigned for {type} message type");
                }
                return;
            }

            UIMessagePanel instance = Instantiate(prefab, worldSpaceCanvas.transform);
            instance.gameObject.SetActive(false);
            panelInstances[type] = instance;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerUIManager] Instantiated {type} panel");
            }
        }

        /// <summary>
        /// Show a message with the specified type
        /// </summary>
        public void ShowMessage(UIMessageType type, string message)
        {
            if (!panelInstances.ContainsKey(type))
            {
                Debug.LogWarning($"[PlayerUIManager] No panel instance for message type: {type}");
                return;
            }

            // Hide current panel if different type
            if (currentVisiblePanel != null && currentVisiblePanel != panelInstances[type])
            {
                currentVisiblePanel.Hide();
            }

            // Show new panel
            UIMessagePanel panel = panelInstances[type];
            panel.Show(message);
            currentVisiblePanel = panel;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerUIManager] Showing {type} message: {message}");
            }
        }

        /// <summary>
        /// Hide a specific message type
        /// </summary>
        public void HideMessage(UIMessageType type)
        {
            if (panelInstances.ContainsKey(type))
            {
                panelInstances[type].Hide();
                if (currentVisiblePanel == panelInstances[type])
                {
                    currentVisiblePanel = null;
                }
            }
        }

        /// <summary>
        /// Hide all messages
        /// </summary>
        public void HideAllMessages()
        {
            foreach (var panel in panelInstances.Values)
            {
                panel.Hide();
            }
            currentVisiblePanel = null;
        }

        /// <summary>
        /// Update message text without hiding/showing
        /// </summary>
        public void UpdateMessageText(UIMessageType type, string message)
        {
            if (panelInstances.ContainsKey(type))
            {
                panelInstances[type].UpdateText(message);
            }
        }

        /// <summary>
        /// Show the warning panel (for boundary violations)
        /// Panel text is defined in the prefab design, not passed as parameter
        /// </summary>
        private void ShowWarning()
        {
            if (panelInstances.ContainsKey(UIMessageType.Warning))
            {
                // Hide current panel if different type
                if (currentVisiblePanel != null && currentVisiblePanel != panelInstances[UIMessageType.Warning])
                {
                    currentVisiblePanel.Hide();
                }

                // Show warning panel (text is already in the panel prefab)
                UIMessagePanel warningPanel = panelInstances[UIMessageType.Warning];
                warningPanel.Show("");  // Empty string since text is in the prefab
                currentVisiblePanel = warningPanel;
            }
        }

        /// <summary>
        /// Hide the warning panel
        /// </summary>
        private void HideWarning()
        {
            if (panelInstances.ContainsKey(UIMessageType.Warning))
            {
                panelInstances[UIMessageType.Warning].Hide();
                if (currentVisiblePanel == panelInstances[UIMessageType.Warning])
                {
                    currentVisiblePanel = null;
                }
            }
        }
    }
}
