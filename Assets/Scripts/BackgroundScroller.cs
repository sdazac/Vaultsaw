using UnityEngine;

/// <summary>
/// Simple scrolling background that uses a single quad with seamless duplication.
/// Much simpler than parallax layers - just follows the main scroll speed.
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private Material backgroundMaterial;
    [SerializeField] private float quadWidth = 20f;
    [SerializeField] private float quadHeight = 6f;
    [SerializeField] private float zPosition = 0f;
    [SerializeField] [Tooltip("Number of quads in the pool (3-5 recommended)")] private int poolSize = 3;
    [SerializeField] [Tooltip("How far to the left of the screen before recycling (-10 to -20 recommended)")] private float recycleThreshold = -15f;
    
    private Transform[] quadPool;
    private float currentScrollPosition = 0f;

    private void Start()
    {
        if (backgroundMaterial == null)
        {
            Debug.LogError("BackgroundScroller: No material assigned!");
            return;
        }

        InitializeBackgroundQuads();
        Debug.Log($"BackgroundScroller: Created {poolSize} quads");
    }

    private void InitializeBackgroundQuads()
    {
        quadPool = new Transform[poolSize];

        // Create initial pool of quads
        for (int i = 0; i < poolSize; i++)
        {
            quadPool[i] = CreateQuad(i);
        }
    }

    private Transform CreateQuad(int index)
    {
        // Create a quad/plane GameObject
        GameObject quadGO = new GameObject($"BackgroundQuad_{index}");
        quadGO.transform.SetParent(transform);

        // Add mesh components
        MeshFilter meshFilter = quadGO.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quadGO.AddComponent<MeshRenderer>();

        // Create a simple quad mesh
        Mesh quadMesh = CreateQuadMesh();
        meshFilter.mesh = quadMesh;

        // Apply material
        meshRenderer.material = backgroundMaterial;

        // Position and scale
        quadGO.transform.localPosition = new Vector3(quadWidth * index, 0f, zPosition);
        quadGO.transform.localScale = new Vector3(quadWidth, quadHeight, 1f);

        return quadGO.transform;
    }

    private Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();

        // Vertices for a quad (-0.5 to 0.5 on XY)
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };

        // UVs
        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        // Triangles (CCW from front)
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private void Update()
    {
        // Check if GameManager exists
        if (GameManager.Instance == null)
        {
            return;
        }

        if (!GameManager.Instance.IsPlaying) return;

        // Check if LevelManager exists
        if (LevelManager.Instance == null)
        {
            return;
        }

        // Get scroll speed
        float scrollSpeed = LevelManager.Instance.GetScrollSpeed();

        // Queue-based recycling: continuously move quads from left to right as they scroll off
        // Find the leftmost quad
        int leftmostIndex = 0;
        float leftmostX = quadPool[0].localPosition.x;

        for (int i = 1; i < quadPool.Length; i++)
        {
            float quadX = quadPool[i].localPosition.x;
            if (quadX < leftmostX)
            {
                leftmostX = quadX;
                leftmostIndex = i;
            }
        }

        // If leftmost quad is completely off-screen (past left edge), recycle it
        if (leftmostX < recycleThreshold)
        {
            // Find rightmost quad position
            float rightmostX = quadPool[0].localPosition.x;
            for (int i = 1; i < quadPool.Length; i++)
            {
                float quadX = quadPool[i].localPosition.x;
                if (quadX > rightmostX)
                    rightmostX = quadX;
            }

            // Move leftmost quad to the right of rightmost quad
            quadPool[leftmostIndex].localPosition = new Vector3(rightmostX + quadWidth, 0f, zPosition);
        }

        // Update scroll position
        currentScrollPosition += scrollSpeed * Time.deltaTime;

        // Update visual positions of all quads based on scroll
        for (int i = 0; i < quadPool.Length; i++)
        {
            Vector3 pos = quadPool[i].localPosition;
            pos.x -= scrollSpeed * Time.deltaTime;
            quadPool[i].localPosition = pos;
        }
    }
}
