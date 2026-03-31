using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public CharacterStats myStats; 

    [Header("Melee Attack")]
    public Transform attackPoint; // Empty Object for attack position
    public float attackRange = 0.5f;
    public int meleeDamage = 20;
    public LayerMask enemyLayers;

    [Header("Item Usage")]
    // Potion logic removed; Medkit logic uses Inventory

    [Header("Cooldowns")]
    public float attackCooldown = 0.5f; // Time in seconds between attacks
    private float nextAttackTime = 0f;

    void Update()
    {
        // Melee Attack (V key)
        if (Input.GetKeyDown(KeyCode.V) && Time.time >= nextAttackTime)
        {
            MeleeAttack();
            nextAttackTime = Time.time + attackCooldown;
        }

        // Use Medkit from Inventory (F key)
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryUseMedkitFromInventory();
        }
    }

    void MeleeAttack()
    {
        // TODO: Trigger Animation here
        if (GetComponent<Animator>() != null)
        {
            GetComponent<Animator>().SetTrigger("Punch");
        }
        
        // Detect enemies in range of attackPoint
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            CharacterStats stats = enemy.GetComponent<CharacterStats>();
            if (stats != null)
            {
                // Pass player position for knockback
                stats.TakeDamage(meleeDamage, transform.position);
            }
        }
    }

    public void TryUseMedkitFromInventory()
    {
        Inventory inv = FindObjectOfType<Inventory>();
        if (inv != null)
        {
            // Find a medkit in the inventory
            ItemData medkit = inv.items.Find(i => i.itemName.ToLower().Contains("medkit"));
            if (medkit != null)
            {
                inv.UseItem(medkit);
            }
            else
            {
                Debug.Log("No Medkit found in inventory!");
            }
        }
    }

    // Draw gizmos in Scene view for debugging
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
