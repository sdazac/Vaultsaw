using UnityEngine;

/// <summary>
/// Obstáculo genérico que mata al jugador si lo toca sin propulsión activa.
/// Puede ser estático o tener movimiento oscilante (sube/baja entre pisos).
/// </summary>
public class ObstacleBehavior : MonoBehaviour
{
    public enum ObstacleType { Static, Oscillating, Rotating }

    [Header("Tipo")]
    public ObstacleType obstacleType = ObstacleType.Static;

    [Header("Oscilación (si aplica)")]
    public float oscillateAmplitude = 2f;
    public float oscillateFrequency = 1f;
    public float oscillateOffset = 0f;      // Para desfasar obstáculos del mismo chunk

    [Header("Rotación (si aplica)")]
    public Vector3 rotationAxis = Vector3.forward;
    public float rotationSpeed = 90f;

    [Header("Visual")]
    public MeshRenderer meshRenderer;
    public Material obstacleMaterial;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
        if (meshRenderer && obstacleMaterial)
            meshRenderer.material = obstacleMaterial;

        // Asegurar tag correcto
        gameObject.tag = "Obstacle";
        // Asegurar collider trigger
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void Update()
    {
        if (!GameManager.Instance.IsPlaying) return;

        switch (obstacleType)
        {
            case ObstacleType.Oscillating:
                float y = startLocalPos.y + Mathf.Sin(
                    (Time.time + oscillateOffset) * oscillateFrequency * Mathf.PI * 2f
                ) * oscillateAmplitude;
                transform.localPosition = new Vector3(startLocalPos.x, y, startLocalPos.z);
                break;

            case ObstacleType.Rotating:
                transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
                break;
        }
    }
}
