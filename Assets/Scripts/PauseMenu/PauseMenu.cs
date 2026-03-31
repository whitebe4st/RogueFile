using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public Button exitButton;

    [Header("Bounce Animation (optional)")]
    [Tooltip("Assign the MenuButtonBounceIntro on your panel child GameObject.")]
    public MenuButtonBounceIntro bounceIntro;

    private bool isOpen;

    void Start()
    {
        if (pausePanel) pausePanel.SetActive(false);
        isOpen = false;

        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    public void Toggle()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            if (pausePanel) pausePanel.SetActive(true);
            if (bounceIntro != null) bounceIntro.PlayIntro();
        }
        else
        {
            if (pausePanel) pausePanel.SetActive(false);
        }

        // Time.timeScale = isOpen ? 0f : 1f;
    }

    void ExitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}