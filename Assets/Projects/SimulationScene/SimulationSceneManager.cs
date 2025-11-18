using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Project
{
    public class SimulationSceneManager : MonoBehaviour
    {
        public static SimulationSceneManager Instance;
        
        [Header("Robot Settings")]
        [SerializeField] private GameObject[] robotRoots;
        [SerializeField] private Transform[] robotSeatAnchors;
        
        [Header("Input Settings")]
        [Tooltip("복귀 버튼으로 사용할 액션 (예: XRI LeftHand/PrimaryButton)")]
        [SerializeField] private InputActionReference returnButtonAction;
        
        private Transform playerObject => GameManager.Instance.playerObject;

        private bool wasReturnButtonPressed = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        
        private void OnEnable()
        {
            if (returnButtonAction != null && returnButtonAction.action != null)
                returnButtonAction.action.Enable();
        }
        
        private void OnDisable()
        {
            if (returnButtonAction != null && returnButtonAction.action != null)
                returnButtonAction.action.Disable();
        }

        private void Start()
        {
            MyDebug.Log($"[{GetType().Name}] # SimulationSceneManager Started");
            
            InitializeRobots();
            
            GameManager.Instance.RegisterRobots(robotRoots, robotSeatAnchors);
        }

        private void Update()
        {
            // 운전 모드일 때만 복귀(하차) 버튼 입력 감지
            if (GameManager.Instance.currentPlayerState == PlayerState.ControlingMode)
            {
                HandleReturnInput();
            }
        }
        
        private void InitializeRobots()
        {
            if (robotRoots == null)
            {
                MyDebug.Log($"[{GetType().Name}] No robot root found");
                return;
            }

            var cnt = 0;
            
            foreach (var robot in robotRoots)
            {
                cnt++;
                if (robot == null)
                {
                    MyDebug.Log($"[{GetType().Name}] '{cnt}'th robot is null");
                }
                
                robot.SetActive(true);
                
                var wheelController = robot.GetComponentInChildren<RobotWheelController>();
                if (wheelController != null)
                {
                    wheelController.enabled = false;
                }
            }
            
            MyDebug.Log($"[{GetType().Name}] All robots initialized to Auto Mode.");
        }

        private void HandleReturnInput()
        {
            // 1. VR 컨트롤러 & 시뮬레이터 입력 감지
            // WasPressedThisFrame()은 눌린 그 순간(1프레임)만 true이므로 별도 bool 관리가 필요 없습니다.
            bool isVRPressed = returnButtonAction != null && 
                               returnButtonAction.action != null && 
                               returnButtonAction.action.WasPressedThisFrame();

            // 2. 키보드 비상키 (ESC 또는 Space) - 시뮬레이터 없이 테스트할 때 유용
            bool isKeyboardPressed = Keyboard.current != null && 
                                     (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame);

            if (isVRPressed || isKeyboardPressed)
            {
                MyDebug.Log("🔘 Return Button Pressed (Input System / Keyboard)");
                
                // 매니저에게 복귀 요청
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ReturnToMonitoring();
                }
            }
        }
    }
}