using UnityEngine;

namespace Project
{
    public enum PlayerState { MonitoringMode, ControlingMode, }

    public enum RobotState { Auto, Manual, }

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