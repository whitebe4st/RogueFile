using UnityEngine;
using EvidenceSystem;

public class EvidenceDebugTool : MonoBehaviour
{
    [Header("Testing Discovery")]
    public string evidenceIDToTest;

    [Header("Testing Connections")]
    public string connectIdA;
    public string connectIdB;

    [ContextMenu("Trigger Discovery")]
    public void TestDiscover()
    {
        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.DiscoverEvidence(evidenceIDToTest);
        }
    }

    [ContextMenu("Trigger Connection")]
    public void TestConnect()
    {
        BoardManager bm = FindObjectOfType<BoardManager>();
        if (bm != null)
        {
            bm.DrawConnection(connectIdA, connectIdB);
        }
        else
        {
            Debug.LogError("BoardManager not found in scene!");
        }
    }
}
