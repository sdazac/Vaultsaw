using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Carriles Y")]
    public float floorY   = -2f;
    public float ceilingY =  2f;

    [Header("Slow Motion")]
    public float slowMotionScale   = 0.3f;
    public float slowMotionLerp    = 3f;

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

    [Header("Llama Propulsión")]
    public Color flameColorInner = new Color(0.4f, 0.8f, 1.0f, 1f);
    public Color flameColorOuter = new Color(0.1f, 0.3f, 1.0f, 0f);
    public float flameRadius     = 0.5f;
    public float flameRate       = 40f;
    public float flameLifetime   = 0.3f;
    public float flameSpeed      = 2f;
    private ParticleSystem propulsionFlame;

    [Header("Propulsión")]
    public float propulsionDuration     = 2f;
    public float propulsionCooldown     = 8f;
    private float propulsionStartTime   = -99f;
    private float propulsionOffTime     = -99f;

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

    Texture2D CreateFlameTexture()
    {
        int width  = 64;
        int height = 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Coordenadas centradas -1 a 1
                float nx = (x / (float)width)  * 2f - 1f;
                float ny = (y / (float)height) * 2f - 1f;

                // Forma de llama — más ancha abajo, punta arriba
                float flameShape = 1f - Mathf.Abs(nx) * (1f + ny * 0.8f);
                flameShape = Mathf.Clamp01(flameShape);

                // Suaviza los bordes
                float alpha = flameShape * (1f - Mathf.Abs(nx));
                alpha = Mathf.Pow(alpha, 1.5f);

                // Gradiente de color — blanco centro, azul exterior
                float heat  = 1f - (float)y / height;
                Color color = Color.Lerp(
                    new Color(0.0f, 0.4f, 1.0f),  // azul exterior
                    new Color(0.8f, 1.0f, 1.0f),  // blanco azulado centro
                    heat * flameShape
                );
                color.a = alpha;

                tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return tex;
    }
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

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

        CreatePropulsionFlame();
    }

    void CreatePropulsionFlame()
    {
        GameObject flameGO = new GameObject("PropulsionFlame");
        flameGO.transform.SetParent(transform);
        flameGO.transform.localPosition = Vector3.zero;
        flameGO.transform.localRotation = Quaternion.identity;

        propulsionFlame = flameGO.AddComponent<ParticleSystem>();

        // Crea material con textura de llama
        Texture2D flameTex = CreateFlameTexture();
        Material flameMat  = new Material(Shader.Find("Vaultsaw/FlameParticle"));
        flameMat.mainTexture = flameTex;
        flameMat.SetColor("_CoreColor",  new Color(0.8f, 1.0f, 1.0f, 1f));
        flameMat.SetColor("_OuterColor", new Color(0.0f, 0.3f, 1.0f, 0f));
        flameMat.SetFloat("_Intensity",  2.0f);

        var renderer          = flameGO.GetComponent<ParticleSystemRenderer>();
        renderer.material     = flameMat;
        renderer.sortingOrder = 2;
        renderer.renderMode   = ParticleSystemRenderMode.Billboard;

        // Main
        var main              = propulsionFlame.main;
        main.loop             = true;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startRotation    = new ParticleSystem.MinMaxCurve(
            -Mathf.PI * 0.25f, Mathf.PI * 0.25f
        );
        main.startColor       = new Color(0.5f, 0.9f, 1.0f, 1f);
        main.gravityModifier  = -0.8f;  // sube fuerte
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.maxParticles     = 150;

        // Emission — muchas partículas para efecto denso
        var emission          = propulsionFlame.emission;
        emission.rateOverTime = 80f;

        // Shape — sale de toda la sierra en abanico hacia la izquierda
        var shape             = propulsionFlame.shape;
        shape.enabled         = true;
        shape.shapeType       = ParticleSystemShapeType.Cone;
        shape.angle           = 35f;
        shape.radius          = 0.4f;
        shape.rotation        = new Vector3(0f, -90f, 0f); // apunta izquierda
        shape.radiusThickness = 1f;

        // Color over Lifetime
        var colorModule       = propulsionFlame.colorOverLifetime;
        colorModule.enabled   = true;
        Gradient gradient     = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.0f),  // blanco caliente
                new GradientColorKey(new Color(0.4f, 0.8f, 1.0f), 0.3f),  // azul claro
                new GradientColorKey(new Color(0.1f, 0.3f, 1.0f), 0.7f),  // azul medio
                new GradientColorKey(new Color(0.0f, 0.1f, 0.5f), 1.0f)   // azul oscuro
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.4f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorModule.color = new ParticleSystem.MinMaxGradient(gradient);

        // Size over Lifetime — crece y se desvanece
        var sizeModule        = propulsionFlame.sizeOverLifetime;
        sizeModule.enabled    = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.2f),
            new Keyframe(0.2f, 1.0f),
            new Keyframe(1.0f, 0.0f)
        );
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Rotation over Lifetime — gira para simular movimiento de llama
        var rotModule         = propulsionFlame.rotationOverLifetime;
        rotModule.enabled     = true;
        rotModule.z           = new ParticleSystem.MinMaxCurve(-90f, 90f);

        // Velocity — estela hacia la izquierda
        var velocity          = propulsionFlame.velocityOverLifetime;
        velocity.enabled      = true;
        velocity.space        = ParticleSystemSimulationSpace.World;
        velocity.x            = new ParticleSystem.MinMaxCurve(-2f);
        velocity.y            = new ParticleSystem.MinMaxCurve(0.5f);

        // Noise — movimiento orgánico de llama
        var noise             = propulsionFlame.noise;
        noise.enabled         = true;
        noise.strength        = 0.5f;
        noise.frequency       = 1.2f;
        noise.scrollSpeed     = 2f;
        noise.damping         = true;

        propulsionFlame.Stop();
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

        if (!newState)
        {
            DeactivatePropulsion();
            return;
        }

        propulsionStartTime = Time.time;
        GameManager.Instance.SetPropulsion(true);
        if (meshRenderer) meshRenderer.material = propulsionMaterial;
        if (propulsionTrail) propulsionTrail.Play();
        if (propulsionFlame) propulsionFlame.Play();
        AudioManager.Instance?.PlayPropulsionToggle(true);
    }

    void DeactivatePropulsion()
    {
        propulsionOffTime = Time.time;
        GameManager.Instance.SetPropulsion(false);
        if (meshRenderer) meshRenderer.material = normalMaterial;
        if (propulsionTrail) propulsionTrail.Stop();
        if (propulsionFlame) propulsionFlame.Stop();
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