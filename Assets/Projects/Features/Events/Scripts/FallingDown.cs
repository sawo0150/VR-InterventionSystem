using UnityEngine;

namespace Project.Event
{
    // Ensure a Rigidbody component (if not, auto add)
    [RequireComponent(typeof(Rigidbody))]
    
    public class FallingDown : MonoBehaviour
    {
        public bool fallOnStart = false;
        
        private Rigidbody rb;
        [SerializeField] private Vector3 fallDirection = new Vector3(0, 0, 1);
        [SerializeField] private float fallForce = 2.0f;
        //[SerializeField] private float torqueForce = 1.0f;

        private Vector3 startPosition;
        private Quaternion startRotation;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            rb.isKinematic = true;

            startPosition = transform.position;
            startRotation = transform.rotation;
        }
        
        void Start()
        {
            // Fall immediately if checked
            if (fallOnStart)
            {
                FallDown();
            }
        }
        
        // Public method to start fall; callable by events
        public void FallDown()
        
        {
            // prevent duplicate calls if already falling
            if (!rb.isKinematic)
            {
                return;
            }

            // Debug.Log(gameObject.name + " starts falling");

            // Deactivate kinematic (allow pyhsics)
            rb.isKinematic = false;
            
        
            // Set Force 
            rb.AddForce(fallDirection.normalized * fallForce, ForceMode.Impulse);
        
            // // Set Torque
            // Vector3 randomTorque = new Vector3(Random.value, Random.value, Random.value);
            // rb.AddTorque(randomTorque * torqueForce, ForceMode.Impulse);
        }

        public void ResetObject()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }
}
