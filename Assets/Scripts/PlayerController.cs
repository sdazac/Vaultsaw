using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Posiciones de Pisos")]
    public float floorY = -2f;
    public float ceilingY = 2f;
    public float midY = -0.5f;      // ← carril central nuevo
    public bool hasMidFloor = false; // ← se activa solo en TuneSection

    [Header("Movimiento")]
    public float jumpDuration = 0.18f;

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

    private bool isOnFloor = true;
    [HideInInspector] public bool isJumping = false;
    private float jumpTimer = 0f;
    private Vector3 jumpStartPos;
    private Vector3 jumpEndPos;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // Kinematic para que no sea empujado por físicas
        transform.position = new Vector3(transform.position.x, floorY, 0f);
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
        HandleJumpMovement();
        RotateSaw();
    }

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            TryJump();
    }

    void HandlePropulsionInput()
    {
        if (Input.GetKeyDown(KeyCode.B) || Input.GetMouseButtonDown(1))
            TogglePropulsion();
    }

    public void TryJump()
{
    if (isJumping) return;
    isJumping = true;
    jumpTimer = 0f;
    jumpStartPos = transform.position;

    if (hasMidFloor)
    {
        // En TuneSection: solo puede estar en el carril del medio
        jumpEndPos = new Vector3(transform.position.x, midY, 0f);
    }
    else
    {
        jumpEndPos = new Vector3(transform.position.x,
                     isOnFloor ? ceilingY : floorY, 0f);
    }
}

    public void TogglePropulsion()
    {
        bool newState = !GameManager.Instance.IsPropulsionActive;
        GameManager.Instance.SetPropulsion(newState);

        if (meshRenderer)
            meshRenderer.material = newState ? propulsionMaterial : normalMaterial;

        if (propulsionTrail)
        {
            if (newState) propulsionTrail.Play();
            else propulsionTrail.Stop();
        }

        AudioManager.Instance?.PlayPropulsionToggle(newState);
    }

    void HandleJumpMovement()
    {
        if (!isJumping) return;

        jumpTimer += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTimer / jumpDuration);
        float easedT = t * t * (3f - 2f * t);

        Vector3 pos = Vector3.Lerp(jumpStartPos, jumpEndPos, easedT);
        pos.z = 0f;
        transform.position = pos;

        if (t >= 1f)
        {
            isJumping = false;
            isOnFloor = !isOnFloor;
            transform.position = jumpEndPos;
        }
    }

    void RotateSaw()
    {
        transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime, Space.Self);
    }

    // ── Colisiones ───────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger con: " + other.tag);

        if (other.CompareTag("Coin"))
            CollectCoin(other.gameObject);
        else if (other.CompareTag("Obstacle"))
            HandleObstacleHit();
        else if (other.CompareTag("Wall"))
        {
            isJumping = false;
            transform.position = new Vector3(
                transform.position.x,
                isOnFloor ? floorY : ceilingY,
                0f
            );
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Colisión con: " + collision.gameObject.tag);

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

    void HandleWallContact()
    {
        // Si toca la pared, forzar al jugador de vuelta a su carril
        if (isOnFloor)
            transform.position = new Vector3(transform.position.x, floorY, 0f);
        else
            transform.position = new Vector3(transform.position.x, ceilingY, 0f);
        
        // Cancelar cualquier salto en progreso
        isJumping = false;
    }

    public bool IsOnFloor() => isOnFloor;
}