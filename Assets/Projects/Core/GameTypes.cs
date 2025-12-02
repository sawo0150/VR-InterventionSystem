using UnityEngine;

namespace Project
{
    public enum  PlayerState
    {
        MonitoringMode, 
        ControllingMode,
        ControllingModeB,
    }

    public enum RobotState { 
        Auto, 
        Manual, 
    }

    public enum EventState
    {
        Idle,           // Event not started (equivalent to Standby)
        Initializing,   // Robot navigating to location
        Active,         // Player has control, obstacles active
        Completed,      // Event finished, ready to reset
        Failed,         // Event failed
    }

    public enum ReturnFlag
    {
        None,
        Interrupt,
        Completed,
        Failed,
    }

    public enum SectorState
    {
        Simulation,
        RealWorld,
    }

    public enum InputMode
    {
        StandardVR,
        RobotControlA,
        RobotControlB,
        None
    }

    [System.Serializable]
    public class ScenarioData
    {
        public int id;
        [TextArea] public string description;
        public EventState eventState = EventState.Idle;
        public GameObject robotObject;
        public Transform seatAnchor;
        public RobotState robotState = RobotState.Auto;
        public RobotNavMeshController robotNavMeshController;

        public ScenarioData(int id, GameObject obj, Transform seatAnchor)
        {
            this.id = id;
            this.robotObject = obj;
            this.seatAnchor = seatAnchor;
            this.robotNavMeshController = obj.GetComponentInChildren<RobotNavMeshController>();
        }
    }

    public struct PoseData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public PoseData(Transform t)
        {
            Position = t.position;
            Rotation = t.rotation;
        }
        
        public PoseData(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
    
    
}