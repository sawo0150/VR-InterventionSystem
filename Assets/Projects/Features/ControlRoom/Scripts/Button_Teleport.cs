using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils; // XROrigin

public class XRMoveOriginOnPress : MonoBehaviour
{
    [Header("XR Rig")]
    public XROrigin xrOrigin;                       // Assign your XR Origin here
    public UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportProvider;  // Optional (for the teleport way)

    [Header("Destination")]
    public Transform destination;                   // Where you want to go

    [Header("Options")]
    public bool useTeleportProvider = true;         // A) Teleport (nice)  B) Direct snap (off)
    public bool matchDestinationYaw = true;         // Face the same yaw as destination
    public bool keepHeadYawIfNotMatching = true;    // If not matching yaw, keep user’s current yaw

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable>();
        if (!interactable)
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        interactable.selectEntered.AddListener(OnPressed);
    }

    void OnDestroy()
    {
        if (interactable)
            interactable.selectEntered.RemoveListener(OnPressed);
    }

    private void OnPressed(SelectEnterEventArgs _)
    {
        if (!xrOrigin || !destination) return;

        if (useTeleportProvider && teleportProvider)
        {
            // ----- A) Proper Teleport -----
            var req = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest
            {
                destinationPosition = destination.position,
                destinationRotation = GetTargetYaw(),
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.TargetUpAndForward
            };
            teleportProvider.QueueTeleportRequest(req);
        }
        else
        {
            // ----- B) Direct Snap (instant) -----
            // Place the XR Origin so that the HMD ends up at destination.position.
            // Keep CharacterController safe while moving.
            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;

            // Head (camera) offset inside the Origin space
            Vector3 camOffset = xrOrigin.CameraInOriginSpacePos;

            // Compute new origin pose
            Quaternion targetRot = GetTargetYaw();
            // Rotate the offset by the target yaw so the camera lands exactly on destination
            Vector3 rotatedOffset = targetRot * camOffset;
            Vector3 newOriginPos = destination.position - rotatedOffset;

            xrOrigin.transform.SetPositionAndRotation(newOriginPos, targetRot);

            if (cc) cc.enabled = true;
        }
    }

    private Quaternion GetTargetYaw()
    {
        if (matchDestinationYaw)
        {
            // Use destination yaw (keep horizon level)
            float y = destination.eulerAngles.y;
            return Quaternion.Euler(0f, y, 0f);
        }
        else if (keepHeadYawIfNotMatching && xrOrigin.Camera)
        {
            // Keep user’s current head yaw
            float y = xrOrigin.Camera.transform.eulerAngles.y;
            return Quaternion.Euler(0f, y, 0f);
        }
        else
        {
            // No rotation change
            return xrOrigin.transform.rotation;
        }
    }
}
