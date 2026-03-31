using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using UnityEngine.UI;
using UnityEngine.Events;

namespace DialogueSystem
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI Components")]
        [Tooltip("The panel containing the dialogue UI. Will be hidden when no dialogue is active.")]
        public GameObject DialogueWindow;
        
        public TextMeshProUGUI SpeakerNameText;
        public TextMeshProUGUI DialogueText;
        public Image SpeakerSprite; // Character portrait
        
        [Tooltip("Optional: The separate panel (e.g. Notebook) containing the choices. Will be shown/hidden via animator.")]
        public GameObject ChoicePanel;

        [Tooltip("Container where choice buttons will be instantiated.")]
        public Transform ChoicesContainer;
        
        [Tooltip("Prefab for a choice button. Must have a Button component and a TextMeshProUGUI child.")]
        public GameObject ChoiceButtonPrefab;

        [Tooltip("Optional: Dedicated prefab for 'Continue' and 'Close' buttons. Falls back to ChoiceButtonPrefab if null.")]
        public GameObject ContinueButtonPrefab;
        
        [Tooltip("Button covering the entire screen/window to allow 'Click to Continue'.")]
        public Button FullScreenButton;

        [Tooltip("If true, the 'Continue...' and 'Close' buttons will be invisible (but still functional via Hand Tracking/Click).")]
        public bool HideContinueButton = false;


        // Exposed for Hand Controller

        // Exposed for Hand Controller
        public List<Button> ActiveChoiceButtons { get; private set; } = new List<Button>();

        [Header("AI Integration (Dify)")]
        public DifyManager difyManager;
        public GameObject ChatInputPanel; // Panel containing InputField and Buttons
        public TMP_InputField ChatInputField;
        public Button ChatSendButton;
        public Button ChatCloseButton;
        public bool HideAIEmotionTags = true; // New Visibility Toggle
        public bool SplitAIResponseOnEmotion = true; // New Splitting Toggle
        public bool SplitAIResponseOnFullStop = false; // New Sentence Splitting Toggle
        private string currentAIEmotion = "Neutral"; // Persistent emotion state

        [Header("Typewriter Settings")]
        public float typewriterSpeed = 0.02f;
        public float choiceDisplayDelay = 0.5f; // New setting
        private Coroutine typewriterCoroutine;
        private Coroutine choiceDelayCoroutine; // New tracking
        private bool isTyping = false;
        private string currentFullText;
        private Queue<string> chatResponseQueue = new Queue<string>();
        private bool isSendingChat = false; // Prevent double-submit
        private DialogueNode currentAIExitNode = null; // Node to play after chat ends
        private CharacterProfile currentProfile = null; // For AI Persona tracking
        private Coroutine thinkingCoroutine = null;
        public bool IsDialogueActive => DialogueWindow != null && DialogueWindow.activeSelf;
        public bool IsDialogueOpen => IsDialogueActive;
        public bool IsChatActive { get; private set; } = false;

        [Header("Events")]
        [Tooltip("Called when a Node with an EventKey is entered. The string argument is the Key.")]
        public DialogueEvent OnDialogueEvent;

        [System.Serializable]
        public class DialogueEvent : UnityEvent<string> { }

        private void Awake()
        {
            // Singleton Pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[Singleton] Duplicate DialogueManager detected on '{gameObject.name}'! Destroying this one because instance already exists on '{Instance.gameObject.name}'.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Optionally persist across scenes
            // DontDestroyOnLoad(gameObject);
            if (OnDialogueEvent == null)
                OnDialogueEvent = new DialogueEvent();
            // Hide on start
            if (DialogueWindow != null)
                DialogueWindow.SetActive(false);
            
            if (ChoicePanel != null)
                ChoicePanel.SetActive(false);
        }

        private void Start()
        {
            // Init UI states
            if (ChatInputPanel != null) ChatInputPanel.SetActive(false);
            if (ChoicePanel != null) ChoicePanel.SetActive(false);
            
            if (ChatSendButton != null) ChatSendButton.onClick.AddListener(SubmitChat);
            if (ChatCloseButton != null) ChatCloseButton.onClick.AddListener(StopChatMode);
            
            // Auto-find Dify Manager if missing
            if (difyManager == null) difyManager = FindObjectOfType<DifyManager>();

            // Ensure Chat Input supports multiple lines and wrapping
            if (ChatInputField != null)
            {
                ChatInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                if (ChatInputField.textComponent != null)
                    ChatInputField.textComponent.enableWordWrapping = true;
            }
        }

        public void StartDialogue(DialogueNode startingNode)
        {
            if (startingNode == null)
            {
                Debug.LogWarning("DialogueManager: Attempted to start dialogue with null node.");
                return;
            }

            // Show window using animator-aware helper
            UpdateUIState(DialogueWindow, true);

            // Lock player movement
            if (PlayerController.Instance != null) PlayerController.Instance.CanMove = false;

            DisplayNode(startingNode);
        }

        private void DisplayNode(DialogueNode node)
        {
            Debug.Log($"Displaying Node: {node.name}");
            
            // Update Text
            if (SpeakerNameText != null) SpeakerNameText.text = node.SpeakerName;
            
            // Update Speaker Image/Sprite
            if (SpeakerSprite != null)
            {
                if (node.characterProfile != null)
                {
                    currentProfile = node.characterProfile;
                    SpeakerSprite.gameObject.SetActive(true);
                    SpeakerSprite.sprite = currentProfile.GetSprite(node.emotionKey);
                    SpeakerSprite.rectTransform.localScale = Vector3.one * currentProfile.portraitScale;
                }
                else
                {
                    currentProfile = null;
                    // Optionally hide or set a default if no profile
                    // SpeakerSprite.gameObject.SetActive(false);
                }
            }

            // Start Typewriter Animation
            bool hasChoices = (node.IsContinuous || (node.Choices != null && node.Choices.Count > 0) || true); // Default to true as close button is auto-gen
            StartTypewriter(node.DialogueText, !IsChatActive);

            // Invoke Events
            if (node.OnNodeEnter != null)
                node.OnNodeEnter.Invoke();
            
            // Invoke String Event
            if (!string.IsNullOrEmpty(node.EventKey))
            {
                if (node.EventKey == "START_CHAT")
                {
                    // Pass the next node from the script as the "Goodbye" node
                    StartChatMode(node.GetNextNode());
                }
                else if (OnDialogueEvent != null)
                {
                    OnDialogueEvent.Invoke(node.EventKey);
                }
            }

            // Clear old choices
            ClearChoices();

            // Create new choices
            if (node.IsContinuous)
            {
                // Auto-generate "Continue" choice
                DialogueChoice continueChoice = new DialogueChoice
                {
                    ChoiceText = "Continue...",
                    NextNode = node.GetNextNode()
                };

                CreateChoiceButton(continueChoice, HideContinueButton, ContinueButtonPrefab);
                
                // Enable Full Screen Click
                SetupFullScreenButton(continueChoice);
            }
            else if (node.Choices != null && node.Choices.Count > 0)
            {
                // Multiple choices - Disable Full Screen Click to force selection
                if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(false);

                for (int i = 0; i < node.Choices.Count; i++)
                {
                    var rawChoice = node.Choices[i];
                    DialogueChoice evaluatedChoice = new DialogueChoice
                    {
                        ChoiceText = rawChoice.ChoiceText,
                        NextNode = node.GetNextNode(i)
                    };
                    CreateChoiceButton(evaluatedChoice); // Never hide explicit choices
                }
            }
            else
            {
                // Auto-generate "Close" choice
                DialogueChoice closeChoice = new DialogueChoice
                {
                    ChoiceText = "Close",
                    NextNode = null // Triggers EndDialogue
                };
                CreateChoiceButton(closeChoice, HideContinueButton, ContinueButtonPrefab);
                
                // Enable Full Screen Click
                SetupFullScreenButton(closeChoice);
            }
        }

        private void OnChoiceSelected(DialogueChoice choice)
        {
            Debug.Log($"OnChoiceSelected: '{choice.ChoiceText}'. NextNode is {(choice.NextNode == null ? "NULL" : choice.NextNode.name)}");
            if (choice.NextNode != null)
            {
                DisplayNode(choice.NextNode);
            }
            else if (IsChatActive && chatResponseQueue.Count > 0)
            {
                DisplayNextChatChunk();
            }
            else
            {
                Debug.Log("NextNode is null, ending dialogue.");
                EndDialogue();
            }
        }

        private void SetupFullScreenButton(DialogueChoice choice)
        {
            if (FullScreenButton != null)
            {
                Debug.Log($"[DialogueUI] Configured FullScreenButton for: '{choice.ChoiceText}'. (Hidden until typing finishes)");
                
                // Keep hidden initially as per user request
                FullScreenButton.gameObject.SetActive(false); 
                
                FullScreenButton.onClick.RemoveAllListeners();
                FullScreenButton.onClick.AddListener(() => {
                    Debug.Log("[DialogueUI] FullScreenButton Clicked.");
                    
                    if (isTyping)
                    {
                        // Note: If the button is hidden during typing, this branch will not be hit 
                        // unless another trigger calls it.
                        FinishTypewriterEarly();
                    }
                    else if (IsChatActive && chatResponseQueue.Count > 0)
                    {
                        DisplayNextChatChunk();
                    }
                    else
                    {
                        OnChoiceSelected(choice);
                    }
                });
            }
            else
            {
                Debug.LogWarning("FullScreenButton is NULL in DialogueManager!");
            }
        }

        private void CreateChoiceButton(DialogueChoice choice, bool hide = false, GameObject overridePrefab = null)
        {
            GameObject prefabToUse = overridePrefab != null ? overridePrefab : ChoiceButtonPrefab;
            if (prefabToUse == null || ChoicesContainer == null) 
            {
                Debug.LogWarning($"[DialogueUI] Cannot create button '{choice.ChoiceText}'. Prefab: {prefabToUse}, Container: {ChoicesContainer}");
                return;
            }

            Debug.Log($"[DialogueUI] Instantiating button: {choice.ChoiceText} (Hide: {hide}) using prefab: {prefabToUse.name}");
            GameObject buttonObj = Instantiate(prefabToUse, ChoicesContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObj.GetComponent<Image>();

            if (buttonText != null)
            {
                buttonText.text = choice.ChoiceText;
                if (hide) buttonText.enabled = false;
            }

            if (buttonImage != null && hide)
            {
                buttonImage.enabled = false;
            }

            if (hide)
            {
                // Ignore layout so it doesn't take up space
                LayoutElement layout = buttonObj.GetComponent<LayoutElement>();
                if (layout == null) layout = buttonObj.AddComponent<LayoutElement>();
                layout.ignoreLayout = true;
            }

            if (button != null)
            {
                button.onClick.AddListener(() => OnChoiceSelected(choice));
                ActiveChoiceButtons.Add(button); // Track it
            }
        }

        private void ClearChoices()
        {
            // Cancel any pending choice reveal
            if (choiceDelayCoroutine != null) StopCoroutine(choiceDelayCoroutine);

            ActiveChoiceButtons.Clear(); // Clear list
            
            // Hide the ChoicePanel (Notebook) immediately when clearing
            UpdateUIState(ChoicePanel, false);

            if (ChoicesContainer == null) return;

            foreach (Transform child in ChoicesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        public void EndDialogue()
        {
            Debug.Log($"EndDialogue called!");
            UpdateUIState(DialogueWindow, false);
            UpdateUIState(ChoicePanel, false);
            ClearChoices();

            // Unlock player movement
            if (PlayerController.Instance != null) PlayerController.Instance.CanMove = true;
        }

        // Helper for String Event
        public void TriggerMinigame(string gameName)
        {
            if (gameName == "RPS")
            {
                if (RPSMiniGame.Instance != null)
                {
                    RPSMiniGame.Instance.StartGame();
                }
                else
                {
                    Debug.LogError("RPSMiniGame Instance not found in scene!");
                }
            }
        }

        // --- Chat Mode Logic ---

        public void StartChatMode(DialogueNode exitNode = null)
        {
            if (difyManager == null)
            {
                Debug.LogError("DifyManager is missing! Cannot start chat.");
                return;
            }

            currentAIExitNode = exitNode;

            // Reset AI emotion to Neutral at the start of a conversation
            currentAIEmotion = "Neutral";

             // Start fresh context for each dialogue trigger
            difyManager.ResetConversation();
            
            IsChatActive = true;
            
            // UI State: Hide choices immediately.
            // Input panel will be automatically shown by OnTypewriterComplete once intro text finishes typing.
            if (ChoicesContainer != null) ChoicesContainer.gameObject.SetActive(false);
            if (ChatInputPanel != null) ChatInputPanel.SetActive(false); 
        }

        public void StopChatMode()
        {
            IsChatActive = false;
            
            // UI State
            if (ChatInputPanel != null) ChatInputPanel.SetActive(false);
            
            if (currentAIExitNode != null)
            {
                Debug.Log($"[DialogueUI] AI Chat stopped. Transitioning to exit node: {currentAIExitNode.name}");
                DisplayNode(currentAIExitNode);
                currentAIExitNode = null; // Clear it to avoid loops
            }
            else
            {
                if (ChoicesContainer != null) ChoicesContainer.gameObject.SetActive(true); // Restore buttons (if any)
                EndDialogue();
            }
        }

        public void SubmitChat()
        {
            if (ChatInputField == null || isSendingChat) return;
            string message = ChatInputField.text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            // ... (rest same) ...
            DifyInputs inputs = new DifyInputs();
            if (SpeakerNameText != null) inputs.npc_name = SpeakerNameText.text;
            if (currentProfile != null) 
            {
                inputs.npc_id = currentProfile.npcID;
            }
            
            // Centralized State Sync
            if (GlobalStateManager.Instance != null)
            {
                GlobalStateManager.Instance.NotifyStateChange();
            }
            
            if (difyManager != null) inputs.game_state = difyManager.globalGameState;

            difyManager.SendMessageToDify(message, inputs, OnDifyResponse, OnDifyError);
            if (ChatInputPanel != null) ChatInputPanel.SetActive(false);

            // UI Polish: Clear input and show thinking
            if (ChatInputField != null) 
            {
                ChatInputField.text = "";
                ChatInputField.SetTextWithoutNotify(""); // Extra safety for TMP
            }
            StartThinkingAnimation();
        }

        private string SanitizeAIResponse(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // 1. Normalize Smart Quotes to standard ones
            text = text.Replace("“", "\"").Replace("”", "\"")
                       .Replace("‘", "'").Replace("’", "'")
                       .Replace("`", "'");

            // 2. Clear Markdown markers (AI often adds bold/italics)
            text = text.Replace("*", "").Replace("_", "");

            // 3. Normalize Unicode & Strip Combining Diacritics (The "Floating Marks")
            // This decomposes characters (e.g., y + accent) and then strips the non-spacing marks.
            text = text.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            text = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);

            // 4. Force strip hidden control characters/noise
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\p{C}+", "");

            // 5. Final Trim of surrounding junk/quotes
            return text.Trim(' ', '\n', '\r', '\t', '\"', '\'');
        }

        private void StartThinkingAnimation()
        {
            if (thinkingCoroutine != null) StopCoroutine(thinkingCoroutine);
            thinkingCoroutine = StartCoroutine(ThinkingRoutine());
        }

        private void StopThinkingAnimation()
        {
            if (thinkingCoroutine != null)
            {
                StopCoroutine(thinkingCoroutine);
                thinkingCoroutine = null;
            }
            // Force clear Thinking text immediately
            if (DialogueText != null) 
            {
                DialogueText.text = "";
                DialogueText.ForceMeshUpdate();
            }
        }

        private System.Collections.IEnumerator ThinkingRoutine()
        {
            string baseText = "Thinking";
            int dots = 0;
            if (DialogueText != null) DialogueText.text = baseText;
            
            while (true)
            {
                dots = (dots + 1) % 4;
                if (DialogueText != null) 
                    DialogueText.text = baseText + new string('.', dots);
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        private void OnDifyResponse(string response)
        {
            isSendingChat = false; // Allow next message
            StopThinkingAnimation();
            SplitAndQueueResponse(response);
            DisplayNextChatChunk();
        }

        private void SplitAndQueueResponse(string text)
        {
            Debug.Log($"[Dify] Splitting response (Length: {text.Length})");
            chatResponseQueue.Clear();

            // 1. Unified Splitter: Splits by double newlines OR emotion tags OR full stops (if enabled)
            // This ensures every distinct "piece" of dialogue is isolated correctly.
            
            // Pattern to match:
            // - Any content inside parentheses: \([^)]+\)
            // - Any sentence ending: [.!?]
            // We use positive lookahead/lookbehind or just split and clean up.
            
            // Strategy: Protect emotion tags by replacing them with a delimiter temporarily, or use a complex regex.
            // Let's use a simpler approach: Split by double newline first.
            string[] rawParagraphs = text.Split(new string[] { "\n\n", "\r\n\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var p in rawParagraphs)
            {
                // We want to split p into segments. Each segment should either:
                // a) Be a full sentence (if SplitAIResponseOnFullStop is true)
                // b) Start with a new emotion (if SplitAIResponseOnEmotion is true)
                
                // Regex finding either sentence endings followed by space OR new emotion tags
                // IMPROVED: Ensure we don't split on single letters like 's or ... at the end of a chunk
                string splitPattern = @"(?=\s*\([^)]+\))|(?<=[.!?])\s+"; 

                string[] segments = System.Text.RegularExpressions.Regex.Split(p, splitPattern);

                foreach (var seg in segments)
                {
                    if (string.IsNullOrWhiteSpace(seg)) continue;
                    
                    string finalSeg = seg.Trim();

                    // Aggressively Sanitize the text chunk
                    string cleanedChunk = SanitizeAIResponse(finalSeg);
                    
                    if (string.IsNullOrEmpty(cleanedChunk)) continue;

                    // Further split by size if it's still too long
                    if (cleanedChunk.Length > 250)
                    {
                         int index = 0;
                         while (index < cleanedChunk.Length)
                         {
                             int length = Mathf.Min(250, cleanedChunk.Length - index);
                             string sub = cleanedChunk.Substring(index, length).Trim();
                             if (!string.IsNullOrEmpty(sub)) chatResponseQueue.Enqueue(sub);
                             index += length;
                         }
                    }
                    else
                    {
                        chatResponseQueue.Enqueue(cleanedChunk);
                    }
                }
            }
            Debug.Log($"[Dify] Queue populated: {chatResponseQueue.Count} chunks.");
        }

        private void DisplayNextChatChunk()
        {
            if (chatResponseQueue.Count > 0)
            {
                // Prevent multiple simultaneous chunk starts
                if (isTyping) return;

                string chunk = chatResponseQueue.Dequeue();
                
                // Parse Emotion Tag
                string processedText = chunk;
                var match = System.Text.RegularExpressions.Regex.Match(chunk, @"^\s*\(([^)]+)\)");
                if (match.Success)
                {
                    currentAIEmotion = match.Groups[1].Value;
                    if (HideAIEmotionTags)
                        processedText = chunk.Substring(match.Length).Trim();
                }

                // IMPORTANT: If after stripping the tag/spaces there's no text, skip to next chunk
                if (string.IsNullOrEmpty(processedText))
                {
                    Debug.Log("[DialogueUI] Skipping empty AI chunk (likely just an emotion tag).");
                    UpdateAISprite(currentAIEmotion); // Update sprite even if text is empty
                    DisplayNextChatChunk();
                    return;
                }

                // Update Sprite
                UpdateAISprite(currentAIEmotion);
                ClearChoices();
                UpdateUIState(ChoicePanel, false);

                // Setup progression button IMMEDIATELY so users can click to skip text
                DialogueChoice aiContinue = new DialogueChoice { ChoiceText = "Continue..." };
                SetupFullScreenButton(aiContinue);
                if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(true);

                StartTypewriter(processedText, true);
            }
            else if (IsChatActive)
            {
                ClearChoices();
                UpdateUIState(ChatInputPanel, true);
                if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(false);
            }
        }

        private void UpdateAISprite(string emotionKey)
        {
            if (SpeakerSprite == null) return;
            
            string currentNPC = SpeakerNameText?.text;
            if (string.IsNullOrEmpty(currentNPC)) return;

            CharacterProfile[] allProfiles = Resources.FindObjectsOfTypeAll<CharacterProfile>();
            bool profileFound = false;
            foreach (var profile in allProfiles)
            {
                if (profile.characterName.StartsWith(currentNPC, System.StringComparison.OrdinalIgnoreCase))
                {
                    Sprite s = profile.GetSprite(emotionKey);
                    if (s != null)
                    {
                        SpeakerSprite.sprite = s;
                        SpeakerSprite.gameObject.SetActive(true);
                        SpeakerSprite.rectTransform.localScale = UnityEngine.Vector3.one * profile.portraitScale;
                        Debug.Log($"[DialogueUI] Sprite updated for {currentNPC} to emotion '{emotionKey}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[DialogueUI] Profile for {currentNPC} found, but no sprite for '{emotionKey}' and no fallback Neutral.");
                    }
                    profileFound = true;
                    break;
                }
            }

            if (!profileFound)
            {
                Debug.LogWarning($"[DialogueUI] No CharacterProfile found with name matching '{currentNPC}'. Check the CharacterName field in your CharacterProfile assets.");
            }
        }

        private void OnDifyError(string error)
        {
            isSendingChat = false; // Recover
            StopThinkingAnimation();
            Debug.LogError($"Dify Error: {error}");
            if (DialogueText != null) DialogueText.text = "*static* (AI connection lost)";
            if (ChatInputPanel != null) ChatInputPanel.SetActive(true); 
        }

        public void CancelAIChat()
        {
            Debug.Log("[DialogueUI] AI Chat Cancelled (Likely due to a Warning or Game Event).");
            
            // Wipe the pending text queue
            chatResponseQueue.Clear();
            
            // Stop active typing out of the AI response
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
                isTyping = false;
            }

            // Stop the thinking dots if it fired before the text arrived
            StopThinkingAnimation();

            // Hide the 'click to continue' overlay if we were mid-sentence
            if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(false);
            
            // Exit Chat state so the incoming scripted Warning Dialogue plays properly
            IsChatActive = false;
        }

        // --- Typewriter Logic ---

        private void StartTypewriter(string text, bool showChoicesWhenDone)
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            
            // Cancel any pending choice reveal from a previous line
            if (choiceDelayCoroutine != null) StopCoroutine(choiceDelayCoroutine);

            // Use UpdateUIState to hide choices (consistent with animator)
            UpdateUIState(ChoicesContainer?.gameObject, false);
            UpdateUIState(ChatInputPanel, false);
            
            // Final sanitization safety net
            string finalSanitizedText = SanitizeAIResponse(text);
            currentFullText = finalSanitizedText;
            
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(finalSanitizedText, showChoicesWhenDone));
        }

        private System.Collections.IEnumerator TypewriterRoutine(string text, bool showChoicesWhenDone)
        {
            isTyping = true;
            if (DialogueText != null)
            {
                // MODERN TYPEWRITER: Use maxVisibleCharacters instead of concatenation
                // This prevents "floating" character artifacts and is much smoother.
                DialogueText.text = text;
                DialogueText.maxVisibleCharacters = 0;
                DialogueText.ForceMeshUpdate();

                int totalVisibleCharacters = text.Length;
                int counter = 0;

                while (counter <= totalVisibleCharacters)
                {
                    DialogueText.maxVisibleCharacters = counter;
                    counter++;
                    yield return new WaitForSeconds(typewriterSpeed);
                }
                
                // Ensure all characters are visible at the end
                DialogueText.maxVisibleCharacters = totalVisibleCharacters;
            }
            
            OnTypewriterComplete(showChoicesWhenDone);
        }

        private void OnTypewriterComplete(bool showChoices)
        {
            isTyping = false;
            typewriterCoroutine = null;

            if (IsChatActive)
            {
                Debug.Log($"[DialogueUI] AI Typewriter complete. Chunks left: {chatResponseQueue.Count}");
                
                if (chatResponseQueue.Count == 0)
                {
                    // End of response: Show Input
                    UpdateUIState(ChoicePanel, false);
                    UpdateUIState(ChoicesContainer?.gameObject, false);
                    UpdateUIState(ChatInputPanel, true);
                    if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(false);
                }
                else
                {
                    // Middle of response: DO NOT show Notebook/ChoicePanel for AI.
                    // Just enable Full-Screen Click to progress.
                    UpdateUIState(ChoicePanel, false);
                    UpdateUIState(ChoicesContainer?.gameObject, false);

                    // Setup logic for the invisble 'Continue' trigger
                    DialogueChoice aiContinue = new DialogueChoice { ChoiceText = "Continue..." };
                    SetupFullScreenButton(aiContinue);

                    if (FullScreenButton != null) FullScreenButton.gameObject.SetActive(true);
                }
            }
            else if (showChoices)
            {
                Debug.Log($"[DialogueUI] Scripted Typewriter complete. Waiting {choiceDisplayDelay}s before showing choices.");
                
                // Use a coroutine to handle the delay
                if (choiceDelayCoroutine != null) StopCoroutine(choiceDelayCoroutine);
                choiceDelayCoroutine = StartCoroutine(ShowChoicesAfterDelay());
            }
        }

        private System.Collections.IEnumerator ShowChoicesAfterDelay()
        {
            if (choiceDisplayDelay > 0)
                yield return new WaitForSeconds(choiceDisplayDelay);

            // If we have hidden buttons (HideContinueButton), don't show the ChoicePanel background.
            bool shouldShowPanel = true;
            if (ActiveChoiceButtons.Count == 1 && HideContinueButton) shouldShowPanel = false;
            
            if (ActiveChoiceButtons.Count == 0) shouldShowPanel = false;

            if (shouldShowPanel)
            {
                UpdateUIState(ChoicePanel, true);
                if (ChoicesContainer != null) UpdateUIState(ChoicesContainer.gameObject, true);
            }

            // Ensure FullScreenButton is available for "Click to Continue"
            if (FullScreenButton != null && !FullScreenButton.gameObject.activeSelf)
            {
                if (ActiveChoiceButtons.Count <= 1)
                {
                    FullScreenButton.gameObject.SetActive(true);
                    FullScreenButton.transform.SetAsLastSibling(); // Move to top of hierarchy
                }
            }

            choiceDelayCoroutine = null;
        }

        private void FinishTypewriterEarly()
        {
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            
            if (DialogueText != null) DialogueText.text = currentFullText;
            
            // Logic to determine which UI to restore
            bool shouldShowChoices = !IsChatActive;
            OnTypewriterComplete(shouldShowChoices);
        }

        private void UpdateUIState(GameObject obj, bool show)
        {
            if (obj == null) return;

            string objName = obj.name;
            UniversalUIAnimator animator = obj.GetComponent<UniversalUIAnimator>();
            
            Debug.Log($"[DialogueUI] UpdateUIState: {objName} -> {(show ? "SHOW" : "HIDE")} (HasAnimator: {animator != null})");

            if (animator != null)
            {
                if (show) animator.Show();
                else animator.Hide();
            }
            else
            {
                obj.SetActive(show);
            }
        }
    }
}
