using UnityEngine;

namespace Project
{
    public enum PlayerState
    {
        MonitoringMode, 
        ControllingMode,
    }

    public enum RobotState { 
        Auto, 
        Manual, 
    }

    public enum EventState
    {
        Standby, 
        Active, 
        Completed, // Resolved 
        Failed,
    }

    public enum ReturnFlag
    {
        None,
        Interrupt,
        Completed,
        Failed,
    }

    [System.Serializable]
    public class ScenarioData
    {
        public int id;
        [TextArea] public string description;
        public EventState eventState = EventState.Standby;
        public GameObject robotObject;
        public Transform seatAnchor;
        public RobotState robotState = RobotState.Auto;
        public RobotWheelController robotWheelController;
        
        public ScenarioData(int id, GameObject obj, Transform seatAnchor)
        {
            this.id = id;
            this.robotObject = obj;
            this.seatAnchor = seatAnchor;
            this.robotWheelController = obj.GetComponentInChildren<RobotWheelController>();
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