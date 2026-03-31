using UnityEngine;
using TMPro;

/// <summary>
/// Attach to your GoldDisplay GameObject.
/// Reads gold from the player's Inventory and shows it as "NG" (e.g. "100G").
/// Auto-refreshes whenever Inventory.OnChanged fires.
/// </summary>
public class GoldDisplay : MonoBehaviour
{
    [Header("References")]
    public Inventory playerInventory;
    public TMP_Text goldText;

    private void Start()
    {
        // Auto-find if not assigned
        if (playerInventory == null)
            playerInventory = FindObjectOfType<Inventory>();

        if (playerInventory != null)
            playerInventory.OnChanged += Refresh;

        Refresh();
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
            playerInventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (goldText == null || playerInventory == null) return;
        goldText.text = $"{playerInventory.gold}G";
    }
}
