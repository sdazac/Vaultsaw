using UnityEngine;

/// <summary>
/// Cámara en perspectiva con vista lateral (2.5D).
/// Sigue al jugador en X con suavizado, Y e Z son fijas.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -15f);

    [Header("Suavizado")]
    public float smoothSpeed = 8f;
    public float leadOffset = 3f;   // La cámara mira un poco adelante del jugador

    [Header("Límites Y (sacudida de muerte)")]
    public float deathShakeMagnitude = 0.4f;
    public float deathShakeDuration = 0.6f;

    private Vector3 velocity = Vector3.zero;
    private bool isShaking = false;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = new Vector3(
            target.position.x + leadOffset + offset.x,
            offset.y,
            offset.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref velocity,
            1f / smoothSpeed
        );
    }

    public void TriggerDeathShake()
    {
        if (!isShaking)
            StartCoroutine(DeathShake());
    }

    System.Collections.IEnumerator DeathShake()
    {
        isShaking = true;
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < deathShakeDuration)
        {
            float t = elapsed / deathShakeDuration;
            float magnitude = deathShakeMagnitude * (1f - t);
            Vector3 shake = (Vector3)Random.insideUnitCircle * magnitude;
            shake.z = 0f;
            transform.position = originalPos + shake;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        isShaking = false;
    }
}
