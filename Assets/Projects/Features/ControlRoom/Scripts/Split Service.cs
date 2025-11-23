using UnityEngine;

/// <summary>
/// Creates and updates two textures (Front=top half, Rear=bottom half) from a MockCameraFeed.
/// Do this once per scene and let multiple binders consume the outputs.
/// </summary>
public class FeedSplitService : MonoBehaviour
{
    [Header("Source (Script 1)")]
    public MockCameraFeed mockSource;                    // 기존 웹캠용만 지원


    [Header("Debug")]
    public bool swapFrontRear = false;
    
    public bool debugLog = false;
    [Tooltip("몇 프레임마다 한 번씩 로그를 찍을지")]
    public int debugLogInterval = 300;

    private int _frameCount = 0;

    // Outputs (read-only for other scripts)
    public Texture2D FrontTex { get; private set; }
    public Texture2D RearTex  { get; private set; }

    private Color32[] fullBuffer;
    private Color32[] frontBuffer;
    private Color32[] rearBuffer;

    private int lastW, lastH;

    void Update()
    {
        _frameCount++;
        bool doLog = debugLog && (debugLogInterval > 0) && (_frameCount % debugLogInterval == 0);

        // WebCamTexture 또는 Texture2D 둘 다 받을 수 있게 Texture로 선언
        Texture src = null;
        
        // 1) 소스는 항상 MockCameraFeed
        if (mockSource == null || !mockSource.HasValidFrame) return;
        src = mockSource.CurrentTexture;
        if (doLog && src != null)
            Debug.Log($"[Split] Source=MockCamera tex={src} type={src.GetType()} size={src.width}x{src.height}");

        if (src == null) return;

        int w = src.width;
        int h = src.height;

        if (w < 16 || h < 16) return;

        if (w != lastW || h != lastH || FrontTex == null || RearTex == null)
            RecreateOutputs(w, h);
        
        // 2) 실제 픽셀을 CPU 버퍼로 복사
        //    - WebCamTexture : GetPixels32(Color32[])
        //    - Texture2D     : GetPixels32() 로 배열 받아서 복사
        if (src is WebCamTexture camTex)
        {
            camTex.GetPixels32(fullBuffer);
        }
        else if (src is Texture2D tex2D)
        {
            var pixels = tex2D.GetPixels32(); // 새 배열
            if (pixels.Length != fullBuffer.Length)
            {
                // 혹시 사이즈가 안 맞으면 다시 잡아준다
                fullBuffer = new Color32[pixels.Length];
            }
            System.Array.Copy(pixels, fullBuffer, pixels.Length);
        }
        else
        {
            Debug.LogWarning($"[FeedSplitService] Unsupported texture type: {src.GetType()}");
            return;
        }

        if (doLog)
        {
            // fullBuffer에서 몇 개 픽셀 샘플
            int midIndex = (h / 2) * w + (w / 2);
            Color32 p0   = fullBuffer[0];
            Color32 pmid = fullBuffer[midIndex];
            Color32 plast= fullBuffer[fullBuffer.Length - 1];
            Debug.Log($"[Split] fullBuffer sample p0={p0} mid={pmid} last={plast}");
        }

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

        if (doLog)
        {
            // front / rear 텍스처의 가운데 픽셀도 한 번 찍어본다
            int fx = w / 2;
            int fy = (halfH / 2);
            var fCol = FrontTex.GetPixel(fx, fy);
            var rCol = RearTex.GetPixel(fx, fy);
            Debug.Log($"[Split] FrontTex center={fCol}, RearTex center={rCol}");
        }

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
