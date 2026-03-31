using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RPSManager updated to use Sentis (HandDetection) instead of legacy Python bridge.
/// Tracks current RPS gesture from HandDetection landmarks.
/// </summary>
public class RPSManager : MonoBehaviour
{
    [Header("References")]
    public HandDetection handDetection;
    public string currentGesture = "None";
    
    [Header("UI Visualization")]
    public RawImage resultImage;
    public Texture rockTexture;
    public Texture paperTexture;
    public Texture scissorsTexture;
    public Texture defaultTexture;

    void Start()
    {
        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarksUpdated;
        else
            Debug.LogError("[RPSManager] No HandDetection found!");
    }

    void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarksUpdated;
    }

    void OnLandmarksUpdated(Vector3[] landmarks)
    {
        string newGesture = SentisGestureDetector.GetRPSGesture(landmarks);
        
        if (newGesture != currentGesture)
        {
            currentGesture = newGesture;
            Debug.Log("RPS Gesture: " + currentGesture);
            UpdateUI(currentGesture);
        }
    }

    void UpdateUI(string gesture)
    {
        if (resultImage == null) return;

        switch (gesture)
        {
            case "Rock":
                if (rockTexture != null) resultImage.texture = rockTexture;
                break;
            case "Paper":
                if (paperTexture != null) resultImage.texture = paperTexture;
                break;
            case "Scissors":
                if (scissorsTexture != null) resultImage.texture = scissorsTexture;
                break;
            default:
                if (defaultTexture != null) resultImage.texture = defaultTexture;
                break;
        }
    }
    
    void OnGUI()
    {
        // Simple UI to show current state
        GUIStyle style = new GUIStyle();
        style.fontSize = 50;
        style.normal.textColor = Color.red;
        
        GUI.Label(new Rect(10, 10, 500, 100), "Gesture: " + currentGesture, style);
    }
}
