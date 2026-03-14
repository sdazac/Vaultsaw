using UnityEngine;

/// <summary>
/// Adjunta este script al hijo "ColliderSensor" del Player.
/// Reenvía todos los OnTriggerEnter al PlayerController del padre.
/// </summary>
public class TriggerForwarder : MonoBehaviour
{
    private PlayerController player;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        player?.ReceiveTrigger(other);
    }
}