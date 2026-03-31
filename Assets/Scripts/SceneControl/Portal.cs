using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Target")]
    public string targetSceneName;
    public string targetSpawnId = "A";

    [Header("Trigger")]
    public string playerTag = "Player";
    public bool lockWhileLoading = true;

    private bool _loading;

    private void OnTriggerEnter(Collider other)
    {
        if (_loading && lockWhileLoading) return;
        if (!other.CompareTag(playerTag)) return;

        _loading = true;

        // บอกซีนถัดไปว่าให้ spawn ที่ไหน
        SceneSpawnData.NextSpawnId = targetSpawnId;

        // เปลี่ยนซีน
        SceneManager.LoadScene(targetSceneName);
    }
}

