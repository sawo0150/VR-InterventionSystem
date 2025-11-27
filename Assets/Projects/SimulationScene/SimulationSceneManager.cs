using UnityEngine;

namespace Project
{
    /// <summary>
    /// Initializes robot data for the simulation scene.
    /// Configures robot GameObjects and their seat anchor positions.
    /// </summary>
    public class SimulationSceneManager : MonoBehaviour
    {
        public static SimulationSceneManager Instance;

        [Header("Robot Settings")]
        [Tooltip("Array of robot GameObjects in the simulation scene")]
        [SerializeField] private GameObject[] rawRobots;

        [Tooltip("Array of Transform anchors where the XR Origin should be positioned when boarding each robot")]
        [SerializeField] private Transform[] robotSeatAnchors;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            MyDebug.Log($"[{GetType().Name}] SimulationSceneManager Started");

            // Initialize GameManager with robot data
            GameManager.Instance.InitializeSimulationData(rawRobots, robotSeatAnchors);
        }
    }
}
