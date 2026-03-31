using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.Events;

public class DifyManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Enter your Dify API Key (App Secret) here")]
    public string apiKey = "YOUR_DIFY_KEY_HERE";
    
    [Tooltip("A unique identifier for the user (e.g., player_123)")]
    public string userId = "unity_player";

    private const string API_URL = "https://api.dify.ai/v1/chat-messages";
    
    // Store conversation ID to maintain dialogue context
    private string currentConversationId = "";

    [Header("Game State")]
    public string globalGameState = "Investigation start.";

    [Header("Minigame State")]
    [Range(0, 100)]
    public int currentStress = 0;
    
    [Tooltip("Drag a UI Slider here to visualize stress")]
    public Slider stressMeterUI;
    
    [Header("UI Feedback")]
    [Tooltip("Text showing 'X questions left'")]
    public TextMeshProUGUI questionsLeftText;
    
    [Tooltip("Text giving the player hints on how to play based on current stress")]
    public TextMeshProUGUI hintText;
    
    [Tooltip("Text Animator for 'Suspect Stress' label, magnitude scales with stress")]
    public UniversalTextAnimator stressTextAnimator;

    [Header("Turn Economy Settings")]
    [Tooltip("How many questions the player is allowed to ask before the interrogation ends.")]
    public int maxQuestions = 5;
    public int currentQuestionsAsked = 0;

    [Tooltip("The stress range the suspect needs to be in at the end to confess.")]
    public int minConfessionStress = 70;
    public int maxConfessionStress = 90;

    [Header("Minigame Events")]
    public UnityEvent OnConfessionSuccess; // Triggered when turns run out AND stress is in the sweet spot
    public UnityEvent OnInterrogationFailed; // Triggered when turns run out but stress is bad
    
    [Tooltip("Fires immediately if stress goes ABOVE the sweet spot during the interrogation")]
    public UnityEvent OnWarningTooHigh;
    
    [Tooltip("Fires immediately if stress goes BELOW the sweet spot during the interrogation")]
    public UnityEvent OnWarningTooLow;

    [Tooltip("Fires when questions run out, stress is TOO HIGH, and it's their first failure.")]
    public UnityEvent OnSecondChanceTooHigh;

    [Tooltip("Fires when questions run out, stress is TOO LOW, and it's their first failure.")]
    public UnityEvent OnSecondChanceTooLow;

    // Internal trackers for the Second Chance mechanic
    private bool hasReceivedSecondChance = false;
    private int originalMaxQuestions = -1;

    public void ResetConversation(bool resetStress = false)
    {
        currentConversationId = "";
        globalGameState = "Investigation start.";
        hasReceivedSecondChance = false; // Reset second chance flag for new play-throughs
        
        if (originalMaxQuestions > 0)
        {
            maxQuestions = originalMaxQuestions; // Restore full turns
        }
        else
        {
            originalMaxQuestions = maxQuestions; // First time init
        }

        if (resetStress)
        {
            ResetStress();
        }
        Debug.Log("[DifyManager] Conversation reset.");
    }

    private void OnValidate()
    {
        UpdateGameUI();
    }

    public void ResetStress()
    {
        currentStress = 0;
        currentQuestionsAsked = 0;
        hasReceivedSecondChance = false;
        
        UpdateGameUI();
        Debug.Log("[DifyManager] Stress meter and turns reset.");
    }

    private void UpdateGameUI()
    {
        if (stressMeterUI != null)
        {
            stressMeterUI.value = currentStress;
        }

        if (stressTextAnimator != null)
        {
            stressTextAnimator.magnitude = Mathf.Clamp01(currentStress / 100f);
        }

        if (questionsLeftText != null)
        {
            int remaining = Mathf.Max(0, maxQuestions - currentQuestionsAsked);
            questionsLeftText.text = $"{remaining} questions left";
        }

        if (hintText != null)
        {
            if (currentStress < minConfessionStress)
            {
                hintText.text = "push suspects a little harder";
            }
            else if (currentStress > maxConfessionStress)
            {
                hintText.text = "maybe going easier is better";
            }
            else
            {
                hintText.text = "stress is perfect, keep it here";
            }
        }
    }

    public void SendMessageToDify(string userMessage, DifyInputs inputs, Action<string> onSuccess, Action<string> onError)
    {
        if (currentQuestionsAsked >= maxQuestions)
        {
            Debug.LogWarning("[DifyManager] Cannot ask more questions, max turns reached!");
            onError?.Invoke("You have run out of time to ask questions.");
            return;
        }

        if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("YOUR_DIFY_KEY"))
        {
            onError?.Invoke("Dify API Key is missing or invalid.");
            return;
        }
        StartCoroutine(PostRequest(userMessage, inputs, onSuccess, onError));
    }

    private IEnumerator PostRequest(string message, DifyInputs inputs, Action<string> onSuccess, Action<string> onError)
    {
        // 1. Construct Request Data
        DifyRequest requestData = new DifyRequest();
        requestData.query = message;
        requestData.user = userId;
        requestData.conversation_id = currentConversationId;
        
        if (inputs != null)
        {
            inputs.current_stress = currentStress;
            requestData.inputs = inputs;
        }

        string jsonPayload = JsonUtility.ToJson(requestData);

        // 2. Setup WebRequest
        using (UnityWebRequest www = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            
            // Set Headers
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // 3. Send
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Dify Link Error: {www.error}\nDetail: {www.downloadHandler.text}");
            }
            else
            {
                // 4. Parse Response
                string responseJson = www.downloadHandler.text;
                try
                {
                    DifyResponse responseData = JsonUtility.FromJson<DifyResponse>(responseJson);
                    
                    // Update conversation context
                    currentConversationId = responseData.conversation_id;
                    
                    try
                    {
                        DifyAnswerData answerData = JsonUtility.FromJson<DifyAnswerData>(responseData.answer);
                        
                        // Update game state
                        currentStress = Mathf.Clamp(currentStress + answerData.stress_delta, 0, 100);
                        currentQuestionsAsked++;
                        
                        UpdateGameUI();

                        bool wasInterrupted = false;

                        // Check Win/Loss or Warnings
                        if (currentQuestionsAsked >= maxQuestions)
                        {
                            if (currentStress >= minConfessionStress && currentStress <= maxConfessionStress)
                            {
                                Debug.Log($"[DifyManager] Interrogation over! Stress is {currentStress} (Sweet spot!). CONFESSION SUCCESS.");
                                DialogueSystem.DialogueManager.Instance?.CancelAIChat();
                                OnConfessionSuccess?.Invoke();
                                wasInterrupted = true;
                            }
                            else
                            {
                                if (!hasReceivedSecondChance)
                                {
                                    Debug.Log($"[DifyManager] Interrogation over! Stress is {currentStress} (Outside sweet spot {minConfessionStress}-{maxConfessionStress}). GIVING SECOND CHANCE.");
                                    
                                    // Calculate half of the CURRENT max. Need to ensure at least 1 turn.
                                    int newMax = Mathf.Max(1, Mathf.FloorToInt(maxQuestions / 2f));
                                    
                                    hasReceivedSecondChance = true;
                                    currentQuestionsAsked = 0;
                                    maxQuestions = newMax;

                                    DialogueSystem.DialogueManager.Instance?.CancelAIChat();
                                    
                                    if (currentStress > maxConfessionStress)
                                    {
                                        OnSecondChanceTooHigh?.Invoke();
                                    }
                                    else
                                    {
                                        OnSecondChanceTooLow?.Invoke();
                                    }
                                }
                                else
                                {
                                    Debug.Log($"[DifyManager] Second Chance Interrogation over! Stress is {currentStress}. FAILED FOR GOOD.");
                                    DialogueSystem.DialogueManager.Instance?.CancelAIChat();
                                    OnInterrogationFailed?.Invoke();
                                }
                                wasInterrupted = true;
                            }
                        }
                        else
                        {
                            // Mid-interrogation warnings (only in the last 20% of turns)
                            int warningThreshold = Mathf.FloorToInt(maxQuestions * 0.8f);
                            
                            if (currentQuestionsAsked >= warningThreshold)
                            {
                                if (currentStress > maxConfessionStress)
                                {
                                    Debug.Log("[DifyManager] Late game warning: Stress is too HIGH. Firing event.");
                                    DialogueSystem.DialogueManager.Instance?.CancelAIChat();
                                    OnWarningTooHigh?.Invoke();
                                    wasInterrupted = true;
                                }
                                else if (currentStress < minConfessionStress)
                                {
                                    Debug.Log("[DifyManager] Late game warning: Stress is too LOW. Firing event.");
                                    DialogueSystem.DialogueManager.Instance?.CancelAIChat();
                                    OnWarningTooLow?.Invoke();
                                    wasInterrupted = true;
                                }
                            }
                        }

                        if (!wasInterrupted)
                        {
                            onSuccess?.Invoke(answerData.reply);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[DifyManager] Failed to parse Dify answer as JSON. Make sure Dify is returning strict JSON.\nRaw output: {responseData.answer}\nException: {ex.Message}");
                        onSuccess?.Invoke(responseData.answer); // Fallback to raw text
                    }
                }
                catch (Exception e)
                {
                    onError?.Invoke($"JSON Parse Error: {e.Message}\nRaw: {responseJson}");
                }
            }
        }
    }
}
