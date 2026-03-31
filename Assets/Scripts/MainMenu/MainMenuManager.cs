using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Name")]
    public string gameSceneName = "IntroCutScene";

    [Header("Panels (Optional)")]
    public GameObject settingsPanel;

    // ==============================
    // NEW GAME
    // ==============================
    public void NewGame()
    {
        Debug.Log("Start New Game");

        // โหลดเข้าเกม
        SceneManager.LoadScene(gameSceneName);
    }

    // ==============================
    // LOAD GAME (ยังไม่ทำระบบ save ปล่อยไว้ก่อน)
    // ==============================
    public void LoadGame()
    {
        Debug.Log("Load Game Clicked");

        // ถ้ายังไม่มีระบบ save ก็ให้เข้าเกมปกติไปก่อน
        SceneManager.LoadScene(gameSceneName);
    }

    // ==============================
    // SETTINGS
    // ==============================
    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ==============================
    // EXIT GAME
    // ==============================
    public void ExitGame()
    {
        Debug.Log("Exit Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}