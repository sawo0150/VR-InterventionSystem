using UnityEngine;

namespace Project.Event
{
    [RequireComponent(typeof(Rigidbody))]
    public class MeteorImpact : MonoBehaviour
    {
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float impactForce = 20f;
        [SerializeField] private float destroyDelay = -1f;
        [SerializeField] private AudioClip impactSound; 
        [SerializeField] private GameObject impactEffectPrefab;

        private bool hasImpacted = false;
        private Rigidbody rb;
        private Collider col;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();

            if (!col.isTrigger)
            {
                Debug.LogWarning(gameObject.name + ": 'MeteorImpact' requires 'Is Trigger = true'. Auto-fixing.");
                col.isTrigger = true;
            }

            if (!rb.isKinematic)
            {
                Debug.LogWarning(gameObject.name + ": Setting Rigidbody to 'Is Kinematic = true' for trigger-based impact.");
                rb.isKinematic = true;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // prevent duplicated calls, impacted to only target tag
            if (hasImpacted || !other.CompareTag(targetTag))
            {
                return;
            }

            // Find the player's knockback script (check parent
            PlayerKnockback playerKnockback = other.GetComponentInParent<PlayerKnockback>();

            if (playerKnockback != null)
            {
                // Calculate push direction (from meteor center to player).
                Vector3 direction = playerKnockback.transform.position - transform.position;

                // Apply the impact via the player's script.
                playerKnockback.AddImpact(direction, impactForce);

                // Spawn impact effects at the meteor's position.
                if (impactEffectPrefab != null)
                {
                    Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                }
                if (impactSound != null)
                {
                    AudioSource.PlayClipAtPoint(impactSound, transform.position);
                }

                // Ensure this only happens once.
                hasImpacted = true;

                // Destroy the meteor after a short delay (allows sound to play).
                if (destroyDelay > 0)
                {
                    Destroy(gameObject, destroyDelay);
                }
            }
        }
    }
}