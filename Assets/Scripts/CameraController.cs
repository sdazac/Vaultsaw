using UnityEngine;

/// <summary>
/// Cámara lateral 2.5D que sigue al jugador en X.
/// Hace zoom dinámico: más cerca en juego normal, aún más cerca en TuneSection.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float leadOffset = 3f;  // cuánto adelanta la cámara al jugador en X

    [Header("Posición Z (zoom)")]
    public float normalZ      = -10f;   // Z en juego normal (más cerca que -15)
    public float tuneSectionZ = -6f;    // Z en TuneSection (aún más cerca)
    public float zoomSpeed    = 3f;     // velocidad de transición entre zooms

    [Header("Posición Y fija")]
    public float normalY      = 0f;     // Y centrado entre piso y techo
    public float tuneSectionY = 0f;     // Y centrado entre midfloors (ajusta si difiere)

    [Header("Suavizado X")]
    public float smoothSpeed  = 8f;

    // Estado interno
    private float targetZ;
    private float targetY;
    private Vector3 velocity = Vector3.zero;
    private PlayerController player;

    void Start()
    {
        player  = FindFirstObjectByType<PlayerController>();
        targetZ = normalZ;
        targetY = normalY;

        // Posición inicial
        if (target != null)
            transform.position = new Vector3(target.position.x + leadOffset, normalY, normalZ);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Detectar si el jugador está en TuneSection
        bool inTune = player != null && player.InTuneSection;

        targetZ = inTune ? tuneSectionZ : normalZ;
        targetY = inTune ? tuneSectionY : normalY;

        // Posición deseada
        Vector3 desired = new Vector3(
            target.position.x + leadOffset,
            targetY,
            targetZ
        );

        // Suavizar X con SmoothDamp
        Vector3 current = transform.position;
        float newX = Mathf.SmoothDamp(current.x, desired.x, ref velocity.x, 1f / smoothSpeed);

        // Suavizar Y y Z con Lerp
        float newY = Mathf.Lerp(current.y, desired.y, zoomSpeed * Time.deltaTime);
        float newZ = Mathf.Lerp(current.z, desired.z, zoomSpeed * Time.deltaTime);

        transform.position = new Vector3(newX, newY, newZ);
    }

    public void TriggerDeathShake()
    {
        StartCoroutine(DeathShake());
    }

    System.Collections.IEnumerator DeathShake()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;
        float duration = 0.6f;
        float magnitude = 0.3f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 shake = (Vector3)Random.insideUnitCircle * magnitude * (1f - t);
            shake.z = 0f;
            transform.position = originalPos + shake;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }
}