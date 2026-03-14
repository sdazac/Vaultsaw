using UnityEngine;
using System.Collections.Generic;

public class TuneSection : MonoBehaviour
{
    [Header("Cajas")]
    public GameObject destructibleBoxPrefab;
    public Transform[] boxSpawnPoints;
    public float boxY = 0f;

    [Header("Triggers de entrada/salida")]
    public float entryTriggerLocalX = 2f;
    public float exitTriggerLocalX  = 18f;
    public float triggerHeight      = 6f;

    [Header("Advertencia")]
    public GameObject warningSignPrefab;
    public float warningOffsetX = -8f;

    private List<DestructibleBox> spawnedBoxes = new List<DestructibleBox>();
    private bool initialized = false;
    private PlayerController player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        InitializeSection();
    }

    void InitializeSection()
    {
        if (initialized) return;
        initialized = true;
        CreateEntryExitTriggers();
        SpawnBoxes();
        SpawnWarningSign();
    }

    void CreateEntryExitTriggers()
    {
        CreateTrigger("TuneEntry", entryTriggerLocalX, TuneTrigger.TriggerType.Entry);
        CreateTrigger("TuneExit",  exitTriggerLocalX,  TuneTrigger.TriggerType.Exit);
    }

    void CreateTrigger(string goName, float localX, TuneTrigger.TriggerType type)
    {
        GameObject t = new GameObject(goName);
        t.transform.SetParent(transform);
        t.transform.localPosition = new Vector3(localX, 0f, 0f);

        BoxCollider col = t.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.5f, triggerHeight, 3f);

        // Usar componente en lugar de tag
        TuneTrigger tt = t.AddComponent<TuneTrigger>();
        tt.triggerType   = type;
        tt.parentSection = this;

        Debug.Log($"[TuneSection] Trigger '{goName}' tipo={type} worldPos={t.transform.position}");
    }

    void SpawnBoxes()
    {
        if (destructibleBoxPrefab == null)
        {
            Debug.LogError("[TuneSection] Falta asignar destructibleBoxPrefab.");
            return;
        }

        int boxCount    = Random.Range(1, 4);
        int playerCoins = GameManager.Instance.Coins;

        int totalCostMin = Mathf.Max(1, playerCoins - Random.Range(0, 5));
        int totalCostMax = playerCoins + Random.Range(0, 8);
        int totalCost    = Mathf.Max(boxCount, Random.Range(totalCostMin, totalCostMax + 1));

        int[] costs = SplitCost(totalCost, boxCount);

        float spacing = 2.5f;
        float centerX = 10f;
        float startX  = centerX - (spacing * (boxCount - 1)) / 2f;

        for (int i = 0; i < boxCount; i++)
        {
            Vector3 localPos = (boxSpawnPoints != null && i < boxSpawnPoints.Length)
                ? boxSpawnPoints[i].localPosition
                : new Vector3(startX + spacing * i, boxY, 0f);

            Vector3 worldPos = transform.TransformPoint(localPos);
            GameObject boxGO = Instantiate(destructibleBoxPrefab, worldPos, Quaternion.identity);
            boxGO.transform.SetParent(transform);

            DestructibleBox box = boxGO.GetComponent<DestructibleBox>();
            if (box != null)
            {
                box.Initialize(costs[i]);
                box.SetParentSection(this);
                spawnedBoxes.Add(box);
            }
        }

        Debug.Log($"[TuneSection] Spawneadas {boxCount} cajas. Costo total: {totalCost}");
    }

    int[] SplitCost(int total, int count)
    {
        int[] result    = new int[count];
        int   remaining = total;
        for (int i = 0; i < count - 1; i++)
        {
            int max    = Mathf.Max(1, remaining - (count - i - 1));
            result[i]  = Random.Range(1, max + 1);
            remaining -= result[i];
        }
        result[count - 1] = Mathf.Max(1, remaining);
        return result;
    }

    void SpawnWarningSign()
    {
        if (warningSignPrefab == null) return;
        GameObject sign = Instantiate(warningSignPrefab,
            transform.position + Vector3.right * warningOffsetX,
            Quaternion.identity);
        sign.transform.SetParent(transform);
    }

    public void OnBoxDestroyed(DestructibleBox box)
    {
        spawnedBoxes.Remove(box);
        Debug.Log($"[TuneSection] Cajas restantes: {spawnedBoxes.Count}");
        if (spawnedBoxes.Count == 0)
        {
            Debug.Log("[TuneSection] ¡Completada!");
            AudioManager.Instance?.PlaySectionCleared();
            if (player != null) player.ExitTuneSection();
        }
    }
}