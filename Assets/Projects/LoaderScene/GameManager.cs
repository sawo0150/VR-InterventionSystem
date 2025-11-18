using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Project
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Target Setup")]
        [SerializeField] public GameObject playerObject;
        
        [Header("Target Scene Names")]
        [SerializeField] public string loaderSceneName = "0_LoaderScene";
        [SerializeField] private string monitoringSceneName = "1_MonitoringScene";
        [SerializeField] private string simulationSceneName = "2_SimulationScene";
        
        
        
        private PlayerState currentPlayerState = PlayerState.MonitoringMode;
        public PlayerState CurrentPlayerState => currentPlayerState;

        public void ChangePlayerState(PlayerState newState)
        {
            var prevState  = currentPlayerState; currentPlayerState = newState;
            MyDebug.Log($"# Player State Changed to: {currentPlayerState} (from {prevState})");
        }

        
        private void Awake()
        {
            // Ensure singleton
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else { Destroy(gameObject); return; }
            
            MyDebug.SetDebuggingFlag(enableDebugLogs);
        }
        
        private void Start()
        {
            MyDebug.Log("# GameManager Started");
            
            CheckAssignments();

            StartCoroutine(LoadScenesSequence());
        }
        
        private void CheckAssignments()
        {
            if (playerObject == null) 
                MyDebug.LogWarning($"[{GetType().Name}] PlayerObject is Missing");
        }

        private IEnumerator LoadScenesSequence()
        {
            MyDebug.Log($"[{GetType().Name}] begin LoadScenesSequence()");
            
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
            
            if (simLoadOp != null) 
            {
                while (!simLoadOp.isDone) yield return null;
            }

            if (monLoadOp != null) 
            {
                while (!monLoadOp.isDone) yield return null;
            }

            yield return new WaitForEndOfFrame();
            
            // Regard Simulation Scene as MainScene
            var simScene = SceneManager.GetSceneByName(simulationSceneName);
            if (simScene.IsValid())
            {
                SceneManager.SetActiveScene(simScene);
                MyDebug.Log($"[{GetType().Name}] Set Active Scene to: {simulationSceneName}");
            }
            
            MyDebug.Log($"[{GetType().Name}] Game Initializing; All Scenes are Loaded");
            
            MyDebug.Log($"[{GetType().Name}] end LoadScenesSequence()");
        }

        public void MovePlayer(GameObject targetAnchor)
        {
            MyDebug.Log($"[{GetType().Name}] Moving Player to '{targetAnchor.name}'...");
            
            var charController = playerObject.GetComponent<CharacterController>();
            var playerRigidbody = playerObject.GetComponent<Rigidbody>();
            
            // 물리설정 해제
            if (charController != null) charController.enabled = false;
            if (playerRigidbody != null) playerRigidbody.isKinematic = true;

            // 부모 설정 및 위치 정렬
            // worldPositionStays: false -> 부모가 바뀌어도 로컬 좌표를 유지
            playerObject.transform.SetParent(targetAnchor.transform, false);
            
            // 확실하게 0점으로 초기화 
            playerObject.transform.localPosition = Vector3.zero;
            playerObject.transform.localRotation = Quaternion.identity;
            playerObject.transform.localScale = Vector3.one;

            // 물리설정 복구
            if (charController != null) charController.enabled = true;
            if (playerRigidbody != null) playerRigidbody.isKinematic = false;
            
            MyDebug.Log($"[{GetType().Name}] Player successfully moved to {targetAnchor.name}");
        }
        
    }
}