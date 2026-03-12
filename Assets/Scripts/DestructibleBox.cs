using UnityEngine;
using TMPro;

/// <summary>
/// Caja destructible que requiere monedas para ser destruida.
/// Bloquea físicamente el paso del jugador hasta ser destruida.
/// Solo responde a golpes en modo propulsión.
/// </summary>
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

    // Estado
    private int remainingCoins;
    private Vector3 originalLocalPos;
    private bool isShaking = false;
    private float shakeTimer = 0f;
    private TuneSection parentSection;

    // Collider sólido que bloquea al jugador
    private Collider solidCollider;
    // Collider trigger para detectar cuando el jugador lo toca
    private Collider triggerCollider;

    void Awake()
    {
        originalLocalPos = transform.localPosition;
        parentSection = GetComponentInParent<TuneSection>();
        SetupColliders();
    }

    void SetupColliders()
    {
        // Busca el BoxCollider existente y conviértelo en SÓLIDO (no trigger)
        // para que bloquee físicamente al jugador
        BoxCollider[] cols = GetComponents<BoxCollider>();

        if (cols.Length == 0)
        {
            // Crear collider sólido
            BoxCollider solid = gameObject.AddComponent<BoxCollider>();
            solid.isTrigger = false;
            solidCollider = solid;

            // Crear collider trigger (un poco más grande) para detectar contacto
            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = Vector3.one * 1.2f;
            triggerCollider = trigger;
        }
        else
        {
            // Usar el existente como sólido
            cols[0].isTrigger = false;
            solidCollider = cols[0];

            // Agregar trigger encima
            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = Vector3.one * 1.2f;
            triggerCollider = trigger;
        }
    }

    public void Initialize(int coinCost)
    {
        requiredCoins = coinCost;
        remainingCoins = coinCost;
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

    // ── Detección de contacto ─────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        HandlePlayerContact();
    }

    void OnTriggerStay(Collider other)
    {
        // Por si el jugador queda pegado, seguir procesando
        if (!other.CompareTag("Player")) return;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;
        HandlePlayerContact();
    }

    void HandlePlayerContact()
    {
        if (!GameManager.Instance.IsPropulsionActive)
        {
            // Sin propulsión = Game Over
            Debug.Log("[Box] Sin propulsión → Game Over");
            GameManager.Instance.TriggerGameOver();
            return;
        }

        TakeHit();
    }

    // ── Lógica de golpes ─────────────────────────────

    public void TakeHit()
    {
        int coinsToSpend = Mathf.Min(coinsPerHit, remainingCoins);

        if (GameManager.Instance.Coins <= 0)
        {
            Debug.Log("[Box] Sin monedas → Game Over");
            GameManager.Instance.TriggerGameOver();
            return;
        }

        GameManager.Instance.SpendCoins(coinsToSpend);
        remainingCoins -= coinsToSpend;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBoxHit();

        TriggerShake();

        if (hitParticles) hitParticles.Play();

        UpdateVisuals();

        if (remainingCoins <= 0)
            DestroyBox();
    }

    void DestroyBox()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBoxDestroy();

        if (destroyParticles)
        {
            var ps = Instantiate(destroyParticles, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }

        parentSection?.OnBoxDestroyed(this);
        Destroy(gameObject);
    }

    // ── Visuales ─────────────────────────────────────

    void UpdateVisuals()
    {
        if (costLabel)
            costLabel.text = remainingCoins.ToString();

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

    public int GetRemainingCoins() => remainingCoins;
    public int GetRequiredCoins() => requiredCoins;
}