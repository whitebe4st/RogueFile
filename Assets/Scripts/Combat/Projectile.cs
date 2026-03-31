using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 10;
    public Rigidbody rb;

    void Start()
    {
        rb.linearVelocity = transform.right * speed; 
    }

    void OnTriggerEnter(Collider hitInfo)
    {
        CharacterStats enemy = hitInfo.GetComponent<CharacterStats>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

       
        if (!hitInfo.CompareTag("Player"))
        { 
            Destroy(gameObject);
        }
    }
}
