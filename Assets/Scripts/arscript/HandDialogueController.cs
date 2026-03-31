using UnityEngine;
using UnityEngine.UI;
using DialogueSystem;
using System.Collections.Generic;

/// <summary>
/// HandDialogueController refactored for Sentis.
/// Uses HandInputReceiver (which now uses Sentis) to select dialogue options.
/// Removed all legacy ProcessLauncher and ProcessControl references.
/// </summary>
public class HandDialogueController : MonoBehaviour
{
    [Header("Dependencies")]
    public HandInputReceiver handInput;

    [Header("Settings")]
    [Tooltip("If true, holding 1 finger near an NPC will start dialogue. If false, you must press E.")]
    public bool EnableHandTrigger = false;
    [Tooltip("If true, Hand Input is ONLY active when there are exactly 4 choices (ignores Yes/No questions).")]
    public bool RestrictToFourChoices = true;

    [Header("Hold Selection Settings")]
    public float holdTimeRequired = 1.5f;
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color confirmColor = Color.green;

    private int lastChoice = 0;
    private float choiceHoldTimer = 0f;

    void Start()
    {
        if (handInput == null)
        {
            handInput = FindAnyObjectByType<HandInputReceiver>();
        }
    }

    void Update()
    {
        if (handInput == null) return;

        int currentHandChoice = handInput.currentChoice; // 0, 1, 2, 3, 4

        // 1. Check if Dialogue is Active
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
        {
            if (DialogueManager.Instance.IsChatActive) return; // Disable Hand Input during Chat
            HandleDialogueInteraction(currentHandChoice);
        }
        else
        {
            HandleWorldInteraction(currentHandChoice);
        }
    }

    private void HandleDialogueInteraction(int handChoice)
    {
        var buttons = DialogueManager.Instance.ActiveChoiceButtons;
        if (buttons == null || buttons.Count == 0) return;

        // NEW: Restrict to exactly 4 choices if enabled
        if (RestrictToFourChoices && buttons.Count != 4)
        {
            return;
        }

        // Check if buttons are "Hidden" (Clean UI Mode)
        var firstBtnImage = buttons[0].GetComponent<Image>();
        if (firstBtnImage != null && !firstBtnImage.enabled) 
        {
            return; 
        }

        // Map Hand Choice (1-4) to List Index (0-3)
        int targetIndex = handChoice - 1;

        // Validation
        if (targetIndex >= 0 && targetIndex < buttons.Count)
        {
            HighlightButton(buttons, targetIndex);

            // Check Hold Logic
            if (handChoice == lastChoice)
            {
                choiceHoldTimer += Time.deltaTime;

                if (choiceHoldTimer >= holdTimeRequired)
                {
                    // CONFIRM SELECTION
                    ConfirmSelection(buttons[targetIndex]);
                    choiceHoldTimer = 0f; // Reset
                    lastChoice = 0; // Reset to force release
                }
            }
            else
            {
                choiceHoldTimer = 0f;
                lastChoice = handChoice;
            }
        }
        else
        {
            // No valid choice selected
            ResetHighlights(buttons);
            choiceHoldTimer = 0f;
            lastChoice = 0;
        }
    }

    private void HandleWorldInteraction(int handChoice)
    {
        if (!EnableHandTrigger) return;

        // Trigger Dialogue if Index Finger (1) is shown and Player is in Range
        if (DialogueTrigger.Current != null && DialogueTrigger.Current.IsPlayerInRange)
        {
            if (handChoice == 1)
            {
                choiceHoldTimer += Time.deltaTime;
                if (choiceHoldTimer > 0.5f) // Short hold
                {
                    DialogueTrigger.Current.TriggerDialogue();
                    choiceHoldTimer = 0;
                }
            }
            else
            {
                choiceHoldTimer = 0;
            }
        }
    }

    private void HighlightButton(List<Button> buttons, int index)
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            Image img = buttons[i].GetComponent<Image>();
            if (img != null)
            {
                if (i == index)
                {
                    // Interpolate color based on hold time
                    float progress = Mathf.Clamp01(choiceHoldTimer / holdTimeRequired);
                    img.color = Color.Lerp(highlightColor, confirmColor, progress);
                }
                else
                {
                    img.color = normalColor;
                }
            }
        }
    }

    private void ResetHighlights(List<Button> buttons)
    {
        foreach (var btn in buttons)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null) img.color = normalColor;
        }
    }

    private void ConfirmSelection(Button btn)
    {
        btn.onClick.Invoke();

        // Reset Logic
        if (DialogueManager.Instance != null && DialogueManager.Instance.ActiveChoiceButtons != null)
            ResetHighlights(DialogueManager.Instance.ActiveChoiceButtons);
    }
}
