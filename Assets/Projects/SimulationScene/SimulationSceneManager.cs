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
        [SerializeField] private GameObject[] rawRobots;
        [SerializeField] private Transform[] robotSeatAnchors;
        
        [Header("Scenario Components")]
        [SerializeField] private (GameObject[],int) scenarioComponents;
        
        private Transform playerObject => GameManager.Instance.playerObject;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }
        

        private void Start()
        {
            MyDebug.Log($"[{GetType().Name}] # SimulationSceneManager Started");
            
            GameManager.Instance.InitializeSimulationData(rawRobots, robotSeatAnchors);
        }
        
    }
}