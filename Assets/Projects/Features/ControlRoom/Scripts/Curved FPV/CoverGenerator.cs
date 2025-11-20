using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HemisphereGenerator : MonoBehaviour
{
    public enum CutMode { Equator, Meridian }          // Equator: upper/lower ; Meridian: front/back
    public enum Half { Upper, Lower, Front, Back }     // Which half to keep

    [Header("Shape")]
    [Min(0.001f)] public float radius = 1f;
    [Range(3, 256)] public int longitudeSegments = 48;  // around (theta)
    [Range(2, 256)] public int latitudeSegments  = 24;  // top-to-bottom (phi)
    public CutMode cutMode = CutMode.Equator;
    public Half half = Half.Upper;
    public bool inwardNormals = false;

    [Header("Cap (only for Equator cuts)")]
    public bool addFlatCap = false;     // flat disk to close the rim
    [Range(1, 64)] public int capSegments = 24;

    [Header("Update")]
    public bool autoRegenerate = true;

    MeshFilter mf;
    Mesh mesh;

    void OnValidate()
    {
        if (autoRegenerate) Regenerate();
    }

    void Reset()
    {
        Regenerate();
    }

    [ContextMenu("Regenerate")]
    public void Regenerate()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mesh)
        {
            mesh = new Mesh();
            mesh.name = "HemisphereMesh";
            mesh.indexFormat = (longitudeSegments * latitudeSegments > 65000) ?
                UnityEngine.Rendering.IndexFormat.UInt32 :
                UnityEngine.Rendering.IndexFormat.UInt16;
            mf.sharedMesh = mesh;
        }

        BuildHemisphere(mesh);
    }

    void BuildHemisphere(Mesh m)
    {
        // Spherical coordinates:
        // phi   ∈ [0, π] from +Y (north pole) down to -Y (south pole)
        // theta ∈ [-π, π] around Y axis: +Z at 0, +X at +π/2
        //
        // Standard mapping:
        // x = r * sin(phi) * sin(theta)
        // y = r * cos(phi)
        // z = r * sin(phi) * cos(theta)
        //
        // For hemispheres, we clamp one of the ranges and remap UVs to [0,1].

        // Determine angular ranges
        float phiMin, phiMax, thetaMin, thetaMax;

        if (cutMode == CutMode.Equator)
        {
            // Upper = phi [0, π/2], Lower = [π/2, π]
            phiMin  = (half == Half.Upper) ? 0f         : 0.5f * Mathf.PI;
            phiMax  = (half == Half.Upper) ? 0.5f* Mathf.PI : Mathf.PI;
            // full theta
            thetaMin = -Mathf.PI;
            thetaMax =  Mathf.PI;
        }
        else
        {
            // Meridian cut (front/back halves).
            // Front (Z+): theta [-π/2, +π/2]
            // Back  (Z-): theta [ +π/2, +3π/2]  (or [-π/2, +π/2] shifted)
            thetaMin = (half == Half.Front) ? -0.5f * Mathf.PI :  0.5f * Mathf.PI;
            thetaMax = (half == Half.Front) ?  0.5f * Mathf.PI :  1.5f * Mathf.PI;
            // full phi
            phiMin  = 0f;
            phiMax  = Mathf.PI;
        }

        int lon = Mathf.Max(3, longitudeSegments);
        int lat = Mathf.Max(2, latitudeSegments);

        int vertCount = (lon + 1) * (lat + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector3[] norms = new Vector3[vertCount];
        Vector2[] uvs   = new Vector2[vertCount];
        int[] indices   = new int[lon * lat * 6];

        // Precompute ranges for UV remap (so the half fills 0..1)
        float dPhi   = (phiMax   - phiMin);
        float dTheta = (thetaMax - thetaMin);

        int vi = 0;
        for (int y = 0; y <= lat; y++)
        {
            float v = (float)y / lat;
            float phi = phiMin + v * dPhi;

            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);

            for (int x = 0; x <= lon; x++)
            {
                float u = (float)x / lon;
                float theta = thetaMin + u * dTheta;

                float sinTheta = Mathf.Sin(theta);
                float cosTheta = Mathf.Cos(theta);

                // Spherical to Cartesian
                float px = radius * sinPhi * sinTheta;
                float py = radius * cosPhi;
                float pz = radius * sinPhi * cosTheta;

                Vector3 pos = new Vector3(px, py, pz);
                Vector3 normal = pos.normalized;

                if (inwardNormals) normal = -normal;

                verts[vi] = pos;
                norms[vi] = normal;
                // UVs remapped to the restricted ranges so hemisphere uses full 0..1
                uvs[vi]   = new Vector2(u, 1f - v);
                vi++;
            }
        }

        // Triangles
        int ti = 0;
        for (int y = 0; y < lat; y++)
        {
            for (int x = 0; x < lon; x++)
            {
                int i0 = (y    ) * (lon + 1) + x;
                int i1 = (y    ) * (lon + 1) + x + 1;
                int i2 = (y + 1) * (lon + 1) + x;
                int i3 = (y + 1) * (lon + 1) + x + 1;

                if (!inwardNormals)
                {
                    indices[ti++] = i0; indices[ti++] = i2; indices[ti++] = i1;
                    indices[ti++] = i1; indices[ti++] = i2; indices[ti++] = i3;
                }
                else
                {
                    // flip winding for inward
                    indices[ti++] = i0; indices[ti++] = i1; indices[ti++] = i2;
                    indices[ti++] = i1; indices[ti++] = i3; indices[ti++] = i2;
                }
            }
        }

        // Assign to mesh
        m.Clear();
        m.vertices  = verts;
        m.normals   = norms;
        m.uv        = uvs;
        m.triangles = indices;
        m.RecalculateBounds();

        // Optional flat cap (only meaningful for Equator halves)
        if (cutMode == CutMode.Equator && addFlatCap)
            AppendEquatorCap(m, phiMin, phiMax, thetaMin, thetaMax);
    }

    void AppendEquatorCap(Mesh m, float phiMin, float phiMax, float thetaMin, float thetaMax)
    {
        // Only valid for Equator halves (phi range spans either [0..π/2] or [π/2..π]).
        // We add a flat disk at the rim plane (y = 0).
        if (!(Mathf.Approximately(phiMin, 0f) && Mathf.Approximately(phiMax, 0.5f * Mathf.PI)) &&
            !(Mathf.Approximately(phiMin, 0.5f * Mathf.PI) && Mathf.Approximately(phiMax, Mathf.PI)))
            return;

        bool isUpper = Mathf.Approximately(phiMax, 0.5f * Mathf.PI); // upper dome

        var baseVerts = m.vertices;
        var baseNorms = m.normals;
        var baseUVs   = m.uv;
        var baseIdx   = m.triangles;

        int ring = Mathf.Max(3, capSegments);

        // Build a center + ring vertices forming a flat disk on Y=0
        Vector3 center = new Vector3(0f, 0f, 0f);
        Vector3 nrm    = isUpper ? Vector3.down : Vector3.up; // normals pointing inside the hemisphere
        if (inwardNormals) nrm = -nrm;

        int vStart = baseVerts.Length;
        int newVertCount = 1 + ring;
        Vector3[] capVerts = new Vector3[newVertCount];
        Vector3[] capNorms = new Vector3[newVertCount];
        Vector2[] capUVs   = new Vector2[newVertCount];

        capVerts[0] = center;
        capNorms[0] = nrm;
        capUVs[0]   = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < ring; i++)
        {
            float t = (float)i / ring;
            float theta = -Mathf.PI + t * (2f * Mathf.PI);  // full circle (we're capping the equator)
            float x = radius * Mathf.Sin(theta);
            float z = radius * Mathf.Cos(theta);
            capVerts[1 + i] = new Vector3(x, 0f, z);
            capNorms[1 + i] = nrm;
            // simple planar UV
            capUVs[1 + i] = new Vector2(0.5f + x / (2f * radius), 0.5f + z / (2f * radius));
        }

        // Triangles for the disk
        int triCount = ring * 3;
        int[] capIdx = new int[triCount];

        int ti = 0;
        for (int i = 0; i < ring; i++)
        {
            int a = vStart;                // center
            int b = vStart + 1 + i;
            int c = vStart + 1 + ((i + 1) % ring);

            // Winding so the cap normal faces the same as we set above
            if (!inwardNormals)
            {
                if (isUpper) { capIdx[ti++] = a; capIdx[ti++] = c; capIdx[ti++] = b; }
                else         { capIdx[ti++] = a; capIdx[ti++] = b; capIdx[ti++] = c; }
            }
            else
            {
                if (isUpper) { capIdx[ti++] = a; capIdx[ti++] = b; capIdx[ti++] = c; }
                else         { capIdx[ti++] = a; capIdx[ti++] = c; capIdx[ti++] = b; }
            }
        }

        // Merge into one mesh
        var mergedVerts = new Vector3[baseVerts.Length + capVerts.Length];
        var mergedNorms = new Vector3[baseNorms.Length + capNorms.Length];
        var mergedUVs   = new Vector2[baseUVs.Length + capUVs.Length];
        var mergedIdx   = new int[baseIdx.Length + capIdx.Length];

        baseVerts.CopyTo(mergedVerts, 0);
        baseNorms.CopyTo(mergedNorms, 0);
        baseUVs.CopyTo(mergedUVs, 0);
        baseIdx.CopyTo(mergedIdx, 0);

        capVerts.CopyTo(mergedVerts, baseVerts.Length);
        capNorms.CopyTo(mergedNorms, baseNorms.Length);
        capUVs.CopyTo(mergedUVs, baseUVs.Length);

        // Offset cap indices
        int offset = baseVerts.Length;
        for (int i = 0; i < capIdx.Length; i++)
            mergedIdx[baseIdx.Length + i] = capIdx[i] + offset;

        m.Clear();
        m.vertices  = mergedVerts;
        m.normals   = mergedNorms;
        m.uv        = mergedUVs;
        m.triangles = mergedIdx;
        m.RecalculateBounds();
    }
}
