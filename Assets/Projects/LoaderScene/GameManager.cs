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

                if (currentActiveScenarioData != null)
                {
                    ReturnToMonitoring(ReturnFlag.Interrupt);
                }
                else
                {
                    ReturnToMonitoring(ReturnFlag.None);
                }
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
                scenarioData.robotObject.tag = "Untagged";
                
                if (scenarioData.robotWheelController)
                {
                    scenarioData.robotWheelController.enabled = false;
                }
                else
                {
                    MyDebug.LogWarning($"[{GetType().Name}] ❌ Robot {i + 1} doesn't have Wheel Controller (searched by GetComponentInChildren<RobotWheelController>)");
                }

                scenarioData.robotState = RobotState.Auto;

                scenarioDataList.Add(scenarioData);
            }
            
            MyDebug.Log($"[{GetType().Name}] ✅ Initialized ({scenarioDataList.Count}) robots successfully");
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
            
            currentActiveScenarioData.eventState = EventState.Active;

            currentActiveScenarioData.robotState = RobotState.Manual;
            MyDebug.Log($"[{GetType().Name}] robot {eventId} state changed to: Manual");
            
            // TODO: Simulation Scene 에서 Triggers 활성화하는 등 InitializeScenario() 만들기
            // TODO: 단순히 트리거만 끄면 되는지, 나무나 돌, 행인 같은걸 없앨 지, rigidbody 만 로봇과 안충돌하게 할 지 등)
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
            
            // 로봇이 Manual 상태일 때만 조작 허용
            var canControl = (scenarioData.robotState == RobotState.Manual);
            scenarioData.robotWheelController.enabled = canControl;

            if (canControl)
            {
                scenarioData.robotObject.tag = "Player";
                MyDebug.Log("🕹️ Manual Control Enabled");
            }
            else
            {
                MyDebug.Log("👁️ Auto Mode (View Only)");
            }

            // 플레이어 이동
            MovePlayer(scenarioData.seatAnchor);
            
            MyDebug.Log($"[{GetType().Name}] ✅ Boarded Robot {robotId} Completely");
        }

        public void ReturnToMonitoring(ReturnFlag flag)
        {
            MyDebug.Log($"[{GetType().Name}] Returning to MonitoringRoom; flag: {flag}");

            if (currentActiveScenarioData != null)
            {
                switch (flag)
                {
                    case ReturnFlag.Completed:
                        MyDebug.Log($"🎉 Event {currentActiveScenarioData.id} Completed");
                        currentActiveScenarioData.eventState = EventState.Completed;
                        break;
                    case ReturnFlag.Failed:
                        MyDebug.Log($"⚠️ Event {currentActiveScenarioData.id} Failed");
                        currentActiveScenarioData.eventState = EventState.Failed;
                        break;
                    case ReturnFlag.Interrupt:
                        MyDebug.Log($"🚫 Interrupted; Treated as Failed");
                        currentActiveScenarioData.eventState = EventState.Failed;
                        break;
                    case ReturnFlag.None:
                        MyDebug.LogWarning($"Semantic error");
                        break;
                }
                currentActiveScenarioData.robotState = RobotState.Auto;
                currentActiveScenarioData.robotWheelController.enabled = false;
                currentActiveScenarioData.robotObject.tag = "Untagged";
                currentActiveScenarioData = null;
            }
            
            ToggleVRFeatures(true);

            currentPlayerState = PlayerState.MonitoringMode;
            MyDebug.Log($"[{GetType().Name}] Change PlayerState to MonitoringMode");
            
            MonitoringSceneManager.Instance.MovePlayerToRespawnAnchor();
            
            MyDebug.Log($"[{GetType().Name}] ✅ Returning to Monitoring Scene Completely");
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