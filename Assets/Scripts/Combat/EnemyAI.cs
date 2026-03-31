using UnityEngine;

public class SimpleEnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int damage = 10;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    public float jumpForce = 8f;
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private Rigidbody rb;
    private float nextJumpTime = 0f;
    public float jumpCooldown = 1.5f;
    public float maxJumpDistance = 5f; // Added limit for jumping

    private Animator anim;
    private bool isAttacking = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;
        
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);
        }

        Vector3 distanceVector = player.position - transform.position;
        // Ignore Y for attack range distance calculation
        distanceVector.y = 0; 
        float distance = distanceVector.magnitude;

        if (distance > attackRange)
        {
            // Move using linearVelocity so gravity works and we don't float on Y axis
            Vector3 direction = distanceVector.normalized;
            rb.linearVelocity = new Vector3(direction.x * moveSpeed, rb.linearVelocity.y, direction.z * moveSpeed);
            
            // เล่นท่าเดินเมื่อไม่ได้โจมตีอยู่
            if (anim != null && !isAttacking)
            {
                anim.Play("Walk");
            }

            // Flip sprite depending on horizontal movement
            if (direction.x > 0)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
        else
        {
            // Stop moving horizontally when in attack range
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            if (Time.time >= nextAttackTime)
            {
                AttackPlayer();
                nextAttackTime = Time.time + attackCooldown;
            }
            else if (anim != null && !isAttacking)
            {
                // เล่นท่าเดินสแตนด์บายระหว่างรอตีรอบถัดไป
                anim.Play("Walk");
            }
        }

        // Jump logic if player is significantly higher AND enemy is close enough
        if (isGrounded && distance <= maxJumpDistance && player.position.y > transform.position.y + 1f && Time.time >= nextJumpTime)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            nextJumpTime = Time.time + jumpCooldown;
        }
    }

    void AttackPlayer()
    {
        // เล่นอนิเมชั่น Atk
        if (anim != null)
        {
            anim.Play("Atk");
            isAttacking = true;
            
            // หน่วงเวลา 0.5 วินาทีเพื่อให้ท่าโจมตีเล่นจนจบ แล้วค่อยอนุญาตให้กลับไปเดินได้
            Invoke("ResetAttackState", 0.75f);
        }

        CharacterStats pStats = player.GetComponent<CharacterStats>();
        if (pStats != null)
        {
            // Pass enemy position for knockback
            pStats.TakeDamage(damage, transform.position);
        }
    }

    void ResetAttackState()
    {
        isAttacking = false;
    }
}
