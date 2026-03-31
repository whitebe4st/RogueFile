using UnityEngine;
using System.Collections.Generic;

public class CameraHideObstacle : MonoBehaviour
{
    public Transform target;
    public LayerMask obstacleLayers;

    private readonly List<Renderer> hiddenRenderers = new List<Renderer>();

    void LateUpdate()
    {
        if (target == null) return;

        foreach (var r in hiddenRenderers)
        {
            if (r != null) r.enabled = true;
        }
        hiddenRenderers.Clear();

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir.normalized,dist,obstacleLayers);

        foreach (var hit in hits)
        {
            var rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = false;
                hiddenRenderers.Add(rend);
            }
        }
    }
}
