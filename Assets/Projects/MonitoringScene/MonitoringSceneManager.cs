using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace Project
{
    public class MonitoringSceneManager : MonoBehaviour
    {
        public static MonitoringSceneManager Instance;
        
        #region Helper Structures
        [System.Serializable]
        public struct EventTriggerButtonPair
        {
            public Button button;
            public int eventId;
        }

        [System.Serializable]
        public struct RobotCamButtonPair
        {
            public Button button;
            public int robotId;
        }
        #endregion
        
        #region Serialied Fields
        [Header("Position Setup")]
        [SerializeField] private Transform respawnAnchor;
        
        [Header("Controllers")]
        [SerializeField] private TutorialUIController tutorialUIController; 
        
        [Header("Scene UI Objects")]
        [SerializeField] private GameObject monitoringCanvasGroup;
        [SerializeField] private GameObject monitoringCanvasA;
        [SerializeField] private GameObject monitoringCanvasB;
        
        [Header("Sector Selection")]
        [SerializeField] private Button sectorButtonA;
        [SerializeField] private Button sectorButtonB;

        [Header("Sector A Buttons")]
        [SerializeField] private EventTriggerButtonPair[] eventTriggerButtons;
        [SerializeField] private RobotCamButtonPair[] robotCamButtons;
        #endregion
        
        #region Runtime Data
        private PoseData cachedRespawnAnchorPose;
        #endregion

        #region Unity Lifecycle
        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        
        private void Start()
        {
            MyDebug.Log("# REAL_INTERVENTION : MonitoringSceneManager Started");
            
            CheckAssignments();
            
            StoreInitialRespawnAnchor();
            
            InitializeButtons();
            ResetUIState();
            
            BeginTutorial();
            MovePlayerToRespawnAnchor();
        }
        #endregion
        
        #region Assignments Checking
        private void CheckAssignments()
        {
            if (tutorialUIController == null) MyDebug.LogWarning($"[{GetType().Name}] tutorialController is missing!");
            if (respawnAnchor == null)      MyDebug.LogWarning($"[{GetType().Name}] respawnAnchor is missing!");
            if (monitoringCanvasGroup == null)   MyDebug.LogWarning($"[{GetType().Name}] monitoringCanvas is missing!");
            // TODO
        }
        #endregion

        #region Initialization & Setup
        private void StoreInitialRespawnAnchor()
        {
            cachedRespawnAnchorPose = new PoseData(respawnAnchor.transform);
        }
        
        private void InitializeButtons()
        {
            sectorButtonA.onClick.AddListener(() => OnSectorAClicked());
            sectorButtonB.onClick.AddListener(() => OnSectorBClicked());
            
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
        }
        
        private void ResetUIState()
        {
            MyDebug.Log($"[{GetType().Name}] reset UI State (hide all canvas)");
            if(monitoringCanvasA) monitoringCanvasA.SetActive(true);
            monitoringCanvasGroup.SetActive(false);
        }
        #endregion
        
        #region Player Movement
        public void MovePlayerToRespawnAnchor()
        {
            respawnAnchor.transform.position = cachedRespawnAnchorPose.Position;
            respawnAnchor.transform.rotation = cachedRespawnAnchorPose.Rotation;
            MyDebug.Log($"[{GetType().Name}] call GameManager.MovePlayer()");
            GameManager.Instance.MovePlayer(respawnAnchor);
        }
        #endregion

        #region Tutorial Flow
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
        #endregion
        
        #region UI Event Handlers
        private void OnSectorAClicked()
        {
            ; // pass
        }

        private void OnSectorBClicked()
        {
            GameManager.Instance.SetSectorState(SectorState.RealWorld);
            RealRobotSceneManager.Instance.MovePlayerToRespawnAnchorB();
        }

        private void OnEventTriggerClicked(int eventId)
        {
            MyDebug.Log($"[{GetType().Name}] Event Triggered: {eventId}");
            GameManager.Instance.StartGameEvent(eventId);
        }

        private void OnRobotCamClicked(int robotId)
        {
            MyDebug.Log($"[{GetType().Name}] Request Control for Robot: {robotId}");

            GameManager.Instance.BoardRobot(robotId);
        }
        #endregion
    }
}