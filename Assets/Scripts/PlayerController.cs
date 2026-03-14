using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Carriles Y")]
    public float floorY   = -2f;
    public float ceilingY =  2f;

    [Header("Slow Motion")]
    public float slowMotionScale   = 0.3f;  // 0.3 = 30% velocidad
    public float slowMotionLerp    = 3f;    // velocidad de transición

    [Header("Física")]
    public float gravityForce  = 35f;
    public float jumpForce     = 22f;
    public float maxVertSpeed  = 18f;

    [Header("Rotación Sierra")]
    public float rotationSpeed = 360f;

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    public Material normalMaterial;
    public Material propulsionMaterial;

    [Header("Partículas")]
    public ParticleSystem coinPickupParticles;
    public ParticleSystem deathParticles;
    public ParticleSystem propulsionTrail;

    [Header("Propulsión")]
    public float propulsionDuration     = 2f;   // segundos activa
    public float propulsionCooldown     = 8f;   // segundos de cooldown
    private float propulsionStartTime   = -99f;
    private float propulsionOffTime     = -99f;

    // Propiedades públicas para la UI
    public bool PropulsionOnCooldown => 
        !GameManager.Instance.IsPropulsionActive && 
        Time.time - propulsionOffTime < propulsionCooldown;

    public float PropulsionCooldownRemaining => 
        Mathf.Max(0f, propulsionCooldown - (Time.time - propulsionOffTime));

    public float PropulsionDurationRemaining =>
        Mathf.Max(0f, propulsionDuration - (Time.time - propulsionStartTime));

    private Rigidbody rb;
    private bool isOnFloor     = true;
    private bool inTuneSection = false;
    public bool InTuneSection => inTuneSection;
    private float lastJumpTime = -1f;
    private const float JUMP_COOLDOWN = 0.2f;
    public float playerRadius = 0.45f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Solo freezear X, Z y rotaciones — Y libre para física real
        rb.constraints = RigidbodyConstraints.FreezePositionX
                       | RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationY
                       | RigidbodyConstraints.FreezeRotationZ;

        transform.position = new Vector3(transform.position.x, floorY + playerRadius, 0f);
    }

    void Start()
    {
        if (meshRenderer && normalMaterial)
            meshRenderer.material = normalMaterial;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;
        HandleJumpInput();
        HandlePropulsionInput();
        RotateSaw();

        if (GameManager.Instance.IsPropulsionActive)
        {
            if (Time.time - propulsionStartTime >= propulsionDuration)
                DeactivatePropulsion();
        }

        // Solo forzar Z=0 en Update, no tocar Y
        Vector3 pos = transform.position;
        pos.z = 0f;
        transform.position = pos;
    }

    void FixedUpdate()
    {
        if (!GameManager.Instance.IsPlaying) return;
        ApplyCustomGravity();
        ClampVerticalSpeed();
    }

    void ApplyCustomGravity()
    {
        float gravDir = isOnFloor ? -1f : 1f;
        rb.AddForce(Vector3.up * gravDir * gravityForce, ForceMode.Acceleration);
    }

    void ClampVerticalSpeed()
    {
        Vector3 v = rb.linearVelocity;
        v.y = Mathf.Clamp(v.y, -maxVertSpeed, maxVertSpeed);
        v.x = 0f;
        v.z = 0f;
        rb.linearVelocity = v;
    }

    void HandleJumpInput()
    {
        if (inTuneSection) return;
        bool pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        if (!pressed) return;
        if (Time.time - lastJumpTime < JUMP_COOLDOWN) return;
        Jump();
    }

    void HandlePropulsionInput()
    {
        if (Input.GetKeyDown(KeyCode.B) || Input.GetMouseButtonDown(1))
            TogglePropulsion();
    }

    void Jump()
    {
        lastJumpTime = Time.time;
        isOnFloor    = !isOnFloor;
        float dir    = isOnFloor ? -1f : 1f;
        rb.linearVelocity = new Vector3(0f, jumpForce * dir, 0f);
    }

    public void TogglePropulsion()
    {
        if (PropulsionOnCooldown) return;

        bool newState = !GameManager.Instance.IsPropulsionActive;

        // Si ya está activa, desactivar manualmente
        if (!newState)
        {
            DeactivatePropulsion();
            return;
        }

        // Activar
        propulsionStartTime = Time.time;
        GameManager.Instance.SetPropulsion(true);
        if (meshRenderer) meshRenderer.material = propulsionMaterial;
        if (propulsionTrail) propulsionTrail.Play();
        AudioManager.Instance?.PlayPropulsionToggle(true);
    }

    void DeactivatePropulsion()
    {
        propulsionOffTime = Time.time;
        GameManager.Instance.SetPropulsion(false);
        if (meshRenderer) meshRenderer.material = normalMaterial;
        if (propulsionTrail) propulsionTrail.Stop();
        AudioManager.Instance?.PlayPropulsionToggle(false);
    }

    public void EnterTuneSection()
    {
        if (inTuneSection) return;
        inTuneSection = true;
        rb.linearVelocity = Vector3.zero;
        StartCoroutine(LerpTimeScale(slowMotionScale));
        Debug.Log("[Player] EnterTuneSection");
    }

    public void ExitTuneSection()
    {
        inTuneSection = false;
        isOnFloor     = true;
        StartCoroutine(LerpTimeScale(1f));
        Debug.Log("[Player] ExitTuneSection");
    }

    IEnumerator LerpTimeScale(float target)
    {
        float start   = Time.timeScale;
        float elapsed = 0f;
        float duration = 0.4f;

        while (elapsed < duration)
        {
            elapsed        += Time.unscaledDeltaTime;
            Time.timeScale  = Mathf.Lerp(start, target, elapsed / duration);
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale      = target;
        Time.fixedDeltaTime = 0.02f * target;
    }

    void RotateSaw()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime, Space.Self);
    }

    public void ReceiveTrigger(Collider other)
    {
        if (other.CompareTag("Coin"))
            CollectCoin(other.gameObject);
        else if (other.CompareTag("Obstacle"))
            HandleObstacleHit();
        else if (other.CompareTag("DestructibleBox"))
        {
            DestructibleBox box = other.GetComponent<DestructibleBox>()
                               ?? other.GetComponentInParent<DestructibleBox>();
            box?.HandlePlayerContact();
        }
        else
        {
            TuneTrigger tt = other.GetComponent<TuneTrigger>();
            if (tt != null)
            {
                if (tt.triggerType == TuneTrigger.TriggerType.Entry)
                    EnterTuneSection();
                else if (tt.triggerType == TuneTrigger.TriggerType.Exit)
                    ExitTuneSection();
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
            HandleObstacleHit();
    }

    void CollectCoin(GameObject coin)
    {
        GameManager.Instance.AddCoin();
        AudioManager.Instance?.PlayCoinCollect();
        if (coinPickupParticles)
        {
            var ps = Instantiate(coinPickupParticles, coin.transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2f);
        }
        coin.SetActive(false);
        Destroy(coin, 0.1f);
    }

    void HandleObstacleHit()
    {
        if (deathParticles)
        {
            var ps = Instantiate(deathParticles, transform.position, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 3f);
        }
        AudioManager.Instance?.PlayDeath();
        GameManager.Instance.TriggerGameOver();
        gameObject.SetActive(false);
    }

    public bool IsOnFloor() => isOnFloor;
}