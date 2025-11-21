using UnityEngine;

/// <summary>
/// Simple bridge: lets another script push a Texture (your front lens)
/// into the material on the CurvedScreenGenerator.
/// </summary>
public class CurvedScreenFeedBinder : MonoBehaviour
{
    public CurvedScreenGenerator curvedScreen; // assign in Inspector

    // Call this every frame (or whenever updated) with the live front lens texture
    public void SetTexture(Texture liveTex)
    {
        if (curvedScreen == null) return;
        if (curvedScreen.screenMaterial == null) return;

        curvedScreen.screenMaterial.mainTexture = liveTex;
    }
}
