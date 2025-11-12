using UnityEngine;
using UnityEngine.Events;


namespace Project.Event
{
    // Ensure a Collider component (if not, auto add)
    [RequireComponent(typeof(Collider))]
    
    public class OneShotTrigger : MonoBehaviour
    {
        [Tooltip("Tag required to activate the trigger")]
        [SerializeField] private string targetTag = "Player";

        [Space(10)]
        [SerializeField] private UnityEvent onTriggerOnce;
        
        private bool hasFired = false;
        private Collider col;

        private void Awake()
        {
            col = GetComponent<Collider>();
            
            // set 'isTrigger' to prevent collision
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // prevent duplicated calls
            if (hasFired || !other.CompareTag(targetTag))
            {
                return;
            }

            // Lock the trigger
            hasFired = true;

            // Debug.Log(gameObject.name + " trigger is activated");

            // Fire the connected event
            onTriggerOnce.Invoke();
            
            // Remove the trigger
            col.enabled = false;
        }
    }
}
