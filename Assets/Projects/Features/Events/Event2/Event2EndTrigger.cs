using UnityEngine;
using UnityEngine.Events;
using Project;



namespace Project
{
    
    /// <summary>
    /// Triggers Event 2 completion when robot reaches end zone.
    /// Shows reset button and handles event completion.
    /// </summary>
    public class Event2EndTrigger : MonoBehaviour
    {
        [Header("UI Settings")]
        [Tooltip("Reset button to show when robot reaches end")]
        public GameObject resetButton;

        [Tooltip("Robot tag to detect (default: Robot)")]
        public string robotTag = "Robot";

        [Header("Event Callbacks")]
        [Tooltip("Called when robot reaches end zone")]
        public UnityEvent onEventComplete;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogs = true;
        [Tooltip("Show trigger zone gizmo in editor")]
        public bool showGizmo = true;
        public Color gizmoColor = Color.yellow;

        private bool eventCompleted = false;
        private GameObject currentRobot;

        private void Start()
        {
            // Hide reset button initially
            if (resetButton != null)
            {
                resetButton.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if robot reached the end
            if (other.CompareTag(robotTag) && !eventCompleted)
            {
                OnRobotReachedEnd(other.gameObject);
            }
        }

        private void OnRobotReachedEnd(GameObject robot)
        {
            eventCompleted = true;
            currentRobot = robot;

            if (enableDebugLogs)
            {
                MyDebug.Log($"[{GetType().Name}] Robot reached end zone at {transform.position}");
            }

            // // Show reset button UI
            // ShowResetButton();

            // Notify event completion
            onEventComplete?.Invoke();
        }
        

        private void OnDrawGizmos()
        {
            if (!showGizmo) return;

            // Draw trigger zone
            Gizmos.color = gizmoColor;

            var box = GetComponent<BoxCollider>();
            if (box != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }

            SphereCollider sphere = GetComponent<SphereCollider>();
            if (sphere != null)
            {
                Gizmos.DrawWireSphere(transform.position, sphere.radius);
            }
        }
    }
}