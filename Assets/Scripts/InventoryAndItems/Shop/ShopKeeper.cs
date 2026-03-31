using UnityEngine;
using DialogueSystem;

public class ShopKeeper : MonoBehaviour
{
    private bool isPlayerInRange = false;
    private Inventory playerInventory;

    [Header("Dialogue Options")]
    [Tooltip("Preferred: Use a Dialogue Graph for greetings")]
    public DialogueGraph greetingGraph;
    [Tooltip("Fallback: Use a specific node")]
    public DialogueNode greetingNode;

    [Tooltip("Preferred: Use a Dialogue Graph for farewells")]
    public DialogueGraph farewellGraph;
    [Tooltip("Fallback: Use a specific node")]
    public DialogueNode farewellNode;

    private bool isTalkingBeforeShop = false;
    private bool isShopOpenByThisKeeper = false;

    private DialogueNode GetGreetingStartNode()
    {
        if (greetingGraph != null) return greetingGraph.GetStartNode();
        return greetingNode;
    }

    private DialogueNode GetFarewellStartNode()
    {
        if (farewellGraph != null) return farewellGraph.GetStartNode();
        return farewellNode;
    }

    void Update()
    {
        if (isPlayerInRange)
        {
            // If shop was opened by this keeper, but is now closed (e.g., via UI button)
            if (isShopOpenByThisKeeper && !ShopManager.Instance.isShopOpen)
            {
                isShopOpenByThisKeeper = false;
                PlayFarewellDialogue();
            }

            // If shop is currently open through this NPC
            if (ShopManager.Instance.isShopOpen && isShopOpenByThisKeeper)
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseShopWithDialogue();
                }
            }
            // If shop is not open, and no dialogue is active, we can interact to open
            else if (!ShopManager.Instance.isShopOpen && (DialogueManager.Instance == null || !DialogueManager.Instance.IsDialogueActive))
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    DialogueNode startNode = GetGreetingStartNode();
                    if (startNode != null && DialogueManager.Instance != null)
                    {
                        isTalkingBeforeShop = true;
                        DialogueManager.Instance.StartDialogue(startNode);
                    }
                    else
                    {
                        OpenShop();
                    }
                }
            }
            
            // Monitor if the greeting dialogue has finished
            if (isTalkingBeforeShop && DialogueManager.Instance != null && !DialogueManager.Instance.IsDialogueActive)
            {
                isTalkingBeforeShop = false;
                OpenShop();
            }
        }
    }

    private void PlayFarewellDialogue()
    {
        DialogueNode startNode = GetFarewellStartNode();
        if (startNode != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(startNode);
        }
    }

    private void OpenShop()
    {
        isShopOpenByThisKeeper = true;
        ShopManager.Instance.OpenShop(playerInventory);
        Debug.Log("Shop Opened");
    }

    private void CloseShopWithDialogue()
    {
        // We set this to false so the Update check doesn't fire PlayFarewellDialogue again
        isShopOpenByThisKeeper = false;
        ShopManager.Instance.CloseShop();
        Debug.Log("Shop Closed by Button/ESC");
        PlayFarewellDialogue();
    }

    private void OnTriggerEnter(Collider other) 
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerInventory = other.GetComponent<Inventory>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            isTalkingBeforeShop = false;
            isShopOpenByThisKeeper = false;
            if (ShopManager.Instance.isShopOpen)
            {
                ShopManager.Instance.CloseShop();
            }
        }
    }
}
