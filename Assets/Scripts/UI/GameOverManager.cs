using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    [Header("UI References")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        // บังคับให้เป็นตัวใหม่ทุกครั้งที่โหลดฉากใหม่เลย ป้องกันระบบจำตัวเก่าแล้วไม่ยอมทำงานรอบสอง
        instance = this;
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Pause the game when player dies
        Time.timeScale = 0f;
        Debug.Log("Game Over UI Shown!");
    }

    public void RestartGame()
    {
        // Must resume time before reloading, otherwise the new scene starts paused
        Time.timeScale = 1f;
        
        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
