using UnityEngine;

namespace Project.Event
{
    [RequireComponent(typeof(Rigidbody))]
    public class MeteorFalling : MonoBehaviour
    {
        [SerializeField] private float stopYLevel = 0.2f;

        private Rigidbody rb;
        private bool isFalling = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            rb.isKinematic = true; 
        }

        // Public method to be called by a trigger or event
        public void Drop()
        {
            // prevent duplicated calls
            if (rb.isKinematic)
            {
                rb.isKinematic = false;
                isFalling = true;
            }
        }

        // Runs on the fixed physics timestep
        // @@ TODO
        private void FixedUpdate()
        {
            // If not falling, do nothing
            if (!isFalling)
            {
                return;
            }

            // Check if the meteor has reached or passed the stop level
            if (transform.position.y <= stopYLevel)
            {
                StopFall();
            }
        }

        private void StopFall()
        // @@ TODO 
        {
            // Stop checking in FixedUpdate
            isFalling = false; 

            // Stop all physical movement (set velocity to zero before making it kinematic)
            rb.linearVelocity = Vector3.zero; 
            rb.isKinematic = true;

            // Clamp position for perfect accuracy.
            Vector3 finalPosition = transform.position;
            finalPosition.y = stopYLevel;
            transform.position = finalPosition;

        }
    }
}


