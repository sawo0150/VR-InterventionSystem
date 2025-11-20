using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binds the textures produced by FeedSplitService to any number of renderers.
/// You can have multiple binders in the scene for different screens.
/// </summary>
public class FeedBindToRenderers : MonoBehaviour
{
    [Header("Source (Split Service)")]
    public FeedSplitService splitService;

    [Header("Targets")]
    public List<Renderer> frontTargets = new List<Renderer>();
    public List<Renderer> rearTargets  = new List<Renderer>();

    [Header("Material Property")]
    [Tooltip("URP Lit uses _BaseMap. Built-in Standard uses _MainTex.")]
    public string textureProperty = "_BaseMap";
    public bool alsoSetMainTex = true;

    private MaterialPropertyBlock mpb = null;

    void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (!splitService) return;

        var front = splitService.FrontTex;
        var rear  = splitService.RearTex;
        if (!front || !rear) return;

        // Assign front
        for (int i = 0; i < frontTargets.Count; i++)
            ApplyTexture(frontTargets[i], front);

        // Assign rear
        for (int i = 0; i < rearTargets.Count; i++)
            ApplyTexture(rearTargets[i], rear);
    }

    private void ApplyTexture(Renderer r, Texture tex)
    {
        if (!r || !tex) return;
        r.GetPropertyBlock(mpb);

        // Primary
        mpb.SetTexture(textureProperty, tex);

        // Optional fallback for different shaders
        if (alsoSetMainTex && textureProperty != "_MainTex")
            mpb.SetTexture("_MainTex", tex);

        r.SetPropertyBlock(mpb);
    }
}
