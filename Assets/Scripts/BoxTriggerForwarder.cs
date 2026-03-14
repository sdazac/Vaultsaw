using UnityEngine;

/// <summary>
/// Adjunta al hijo "BoxSensor" de DestructibleBox.
/// Cuando el sensor trigger detecta al jugador, llama HandlePlayerContact() en la caja padre.
/// </summary>
public class BoxTriggerForwarder : MonoBehaviour
{
    private DestructibleBox box;

    void Awake()
    {
        box = GetComponentInParent<DestructibleBox>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || 
            other.GetComponentInParent<PlayerController>() != null)
        {
            box?.HandlePlayerContact();
        }
    }
}