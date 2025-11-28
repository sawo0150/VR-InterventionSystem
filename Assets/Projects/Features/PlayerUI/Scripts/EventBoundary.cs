using UnityEngine;

namespace Project
{
    /// <summary>
    /// Marker component for event boundary box colliders.
    /// Attach this to any GameObject with a BoxCollider to define event safe zones.
    /// Multiple EventBoundary components can be used per event - robot must stay within ANY of them.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class EventBoundary : MonoBehaviour
    {
        [Header("Boundary Settings")]
        [Tooltip("Enable to show boundary visualization in editor")]
        public bool showGizmos = true;

        [Tooltip("Color of the boundary gizmo")]
        public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency

        private BoxCollider boxCollider;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();

            // Ensure the collider is set to trigger
            if (!boxCollider.isTrigger)
            {
                Debug.LogWarning($"[EventBoundary] BoxCollider on {gameObject.name} should be set as trigger. Auto-setting to trigger.");
                boxCollider.isTrigger = true;
            }
        }

        /// <summary>
        /// Check if a world position is inside this boundary
        /// </summary>
        public bool ContainsPoint(Vector3 worldPosition)
        {
            if (boxCollider == null) return false;

            // Convert world position to local space
            Vector3 localPoint = transform.InverseTransformPoint(worldPosition);

            // Get bounds in local space
            Vector3 center = boxCollider.center;
            Vector3 halfSize = boxCollider.size * 0.5f;

            // Check if point is within bounds
            return (localPoint.x >= center.x - halfSize.x && localPoint.x <= center.x + halfSize.x) &&
                   (localPoint.y >= center.y - halfSize.y && localPoint.y <= center.y + halfSize.y) &&
                   (localPoint.z >= center.z - halfSize.z && localPoint.z <= center.z + halfSize.z);
        }

        /// <summary>
        /// Get the BoxCollider component
        /// </summary>
        public BoxCollider GetBoxCollider()
        {
            if (boxCollider == null)
                boxCollider = GetComponent<BoxCollider>();
            return boxCollider;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            // Draw wireframe box
            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);

            // Draw semi-transparent filled box
            Color fillColor = gizmoColor;
            fillColor.a = 0.1f;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(col.center, col.size);

            Gizmos.matrix = Matrix4x4.identity;
        }

        private void OnDrawGizmosSelected()
        {
            // Draw brighter when selected
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
