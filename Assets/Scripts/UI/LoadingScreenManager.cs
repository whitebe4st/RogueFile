using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("The parent GameObject containing the loading screen UI")]
    public GameObject loadingScreenUI;
    
    [Tooltip("The background image that covers the screen (used for fading)")]
    public Image backgroundImage;
    
    [Tooltip("Optional: A slider to show loading progress")]
    public Slider progressBar;
    
    [Tooltip("Optional: Text to display 'Loading...' or percentages")]
    public TextMeshProUGUI loadingText;

    [Header("Settings")]
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        // Enforce Singleton pattern and keep alive across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure it starts hidden
            if (loadingScreenUI != null)
                loadingScreenUI.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from anywhere to load a new scene with the loading screen.
    /// Example: LoadingScreenManager.Instance.LoadScene("Level2");
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // 1. Prepare and Show the loading screen UI
        if (loadingScreenUI != null) loadingScreenUI.SetActive(true);
        if (progressBar != null) progressBar.value = 0f;
        if (loadingText != null) loadingText.text = "Loading...";

        // 2. Fade in the black background
        yield return StartCoroutine(Fade(1f));

        // 3. Start loading the actual scene asynchronously
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        
        // Prevent the scene from switching instantly before we are ready
        operation.allowSceneActivation = false; 

        // 4. Update progress bar while loading
        while (operation.progress < 0.9f)
        {
            // Unity's async progress stops at 0.9 when allowSceneActivation is false.
            // We map 0-0.9 to 0-1 for the UI slider.
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (progressBar != null) progressBar.value = progress;
            if (loadingText != null) loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";

            yield return null;
        }

        // 5. Ensure progress bar is full
        if (progressBar != null) progressBar.value = 1f;
        if (loadingText != null) loadingText.text = "Loading... 100%";
        
        // Optional: wait a tiny bit so the user actually sees 100%
        yield return new WaitForSeconds(0.2f);

        // 6. Allow the scene to activate
        operation.allowSceneActivation = true;

        // Wait until the scene has fully swapped over
        while (!operation.isDone)
        {
            yield return null;
        }

        // 7. Fade out the black background to reveal the new scene
        yield return StartCoroutine(Fade(0f));
        
        // 8. Hide the loading screen UI completely
        if (loadingScreenUI != null) loadingScreenUI.SetActive(false);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (backgroundImage == null) yield break;

        Color currentColor = backgroundImage.color;
        float startAlpha = currentColor.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float newAlpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            backgroundImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
            yield return null;
        }

        backgroundImage.color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);
    }
}
