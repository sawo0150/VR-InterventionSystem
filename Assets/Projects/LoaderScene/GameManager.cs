using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Project
{
    public enum GameState
    {
        Monitoring,
        Controlling
    }
    
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Target Setup")]
        [SerializeField] private Transform playerObject;
        [Tooltip("if player object is empty, search the object by this string")]
        [SerializeField] private string playerObjectName = "My Complete XR Origin (XR Rig)";
        [SerializeField] private string targetAnchorName = "MonitoringScene Anchor Offset";
        
        
        [Header("Target Scene Names")]
        [SerializeField] private string monitoringSceneName = "1_MonitoringScene";
        [SerializeField] private string simulationSceneName = "2_SimulationScene";
        
        
        private GameState currentState = GameState.Monitoring;
        public GameState CurrentState => currentState;

        public void SetState(GameState newState)
        {
            var prevState  = currentState;
            currentState = newState;
            MyDebug.Log($"# Game State Changed: {currentState} (from {prevState})");
        }

        
        private void Awake()
        {
            // Ensure singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            MyDebug.SetDebuggingFlag(enableDebugLogs);
        }
        
        private void Start()
        {
            MyDebug.Log("# GameManager Started");
            
            if (playerObject == null)
            {
                Debug.LogWarning("Can't find XR Origin; Try to search object by name ...");
                var originObj = GameObject.Find(playerObjectName);
                if (originObj != null)
                {
                    playerObject = originObj.transform;
                }
                else
                {
                    Debug.LogError("Can't find XR Origin; Check GameManger in LoaderScene");
                }
            }

            StartCoroutine(LoadScenesSequence());
        }

        private IEnumerator LoadScenesSequence()
        {
            MyDebug.Log("    -- Begin LoadScenesSequence()");
            
            AsyncOperation simLoadOp = null;
            AsyncOperation monLoadOp = null;
            
            // Load other scenes (in parallel)
            if (!SceneManager.GetSceneByName(simulationSceneName).isLoaded)
            {
                simLoadOp = SceneManager.LoadSceneAsync(simulationSceneName, LoadSceneMode.Additive);
                // 로딩이 끝나도 씬을 바로 활성화하지 않게 할 수도 있음 (선택 사항)
                // simLoadOp.allowSceneActivation = false;
            }

            if (!SceneManager.GetSceneByName(monitoringSceneName).isLoaded)
            {
                monLoadOp = SceneManager.LoadSceneAsync(monitoringSceneName, LoadSceneMode.Additive);
                // simLoadOp.allowSceneActivation = false;
            }

            yield return new WaitForEndOfFrame();
            
            // Regard Simulation Scene as MainScene
            var simScene = SceneManager.GetSceneByName(simulationSceneName);
            if (simScene.IsValid())
            {
                SceneManager.SetActiveScene(simScene);
                MyDebug.Log($"Set Active Scene to: {simulationSceneName}");
            }
            
            MyDebug.Log("Game Initializing; All Scenes are Loaded");

            // Move player to monitoring room
            MovePlayerToMonitoringRoom();
            
            MyDebug.Log("    -- End LoadScenesSequence()");
        }

        private void MovePlayerToMonitoringRoom()
        {
            MyDebug.Log("    -- begin MovePlayerToMonitoringRoom()");
            
            var targetAnchor = GameObject.Find(targetAnchorName);

            if (targetAnchor == null)
            {
                MyDebug.LogError($"'{targetAnchorName}' Anchor not found in loaded scenes");
                return;
            }

            if (playerObject == null)
            {
                MyDebug.LogError($"Player Object not found");
                return;
            }
            
            // Disable player's physics temporarily (to prevent collision during teleport)
            CharacterController charController = playerObject.GetComponent<CharacterController>();
            Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
            if (charController != null) charController.enabled = false;
            if (playerRigidbody != null) playerRigidbody.useGravity = false;
            
            // Move the player (by set parent anchor)
            playerObject.SetParent(targetAnchor.transform, false);
            
            // Align player's transform
            playerObject.localScale = Vector3.one;
            playerObject.localPosition = Vector3.zero;
            playerObject.localRotation = Quaternion.identity;
            
            // Enable player's physics
            if (charController != null) charController.enabled = true;
            if (playerRigidbody != null) playerRigidbody.useGravity = true;
            
            MyDebug.Log("Player moved to Monitoring Room");
            
            MyDebug.Log("    -- end MovePlayerToMonitoringRoom()");
        }
        
    }
}