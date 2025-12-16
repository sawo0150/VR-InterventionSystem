using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using VRInterventionSystem.Audio;

namespace Project
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        
        #region Settings & Serialized Fields
        // -------------------------------------------------------------------------
        // Configuration & Inspector Setup
        // -------------------------------------------------------------------------
        [Header("Global Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Scene Configuration")]
        [SerializeField] public string loaderSceneName = "0_LoaderScene";
        [SerializeField] private string monitoringSceneName = "1_MonitoringScene";
        [SerializeField] private string simulationSceneName = "2_SimulationScene";
        [SerializeField] private string realRobotSceneName = "5_RealRobotScene";
        
        [Header("Player References")]
        [SerializeField] public Transform playerObject;
        [SerializeField] private CharacterController playerCharacterController;
        [SerializeField] private GameObject locomotionSystem;
        [SerializeField] private GameObject leftController;
        [SerializeField] private GameObject rightController;
        
        [Header("Event Data")]
        public List<ScenarioData> scenarioDataList = new List<ScenarioData>();

        [Header("Event Alert Messages")]
        [Tooltip("Alert messages shown when each event is activated (index 0 = Event 1, index 1 = Event 2, etc.)")]
        [SerializeField] private string[] eventAlertMessages = new string[]
        {
            "Event 1: Slope Hazard Activated",
            "Event 2: Traffic Zone Activated",
            "Event 3: Construction Area Activated"
        };

        [Tooltip("How long alert panels stay visible (in seconds)")]
        [SerializeField] [Range(1f, 10f)] private float alertDisplayDuration = 3f;

        [Header("Input System  Settings")]
        [Tooltip("복귀 버튼으로 사용할 액션 (예: XRI LeftHand/PrimaryButton)")]
        [SerializeField] private InputActionReference returnButtonAction;

        [Tooltip("XR 컨트롤러 썸스틱 입력 (예: XRI RightHand/Primary2DAxis) - Vector2 형식")]
        [SerializeField] private InputActionReference xrThumbstickInputAction;

        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private string globalMapName = "Global";
        [SerializeField] private string[] standardVRMaps = { "XRI Right Locomotion", "XRI Right Interaction", "XRI Left Interaction", "XR Left Locomotion" };
        [SerializeField] private string[] robotControlMaps_Sim = { "XRI Right Locomotion/Jump", "XRI Left/Thumbstick", "XRI Right Interaction", "XRI Left Interaction" };
        [SerializeField] private string[] robotControlMaps_Real = { "MOD_Joystick", };
        #endregion
        
        #region Runtime States
        // -------------------------------------------------------------------------
        // Internal State Management
        // -------------------------------------------------------------------------
        private PlayerState currentPlayerState = PlayerState.MonitoringMode;
        private ScenarioData currentActiveScenarioData;
        private InputMode currentInputMode;
        private SectorState currentSectorState =  SectorState.Simulation;
        #endregion
        
        #region Unity Lifecycle
        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }
            
            MyDebug.SetDebuggingFlag(enableDebugLogs);
        }
        
        private void Start()
        {
            MyDebug.Log($"[{GetType().Name}] Initializing...");
            
            // 1. 연결 상태 검증
            CheckAssignments();

            InitializeInputSystem();
            
            // 2. 씬 로딩 시작
            StartCoroutine(LoadScenesSequence());
        }

        private void OnEnable()
        {
            returnButtonAction.action.Enable();
        }

        private void OnDisable()
        {
            returnButtonAction.action.Disable();
        }
        
        private void Update()
        {
            if (currentPlayerState == PlayerState.ControllingMode || currentInputMode == InputMode.RobotControlB)
            {
                HandleReturnInput();
            }
        }
        #endregion
        
        #region Assignments  Checking
        private void CheckAssignments()
        {
            if (playerObject == null)              MyDebug.LogError($"[{GetType().Name}] ❌ PlayerObject is Missing");
            if (playerCharacterController == null) MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Player CharacterController is Missing");
            if (locomotionSystem == null)          MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Locomotion System not found");
            if (leftController == null)            MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Left Controller not found");
            if (rightController == null)           MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Right Controller not found");
            if (returnButtonAction == null || returnButtonAction.action == null)        MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Return Button Action not found");
            if (inputActionAsset == null)          MyDebug.LogError($"[{GetType().Name}] ❌ Input Action Asset is Missing");
        }
        
        #endregion
        
        #region Scene Loading
        // -------------------------------------------------------------------------
        // Scene Loading
        // -------------------------------------------------------------------------
        private IEnumerator LoadScenesSequence()
        {
            MyDebug.Log($"[{GetType().Name}] Loading Scenes...");
            
            AsyncOperation simLoadOp = null;
            AsyncOperation monLoadOp = null;
            AsyncOperation realLoadOp = null;
            
            // Load other scenes (in parallel)
            if (!SceneManager.GetSceneByName(simulationSceneName).isLoaded)
                simLoadOp = SceneManager.LoadSceneAsync(simulationSceneName, LoadSceneMode.Additive);

            if (!SceneManager.GetSceneByName(monitoringSceneName).isLoaded)
                monLoadOp = SceneManager.LoadSceneAsync(monitoringSceneName, LoadSceneMode.Additive);
            
            if (!SceneManager.GetSceneByName(realRobotSceneName).isLoaded)
                realLoadOp = SceneManager.LoadSceneAsync(realRobotSceneName, LoadSceneMode.Additive);
            
            // Wait for asynchronous scene loading to finish
            if (simLoadOp != null) while (!simLoadOp.isDone) yield return null;
            if (monLoadOp != null) while (!monLoadOp.isDone) yield return null;
            if (realLoadOp != null) while (!realLoadOp.isDone) yield return null;

            yield return new WaitForEndOfFrame();
            
            // Regard Simulation Scene as MainScene (set active scene)
            var simScene = SceneManager.GetSceneByName(simulationSceneName);
            if (simScene.IsValid())
            {
                SceneManager.SetActiveScene(simScene);
                MyDebug.Log($"[{GetType().Name}] Set Active Scene to: {simulationSceneName}");
            }
            
            MyDebug.Log($"[{GetType().Name}] ✅ All Scenes Loaded & Ready");
        }
        #endregion
        
        #region Input System Management & Interrupt Handling
        // -------------------------------------------------------------------------
        // Input System Management & Interrupt Handling
        // -------------------------------------------------------------------------

        private void InitializeInputSystem()
        {
            var globalMap = inputActionAsset.FindActionMap(globalMapName);
            if (globalMap != null) globalMap.Enable();
            
            SetInputMode(InputMode.StandardVR);
        }

        public void SetInputMode(InputMode newInputMode)
        {
            if (currentInputMode == newInputMode) return;

            MyDebug.Log($"[{GetType().Name}] Change Input Mode: {currentInputMode} -> {newInputMode}");

            // 1. Disable current maps
            ToggleInputMaps(GetMapsForMode(currentInputMode), false);

            // 2. Enable new maps
            ToggleInputMaps(GetMapsForMode(newInputMode), true);

            currentInputMode = newInputMode;
        }

        private string[] GetMapsForMode(InputMode inputMode)
        {
            switch (inputMode)
            {
                case InputMode.StandardVR: return standardVRMaps;
                case InputMode.RobotControlA: return robotControlMaps_Sim;
                case InputMode.RobotControlB: return robotControlMaps_Real;
                default: return new string[0];
            }
        }

        private void ToggleInputMaps(string[] mapNames, bool enable)
        {
            if (mapNames == null) return;
            
            foreach (var mapName in mapNames)
            {
                //MyDebug.Log($"[{GetType().Name}] Set Input Map {mapName} {enable}");
                var map = inputActionAsset.FindActionMap(mapName);
                if (map != null)
                {
                    if (enable) map.Enable();
                    else map.Disable();
                }
                else
                {
                    MyDebug.LogWarning($"[{GetType().Name}] Input Map '{mapName}' not found");
                }
            }
        }
        
        private void HandleReturnInput()
        {
            // 1. VR 컨트롤러 입력
            bool isVRPressed = returnButtonAction != null && 
                               returnButtonAction.action != null && 
                               returnButtonAction.action.WasPressedThisFrame();

            // 2. 키보드 비상키
            bool isKeyboardPressed = Keyboard.current != null && 
                                     (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

            if (isVRPressed || isKeyboardPressed)
            {
                MyDebug.Log($"[{GetType().Name}] 🔘 Return Button Pressed; Sector: {currentSectorState}");

                switch (currentSectorState)
                {
                    case SectorState.Simulation:
                        ReturnToMonitoring();
                        break;
                    case SectorState.RealWorld:
                        RealRobotSceneManager.Instance.ReturnToMonitoringB();
                        break;
                }
            }
        }
        #endregion
        
        #region Player Movement
        // -------------------------------------------------------------------------
        // Player Movement
        // -------------------------------------------------------------------------
        public void MovePlayer(Transform targetAnchor)
        {
            MyDebug.Log($"[{GetType().Name}] Moving Player to '{targetAnchor.name}'...");

            var charController = playerCharacterController;
            
            // Disable Physics (prevent collision while moving)
            if (charController) charController.enabled = false;
            
            // Docking (Set Parent and Initialize Pose)
            playerObject.transform.SetParent(targetAnchor, false);
            playerObject.transform.localPosition = Vector3.zero;
            playerObject.transform.localRotation = Quaternion.identity;
            playerObject.transform.localScale = Vector3.one;
            
            // Enable Physics & Settings
            switch (currentPlayerState)
            {
                case PlayerState.MonitoringMode:
                    charController.stepOffset = 0.3f;
                    charController.minMoveDistance = 0.001f;
                    break;
                case PlayerState.ControllingMode:
                    // 차량 위로 옮길 경우, 로봇 스케일 때문에 stepOffset 에러 발생
                    charController.stepOffset = 0.01f; // 앉아있을 땐 계단 오를 일이 없으므로 최소화
                    charController.minMoveDistance = 0; // 미세 떨림 방지
                    break;
            }
            charController.enabled = true;

            if (playerObject.parent != targetAnchor)
            {
                MyDebug.LogError($"❌ Parenting Failed; Current Parent: {playerObject.parent?.name}");
            }
            else
            {
                MyDebug.Log($"[{GetType().Name}] ✅ Player successfully moved to {targetAnchor.name}");
            }
        }
        
        public void BoardRobot(int robotId)
        {
            if (MinimapButtonManager.Instance != null && MinimapButtonManager.Instance.IsEventButtonEnabled(robotId))
            {
                MinimapButtonManager.Instance.DisableEventButton(robotId);
            }
            MyDebug.Log($"[{GetType().Name}] Boarding to Robot {robotId}...");

            var scenarioData = GetScenarioData(robotId);
            if (scenarioData == null)
            {
                MyDebug.LogError($"[{GetType().Name}] Robot {robotId} is not found");
                return;
            }

            // Disable previous robot's controller if switching between robots
            if (currentActiveScenarioData != null && currentActiveScenarioData.id != robotId)
            {
                MyDebug.Log($"[{GetType().Name}] Disabling previous robot {currentActiveScenarioData.id} controller");
                currentActiveScenarioData.robotNavMeshController.enableXRInput = false;
                currentActiveScenarioData.robotNavMeshController.enableKeyboardInput = true;
                currentActiveScenarioData.robotNavMeshController.enabled = false;
            }

            // Update current active scenario data to the new robot
            currentActiveScenarioData = scenarioData;

            // 플레이어 상태 변경
            currentPlayerState = PlayerState.ControllingMode;
            MyDebug.Log($"[{GetType().Name}] Change PlayerState to ControllingMode");

            // VR 기능 제어 (입력 유지, 인터랙션 끄기)
            ToggleVRFeatures(false);

            // 로봇이 Manual 상태이고 이벤트가 Active 상태일 때만 조작 허용
            // Initializing 상태(로봇이 이벤트 위치로 이동 중)에는 조작 불가
            MyDebug.Log($"[{GetType().Name}] BoardRobot Debug - RobotState: {scenarioData.robotState}, EventState: {scenarioData.eventState}");

            var canControl = (scenarioData.robotState == RobotState.Manual) &&
                             (scenarioData.eventState == EventState.Active);

            if (canControl)
            {
                // Configure XR input for robot control
                scenarioData.robotNavMeshController.enableXRInput = true;
                scenarioData.robotNavMeshController.xrThumbstickAction = xrThumbstickInputAction;
                scenarioData.robotNavMeshController.enableKeyboardInput = false; // Disable keyboard when in VR mode

                scenarioData.robotNavMeshController.enabled = true;
                // Keep "Robot" tag (event triggers need it to detect robot arrival)
                MyDebug.Log("🕹️ Manual Control Enabled (XR Input Active)");
            }
            else
            {
                // Only disable if it's currently enabled (don't interfere with Auto mode)
                if (scenarioData.robotNavMeshController.enabled)
                {
                    scenarioData.robotNavMeshController.enabled = false;
                }

                if (scenarioData.eventState == EventState.Initializing)
                {
                    MyDebug.Log($"👁️ View Only Mode (Robot navigating to event location) - RobotState: {scenarioData.robotState}");
                }
                else
                {
                    MyDebug.Log($"👁️ Auto Mode (View Only) - RobotState: {scenarioData.robotState}, EventState: {scenarioData.eventState}");
                }
            }

            // 플레이어 이동
            MovePlayer(scenarioData.seatAnchor);

            MyDebug.Log($"[{GetType().Name}] ✅ Boarded Robot {robotId} Completely");
        }

        public void ReturnToMonitoring()
        {
            MyDebug.Log($"[{GetType().Name}] Returning to MonitoringRoom");

            if (currentActiveScenarioData != null)
            {
                // Disable XR input when leaving robot
                currentActiveScenarioData.robotNavMeshController.enableXRInput = false;
                currentActiveScenarioData.robotNavMeshController.enableKeyboardInput =
                    true; // Re-enable keyboard for testing
                currentActiveScenarioData.robotNavMeshController.enabled = false;

                currentActiveScenarioData = null;
            }

            ToggleVRFeatures(true);
            SetInputMode(InputMode.StandardVR);

            currentPlayerState = PlayerState.MonitoringMode;
            MyDebug.Log($"[{GetType().Name}] Change PlayerState to MonitoringMode");

            MonitoringSceneManager.Instance.MovePlayerToRespawnAnchor();

            MyDebug.Log($"[{GetType().Name}] ✅ Returning to Monitoring Scene Completely");
        }
        #endregion
        
        #region Robot & Scenario Initialization
        // -------------------------------------------------------------------------
        // Robot & Scenario Initialization
        // -------------------------------------------------------------------------
        public void InitializeSimulationData(GameObject[] rawRobots, Transform[] seatAnchors)
        {
            MyDebug.Log($"[{GetType().Name}] Initializing Robot Data...");

            if (rawRobots == null || seatAnchors == null || rawRobots.Length != seatAnchors.Length)
            {
                MyDebug.LogError($"[{GetType().Name}] ❌ Data Mismatch or Null");
                return;
            }

            scenarioDataList.Clear();

            for (var i = 0; i < rawRobots.Length; i++)
            {
                var scenarioData = new ScenarioData(i + 1, rawRobots[i], seatAnchors[i]);

                scenarioData.robotObject.SetActive(true);
                scenarioData.robotObject.tag = "Robot";

                if (scenarioData.robotNavMeshController)
                {
                    scenarioData.robotNavMeshController.enabled = false;
                }
                else
                {
                    MyDebug.LogWarning($"[{GetType().Name}] ❌ Robot {i + 1} doesn't have NavMesh Controller (searched by GetComponentInChildren<RobotNavMeshController>)");
                }

                scenarioData.robotState = RobotState.Auto;

                scenarioDataList.Add(scenarioData);
            }

            MyDebug.Log($"[{GetType().Name}] ✅ Initialized ({scenarioDataList.Count}) robots successfully");
        }

        public void InitializeEvents(MonoBehaviour[] eventControllers)
        {
            if (eventControllers == null || eventControllers.Length == 0)
            {
                MyDebug.LogWarning($"[{GetType().Name}] No event controllers provided");
                return;
            }

            int validEvents = 0;
            for (int i = 0; i < eventControllers.Length; i++)
            {
                if (eventControllers[i] != null && eventControllers[i] is IEvent)
                {
                    validEvents++;
                }
            }

            MyDebug.Log($"[{GetType().Name}] ✅ Initialized {validEvents}/{eventControllers.Length} events");
        }
        #endregion
        
        #region Event Management
        // ------------------------------------------------------------------------- //
        // Event Management
        // ------------------------------------------------------------------------- //
        
        public void StartGameEvent(int eventId)
        {
            MyDebug.Log($"[{GetType().Name}] 🔥 Event {eventId} Started;");

            if (currentActiveScenarioData != null)
            {
                MyDebug.LogWarning($"[{GetType().Name}] Scenario {currentActiveScenarioData.id} is not ended; Discard it");
            }

            var scenarioData = GetScenarioData(eventId);

            currentActiveScenarioData = scenarioData;

            if (scenarioData == null)
            {
                MyDebug.LogError($"[{GetType().Name}] Scenario {eventId} is not found");
                return;
            }

            // Disable other event trigger buttons
            if (MonitoringSceneManager.Instance != null)
            {
                MonitoringSceneManager.Instance.DisableOtherEventButtons(eventId);
            }

            // Set event state to Initializing (robot will navigate to event location)
            currentActiveScenarioData.eventState = EventState.Initializing;
            // Trigger the event in SimulationSceneManager
            SimulationSceneManager.Instance.StartEvent(eventId);
        }

        /// <summary>
        /// Called when event transitions from Initializing to Active
        /// Enables manual control if player is already boarded
        /// </summary>
        public void OnEventActivated(int eventId)
        {
            MyDebug.Log($"[{GetType().Name}] Event {eventId} Activated (reached start location)");

            var scenarioData = GetScenarioData(eventId);
            if (scenarioData == null)
            {
                MyDebug.LogError($"[{GetType().Name}] Scenario {eventId} is not found");
                return;
            }

            currentActiveScenarioData = scenarioData;

            // Update event state to Active
            currentActiveScenarioData.eventState = EventState.Active;
            currentActiveScenarioData.robotState = RobotState.Manual;

            // Show alert panel with event-specific message
            ShowEventAlert(eventId);

            // Enable the corresponding minimap button
            if (MinimapButtonManager.Instance != null && currentPlayerState == PlayerState.MonitoringMode)
            {
                MinimapButtonManager.Instance.EnableEventButton(eventId);
            }

            // If player is currently controlling this robot, enable manual control now
            if (currentActiveScenarioData != null &&
                currentActiveScenarioData.id == eventId)
            {
                // Enable manual control
                scenarioData.robotNavMeshController.enableXRInput = true;
                scenarioData.robotNavMeshController.xrThumbstickAction = xrThumbstickInputAction;
                scenarioData.robotNavMeshController.enableKeyboardInput = false;
                scenarioData.robotNavMeshController.enabled = true;
                // Keep "Robot" tag (event triggers need it to detect robot arrival)

                MyDebug.Log("🕹️ Manual Control NOW Enabled (Event Active)");
            }
        }

        /// <summary>
        /// Shows an alert panel when an event is activated
        /// </summary>
        private void ShowEventAlert(int eventId)
        {
            MyDebug.Log($"[GameManager] ShowEventAlert called for Event {eventId}");

            if (PlayerUIManager.Instance == null)
            {
                MyDebug.LogWarning("[GameManager] PlayerUIManager.Instance is NULL - cannot show event alert");
                return;
            }

            MyDebug.Log($"[GameManager] PlayerUIManager.Instance found");

            // Validate event ID
            int messageIndex = eventId - 1; // Convert to 0-based index
            MyDebug.Log($"[GameManager] Event ID: {eventId}, Message Index: {messageIndex}, Array Length: {eventAlertMessages.Length}");

            if (messageIndex < 0 || messageIndex >= eventAlertMessages.Length)
            {
                MyDebug.LogError($"[GameManager] No alert message configured for Event {eventId} (index {messageIndex} out of range 0-{eventAlertMessages.Length - 1})");
                return;
            }

            // Get the message for this event
            string alertMessage = eventAlertMessages[messageIndex];
            MyDebug.Log($"[GameManager] Alert message retrieved: \"{alertMessage}\"");

            // Show alert panel with auto-hide
            MyDebug.Log($"[GameManager] Calling PlayerUIManager.ShowMessage(UIMessageType.Alert, \"{alertMessage}\", {alertDisplayDuration})");
            PlayerUIManager.Instance.ShowMessage(UIMessageType.Alert, alertMessage, alertDisplayDuration);

            MyDebug.Log($"[GameManager] ShowMessage call completed - alert should now be visible");
        }

        /// <summary>
        /// Reset event and robot states for a specific event
        /// Called when event completes and resets
        /// </summary>
        public void ResetEventStates(int eventId)
        {
            var scenarioData = GetScenarioData(eventId);
            if (scenarioData == null)
            {
                MyDebug.LogError($"[{GetType().Name}] Cannot reset states - Scenario {eventId} not found");
                return;
            }

            // Reset states to Idle and Auto
            scenarioData.eventState = EventState.Idle;
            scenarioData.robotState = RobotState.Auto;

            MyDebug.Log($"[{GetType().Name}] Reset Event {eventId} states: eventState=Idle, robotState=Auto");
        }
        #endregion

        #region Public Getter, Setter & Helper Methods
        // -------------------------------------------------------------------------
        // Public Getter & Setter / Helper Methods
        // -------------------------------------------------------------------------

        /// <summary>
        /// Get the current player state (MonitoringMode or ControllingMode)
        /// </summary>
        public PlayerState GetPlayerState()
        {
            return currentPlayerState;
        }

        /// <summary>
        /// Get the Transform of the currently controlled robot
        /// Returns null if no robot is currently being controlled
        /// </summary>
        public Transform GetCurrentRobotTransform()
        {
            if (currentActiveScenarioData != null && currentActiveScenarioData.robotObject != null)
            {
                return currentActiveScenarioData.robotObject.transform;
            }
            return null;
        }

        public void SetPlayerState(PlayerState playerState)
        {
            currentPlayerState = playerState;
        }

        public SectorState GetSectorState()
        {
            return currentSectorState;
        }

        public void SetSectorState(SectorState sectorState)
        {
            currentSectorState = sectorState;
        }

        private ScenarioData GetScenarioData(int id)
        {
            return scenarioDataList.Find(s => s.id == id);
        }
        #endregion
        
        #region VR Feature Control
        // -------------------------------------------------------------------------
        // VR Feature Control
        // -------------------------------------------------------------------------
        public void ToggleVRFeatures(bool enable)
        {
            // 이동 시스템: 끄면 됨 (로봇 조종 중에는 텔레포트/이동 불가)
            if (locomotionSystem) locomotionSystem.SetActive(enable);

            // 컨트롤러 인터랙션은 항상 활성화 유지 (UI 조작을 위해)
            // 로봇 조종 중에도 UI 패널, 버튼 등과 상호작용할 수 있어야 함
            // ToggleControllerInteractors(leftController, enable);
            // ToggleControllerInteractors(rightController, enable);
        }
        
        private void ToggleControllerInteractors(GameObject controller, bool enable)
        {
            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>();
            foreach (var interactor in interactors)
            {
                interactor.enabled = enable;
            }
        }
        #endregion
    }
}