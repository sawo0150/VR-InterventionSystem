using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;

namespace Project
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        // --- Settings & References ---
        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Player References")]
        [SerializeField] public Transform playerObject;
        [SerializeField] private CharacterController playerCharacterController;
        [SerializeField] private GameObject locomotionSystem;
        [SerializeField] private GameObject leftController;
        [SerializeField] private GameObject rightController;
        
        [Header("Scene Configuration")]
        [SerializeField] public string loaderSceneName = "0_LoaderScene";
        [SerializeField] private string monitoringSceneName = "1_MonitoringScene";
        [SerializeField] private string simulationSceneName = "2_SimulationScene";
        
        [Header("Event Control")]
        public List<ScenarioData> scenarioDataList = new List<ScenarioData>();
        
        [Header("Input Settings")]
        [Tooltip("복귀 버튼으로 사용할 액션 (예: XRI LeftHand/PrimaryButton)")]
        [SerializeField] private InputActionReference returnButtonAction;

        [Tooltip("XR 컨트롤러 썸스틱 입력 (예: XRI RightHand/Primary2DAxis) - Vector2 형식")]
        [SerializeField] private InputActionReference xrThumbstickInputAction;
        
        // --- Runtime Data ---
        //public PlayerState currentPlayerState { get; private set; } = PlayerState.MonitoringMode;
        private PlayerState currentPlayerState = PlayerState.MonitoringMode;
        private ScenarioData currentActiveScenarioData;
        
        
        // ---
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
            if (currentPlayerState == PlayerState.ControllingMode)
            {
                HandleReturnInput();
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
                MyDebug.Log("🔘 Return Button Pressed (Detected by GameManager)");
                ReturnToMonitoring();
            }
        }

        // -------------------------------------------------------------------------
        // 1. Scene Initialization & Setup
        // -------------------------------------------------------------------------
        
        private void CheckAssignments()
        {
            if (playerObject == null)              MyDebug.LogWarning($"[{GetType().Name}] ❌ PlayerObject is Missing");
            if (playerCharacterController == null) MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Player CharacterController is Missing");
            if (locomotionSystem == null)          MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Locomotion System not found");
            if (leftController == null)            MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Left Controller not found");
            if (rightController == null)           MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Right Controller not found");
            if (returnButtonAction == null || returnButtonAction.action == null)        MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Return Button Action not found");
        }
        

        private IEnumerator LoadScenesSequence()
        {
            MyDebug.Log($"[{GetType().Name}] Loading Scenes...");
            
            AsyncOperation simLoadOp = null;
            AsyncOperation monLoadOp = null;
            
            // Load other scenes (in parallel)
            if (!SceneManager.GetSceneByName(simulationSceneName).isLoaded)
                simLoadOp = SceneManager.LoadSceneAsync(simulationSceneName, LoadSceneMode.Additive);

            if (!SceneManager.GetSceneByName(monitoringSceneName).isLoaded)
                monLoadOp = SceneManager.LoadSceneAsync(monitoringSceneName, LoadSceneMode.Additive);
            
            // Wait for asynchronous scene loading to finish
            if (simLoadOp != null) while (!simLoadOp.isDone) yield return null;
            if (monLoadOp != null) while (!monLoadOp.isDone) yield return null;

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
        
        // -------------------------------------------------------------------------
        // 2. Player Movement
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
                    charController.stepOffset = 0.5f;
                    charController.minMoveDistance = 0.001f;
                    break;
                case PlayerState.ControllingMode:
                    // 차량 위로 옮길 경우, 로봇 스케일 때문에 stepOffset 에러 발생
                    charController.stepOffset = 0.01f; // 앉아있을 땐 계단 오를 일이 없으므로 최소화
                    charController.minMoveDistance = 0; // 미세 떨림 방지
                    break;
                default:
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
        
        // -------------------------------------------------------------------------
        // 3. Robot & Scenario Initialization
        // -------------------------------------------------------------------------
        
        public void InitializeSimulationData(GameObject[] rawRobots, Transform[] seatAnchors, MonoBehaviour[] eventControllers)
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

            // Initialize events (delegate to SimulationSceneManager for actual event setup)
            InitializeEvents(eventControllers);
        }

        private void InitializeEvents(MonoBehaviour[] eventControllers)
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
        
        public void InitializeRobots() { }
        public void InitializeScenarios() { }
        
        // ------------------------------------------------------------------------- //
        // 5. Event Management
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
        
        // -------------------------------------------------------------------------
        // 5. Robot Interaction (Boarding, Returning)
        // -------------------------------------------------------------------------
        
        
        public void BoardRobot(int robotId)
        {
            MyDebug.Log($"[{GetType().Name}] Boarding to Robot {robotId}...");

            var scenarioData = GetScenarioData(robotId);
            if (scenarioData == null)
            {
                MyDebug.LogError($"[{GetType().Name}] Robot {robotId} is not found");
                return;
            }

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
                scenarioData.robotNavMeshController.enabled = false;

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
                currentActiveScenarioData.robotNavMeshController.enableKeyboardInput = true; // Re-enable keyboard for testing
                currentActiveScenarioData.robotNavMeshController.enabled = false;

                currentActiveScenarioData = null;
            }

            ToggleVRFeatures(true);

            currentPlayerState = PlayerState.MonitoringMode;
            MyDebug.Log($"[{GetType().Name}] Change PlayerState to MonitoringMode");

            MonitoringSceneManager.Instance.MovePlayerToRespawnAnchor();

            MyDebug.Log($"[{GetType().Name}] ✅ Returning to Monitoring Scene Completely");
        }

        // -------------------------------------------------------------------------
        // Public Getter Methods
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

        // -------------------------------------------------------------------------
        // Helper Methods
        // -------------------------------------------------------------------------
        private ScenarioData GetScenarioData(int id)
        {
            return scenarioDataList.Find(s => s.id == id);
        }
        
        // -------------------------------------------------------------------------
        // 4. VR Feature Control
        // -------------------------------------------------------------------------

        private void ToggleVRFeatures(bool enable)
        {
            // 이동 시스템: 끄면 됨
            if (locomotionSystem) locomotionSystem.SetActive(enable);
            
            // 컨트롤러: 오브젝트 자체를 끄면 입력(Input)도 끊길 수 있음.
            // 따라서 Interactor(Ray, Direct) 컴포넌트만 끄는 것이 안전함.
            ToggleControllerInteractors(leftController, enable);
            ToggleControllerInteractors(rightController, enable);
        }
        
        private void ToggleControllerInteractors(GameObject controller, bool enable)
        {
            var interactors = controller.GetComponentsInChildren<XRBaseInteractor>();
            foreach (var interactor in interactors)
            {
                interactor.enabled = enable;
            }
            
            // (선택) 시각적 모델(손) 숨기기
            // var model = controller.transform.Find("Model Parent"); // 이름 확인 필요
            // if (model) model.gameObject.SetActive(enable);
        }
    }
}