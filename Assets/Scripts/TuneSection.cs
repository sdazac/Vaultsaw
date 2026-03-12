using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TuneSection : MonoBehaviour
{
    [Header("Cajas")]
    public GameObject destructibleBoxPrefab;
    public Transform[] boxSpawnPoints;
    public float boxY = 0f;

    [Header("Rampas")]
    public GameObject rampTopPrefab;      // Prefab rampa superior (opcional)
    public GameObject rampBottomPrefab;   // Prefab rampa inferior (opcional)
    public float rampOpenY_Top = 3.5f;    // Y cuando está abierta (fuera del camino)
    public float rampClosedY_Top = 1.6f;  // Y cuando está cerrada (bloqueando)
    public float rampOpenY_Bottom = -3.5f;
    public float rampClosedY_Bottom = -1.6f;
    public float rampAnimDuration = 0.4f; // segundos que tarda en cerrarse/abrirse

    [Header("Advertencia")]
    public GameObject warningSignPrefab;
    public float warningOffsetX = -8f;

    // Internos
    private List<DestructibleBox> spawnedBoxes = new List<DestructibleBox>();
    private GameObject rampTop;
    private GameObject rampBottom;
    private bool initialized = false;

    void Start()
    {
        InitializeSection();
    }

    void InitializeSection()
    {
        if (initialized) return;
        initialized = true;

        // Activar carril del medio en el jugador
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.hasMidFloor = true;
            // Forzar al jugador directamente al carril del medio sin animación
        player.transform.position = new Vector3(
            player.transform.position.x,
            player.midY,
            0f
        );
        player.isJumping = false;
                }

        SpawnBoxes();
        SpawnWarningSign();
    }

    // ── Rampas ───────────────────────────────────────

    void SpawnRamps()
    {
        // Rampa superior
        if (rampTopPrefab != null)
        {
            rampTop = Instantiate(rampTopPrefab, transform);
            rampTop.transform.localPosition = new Vector3(10f, rampOpenY_Top, 0f);
        }
        else
        {
            // Crear rampa procedural si no hay prefab
            rampTop = CreateProceduralRamp("RampTop", true);
        }

        // Rampa inferior
        if (rampBottomPrefab != null)
        {
            rampBottom = Instantiate(rampBottomPrefab, transform);
            rampBottom.transform.localPosition = new Vector3(10f, rampOpenY_Bottom, 0f);
        }
        else
        {
            rampBottom = CreateProceduralRamp("RampBottom", false);
        }

        // Cerrar rampas con animación
        StartCoroutine(AnimateRamp(rampTop, rampOpenY_Top, rampClosedY_Top));
        StartCoroutine(AnimateRamp(rampBottom, rampOpenY_Bottom, rampClosedY_Bottom));
    }

    GameObject CreateProceduralRamp(string rampName, bool isTop)
    {
        GameObject ramp = new GameObject(rampName);
        ramp.transform.SetParent(transform);

        // Cuerpo principal de la rampa
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(ramp.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(20f, 0.4f, 2f);

        // Material rojo para que sea visible
        MeshRenderer mr = body.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (mat.shader.name == "Hidden/InternalErrorShader")
                mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.8f, 0.2f, 0.2f); // rojo
            mr.material = mat;
        }

        // Collider trigger con tag Wall
        BoxCollider col = body.GetComponent<BoxCollider>();
        if (col == null) col = body.AddComponent<BoxCollider>();
        col.isTrigger = true;

        // Tag Wall en el body
        if (!HasTag("Wall"))
            Debug.LogWarning("Falta crear el tag 'Wall' en Project Settings → Tags");

        body.tag = "Wall";

        // Posición inicial
        float startY = isTop ? rampOpenY_Top : rampOpenY_Bottom;
        ramp.transform.localPosition = new Vector3(10f, startY, 0f);

        return ramp;
    }

    bool HasTag(string tagName)
    {
        try { GameObject.FindWithTag(tagName); return true; }
        catch { return false; }
    }

    IEnumerator AnimateRamp(GameObject ramp, float fromY, float toY)
    {
        if (ramp == null) yield break;

        float elapsed = 0f;
        Vector3 startPos = ramp.transform.localPosition;
        Vector3 endPos = new Vector3(startPos.x, toY, startPos.z);

        while (elapsed < rampAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / rampAnimDuration);
            ramp.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        ramp.transform.localPosition = endPos;
    }

    // ── Cajas ────────────────────────────────────────

    void SpawnBoxes()
    {
        int boxCount = Random.Range(1, 4);
        int playerCoins = GameManager.Instance.Coins;

        int totalCostMin = Mathf.Max(1, playerCoins - Random.Range(0, 5));
        int totalCostMax = playerCoins + Random.Range(0, 8);
        int totalCost = Random.Range(totalCostMin, totalCostMax + 1);
        totalCost = Mathf.Max(boxCount, totalCost);

        int[] costs = SplitCost(totalCost, boxCount);

        float spacing = 2.5f;
        float centerX = 10f;
        float startX = centerX - (spacing * (boxCount - 1)) / 2f;

        for (int i = 0; i < boxCount; i++)
        {
            Vector3 localPos;

            if (boxSpawnPoints != null && i < boxSpawnPoints.Length)
                localPos = boxSpawnPoints[i].localPosition;
            else
                localPos = new Vector3(startX + spacing * i, boxY, 0f);

            Vector3 worldPos = transform.TransformPoint(localPos);
            GameObject boxGO = Instantiate(destructibleBoxPrefab, worldPos, Quaternion.identity);
            boxGO.transform.SetParent(transform);

            DestructibleBox box = boxGO.GetComponent<DestructibleBox>();
            if (box != null)
            {
                box.Initialize(costs[i]);
                spawnedBoxes.Add(box);
            }
        }
    }

    int[] SplitCost(int total, int count)
    {
        int[] result = new int[count];
        int remaining = total;
        for (int i = 0; i < count - 1; i++)
        {
            result[i] = Random.Range(1, remaining - (count - i - 1) + 1);
            remaining -= result[i];
        }
        result[count - 1] = remaining;
        return result;
    }

    void SpawnWarningSign()
    {
        if (warningSignPrefab == null) return;
        Vector3 signPos = transform.position + Vector3.right * warningOffsetX;
        GameObject sign = Instantiate(warningSignPrefab, signPos, Quaternion.identity);
        sign.transform.SetParent(transform);
    }

    // ── Callback cuando se destruye una caja ─────────

public void OnBoxDestroyed(DestructibleBox box)
{
    spawnedBoxes.Remove(box);
    if (spawnedBoxes.Count == 0)
    {
        // Desactivar carril del medio
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
            player.hasMidFloor = false;

        AudioManager.Instance?.PlaySectionCleared();
    }
}
}