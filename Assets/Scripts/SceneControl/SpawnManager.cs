using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Tooltip("ถ้าไม่เจอ spawnId ที่ส่งมา จะใช้จุดแรกในซีนแทน")]
    public bool fallbackToAnySpawnPoint = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // หา Player ในซีนใหม่ (ถ้าคุณใช้ DontDestroyOnLoad กับ Player ก็หาได้เหมือนกัน)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SpawnManager: ไม่เจอ Player (Tag = Player) ในซีน " + scene.name);
            return;
        }

        SpawnPoint[] points = FindObjectsOfType<SpawnPoint>();
        if (points == null || points.Length == 0)
        {
            Debug.LogWarning("SpawnManager: ในซีนไม่มี SpawnPoint เลย");
            return;
        }

        string id = SceneSpawnData.NextSpawnId;

        SpawnPoint chosen = null;
        if (!string.IsNullOrEmpty(id))
        {
            foreach (var p in points)
            {
                if (p.spawnId == id)
                {
                    chosen = p;
                    break;
                }
            }
        }

        if (chosen == null && fallbackToAnySpawnPoint)
            chosen = points[0];

        if (chosen == null)
        {
            Debug.LogWarning($"SpawnManager: ไม่เจอ SpawnPoint ที่ id={id} และไม่มี fallback");
            return;
        }

        TeleportPlayer(player, chosen.transform);
    }

    private void TeleportPlayer(GameObject player, Transform target)
    {
        // ถ้าเป็น CharacterController แนะนำปิดก่อนย้าย ไม่งั้นบางทีมันเด้ง
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // ถ้าใช้ Rigidbody ก็ควรรีเซ็ต velocity
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = target.position;
            rb.rotation = target.rotation;
        }
        else
        {
            player.transform.SetPositionAndRotation(target.position, target.rotation);
        }

        if (cc != null) cc.enabled = true;
    }
}
