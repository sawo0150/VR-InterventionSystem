using UnityEngine;

namespace Project.Event
{
    [RequireComponent(typeof(Collider))]
    public class Blocking : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float pushBackSpeed = 5f;
        [Tooltip("z-axis equals the blue arrow")]
        [SerializeField] private Vector3 localPushDirection = new Vector3(0, 0, 1); 
        
        private Vector3 normalizedLocalPushDir;
        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
            
            normalizedLocalPushDir = localPushDirection.normalized;
        }

        // Called every frame while a collider stays inside the trigger
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(targetTag))
            {
                // Find the CharacterController (works if it's on the root or parent)
                CharacterController controller = other.GetComponentInParent<CharacterController>();
                if (controller != null)
                {
                    // Convert the cached local direction to world space
                    Vector3 worldPushDirection = transform.TransformDirection(localPushDirection.normalized);
                
                    // Calculate the movement vector for this frame
                    Vector3 moveVector = worldPushDirection * pushBackSpeed * Time.deltaTime;

                    // Push the controller
                    controller.Move(moveVector);
                }
            }
        }
    }
}