using UnityEngine;
using TMPro;

public class MiniGameInterac : MonoBehaviour
{
    public SignalMatchGame minigame;      // ≈“° SignalMatchGame Object „ Ë
    public TMP_Text promptText;           // (optional) ¢ÈÕ§«“¡ ìPress Eî
    public string playerTag = "Player";

    private bool playerInside;

    void Start()
    {
        if (promptText) promptText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            minigame.StartMinigame();
            if (promptText) promptText.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        if (promptText) promptText.gameObject.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        if (promptText) promptText.gameObject.SetActive(false);
    }
}
