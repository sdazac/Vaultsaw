using UnityEngine;

/// <summary>
/// Comportamiento de la moneda: gira en su eje, flota suavemente,
/// y tiene un pequeño efecto magnético cuando el jugador se acerca.
/// </summary>
public class CoinBehavior : MonoBehaviour
{
    [Header("Animación")]
    public float rotationSpeed = 180f;     // grados/seg en Y
    public float floatAmplitude = 0.15f;   // amplitud del float vertical
    public float floatFrequency = 2f;      // Hz del float

    [Header("Magnetismo")]
    public float magnetRadius = 1.5f;      // radio a partir del cual la moneda se acerca
    public float magnetSpeed = 12f;

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    public Material coinMaterial;          // Material dorado brillante

    private Vector3 startPosition;
    private Transform playerTransform;
    private bool isBeingMagneted = false;

    void Start()
    {
        startPosition = transform.position;
        // Buscar el jugador
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player) playerTransform = player.transform;

        if (meshRenderer && coinMaterial)
            meshRenderer.material = coinMaterial;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        // Girar
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);

        // Flotación (solo si no está siendo atraída)
        if (!isBeingMagneted)
        {
            float yOffset = Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude;
            transform.position = new Vector3(
                transform.position.x,
                startPosition.y + yOffset,
                transform.position.z
            );
        }

        // Magnetismo
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist < magnetRadius)
            {
                isBeingMagneted = true;
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    playerTransform.position,
                    magnetSpeed * Time.deltaTime
                );
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRadius);
    }
}
