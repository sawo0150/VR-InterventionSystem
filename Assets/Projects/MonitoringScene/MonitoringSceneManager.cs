using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Project
{
    public class MonitoringSceneManager : MonoBehaviour
    {
        public static MonitoringSceneManager Instance;
        
        [Header("Position Setup")]
        [SerializeField] private GameObject respawnAnchor;
        
        [Header("Controllers")]
        [SerializeField] private TutorialUIController tutorialUIController; 
        
        [Header("Scene UI Objects")]
        [SerializeField] private GameObject monitoringCanvasGroup;
        [SerializeField] private GameObject monitoringCanvasA;
        [SerializeField] private GameObject monitoringCanvasB;
        
        [Header("Sector Selection")]
        [SerializeField] private Button[] sectorButtons;

        [Header("Sector A Buttons")]
        [SerializeField] private Button[] eventTriggerButtons;
        [SerializeField] private Button[] robotCamButtons;
        
        [Header("Sector B Buttons")]
        [SerializeField] private Button getControlButton;
        
        [Header("UI - System")]
        [SerializeField] private Button restartAppButton;
        [SerializeField] private Button resetRobotsButton;
        
        private GameObject playerObject => GameManager.Instance.playerObject;
        private PoseData cachedRespawnAnchorPose;
        private PoseData cachedPlayerPose;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        
        private void Start()
        {
            MyDebug.Log("# MonitoringSceneManager Started");
            
            CheckAssignments();
            
            StoreInitialRespawnAnchor();
            MovePlayerToRespawnAnchor();
            
            InitializeButtons(); // connect target buttons
            ResetUIState(); // hide all canvas

            BeginTutorial();
        }
        
        private void CheckAssignments()
        {
            if (tutorialUIController == null) MyDebug.LogWarning($"[{GetType().Name}] tutorialController is missing!");
            if (respawnAnchor == null)      MyDebug.LogWarning($"[{GetType().Name}] respawnAnchor is missing!");
            if (monitoringCanvasGroup == null)   MyDebug.LogWarning($"[{GetType().Name}] monitoringCanvas is missing!");
            if (restartAppButton == null)   MyDebug.LogWarning($"[{GetType().Name}] restartAppButton is missing!");
            if (resetRobotsButton == null)  MyDebug.LogWarning($"[{GetType().Name}] resetRobotsButton is missing!");
        }

        private void StoreInitialRespawnAnchor()
        {
            cachedRespawnAnchorPose = new PoseData(respawnAnchor.transform);
        }

        private void MovePlayerToRespawnAnchor()
        {
            if (GameManager.Instance.CurrentPlayerState == PlayerState.ControlingMode)
            {
                /*
                 * aaaaa
                 */
                return;
            }

            respawnAnchor.transform.position = cachedRespawnAnchorPose.Position;
            respawnAnchor.transform.rotation = cachedRespawnAnchorPose.Rotation;
            MyDebug.Log($"[{GetType().Name}] call GameManager.MovePlayer()");
            GameManager.Instance.MovePlayer(respawnAnchor);
        }
        
        private void ResetUIState()
        {
            MyDebug.Log($"[{GetType().Name}] reset UI State (hide all canvas)");
            monitoringCanvasGroup.SetActive(false);
        }

        private void InitializeButtons()
        {
            for (var i = 0; i < sectorButtons.Length; i++)
            {
                var index = i;
                sectorButtons[i].onClick.AddListener(() => OnSectorClicked(index));
            }

            for (var i = 0; i < eventTriggerButtons.Length; i++)
            {
                var index = i + 1;
                eventTriggerButtons[i].onClick.AddListener(() => OnEventTriggerClicked(index));
            }
            
            for (int i = 0; i < robotCamButtons.Length; i++)
            {
                int robotIndex = i + 1;
                robotCamButtons[i].onClick.RemoveAllListeners();
                robotCamButtons[i].onClick.AddListener(() => OnRobotCamClicked(robotIndex));
            }
            

            restartAppButton.onClick.AddListener(OnRestartAppClicked);
            resetRobotsButton.onClick.AddListener(OnResetRobotsClicked);
        }

        private void BeginTutorial()
        {
            MyDebug.Log($"[{GetType().Name}] Begin Tutorial");
            tutorialUIController.BeginTutorial(OnTutorialCompleted);
        }
        
        private void OnTutorialCompleted()
        {
            MyDebug.Log($"[{GetType().Name}] Tutorial Complete; Show Monitoring Panel");
            monitoringCanvasGroup.SetActive(true);
        }
        
        // ==========================================================================
        // ==========================================================================
        
        private void OnSectorClicked(int index)
        {
            MyDebug.Log($"[{GetType().Name}] Sector Changed to: {index}");
            // TODO: 미니맵 이미지를 바꾸거나 카메라 위치를 이동하는 로직 추가
        }

        private void OnEventTriggerClicked(int eventIndex)
        {
            MyDebug.Log($"[{GetType().Name}] Event Triggered: {eventIndex}");
            // TODO: 장애물 생성 등 시뮬레이션 이벤트 실행
        }

        private void OnRobotCamClicked(int robotIndex)
        {
            MyDebug.Log($"[{GetType().Name}] Request Control for Robot: {robotIndex}");

            if (SimulationSceneManager.Instance != null)
            {
                SimulationSceneManager.Instance.MoveCameraToOffset(robotIndex);
            }
            else
            {
                MyDebug.LogError("SimulationSceneManager is missing!");
            }
        }
        
        // ==========================================================================
        // ==========================================================================
        
        private void OnRestartAppClicked()
        {
            MyDebug.Log($"[{GetType().Name}] Soft Restarting ...");
            
            MovePlayerToRespawnAnchor();
            ResetUIState();
            tutorialUIController.BeginTutorial(OnTutorialCompleted);
        }

        private void OnResetRobotsClicked()
        {
            
        }
    }
}