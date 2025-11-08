using UnityEngine;

public class LocalWebcamSource : MonoBehaviour
{

    public string deviceName = "";
    public int width = 1280;
    public int height = 720;
    public int fps = 30;

    private WebCamTexture webCamTexture;
    public Texture Texture => webCamTexture;   // 외부에서 이 Texture만 쓰게


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("웹캠을 찾을 수 없습니다.");
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = devices[0].name;
        }

        webCamTexture = new WebCamTexture(deviceName, width, height, fps);
        webCamTexture.Play();

    }

    void OnDisable()
    {
        if (webCamTexture != null && webCamTexture.isPlaying)
            webCamTexture.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
