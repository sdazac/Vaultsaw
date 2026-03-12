using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Chunk Inicial")]
    public GameObject startChunkPrefab;

    [Header("Chunks Normales")]
    public GameObject[] normalChunkPrefabs;
    public int initialChunks = 8;
    public float chunkWidth = 20f;

    [Header("Tune Section")]
    public GameObject tuneSectionPrefab;
    public int chunksBetweenTunes = 6;

    [Header("Velocidad")]
    public float scrollSpeed = 6f;
    public float speedIncreasePerSecond = 0.02f;
    public float maxSpeed = 18f;

    [Header("Monedas")]
    public GameObject coinPrefab;
    public int coinsPerChunk = 4;
    public float coinY_floor = -1.5f;
    public float coinY_ceiling = 1.5f;
    [Range(0f, 1f)] public float coinSpawnChance = 0.7f;

    [Header("Referencias")]
    public Transform playerTransform;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float spawnX;
    private int chunksSpawned = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (startChunkPrefab != null)
        {
            Vector3 pos = new Vector3(spawnX, 0f, 0f);
            activeChunks.Add(Instantiate(startChunkPrefab, pos, Quaternion.identity));
            spawnX += chunkWidth;
            chunksSpawned++;
        }          

// Resto de chunks normales
        for (int i = 1; i < initialChunks; i++)
            SpawnNextChunk();
            }

            void Update()
            {
                if (!GameManager.Instance.IsPlaying) return;

                scrollSpeed = Mathf.Min(scrollSpeed + speedIncreasePerSecond * Time.deltaTime, maxSpeed);
                float delta = scrollSpeed * Time.deltaTime;

                // Mover chunks a la izquierda
                for (int i = 0; i < activeChunks.Count; i++)
                {
                    if (activeChunks[i] != null)
                        activeChunks[i].transform.position += Vector3.left * delta;
                }

                // El punto de spawn también se mueve con los chunks
                spawnX -= delta;

                GameManager.Instance.AddDistanceScore(delta);

                // Spawn: mantener chunks suficientes adelante
                float playerX = playerTransform != null ? playerTransform.position.x : 0f;
                while (spawnX < playerX + chunkWidth * 4f)
                    SpawnNextChunk();

                // Destroy: eliminar chunks que quedaron atrás
                float destroyX = playerX - chunkWidth * 2f;
                for (int i = activeChunks.Count - 1; i >= 0; i--)
                {
                    if (activeChunks[i] == null) { activeChunks.RemoveAt(i); continue; }
                    float rightEdge = activeChunks[i].transform.position.x + chunkWidth;
                    if (rightEdge < destroyX)
                    {
                        Destroy(activeChunks[i]);
                        activeChunks.RemoveAt(i);
                    }
                }
}

    void SpawnNextChunk()
    {
        chunksSpawned++;
        bool isTune = (chunksSpawned % chunksBetweenTunes == 0) && tuneSectionPrefab != null;

        GameObject prefab = isTune
            ? tuneSectionPrefab
            : normalChunkPrefabs[Random.Range(0, normalChunkPrefabs.Length)];

        Vector3 pos = new Vector3(spawnX, 0f, 0f);
        GameObject chunk = Instantiate(prefab, pos, Quaternion.identity);
        activeChunks.Add(chunk);

        if (!isTune)
            SpawnCoinsInChunk(chunk, pos);

        spawnX += chunkWidth;
    }

    void SpawnCoinsInChunk(GameObject chunk, Vector3 chunkOrigin)
    {
        if (coinPrefab == null) return;
        float spacing = chunkWidth / (coinsPerChunk + 1f);
        for (int i = 1; i <= coinsPerChunk; i++)
        {
            if (Random.value > coinSpawnChance) continue;
            float x = chunkOrigin.x + spacing * i;
            float y = Random.value > 0.5f ? coinY_floor : coinY_ceiling;
            GameObject coin = Instantiate(coinPrefab, new Vector3(x, y, 0f), Quaternion.identity);
            coin.transform.SetParent(chunk.transform);
        }
    }

    public float GetScrollSpeed() => scrollSpeed;
}