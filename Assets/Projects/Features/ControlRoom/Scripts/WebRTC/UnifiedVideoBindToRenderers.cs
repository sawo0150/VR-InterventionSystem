using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 텍스처(웹캠 또는 WebRTC)를 상/하 절반으로 나눠
/// frontTargets / rearTargets 에 GPU에서 바로 바인딩하는 바인더.
/// FeedSplitService를 사용하지 않는다.
/// </summary>
public class UnifiedVideoBindToRenderers : MonoBehaviour
{
    public enum SourceType { MockCamera, WebRTC }

    [Header("Source")]
    public SourceType sourceType = SourceType.MockCamera;
    public MockCameraFeed mockSource;                    // WebCamTexture 소스
    public WebRTCReceiver_controlRoom webrtcSource;      // WebRTC 소스

    [Header("Targets")]
    public List<Renderer> frontTargets = new List<Renderer>();
    public List<Renderer> rearTargets = new List<Renderer>();

    [Header("Material Property")]
    [Tooltip("URP Lit uses _BaseMap. Built-in Standard uses _MainTex.")]
    public string textureProperty = "_BaseMap";
    public bool alsoSetMainTex = true;

    [Header("Options")]
    [Tooltip("true면 위쪽 half가 Front, false면 위쪽 half가 Rear")]
    public bool topIsFront = true;

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        Texture tex = null;

        switch (sourceType)
        {
            case SourceType.MockCamera:
                if (mockSource == null || !mockSource.HasValidFrame) return;
                tex = mockSource.CurrentTexture; // WebCamTexture (GPU)
                break;

            case SourceType.WebRTC:
                if (webrtcSource == null || !webrtcSource.HasRemoteFrame) return;
                tex = webrtcSource.RemoteTexture; // WebRTC GPU 텍스처
                break;
        }

        if (!tex) return;

        // ST(Tiling/Offset) 설정
        // Vector4(xScale, yScale, xOffset, yOffset)
        Vector4 frontST, rearST;

        if (topIsFront)
        {
            // Front: 위쪽 절반 (y: 0.5~1.0)
            frontST = new Vector4(1f, 0.5f, 0f, 0.5f);
            // Rear: 아래쪽 절반 (y: 0.0~0.5)
            rearST = new Vector4(1f, 0.5f, 0f, 0f);
        }
        else
        {
            // Front: 아래쪽 절반
            frontST = new Vector4(1f, 0.5f, 0f, 0f);
            // Rear: 위쪽 절반
            rearST = new Vector4(1f, 0.5f, 0f, 0.5f);
        }

        foreach (var r in frontTargets)
            ApplyTextureHalf(r, tex, frontST);

        foreach (var r in rearTargets)
            ApplyTextureHalf(r, tex, rearST);
    }

    private void ApplyTextureHalf(Renderer r, Texture tex, Vector4 st)
    {
        if (!r || !tex) return;

        r.GetPropertyBlock(mpb);

        // 메인 텍스처
        mpb.SetTexture(textureProperty, tex);

        // Tiling/Offset 설정 (_BaseMap_ST 또는 _MainTex_ST)
        string stProp = textureProperty + "_ST";
        mpb.SetVector(stProp, st);

        if (alsoSetMainTex && textureProperty != "_MainTex")
        {
            mpb.SetTexture("_MainTex", tex);
            mpb.SetVector("_MainTex_ST", st);
        }

        r.SetPropertyBlock(mpb);
    }
}
