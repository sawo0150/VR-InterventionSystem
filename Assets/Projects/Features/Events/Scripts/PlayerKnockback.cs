using UnityEngine;

namespace Project.Event
{
    [RequireComponent(typeof(CharacterController))]
    
    public class PlayerKnockback : MonoBehaviour
    {
        [SerializeField] private float drag = 5.0f;
        [SerializeField] private float impactThreshold = 0.2f;

        private CharacterController controller;
        private Vector3 impactVector = Vector3.zero;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            // Check if the impact force is still significant
            if (impactVector.magnitude > impactThreshold)
            {
                // Apply the impact force as movement
                // This combines with CharacterController's own gravity/moves
                controller.Move(impactVector * Time.deltaTime);
                
                // Apply drag to the impact force
                impactVector = Vector3.Lerp(impactVector, Vector3.zero, drag * Time.deltaTime);
            }
            else
            {
                // Snap to zero if the force is very small
                impactVector = Vector3.zero;
            }

        }

        // Public method called by external scripts
        public void AddImpact(Vector3 direction, float force)
        {
            direction.Normalize();
        
            // Force knockback to be horizontal (no flying up)
            direction.y = 0; 
        
            // Add the new impact force to any existing impact (allows impacts to stack)
            impactVector += direction * force;
        }
    }
}

