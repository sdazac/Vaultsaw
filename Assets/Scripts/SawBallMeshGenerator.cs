using UnityEngine;

/// <summary>
/// Genera proceduralmente la malla de la bola-sierra (dientes de sierra alrededor de un círculo).
/// Adjunta este script al GameObject del jugador junto con MeshFilter y MeshRenderer.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SawBallMeshGenerator : MonoBehaviour
{
    [Header("Forma Base")]
    public float innerRadius = 0.35f;    // Radio del círculo central
    public float outerRadius = 0.55f;    // Radio de las puntas de la sierra
    public int teethCount = 12;          // Número de dientes

    [Header("Diente")]
    [Range(0f, 1f)]
    public float toothSharpness = 0.5f;  // 0=redondo, 1=muy puntiagudo

    [Header("Profundidad 3D")]
    public float depth = 0.2f;           // Grosor del disco (eje Z)

private ParticleSystem propulsionFlame;

    void Awake()
    {
        GenerateMesh();
    }

    [ContextMenu("Regenerar Malla")]
    public void GenerateMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = new Mesh();
        mesh.name = "SawBall";

        int segments = teethCount * 2; // alternamos inner/outer
        int vertsPerRing = segments;

        // Dos anillos (frente y atrás) + centro
        Vector3[] verts = new Vector3[vertsPerRing * 2 + 2];
        Vector2[] uvs = new Vector2[verts.Length];
        int[] tris;

        float angleStep = 360f / segments;
        float halfDepth = depth * 0.5f;

        // Frente (z = +halfDepth) y atrás (z = -halfDepth)
        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            bool isTip = (i % 2 == 0);

            float r = isTip ? outerRadius : innerRadius;
            float x = Mathf.Cos(angle) * r;
            float y = Mathf.Sin(angle) * r;

            verts[i] = new Vector3(x, y, halfDepth);
            verts[i + vertsPerRing] = new Vector3(x, y, -halfDepth);

            uvs[i] = new Vector2(x / outerRadius * 0.5f + 0.5f, y / outerRadius * 0.5f + 0.5f);
            uvs[i + vertsPerRing] = uvs[i];
        }

        // Centros
        int frontCenter = vertsPerRing * 2;
        int backCenter = frontCenter + 1;
        verts[frontCenter] = new Vector3(0, 0, halfDepth);
        verts[backCenter] = new Vector3(0, 0, -halfDepth);
        uvs[frontCenter] = new Vector2(0.5f, 0.5f);
        uvs[backCenter] = new Vector2(0.5f, 0.5f);

        // Triangles
        int faceTriCount = segments * 3;       // cada cara = segments triángulos desde el centro
        int sideTriCount = segments * 6;       // lados = quads
        tris = new int[faceTriCount * 2 + sideTriCount];
        int ti = 0;

        // Cara frontal (CCW mirando desde +Z → CW en Unity)
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris[ti++] = frontCenter;
            tris[ti++] = next;
            tris[ti++] = i;
        }

        // Cara trasera (CW mirando desde -Z)
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            tris[ti++] = backCenter;
            tris[ti++] = i + vertsPerRing;
            tris[ti++] = next + vertsPerRing;
        }

        // Lados
        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            int f0 = i, f1 = next;
            int b0 = i + vertsPerRing, b1 = next + vertsPerRing;

            tris[ti++] = f0; tris[ti++] = b0; tris[ti++] = f1;
            tris[ti++] = f1; tris[ti++] = b0; tris[ti++] = b1;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;

        // Actualizar collider circular
        SphereCollider sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.radius = (innerRadius + outerRadius) * 0.5f;
        sc.isTrigger = true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () => {
            if (this != null) GenerateMesh();
        };
    }
#endif
}
