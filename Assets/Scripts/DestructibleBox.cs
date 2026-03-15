using UnityEngine;
using TMPro;

public class DestructibleBox : MonoBehaviour
{
    [Header("Configuración")]
    public int requiredCoins = 10;
    public int coinsPerHit = 1;

    [Header("Visuales")]
    public MeshRenderer boxMeshRenderer;
    public Material intactMaterial;
    public Material damagedMaterial;
    public Material criticalMaterial;
    public TextMeshPro costLabel;

    [Header("Efectos")]
    public ParticleSystem hitParticles;
    public ParticleSystem destroyParticles;

    [Header("Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.15f;

    private int remainingCoins;
    private Vector3 originalLocalPos;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private TuneSection parentSection;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void SetParentSection(TuneSection section)
    {
        parentSection = section;
    }

    public void Initialize(int coinCost)
    {
        requiredCoins = Mathf.Max(1, coinCost);
        remainingCoins = requiredCoins;
        UpdateVisuals();
    }

    void Start()
    {
        if (remainingCoins == 0)
        {
            remainingCoins = requiredCoins;
            UpdateVisuals();
        }
    }

    void Update()
    {
        if (!isShaking) return;
        shakeTimer -= Time.deltaTime;
        if (shakeTimer > 0f)
        {
            Vector3 shake = (Vector3)Random.insideUnitCircle * shakeIntensity;
            shake.z = 0f;
            transform.localPosition = originalLocalPos + shake;
        }
        else
        {
            isShaking = false;
            transform.localPosition = originalLocalPos;
        }
    }

    /// <summary>Llamado desde PlayerController cuando toca esta caja.</summary>
    public void HandlePlayerContact()
    {
        if (!GameManager.Instance.IsPropulsionActive)
        {
            Debug.Log("[Box] Jugador sin propulsión → Game Over");
            HandlePlayerDeath();
            return;
        }

        TakeHit();
    }

    void HandlePlayerDeath()
    {
        // Obtener referencia al jugador
        PlayerController player = FindFirstObjectByType<PlayerController>();
        
        if (player != null)
        {
            // Reproducir efectos de muerte
            if (player.deathParticles)
            {
                var ps = Instantiate(player.deathParticles, player.transform.position, Quaternion.identity);
                ps.Play();
                Destroy(ps.gameObject, 3f);
            }
            AudioManager.Instance?.PlayDeath();
            player.gameObject.SetActive(false);
        }
        
        GameManager.Instance.TriggerGameOver();
    }

    void TakeHit()
    {
        Debug.Log("[Box] TakeHit - Monedas disponibles: " + GameManager.Instance.Coins + 
                  ", Costo de caja: " + requiredCoins);

        // Verificar si tienes suficientes monedas para pagar TODO el costo de la caja
        if (GameManager.Instance.Coins < requiredCoins)
        {
            Debug.Log("[Box] Monedas insuficientes (" + GameManager.Instance.Coins + " < " + requiredCoins + ") → Game Over");
            GameManager.Instance.TriggerGameOver();
            return;
        }

        // Restar el costo TOTAL de la caja de una vez
        GameManager.Instance.SpendCoins(requiredCoins);
        
        Debug.Log("[Box] Monedas restadas. Nuevo total: " + GameManager.Instance.Coins);

        AudioManager.Instance?.PlayBoxHit();
        TriggerShake();
        if (hitParticles) hitParticles.Play();
        
        // Destruir la caja inmediatamente
        DestroyBox();
    }

    void DestroyBox()
    {
        AudioManager.Instance?.PlayBoxDestroy();

        if (destroyParticles)
        {
            var ps = Instantiate(destroyParticles, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        Debug.Log("[Box] Destruida - Sumando 50 puntos. Score actual: " + GameManager.Instance.Score);
        GameManager.Instance.AddScore(50);
        Debug.Log("[Box] Score después de AddScore: " + GameManager.Instance.Score);

        parentSection?.OnBoxDestroyed(this);
        Destroy(gameObject);
    }

    void UpdateVisuals()
    {
        if (costLabel) costLabel.text = remainingCoins.ToString();
        if (boxMeshRenderer == null) return;

        float ratio = requiredCoins > 0 ? (float)remainingCoins / requiredCoins : 1f;
        if (ratio > 0.5f && intactMaterial)
            boxMeshRenderer.material = intactMaterial;
        else if (ratio > 0.25f && damagedMaterial)
            boxMeshRenderer.material = damagedMaterial;
        else if (criticalMaterial)
            boxMeshRenderer.material = criticalMaterial;
    }

    void TriggerShake()
    {
        isShaking = true;
        shakeTimer = shakeDuration;
        transform.localPosition = originalLocalPos;
    }
}