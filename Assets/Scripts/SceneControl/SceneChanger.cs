using UnityEngine;
using UnityEngine.SceneManagement;
using DialogueSystem;

public class SceneChanger : MonoBehaviour
{
    [Header("Target Scene")]
    public string sceneToLoad = "Main";

    [Header("Mode A: Trigger (walk into collider)")]
    public bool enableTriggerChange = true;

    [Header("Mode B: Dialogue EventKey")]
    public bool enableDialogueEventChange = true;
    public string triggerKey = "GoBackMain";
    public DialogueTrigger ownerTrigger;        
    public bool requireCurrentOwner = true;     
    public bool closeDialogueBeforeLoad = true;

    private bool _loading;

    void Awake()
    {
        if (ownerTrigger == null)
            ownerTrigger = GetComponent<DialogueTrigger>();
    }

    void Start()
    {
        if (enableDialogueEventChange && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEvent.AddListener(HandleDialogueEvent);
        }
    }

    void OnDestroy()
    {
        if (enableDialogueEventChange && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEvent.RemoveListener(HandleDialogueEvent);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enableTriggerChange) return;
        if (_loading) return;
        if (!other.CompareTag("Player")) return;

        _loading = true;
        Debug.Log("[SceneChanger] Trigger -> Load " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }

  
    public void HandleDialogueEvent(string key)
    {
        if (!enableDialogueEventChange) return;
        if (_loading) return;

        Debug.Log($"[SceneChanger] got key='{key}', need='{triggerKey}'");

        if (key != triggerKey) return;

        if (requireCurrentOwner && ownerTrigger != null && DialogueTrigger.Current != ownerTrigger)
            return;

        _loading = true;

        if (closeDialogueBeforeLoad && DialogueManager.Instance != null)
            DialogueManager.Instance.EndDialogue();

        Debug.Log("[SceneChanger] DialogueEvent -> Load " + sceneToLoad);
        SceneManager.LoadScene(sceneToLoad);
    }
}
