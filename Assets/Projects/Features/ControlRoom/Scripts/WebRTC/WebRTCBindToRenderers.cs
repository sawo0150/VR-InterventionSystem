using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WebRTC에서 받은 단일 텍스처를 상/하 절반으로 나눠
/// frontTargets / rearTargets 에 GPU에서 바로 바인딩하는 바인더.
/// FeedSplitService를 전혀 사용하지 않는다.
/// </summary>
public class WebRTCBindToRenderers : MonoBehaviour
{
    [Header("Source (WebRTC)")]
    public WebRTCReceiver_controlRoom webrtcSource;

    [Header("Targets")]
    public List<Renderer> frontTargets = new List<Renderer>();
    public List<Renderer> rearTargets = new List<Renderer>();

    [Header("Material Property")]
    [Tooltip("URP Lit uses _BaseMap. Built-in Standard uses _MainTex.")]
    public string textureProperty = "_BaseMap";
    public bool alsoSetMainTex = true;

    [Header("Options")]
    [Tooltip("true면 위쪽이 Front, false면 위쪽이 Rear")]
    public bool topIsFront = true;

    private MaterialPropertyBlock mpb;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (webrtcSource == null || !webrtcSource.HasRemoteFrame) return;

        var tex = webrtcSource.RemoteTexture;
        if (!tex) return;

        // ST(Tiling/Offset) 계산
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
