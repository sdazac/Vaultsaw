using UnityEngine;

public class CoinRotator : MonoBehaviour
{
    [Header("Rotación")]
    public float rotationSpeed = 180f; // grados por segundo

    void Update()
    {
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.Self);
    }
}