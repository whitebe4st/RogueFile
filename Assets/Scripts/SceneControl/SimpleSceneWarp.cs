using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneWarp : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad;      
    public string targetSpawnId;   

    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
           
            SceneSpawnData.NextSpawnId = targetSpawnId;

         
            Debug.Log($"[Warp] Moving to {sceneToLoad} at point {targetSpawnId}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}