using UnityEngine;

/// <summary>
/// Generates an inward-facing cylindrical "windshield" mesh and assigns a material.
/// You can control radius, arc angle, height, and mesh resolution in the Inspector.
/// 
/// Usage:
/// 1. Add this script to an empty GameObject (e.g. "FrontScreenHolder") that's parented to your XR camera.
/// 2. Press Play OR call GenerateMesh() in Start to build the mesh.
/// 3. Assign a Material that uses your live webcam Texture on its mainTexture.
/// </summary>
[ExecuteInEditMode]
public class CurvedScreenGenerator : MonoBehaviour
{
    [Header("Geometry")]
    [Tooltip("Radius of the cylinder arc (meters). Bigger = further away / flatter.")]
    public float radius = 1.0f;

    [Tooltip("Total horizontal arc in degrees. 180 = half-circle around you.")]
    [Range(10f, 200f)]
    public float arcDegrees = 180f;

    [Tooltip("Vertical size of the screen in meters.")]
    public float height = 1.0f;

    [Header("Mesh Resolution")]
    [Tooltip("Horizontal subdivisions (more = smoother curve).")]
    [Range(3, 256)]
    public int segmentsHorizontal = 64;

    [Tooltip("Vertical subdivisions.")]
    [Range(2, 128)]
    public int segmentsVertical = 16;

    [Header("Rendering")]
    [Tooltip("Material that will display the camera feed. The script will assign this to the MeshRenderer.")]
    public Material screenMaterial;

    // internally cached mesh objects
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void OnEnable()
    {
        EnsureComponents();
        GenerateMesh();
    }

    // If you tweak values in the Inspector in Edit mode, update mesh live.
    void OnValidate()
    {
        // Clamp sanity
        radius = Mathf.Max(0.01f, radius);
        height = Mathf.Max(0.01f, height);
        segmentsHorizontal = Mathf.Max(3, segmentsHorizontal);
        segmentsVertical = Mathf.Max(2, segmentsVertical);

        EnsureComponents();
        GenerateMesh();
    }

    private void EnsureComponents()
    {
        if (!meshFilter)
        {
            meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter) meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        if (!meshRenderer)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (!meshRenderer) meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        if (screenMaterial != null)
        {
            meshRenderer.sharedMaterial = screenMaterial;
        }
    }

    /// <summary>
    /// Builds a curved mesh that faces inward toward local +Z (camera looking forward).
    /// The mesh is centered vertically around y=0, and centered horizontally at local Z axis.
    /// </summary>
    public void GenerateMesh()
    {
        Mesh m = new Mesh();
        m.name = "CurvedScreenMesh";

        // We'll build a grid:
        // u = horizontal (theta around the arc)
        // v = vertical (y up)
        //
        // theta runs from -arc/2 to +arc/2
        // y runs from -height/2 to +height/2
        //
        // Vertex position on cylinder surface:
        // x = radius * sin(theta)
        // z = radius * cos(theta)
        //
        // We want the normals to face INWARD toward the viewer at origin.
        // For an inward-facing surface, we will flip triangle winding order.

        int vertCountX = segmentsHorizontal + 1;
        int vertCountY = segmentsVertical + 1;
        Vector3[] verts = new Vector3[vertCountX * vertCountY];
        Vector3[] normals = new Vector3[vertCountX * vertCountY];
        Vector2[] uvs = new Vector2[vertCountX * vertCountY];

        float halfArcRad = (arcDegrees * 0.5f) * Mathf.Deg2Rad;
        float halfHeight = height * 0.5f;

        for (int y = 0; y < vertCountY; y++)
        {
            // v from 0..1
            float v01 = (float)y / (vertCountY - 1);

            // world Y position
            float yPos = Mathf.Lerp(-halfHeight, +halfHeight, v01);

            for (int x = 0; x < vertCountX; x++)
            {
                // u from 0..1
                float u01 = (float)x / (vertCountX - 1);

                // theta from -halfArc to +halfArc
                float theta = Mathf.Lerp(-halfArcRad, +halfArcRad, u01);

                float sinT = Mathf.Sin(theta);
                float cosT = Mathf.Cos(theta);

                // position on cylinder
                float px = radius * sinT;
                float pz = radius * cosT;
                float py = yPos;

                int idx = y * vertCountX + x;
                verts[idx] = new Vector3(px, py, pz);

                // Normal should point toward the center (0,py,0) approximately:
                // center of curvature is at (0,py,0) but the viewer (camera) will
                // likely sit near (0,0,0). We'll just use -position on XZ plane.
                Vector3 inward = new Vector3(-sinT, 0f, -cosT);
                normals[idx] = inward.normalized;

                // UV mapping:
                // Horizontal 0..1 across the arc.
                // Vertical 0..1 bottom->top.
                // This assumes your texture is already cropped to just the lens.
                uvs[idx] = new Vector2(u01, v01);
            }
        }

        // Triangles
        int quads = segmentsHorizontal * segmentsVertical;
        int[] tris = new int[quads * 6];
        int t = 0;
        for (int y = 0; y < segmentsVertical; y++)
        {
            for (int x = 0; x < segmentsHorizontal; x++)
            {
                int i0 = y * vertCountX + x;
                int i1 = y * vertCountX + (x + 1);
                int i2 = (y + 1) * vertCountX + x;
                int i3 = (y + 1) * vertCountX + (x + 1);

                // We're inside the cylinder looking outward,
                // so we want winding order reversed (clockwise from camera POV).
                // We'll build triangles (i0,i2,i1) and (i2,i3,i1)

                tris[t++] = i0;
                tris[t++] = i2;
                tris[t++] = i1;

                tris[t++] = i2;
                tris[t++] = i3;
                tris[t++] = i1;
            }
        }

        m.vertices = verts;
        m.normals = normals;
        m.uv = uvs;
        m.triangles = tris;
        m.RecalculateBounds();
        // We already set normals manually; skip RecalculateNormals to keep inward-facing normals.

        meshFilter.sharedMesh = m;

        // Make sure renderer has the right material
        if (screenMaterial != null)
        {
            meshRenderer.sharedMaterial = screenMaterial;
        }
    }
}
