using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DialogueSystem;

/// <summary>
/// RPSMiniGame updated to use Sentis (HandDetection) instead of legacy Python bridge.
/// No more ProcessControl or RPSReceiver dependencies.
/// </summary>
public class RPSMiniGame : MonoBehaviour
{
    public static RPSMiniGame Instance { get; private set; }

    [Header("Dependencies")]
    public RPSManager rpsManager;           // Use the manager instead of the old receiver
    public GameObject RPSPanel;             // The UI Panel overlay
    public TextMeshProUGUI StatusText;      // "3...", "2...", "WIN!"
    public Image PlayerMoveImage;
    public Image NPCMoveImage;

    [Header("Assets")]
    public Sprite RockSprite;
    public Sprite PaperSprite;
    public Sprite ScissorsSprite;
    public Sprite DefaultSprite;

    [Header("Game Settings")]
    public float CountdownTime = 3f;

    // Events to hook back into Dialogue
    public DialogueNode WinNode;
    public DialogueNode LoseNode;
    public DialogueNode DrawNode;

    private bool isPlaying = false;

    private void Awake()
    {
        Instance = this;
        if (RPSPanel != null) RPSPanel.SetActive(false);
    }

    private void Start()
    {
        if (rpsManager == null)
        {
            rpsManager = FindAnyObjectByType<RPSManager>();
        }
    }

    /// <summary>
    /// Call this from a Dialogue Event! 
    /// Key: "START_RPS"
    /// </summary>
    public void StartGame()
    {
        if (isPlaying) return;
        StartCoroutine(GameRoutine());
    }

    private IEnumerator GameRoutine()
    {
        isPlaying = true;
        
        // Hide Dialogue Window immediately (transitioning to game)
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueWindow != null)
        {
            DialogueManager.Instance.DialogueWindow.SetActive(false);
        }

        // Show Panel
        if (RPSPanel != null) RPSPanel.SetActive(true);
        if (PlayerMoveImage != null) PlayerMoveImage.sprite = DefaultSprite;
        if (NPCMoveImage != null) NPCMoveImage.sprite = DefaultSprite;
        if (StatusText != null) StatusText.text = "Get Ready...";

        // Reset data first
        if (rpsManager != null) rpsManager.currentGesture = ""; 
        
        // Brief wait for hand tracking to settle
        yield return new WaitForSeconds(0.5f);

        // Countdown
        float timer = CountdownTime;
        while (timer > 0)
        {
            if (StatusText != null) StatusText.text = Mathf.CeilToInt(timer).ToString();
            yield return null;
            timer -= Time.deltaTime;
        }

        if (StatusText != null) StatusText.text = "SHOW!";
        yield return new WaitForSeconds(0.2f); // Small buffer

        // Capture Input
        string playerMove = GetPlayerMove();
        string npcMove = GetNPCMove();

        // Update UI
        UpdateHandUI(PlayerMoveImage, playerMove);
        UpdateHandUI(NPCMoveImage, npcMove);

        // Determine Winner
        string result = DetermineWinner(playerMove, npcMove);
        if (StatusText != null) StatusText.text = result;

        yield return new WaitForSeconds(2.0f); // Show result for 2 seconds

        // Cleanup & Return to Dialogue
        if (RPSPanel != null) RPSPanel.SetActive(false);
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueWindow != null)
        {
            DialogueManager.Instance.DialogueWindow.SetActive(true);
        }

        // Trigger Next Node
        FinishGame(result);

        isPlaying = false;
    }

    private string GetPlayerMove()
    {
        if (rpsManager == null) return "Rock"; 

        string move = rpsManager.currentGesture;
        if (string.IsNullOrEmpty(move) || move == "None") return "Rock"; // Fallback
        
        return move;
    }

    private string GetNPCMove()
    {
        int rnd = Random.Range(0, 3);
        if (rnd == 0) return "Rock";
        if (rnd == 1) return "Paper";
        return "Scissors";
    }

    private void UpdateHandUI(Image img, string move)
    {
        if (img == null) return;
        switch (move)
        {
            case "Rock": img.sprite = RockSprite; break;
            case "Paper": img.sprite = PaperSprite; break;
            case "Scissors": img.sprite = ScissorsSprite; break;
        }
    }

    private string DetermineWinner(string p, string n)
    {
        if (p == n) return "Draw";
        
        if (p == "Rock") return (n == "Scissors") ? "Win" : "Lose";
        if (p == "Paper") return (n == "Rock") ? "Win" : "Lose";
        if (p == "Scissors") return (n == "Paper") ? "Win" : "Lose";

        return "Draw";
    }

    private void FinishGame(string result)
    {
        DialogueNode targetNode = null;

        if (result == "Win") targetNode = WinNode;
        else if (result == "Lose") targetNode = LoseNode;
        else targetNode = DrawNode ?? WinNode; // Fallback to Win or define Draw

        if (targetNode != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(targetNode);
        }
    }
}
