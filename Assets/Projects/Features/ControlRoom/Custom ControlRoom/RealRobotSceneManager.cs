using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using RealRobot;

namespace Project
{
    /// <summary>
    /// 로봇 씬의 모든 기능(이동, 위치 변경, 트리거 관리)을 총괄하는 통합 매니저
    /// </summary>
    public class RealRobotSceneManager : MonoBehaviour
    {
        public static RealRobotSceneManager Instance;

        [Space(10)]
        [Header("Locations (Drag Transforms Here)")]
        [SerializeField] private Transform respawnAnchor;  // 초기 위치
        [SerializeField] private Transform location1;  // 장소 1
        [SerializeField] private Transform location2;  // 장소 2

        [SerializeField] private Button sectorButtonA;
        [SerializeField] private Button location1Button;
        [SerializeField] private Button location2Button;

        private PoseData cachedRespawnAnchorPose;

        
        // =========================================================================
        // 초기화 및 실행 (Start)
        // =========================================================================
        
        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else { Destroy(gameObject); return; }
        }
        private void Start()
        {
            Debug.Log($"[{GetType().Name}] Initializing Scene...");

            CheckAssignments();
            
            StoreInitialRespawnAnchor();

            InitializeButtons();
        }

        private void CheckAssignments()
        {
            //
        }
        
        private void StoreInitialRespawnAnchor()
        {
            cachedRespawnAnchorPose = new PoseData(respawnAnchor.transform);
        }

        private void InitializeButtons()
        {
            sectorButtonA.onClick.AddListener(() => OnSectorAClicked());
            location1Button.onClick.AddListener(() => GoToLocation1());
            location2Button.onClick.AddListener(() => GoToLocation2());
        }

        // =========================================================================
        // [기능 2] 위치 이동 및 입력 맵 전환 함수들 (Public API)
        // =========================================================================

        public void MovePlayerToSectorB()
        {
            MyDebug.Log($"[{GetType().Name}] begin MovePlayerToSectorB()");
            respawnAnchor.transform.position = cachedRespawnAnchorPose.Position;
            respawnAnchor.transform.rotation = cachedRespawnAnchorPose.Rotation;
            
            GameManager.Instance.SetInputMode(InputMode.StandardVR);
            GameManager.Instance.ToggleVRFeatures(true);
            GameManager.Instance.MovePlayer(respawnAnchor);
        }
        

        public void GoToLocation1()
        {
            MyDebug.Log($"[{GetType().Name}] >>> Command: Move to Location 1");
            
            GameManager.Instance.SetInputMode(InputMode.RobotControlB);
            GameManager.Instance.ToggleVRFeatures(false);
            GameManager.Instance.MovePlayer(location1);
        }

        public void GoToLocation2()
        {
            MyDebug.Log($"[{GetType().Name}] >>> Command: Move to Location 2");
            
            GameManager.Instance.SetInputMode(InputMode.RobotControlB);
            GameManager.Instance.MovePlayer(location2);
        }

        private void OnSectorAClicked()
        {
            MonitoringSceneManager.Instance.MovePlayerToRespawnAnchor();
        }
    }
}