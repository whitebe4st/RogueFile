using UnityEngine;

public class PlayerControllerCombat : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Ground Check")]
    public Transform groundCheck; 
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private Rigidbody rb;
    private CapsuleCollider playerCollider;
    private Animator anim;
    private string currentAnimState;
    private SpriteRenderer sr;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>(); // หา Animator ในตัวผู้เล่น
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    // ฟังก์ชันช่วยเปลี่ยนท่าทาง (ป้องกันไม่ให้อนิเมชั่นเริ่มใหม่ทุกเฟรมถ้ายืนหรือเดินท่าเดิมอยู่)
    public void ChangeAnimationState(string newState)
    {
        // ป้องกันไม่ให้เปลี่ยนท่าถ้าตอนนี้กำลังเล่นท่าโจมตีอยู่ (ท่า Punch)
        if (anim != null && (anim.GetCurrentAnimatorStateInfo(0).IsName("Punch") || anim.GetCurrentAnimatorStateInfo(0).IsName("Punch 1")))
        {
            return; 
        }

        if (currentAnimState == newState) return;

        if (anim != null) 
        {
            anim.Play(newState);
            currentAnimState = newState;
        }
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, checkRadius, groundLayer);

        Move();
        Jump(); 
    }

    private float idleCooldownTimer = 0f;

    void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        rb.linearVelocity = new Vector3(moveX * moveSpeed, rb.linearVelocity.y, moveZ * moveSpeed);

        // Preserve current absolute scale but flip the X direction
        Vector3 currentScale = transform.localScale;
        if (moveX > 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }
        else if (moveX < 0)
        {
            transform.localScale = new Vector3(-Mathf.Abs(currentScale.x), currentScale.y, currentScale.z);
        }

        // จัดการเรื่องการเล่นอนิเมชั่นเดินและยืน
        // ป้องกันไม่ให้อนิเมชั่นเดินทำงานทับท่ากระโดด (เช็คว่าไม่ได้กำลังพุ่งขึ้นไป)
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            if (moveX != 0 || moveZ != 0) 
            {
                ChangeAnimationState("walk_right"); // ใช้คลิป walk_right เพียงอย่างเดียว ตัวสเกลจะกลับด้านซ้ายขวาให้เอง
                idleCooldownTimer = 0f; // รีเซ็ตเวลาเมื่อมีการขยับ
            }
            else // หยุดอยู่กับที่
            {
                idleCooldownTimer += Time.deltaTime;
                
                if (idleCooldownTimer >= 3f)
                {
                    ChangeAnimationState("idle"); // เล่นท่า idle (หันตามค่าสเกล X ล่าสุดอัตโนมัติ)
                }
                else
                {
                    // แสดงท่า Stand ทันทีที่หยุดยืน (รอครบ 3 วิถึงจะเปลี่ยนเป็น idle)
                    ChangeAnimationState("Stand"); 
                }
            }
        }
    }

    void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            ChangeAnimationState("Jump");
            idleCooldownTimer = 0f;
        }
        else if (!isGrounded)
        {
            // ถ้ากำลังตกลงมา ให้เปลี่ยนเป็นท่าตก (ใน Animator ใช้ชื่อ fall ตัวพิมพ์เล็ก)
            if (rb.linearVelocity.y < -0.1f)
            {
                ChangeAnimationState("fall");
            }
        }
    }

    void LateUpdate()
    {
        if (sr != null && anim != null)
        {
            // ตรวจสอบสถานะการเล่นอนิเมชั่นปัจจุบันจริงๆ จาก Animator
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            bool isPunching = stateInfo.IsName("Punch") || stateInfo.IsName("Punch 1");
            
            // ภาพสไปรต์ของท่า Jump, fall, Punch, idle รวมถึงท่า Stand น่าจะถูกวาดมาหันซ้ายเป็นต้นฉบับ (ตรงข้ามกับท่า walk ที่หันขวา)
            // พอสเกลพลิกด้าน มันเลยสลับผิดทาง เราจึงใช้ flipX ช่วยกลับด้านภาพเฉพาะตอนเล่นท่าเหล่านี้
            if (currentAnimState == "Jump" || currentAnimState == "fall" || currentAnimState == "idle" || currentAnimState == "Stand" || isPunching || stateInfo.IsName("idle") || stateInfo.IsName("Stand"))
            {
                sr.flipX = true;
            }
            else
            {
                sr.flipX = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}
