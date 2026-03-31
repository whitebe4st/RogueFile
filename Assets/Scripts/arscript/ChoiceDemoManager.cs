using UnityEngine;
using UnityEngine.UI;

public class ChoiceDemoManager : MonoBehaviour
{
    public HandInputReceiver handInput;
    
    // Optional: reference to 4 objects to highlight
    public GameObject choice1Obj;
    public GameObject choice2Obj;
    public GameObject choice3Obj;
    public GameObject choice4Obj;

    private int lastChoice = -1;

    void Update()
    {
        if (handInput == null) return;

        int choice = handInput.currentChoice;

        if (choice != lastChoice)
        {
            Debug.Log("Selected Choice: " + choice);
            lastChoice = choice;
            UpdateVisuals(choice);
        }
    }

    void UpdateVisuals(int choice)
    {
        // Reset all first (simple method)
        SetHighlight(choice1Obj, false);
        SetHighlight(choice2Obj, false);
        SetHighlight(choice3Obj, false);
        SetHighlight(choice4Obj, false);

        // Highlight selected
        switch (choice)
        {
            case 1: SetHighlight(choice1Obj, true); break;
            case 2: SetHighlight(choice2Obj, true); break;
            case 3: SetHighlight(choice3Obj, true); break;
            case 4: SetHighlight(choice4Obj, true); break;
        }
    }

    void SetHighlight(GameObject obj, bool active)
    {
        if (obj == null) return;
        
        // Try to get Renderer (3D object)
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = active ? Color.green : Color.white;
        }

        // Try to get Image (UI object)
        var image = obj.GetComponent<Image>();
        if (image != null)
        {
            image.color = active ? Color.green : Color.white;
        }
    }
}
