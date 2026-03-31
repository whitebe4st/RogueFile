using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("A, B, C, D, E, F, AL, AR, BL, BR, CL, CR")]
    public string spawnId = "A";

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.7f);
    }
}