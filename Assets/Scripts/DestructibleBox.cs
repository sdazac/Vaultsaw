using UnityEngine;
using TMPro;

public class DestructibleBox : MonoBehaviour
{
    [Header("Configuración")]
    public int requiredCoins = 10;
    public int hitsToDestroy = 3;       // ← golpes necesarios para destruir

    [Header("Visuales")]
    public MeshRenderer boxMeshRenderer;
    public Material crackMaterial;      // ← solo un material con el shader
    public TextMeshPro costLabel;

    [Header("Efectos")]
    public ParticleSystem hitParticles;
    public ParticleSystem destroyParticles;

    [Header("Shake")]
    public float shakeIntensity = 0.1f;
    public float shakeDuration  = 0.15f;

    private int    remainingCoins;
    private int    hitsReceived  = 0;
    private Vector3 originalLocalPos;
    private bool   isShaking    = false;
    private float  shakeTimer   = 0f;
    private TuneSection parentSection;
    private Material instanceMat; // material de instancia para no afectar otros

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
        requiredCoins  = Mathf.Max(1, coinCost);
        remainingCoins = requiredCoins;

        // Crea instancia del material para que cada caja sea independiente
        if (boxMeshRenderer && crackMaterial)
        {
            instanceMat = new Material(crackMaterial);
            boxMeshRenderer.material = instanceMat;
        }

        UpdateShader();
        UpdateLabel();
    }

    void Start()
    {
        if (remainingCoins == 0)
        {
            remainingCoins = requiredCoins;
            UpdateShader();
            UpdateLabel();
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
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
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
        int costPerHit = Mathf.CeilToInt(requiredCoins / (float)hitsToDestroy);

        if (GameManager.Instance.Coins < costPerHit)
        {
            Debug.Log("[Box] Monedas insuficientes → Game Over");
            GameManager.Instance.TriggerGameOver();
            return;
        }

        // Resta monedas por golpe
        GameManager.Instance.SpendCoins(costPerHit);
        remainingCoins -= costPerHit;
        hitsReceived++;

        AudioManager.Instance?.PlayBoxHit();
        TriggerShake();
        if (hitParticles) hitParticles.Play();

        // Actualiza el shader con el nuevo nivel de daño
        UpdateShader();
        UpdateLabel();

        Debug.Log($"[Box] Golpe {hitsReceived}/{hitsToDestroy} — Daño: {GetDamageRatio():F2}");

        // Destruye cuando recibe todos los golpes
        if (hitsReceived >= hitsToDestroy)
            DestroyBox();
    }

    float GetDamageRatio()
    {
        return (float)hitsReceived / hitsToDestroy;
    }

    void UpdateShader()
    {
        if (instanceMat == null) return;

        // Actualiza el parámetro _Damage del shader (0=sano, 1=destruido)
        float damage = GetDamageRatio();
        instanceMat.SetFloat("_Damage", damage);
    }

    void UpdateLabel()
    {
        if (costLabel) costLabel.text = remainingCoins.ToString();
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

        GameManager.Instance.AddScore(50);
        parentSection?.OnBoxDestroyed(this);
        Destroy(gameObject);
    }

    void TriggerShake()
    {
        isShaking  = true;
        shakeTimer = shakeDuration;
        transform.localPosition = originalLocalPos;
    }
}