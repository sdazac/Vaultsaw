using UnityEngine;

/// <summary>
/// Componente auxiliar para los prefabs de Chunk.
/// Cada chunk tiene un piso, techo, y opcionalmente obstáculos.
/// Este script configura las paredes al instanciar el prefab.
/// </summary>
public class ChunkBuilder : MonoBehaviour
{
    [Header("Dimensiones del Chunk")]
    public float width = 20f;
    public float height = 6f;          // Distancia entre piso y techo
    public float wallThickness = 0.5f;

    [Header("Referencias")]
    public Transform floorTransform;
    public Transform ceilingTransform;
    public Transform leftWall;
    public Transform rightWall;

    [Header("Materiales")]
    public Material floorMaterial;
    public Material wallMaterial;

    void Awake()
    {
        ConfigureWalls();
    }

    void ConfigureWalls()
    {
        // Piso
        if (floorTransform)
        {
            floorTransform.localPosition = new Vector3(width / 2f, -height / 2f - wallThickness / 2f, 0f);
            floorTransform.localScale = new Vector3(width, wallThickness, 2f);
            ApplyMaterial(floorTransform, floorMaterial);
        }

        // Techo
        if (ceilingTransform)
        {
            ceilingTransform.localPosition = new Vector3(width / 2f, height / 2f + wallThickness / 2f, 0f);
            ceilingTransform.localScale = new Vector3(width, wallThickness, 2f);
            ApplyMaterial(ceilingTransform, floorMaterial);
        }
    }

    void ApplyMaterial(Transform t, Material mat)
    {
        if (mat == null) return;
        var mr = t.GetComponent<MeshRenderer>();
        if (mr) mr.material = mat;
    }

    /// <summary>
    /// Genera automáticamente un chunk simple con piso y techo.
    /// Llama esto desde código de editor o en Start si no tienes prefabs listos.
    /// </summary>
    [ContextMenu("Auto-Generar Chunk")]
    public void AutoGenerateChunk()
    {
        // Piso
        CreatePanel("Floor", new Vector3(width / 2f, -height / 2f, 0f),
                    new Vector3(width, wallThickness, 2f));
        // Techo
        CreatePanel("Ceiling", new Vector3(width / 2f, height / 2f, 0f),
                    new Vector3(width, wallThickness, 2f));
    }

    GameObject CreatePanel(string name, Vector3 localPos, Vector3 scale)
    {
        // Busca si ya existe
        Transform existing = transform.Find(name);
        if (existing) return existing.gameObject;

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(transform);
        panel.transform.localPosition = localPos;
        panel.transform.localScale = scale;

        // Remover BoxCollider del panel (el jugador no choca con el suelo físicamente)
        Destroy(panel.GetComponent<BoxCollider>());

        if (floorMaterial)
            panel.GetComponent<MeshRenderer>().material = floorMaterial;

        return panel;
    }
}
