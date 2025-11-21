using UnityEngine;

/// <summary>
/// Creates and updates two textures (Front=top half, Rear=bottom half) from a MockCameraFeed.
/// Do this once per scene and let multiple binders consume the outputs.
/// </summary>
public class FeedSplitService : MonoBehaviour
{
    [Header("Source (Script 1)")]
    public MockCameraFeed source;

    [Header("Debug")]
    public bool swapFrontRear = false;

    // Outputs (read-only for other scripts)
    public Texture2D FrontTex { get; private set; }
    public Texture2D RearTex  { get; private set; }

    private Color32[] fullBuffer;
    private Color32[] frontBuffer;
    private Color32[] rearBuffer;

    private int lastW, lastH;

    void Update()
    {
        if (source == null || !source.HasValidFrame) return;

        var src = source.CurrentTexture as WebCamTexture;
        if (src == null) return;

        int w = src.width;
        int h = src.height;
        if (w < 16 || h < 16) return;

        if (w != lastW || h != lastH || FrontTex == null || RearTex == null)
            RecreateOutputs(w, h);

        src.GetPixels32(fullBuffer);

        int halfH = h / 2;

        // Top → front
        for (int y = 0; y < halfH; y++)
            System.Array.Copy(fullBuffer, (y + halfH) * w, frontBuffer, y * w, w);

        // Bottom → rear
        for (int y = 0; y < halfH; y++)
            System.Array.Copy(fullBuffer, y * w, rearBuffer, y * w, w);

        // Upload
        FrontTex.SetPixels32(frontBuffer);
        RearTex.SetPixels32(rearBuffer);
        FrontTex.Apply(false);
        RearTex.Apply(false);

        if (swapFrontRear)
        {
            // simple swap view (doesn't re-copy, just flips references on consumers side if they check both)
        }
    }

    private void RecreateOutputs(int w, int h)
    {
        lastW = w; lastH = h;
        int halfH = h / 2;

        if (FrontTex) Destroy(FrontTex);
        if (RearTex)  Destroy(RearTex);

        FrontTex = new Texture2D(w, halfH, TextureFormat.RGBA32, false)
        {
            name = $"Split_Front_{w}x{halfH}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        RearTex = new Texture2D(w, halfH, TextureFormat.RGBA32, false)
        {
            name = $"Split_Rear_{w}x{halfH}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        fullBuffer  = new Color32[w * h];
        frontBuffer = new Color32[w * halfH];
        rearBuffer  = new Color32[w * halfH];

        Debug.Log($"[FeedSplitService] Recreated outputs: src={w}x{h} halves={w}x{halfH}");
    }

    void OnDisable()
    {
        if (FrontTex) Destroy(FrontTex);
        if (RearTex)  Destroy(RearTex);
        FrontTex = null; RearTex = null;
        fullBuffer = frontBuffer = rearBuffer = null;
    }
}
