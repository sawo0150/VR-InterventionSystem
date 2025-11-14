using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class WebcamScreen : MonoBehaviour
{
    public LocalWebcamSource source;   // Inspector에서 연결
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (source != null && source.Texture != null)
        {
            rend.material.mainTexture = source.Texture;
        }

    }
}
