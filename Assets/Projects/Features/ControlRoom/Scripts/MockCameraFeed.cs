using UnityEngine;

/// <summary>
/// Temporary camera feed provider.
/// - Starts a WebCamTexture and keeps it running
/// - Exposes the current Texture so other scripts can consume it
/// Replace this later with your network-fed image source.
/// </summary>
public class MockCameraFeed : MonoBehaviour
{
    [Header("Device selection (optional)")]
    [Tooltip("If not empty, picks the first device whose name contains this text.")]
    public string preferredDeviceContains = "Insta";  // e.g., "Insta", "Logi", etc.

    [Header("Requested capture settings (Unity negotiates)")]
    public int requestedWidth  = 1920;
    public int requestedHeight = 1080;
    public int requestedFPS    = 30;

    [Header("Behavior")]
    public bool playOnStart = true;

    private WebCamTexture webcamTex;

    /// <summary> Returns the live WebCamTexture (null if not started). </summary>
    public WebCamTexture WebcamTex => webcamTex;

    /// <summary> A generic Texture handle for consumers. </summary>
    public Texture CurrentTexture => webcamTex;

    /// <summary> True if we have a running feed with non-zero dimensions. </summary>
    public bool HasValidFrame => webcamTex != null && webcamTex.didUpdateThisFrame && webcamTex.width > 16;

    private void Start()
    {
        if (playOnStart)
            StartCamera();
    }

    public void StartCamera()
    {
        if (webcamTex != null && webcamTex.isPlaying) return;

        var deviceName = PickDeviceName();
        webcamTex = string.IsNullOrEmpty(deviceName)
            ? new WebCamTexture(requestedWidth, requestedHeight, requestedFPS)
            : new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);

        if (webcamTex == null)
        {
            Debug.LogError("[MockCameraFeed] Failed to create WebCamTexture.");
            return;
        }

        webcamTex.Play();

        if (!webcamTex.isPlaying)
        {
            Debug.LogError("[MockCameraFeed] WebCamTexture failed to start.");
        }
        else
        {
            Debug.Log($"[MockCameraFeed] Started camera: {(string.IsNullOrEmpty(deviceName) ? "(default)" : deviceName)} @ {requestedWidth}x{requestedHeight}@{requestedFPS}.");
        }
    }

    public void StopCamera()
    {
        if (webcamTex != null && webcamTex.isPlaying)
        {
            webcamTex.Stop();
        }
    }

    private string PickDeviceName()
    {
        var devices = WebCamTexture.devices;
        if (devices == null || devices.Length == 0)
        {
            Debug.LogWarning("[MockCameraFeed] No webcam devices found.");
            return null;
        }

        if (!string.IsNullOrEmpty(preferredDeviceContains))
        {
            foreach (var d in devices)
            {
                if (d.name.ToLower().Contains(preferredDeviceContains.ToLower()))
                    return d.name;
            }
        }

        // Fallback to first device
        return devices[0].name;
    }

    private void OnDisable()  => StopCamera();
    private void OnApplicationQuit() => StopCamera();
}
