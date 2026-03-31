using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;

    private bool isPlayerInRange = false;
    private Inventory playerInventory;

    void Update()
    {
        
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInventory != null)
            {
                playerInventory.AddItem(item);
                Destroy(gameObject);
            }
        }
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
            playerInventory = null;
        }
    }
}