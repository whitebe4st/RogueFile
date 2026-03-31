using UnityEngine;

public class DoorTeleport : MonoBehaviour
{
    [Header("Teleport target")]
    public Transform targetPoint;   // จุดปลายทาง

    [Header("Optional offset")]
    public Vector3 offset;          // กันชนประตูซ้ำ

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Teleport(other.transform);
        }
    }

    void Teleport(Transform player)
    {
        if (targetPoint != null)
        {
            player.position = targetPoint.position + offset;
        }
        else
        {
            Debug.LogWarning("DoorTeleport: targetPoint not assigned");
        }
    }
}