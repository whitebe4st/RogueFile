using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Knockback Settings")]
    public float knockbackForce = 5f;
    private Rigidbody rb;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }

    // Overload for damage without knockback source
    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector3.zero);
    }

    // New TakeDamage that accepts a source position for knockback calculation
    public void TakeDamage(int damage, Vector3 damageSourcePosition)
    {
        currentHealth -= damage;
        Debug.Log(transform.name + " takes " + damage + " damage.");

        // Apply Knockback
        if (rb != null && damageSourcePosition != Vector3.zero)
        {
            // Calculate direction away from the attacker on the X axis
            float xDirection = (transform.position.x >= damageSourcePosition.x) ? 1f : -1f;
            
            // Create a specific knockback vector: strong X push, slight Y pop
            Vector3 knockbackDir = new Vector3(xDirection * 1.5f, 0.5f, 0f);

            // Reset velocity before applying force so it feels consistent
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log(transform.name + " healed. Current HP: " + currentHealth);
    }

    void Die()
    {
        Debug.Log(transform.name + " died.");

        // เช็คว่าตัวที่ตายคือ Player หรือไม่ (ดูจากการมีสคริปต์ PlayerControllerCombat)
        if (GetComponent<PlayerControllerCombat>() != null)
        {
            // สั่งให้กระดาน Game Over โผล่ขึ้นมาแทน
            if (GameOverManager.instance != null)
            {
                GameOverManager.instance.ShowGameOver();
            }
            else
            {
                Debug.LogWarning("Player died but no GameOverManager found in scene!");
            }
            
            // ปิดการใช้งานตัวละครชั่วคราวแทนการทำลายทิ้ง เพื่อหลีกเลี่ยง Null Reference Error 
            gameObject.SetActive(false);
        }
        else
        {
            // ถ้าเป็นศัตรูตาย ให้ดรอปของแล้วทำลายทิ้ง
            EnemyLoot loot = GetComponent<EnemyLoot>();
            if (loot != null)
            {
                loot.DropLoot(); 
            }
            // wait for dying animation

            Destroy(gameObject);
        }
    }
}
