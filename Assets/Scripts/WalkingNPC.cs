using System.Collections;
using UnityEngine;

public class WalkingNPC : MonoBehaviour
{
    public Transform npcRoamingPoint;
    public float moveSpeed = 2f;
    public float waitTime = 2f;
    public bool loopWaypoints = true;

    private Transform[] waypoints;
    private int currentWaypointIndex;
    private bool isWaiting;

    void Start()
    {
        waypoints = new Transform[npcRoamingPoint.childCount];

        for (int i = 0; i < npcRoamingPoint.childCount; i++)
        {
            waypoints[i] = npcRoamingPoint.GetChild(i);
        }
    }

    // Update is called once per frame
    void Update()
    {

        if(isWaiting)
        {
            return;
        }

        MoveToWaypoint();

    }

    void MoveToWaypoint()
    {
        if (waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];

        transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            StartCoroutine(WaitAtWayPoint());
        }

    }

    IEnumerator WaitAtWayPoint()
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);

        currentWaypointIndex = loopWaypoints ? (currentWaypointIndex + 1) % waypoints.Length : Mathf.Min(currentWaypointIndex +1, waypoints.Length - 1 );

        isWaiting = false;
    }

}
