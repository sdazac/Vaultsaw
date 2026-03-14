using UnityEngine;

/// <summary>
/// Componente que marca un trigger como entrada o salida de TuneSection.
/// No depende de tags — el PlayerController lo detecta por GetComponent.
/// </summary>
public class TuneTrigger : MonoBehaviour
{
    public enum TriggerType { Entry, Exit }
    public TriggerType triggerType = TriggerType.Entry;
    public TuneSection parentSection;
}