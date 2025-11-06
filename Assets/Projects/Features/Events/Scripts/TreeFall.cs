using UnityEngine;

public class TreeFall : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Fall Settings")]
    [SerializeField] private bool fallOnStart = false;
    [SerializeField] private float fallForce = 50f;
    [SerializeField] private float forceHeight = 2f; // Height at which force is applied
    [SerializeField] private Vector3 fallDirection = Vector3.forward;

    void Start()
    {
        // Get or add Rigidbody component
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Start frozen in place
        rb.isKinematic = true;

        // Optional: fall immediately on start for testing
        if (fallOnStart)
        {
            TriggerFall();
        }
    }

    /// <summary>
    /// Call this method to make the tree fall down
    /// </summary>
    public void TriggerFall()
    {
        // Enable physics
        rb.isKinematic = false;

        // Apply force at a point above the base to create rotation
        Vector3 forcePosition = transform.position + Vector3.up * forceHeight;
        rb.AddForceAtPosition(fallDirection.normalized * fallForce, forcePosition, ForceMode.Impulse);
    }

    /// <summary>
    /// Call this method to make the tree fall in a specific direction
    /// </summary>
    public void TriggerFall(Vector3 direction)
    {
        fallDirection = direction;
        TriggerFall();
    }
}
