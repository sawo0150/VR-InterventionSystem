using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // [필수] UI 컴포넌트를 인식하기 위해 추가

/// <summary>
/// 기존 기능: 3D Mesh Renderer에 웹캠/WebRTC 영상 분할 적용
/// 추가 기능: UI RawImage에도 동일한 영상 분할 적용 (에러 방지 포함)
/// </summary>
public class UnifiedVideoBindToRenderers : MonoBehaviour
{
    public enum SourceType { MockCamera, WebRTC }

    [Header("Source")]
    public SourceType sourceType = SourceType.MockCamera;
    public MockCameraFeed mockSource;                    // WebCamTexture 소스
    public WebRTCReceiver_controlRoom webrtcSource;      // WebRTC 소스

    [Header("3D Targets (Mesh Renderer)")]
    public List<Renderer> frontTargets = new List<Renderer>();
    public List<Renderer> rearTargets = new List<Renderer>();

    // ▼▼▼ [추가됨] UI 타겟 리스트 (비워둬도 에러 안 남) ▼▼▼
    [Header("UI Targets (Raw Image)")]
    public List<RawImage> frontUiTargets = new List<RawImage>();
    public List<RawImage> rearUiTargets = new List<RawImage>();

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
                tex = mockSource.CurrentTexture; 
                break;

            case SourceType.WebRTC:
                if (webrtcSource == null || !webrtcSource.HasRemoteFrame) return;
                tex = webrtcSource.RemoteTexture; 
                break;
        }

        if (!tex) return;

        // Vector4(TilingX, TilingY, OffsetX, OffsetY)
        Vector4 frontST, rearST;

        if (topIsFront)
        {
            // Front: 위쪽 절반 (Offset Y = 0.5)
            frontST = new Vector4(1f, 0.5f, 0f, 0.5f);
            // Rear: 아래쪽 절반 (Offset Y = 0.0)
            rearST = new Vector4(1f, 0.5f, 0f, 0f);
        }
        else
        {
            frontST = new Vector4(1f, 0.5f, 0f, 0f);
            rearST = new Vector4(1f, 0.5f, 0f, 0.5f);
        }

        // --- [기존 로직] 3D Renderer 적용 ---
        // 리스트가 null이어도 안전하게 넘어가도록 ?. 연산자 사용 가능하지만, 
        // Unity Inspector에서 초기화되므로 foreach도 안전함.
        if (frontTargets != null)
        {
            foreach (var r in frontTargets) ApplyTextureHalf(r, tex, frontST);
        }
        if (rearTargets != null)
        {
            foreach (var r in rearTargets) ApplyTextureHalf(r, tex, rearST);
        }

        // --- [추가 로직] UI RawImage 적용 ---
        // 셰이더의 Vector4(Tiling, Offset)를 UI의 Rect(Pos, Size)로 변환
        // Rect(x, y, width, height) <-> Vector4(OffsetX, OffsetY, TilingX, TilingY)
        // 주의: 코드상의 st 순서는 (TilingX, TilingY, OffsetX, OffsetY) 임.
        Rect frontRect = new Rect(frontST.z, frontST.w, frontST.x, frontST.y);
        Rect rearRect = new Rect(rearST.z, rearST.w, rearST.x, rearST.y);

        if (frontUiTargets != null)
        {
            foreach (var img in frontUiTargets) ApplyTextureHalfUI(img, tex, frontRect);
        }
        if (rearUiTargets != null)
        {
            foreach (var img in rearUiTargets) ApplyTextureHalfUI(img, tex, rearRect);
        }
    }

    // 기존 메서드 (건드리지 않음)
    private void ApplyTextureHalf(Renderer r, Texture tex, Vector4 st)
    {
        if (!r || !tex) return; // r이 삭제되었거나 없으면 패스

        r.GetPropertyBlock(mpb);

        mpb.SetTexture(textureProperty, tex);
        string stProp = textureProperty + "_ST";
        mpb.SetVector(stProp, st);

        if (alsoSetMainTex && textureProperty != "_MainTex")
        {
            mpb.SetTexture("_MainTex", tex);
            mpb.SetVector("_MainTex_ST", st);
        }

        r.SetPropertyBlock(mpb);
    }

    // [추가 메서드] UI용 적용 함수
    private void ApplyTextureHalfUI(RawImage img, Texture tex, Rect uvRect)
    {
        if (!img || !tex) return; // UI가 삭제되었거나 없으면 패스

        // 불필요한 재할당 방지 (최적화)
        if (img.texture != tex) 
            img.texture = tex;
            
        if (img.uvRect != uvRect) 
            img.uvRect = uvRect;
    }
}