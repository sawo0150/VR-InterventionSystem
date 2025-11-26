using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Project
{
    public class MonitoringSceneManager : MonoBehaviour
    {
        public static MonitoringSceneManager Instance;

        [System.Serializable]
        public struct EventTriggerButtonPair
        {
            public Button button;
            public int eventId;
        }

        [System.Serializable]
        public struct SectorButtonPair
        {
            public Button button;
            public int sectorId;
        }

        [System.Serializable]
        public struct RobotCamButtonPair
        {
            public Button button;
            public int robotId;
        }
        
        
        [Header("Position Setup")]
        [SerializeField] private Transform respawnAnchor;
        
        [Header("Controllers")]
        [SerializeField] private TutorialUIController tutorialUIController; 
        
        [Header("Scene UI Objects")]
        [SerializeField] private GameObject monitoringCanvasGroup;
        [SerializeField] private GameObject monitoringCanvasA;
        [SerializeField] private GameObject monitoringCanvasB;
        
        [Header("Sector Selection")]
        [SerializeField] private SectorButtonPair[] sectorButtons;

        [Header("Sector A Buttons")]
        [SerializeField] private EventTriggerButtonPair[] eventTriggerButtons;
        [SerializeField] private RobotCamButtonPair[] robotCamButtons;
        
        [Header("Sector B Buttons")]
        [SerializeField] private Button getControlButton;
        
        [Header("UI - System")]
        [SerializeField] private Button restartAppButton;
        [SerializeField] private Button resetRobotsButton;
        
        private Transform playerObject => GameManager.Instance.playerObject;
        private PoseData cachedRespawnAnchorPose;
        private PoseData cachedPlayerPose;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        
        private void Start()
        {
            MyDebug.Log("# REAL_INTERVENTION : MonitoringSceneManager Started");
            
            CheckAssignments();
            
            InitializeButtons();
            ResetUIState();

            BeginTutorial();
            
            StoreInitialRespawnAnchor();
            MovePlayerToRespawnAnchor();
        }
        
        private void CheckAssignments()
        {
            if (tutorialUIController == null) MyDebug.LogWarning($"[{GetType().Name}] tutorialController is missing!");
            if (respawnAnchor == null)      MyDebug.LogWarning($"[{GetType().Name}] respawnAnchor is missing!");
            if (monitoringCanvasGroup == null)   MyDebug.LogWarning($"[{GetType().Name}] monitoringCanvas is missing!");
            if (restartAppButton == null)   MyDebug.LogWarning($"[{GetType().Name}] restartAppButton is missing!");
            if (resetRobotsButton == null)  MyDebug.LogWarning($"[{GetType().Name}] resetRobotsButton is missing!");
            // TODO
        }

        private void StoreInitialRespawnAnchor()
        {
            cachedRespawnAnchorPose = new PoseData(respawnAnchor.transform);
        }

        public void MovePlayerToRespawnAnchor()
        {
            respawnAnchor.transform.position = cachedRespawnAnchorPose.Position;
            respawnAnchor.transform.rotation = cachedRespawnAnchorPose.Rotation;
            MyDebug.Log($"[{GetType().Name}] call GameManager.MovePlayer()");
            GameManager.Instance.MovePlayer(respawnAnchor);
        }
        
        private void ResetUIState()
        {
            MyDebug.Log($"[{GetType().Name}] reset UI State (hide all canvas)");
            if(monitoringCanvasA) monitoringCanvasA.SetActive(true);
            monitoringCanvasGroup.SetActive(false);
        }

        private void InitializeButtons()
        {
            foreach (var pair in sectorButtons)
            {
                var targetSectorId = pair.sectorId;
                pair.button.onClick.RemoveAllListeners();
                pair.button.onClick.AddListener(() => OnSectorClicked(targetSectorId));
            }
            foreach (var pair in eventTriggerButtons)
            {
                var targetEventId = pair.eventId;
                pair.button.onClick.RemoveAllListeners();
                pair.button.onClick.AddListener(() => OnEventTriggerClicked(targetEventId));
            }

            foreach (var pair in robotCamButtons)
            {
                var  targetRobotId = pair.robotId;
                pair.button.onClick.RemoveAllListeners();
                pair.button.onClick.AddListener(() => OnRobotCamClicked(targetRobotId));
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
        
        private void OnSectorClicked(int sectorId)
        {
            MyDebug.Log($"[{GetType().Name}] Sector Changed to: {sectorId}");
            
            if (sectorId == 1)
            {
                // Sector A 활성화
                if(monitoringCanvasA) monitoringCanvasA.SetActive(true);
                if(monitoringCanvasB) monitoringCanvasB.SetActive(false);
                
                MyDebug.Log("Switched to Canvas A");
            }
            else if (sectorId == 2)
            {
                // Sector B 활성화
                if(monitoringCanvasA) monitoringCanvasA.SetActive(false);
                if(monitoringCanvasB) monitoringCanvasB.SetActive(true);
                
                MyDebug.Log("Switched to Canvas B");
            }
        }

        private void OnEventTriggerClicked(int eventId)
        {
            MyDebug.Log($"[{GetType().Name}] Event Triggered: {eventId}");
            GameManager.Instance.StartGameEvent(eventId);
            // TODO: 장애물 생성 등 시뮬레이션 이벤트 실행
            
        }

        private void OnRobotCamClicked(int robotId)
        {
            MyDebug.Log($"[{GetType().Name}] Request Control for Robot: {robotId}");

            GameManager.Instance.BoardRobot(robotId);
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
            // TODO
        }
    }
}