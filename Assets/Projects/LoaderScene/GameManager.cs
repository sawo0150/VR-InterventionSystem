using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
        
        // --- Runtime Data ---
        private GameObject[] robots;
        private Transform[] robotSeatAnchors;
        private int currentRobotIndex = -1;
        
        public PlayerState currentPlayerState { get; private set; } = PlayerState.MonitoringMode;
        
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
        
        // -------------------------------------------------------------------------
        // 1. Initialization & Setup
        // -------------------------------------------------------------------------
        
        private void CheckAssignments()
        {
            if (playerObject == null)              MyDebug.LogWarning($"[{GetType().Name}] ❌ PlayerObject is Missing");
            if (playerCharacterController == null) MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Player CharacterController is Missing");
            if (locomotionSystem == null)          MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Locomotion System not found");
            if (leftController == null)            MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Left Controller not found");
            if (rightController == null)           MyDebug.LogWarning($"[{GetType().Name}] ⚠️ Right Controller not found");
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
        // 2. Player Movement (Core Logic)
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
                case PlayerState.ControlingMode:
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
        
        // -------------------------------------------------------------------------
        // 3. Robot Interaction (Boarding & Returning)
        // -------------------------------------------------------------------------

        public void RegisterRobots(GameObject[] _robots, Transform[] seats)
        {
            MyDebug.Log($"[{GetType().Name}] Registering robots...");
            
            if (_robots == null || seats == null || _robots.Length != seats.Length)
            {
                MyDebug.LogError($"[{GetType().Name}] ❌ Register Failed; Arrays are null or length mismatch");
                return;
            }
            
            for (var i = 0; i < _robots.Length; i++)
            {
                if (_robots[i] == null)
                {
                    MyDebug.LogError($"[{GetType().Name}] ❌ Robot at index {i} is NULL");
                    continue;
                }
                if (seats[i] == null)
                {
                    MyDebug.LogError($"[{GetType().Name}] ❌ Seat Anchor at index {i} is NULL");
                    continue;
                }

                // 초기 상태 설정
                _robots[i].SetActive(true); // 항상 켜둠 (자율주행 등)
                _robots[i].tag = "Untagged"; // 태그 초기화
            }
            
            this.robots = _robots;
            this.robotSeatAnchors = seats;
            
            MyDebug.Log($"[{GetType().Name}] ✅ Registered {robots.Length} robots successfully");
        }

        public void BoardRobot(int index)
        {
            MyDebug.Log($"[{GetType().Name}] Boarding to Robot {index}...");

            var arrayIndex = index - 1;
            
            // 상태 변경
            currentPlayerState = PlayerState.ControlingMode;
            MyDebug.Log($"[{GetType().Name}] Change PlayerState to ControllingMode");

            // VR 기능 제어 (입력 유지, 인터랙션 끄기)
            ToggleVRFeatures(false);

            // 로봇 설정
            var targetRobot = robots[arrayIndex];
            targetRobot.tag = "Player";
            
            var wheel = targetRobot.GetComponentInChildren<RobotWheelController>();
            if (wheel) wheel.enabled = true;

            currentRobotIndex = index;
            
            // 플레이어 이동
            MovePlayer(robotSeatAnchors[arrayIndex]);
            
            MyDebug.Log($"[{GetType().Name}] ✅ Boarded Robot {index} Completely");
        }

        public void ReturnToMonitoring()
        {
            MyDebug.Log($"[{GetType().Name}] Returning to Monitoring...");
            
            var arrayIndex = currentRobotIndex - 1;
            var targetRobot = robots[arrayIndex];
            
            var wheel = targetRobot.GetComponentInChildren<RobotWheelController>();
            if (wheel) wheel.enabled = false;
            targetRobot.tag = "Untagged";
            currentRobotIndex = -1;
            
            ToggleVRFeatures(true);

            currentPlayerState = PlayerState.MonitoringMode;
            
            MonitoringSceneManager.Instance.MovePlayerToRespawnAnchor();
            
            MyDebug.Log($"[{GetType().Name}] ✅ Returning to Monitoring Scene Completely");
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