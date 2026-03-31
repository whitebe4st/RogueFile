using UnityEngine;

/// <summary>
/// HandInputReceiver updated to use Sentis (HandDetection) instead of legacy UDP.
/// Maps 1-4 fingers to currentChoice for dialogue selection.
/// </summary>
public class HandInputReceiver : MonoBehaviour
{
    [Header("References")]
    public HandDetection handDetection;
    
    [Header("State")]
    public int currentChoice = 0; // 0=None, 1=Index, 2=Middle, 3=Ring, 4=Pinky

    void Start()
    {
        if (handDetection == null)
            handDetection = FindAnyObjectByType<HandDetection>();

        if (handDetection != null)
            handDetection.OnLandmarksUpdated += OnLandmarksUpdated;
        else
            Debug.LogError("[HandInputReceiver] No HandDetection found!");
    }

    void OnDestroy()
    {
        if (handDetection != null)
            handDetection.OnLandmarksUpdated -= OnLandmarksUpdated;
    }

    private void OnLandmarksUpdated(Vector3[] landmarks)
    {
        // Simple mapping: use number of extended fingers to select options 1-4
        // (This matches the old behavior or can be refined)
        int count = SentisGestureDetector.CountExtendedFingers(landmarks);
        
        // Clamp to 1-4 if fingers are showing, else 0
        if (count >= 1 && count <= 4)
            currentChoice = count;
        else if (count >= 5)
            currentChoice = 4; // Max out at 4 choices for dialogue
        else
            currentChoice = 0;
    }
}
