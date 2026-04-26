using UnityEngine;

/// <summary>
/// Manages 3D parallax background layers that scroll with depth-based parallax effect.
/// Works with perspective 3D cameras by positioning planes at different Z depths
/// and scaling them to maintain visual consistency.
/// </summary>
public class ParallaxManager : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Header("Visual")]
        public Material material;
        public string layerName = "Parallax Layer";
        
        [Header("Depth & Scale")]
        [Tooltip("Z position for this layer (higher = further away)")]
        public float zPosition = 10f;
        
        [Tooltip("Base horizontal scale at Z distance. Multiply by aspect ratio for width.")]
        public float baseScale = 10f;
        
        [Header("Scrolling")]
        [Tooltip("Speed multiplier (0.1 = 10% of main scroll speed, 0.5 = 50%, etc)")]
        [Range(0.1f, 1f)]
        public float speedMultiplier = 0.5f;
        
        [Tooltip("Height of this layer (Y scale). Keep consistent for proper layering.")]
        public float heightScale = 6f;
        
        // Runtime data
        [HideInInspector] public Transform[] planePool;
        [HideInInspector] public float currentScrollPosition = 0f;
        
        [Tooltip("Number of planes in the pool for this layer")]
        public int poolSize = 3;
    }

    [SerializeField] private ParallaxLayer[] parallaxLayers;
    [SerializeField] private float aspectRatio = 16f / 9f;
    [SerializeField] private float recycleThreshold = 50f; // Distance before recycling planes
    
    private float totalScrollDistance = 0f;

    private void Start()
    {
        if (parallaxLayers == null || parallaxLayers.Length == 0)
        {
            Debug.LogError("ParallaxManager: No parallax layers configured!");
            return;
        }

        InitializeLayers();
        Debug.Log($"ParallaxManager: Initialized {parallaxLayers.Length} layers");
    }

    private void InitializeLayers()
    {
        foreach (var layer in parallaxLayers)
        {
            if (layer.material == null)
            {
                Debug.LogWarning($"ParallaxManager: Layer '{layer.layerName}' has no material assigned!");
                continue;
            }

            layer.planePool = new Transform[layer.poolSize];
            
            // Create initial pool of planes
            for (int i = 0; i < layer.poolSize; i++)
            {
                layer.planePool[i] = CreatePlane(layer, i);
            }
            
            Debug.Log($"ParallaxManager: Created pool for '{layer.layerName}' with {layer.poolSize} planes");
        }
    }

    private Transform CreatePlane(ParallaxLayer layer, int index)
    {
        // Create a quad/plane GameObject
        GameObject planeGO = new GameObject($"{layer.layerName}_{index}");
        planeGO.transform.SetParent(transform);
        
        // Add mesh components
        MeshFilter meshFilter = planeGO.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planeGO.AddComponent<MeshRenderer>();
        
        // Create a simple quad mesh
        Mesh quadMesh = CreateQuadMesh();
        meshFilter.mesh = quadMesh;
        
        // Apply material
        meshRenderer.material = layer.material;
        
        // Position and scale
        float planeWidth = layer.baseScale * aspectRatio;
        planeGO.transform.localPosition = new Vector3(planeWidth * index, 0f, layer.zPosition);
        planeGO.transform.localScale = new Vector3(planeWidth, layer.heightScale, 1f);
        
        return planeGO.transform;
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

        float scrollSpeed = LevelManager.Instance.GetScrollSpeed();
        float deltaDistance = scrollSpeed * Time.deltaTime;
        totalScrollDistance += deltaDistance;

        // Update all layers
        foreach (var layer in parallaxLayers)
        {
            UpdateLayer(layer, deltaDistance);
        }
    }

    private void UpdateLayer(ParallaxLayer layer, float deltaDistance)
    {
        // Calculate actual speed for this layer based on multiplier
        float layerSpeed = LevelManager.Instance.GetScrollSpeed() * layer.speedMultiplier;
        layer.currentScrollPosition += layerSpeed * Time.deltaTime;

        float planeWidth = layer.baseScale * aspectRatio;

        // Update each plane in the pool
        for (int i = 0; i < layer.planePool.Length; i++)
        {
            Transform planeTransform = layer.planePool[i];
            
            // Calculate X position with scrolling
            float baseX = planeWidth * i;
            float scrolledX = baseX - layer.currentScrollPosition;
            
            // Recycle plane if it scrolled too far left
            if (scrolledX < -planeWidth * 1.5f)
            {
                // Find rightmost plane to position after
                float rightmostX = float.NegativeInfinity;
                foreach (var plane in layer.planePool)
                {
                    float planeX = plane.localPosition.x;
                    if (planeX > rightmostX)
                        rightmostX = planeX;
                }
                
                // Position this plane to the right of the rightmost one
                scrolledX = rightmostX + planeWidth;
                layer.currentScrollPosition = baseX - scrolledX;
            }
            
            // Update position
            Vector3 newPos = planeTransform.localPosition;
            newPos.x = scrolledX;
            planeTransform.localPosition = newPos;
        }
    }

    public void SetLayerMaterial(int layerIndex, Material newMaterial)
    {
        if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
        {
            parallaxLayers[layerIndex].material = newMaterial;
            
            // Update all planes in that layer
            foreach (var plane in parallaxLayers[layerIndex].planePool)
            {
                plane.GetComponent<MeshRenderer>().material = newMaterial;
            }
        }
    }

    public void SetLayerSpeedMultiplier(int layerIndex, float multiplier)
    {
        if (layerIndex >= 0 && layerIndex < parallaxLayers.Length)
        {
            parallaxLayers[layerIndex].speedMultiplier = Mathf.Clamp01(multiplier);
        }
    }
}
