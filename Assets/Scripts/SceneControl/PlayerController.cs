using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public static PlayerController Instance { get; private set; }
    public float groundCheckDistance = 0.6f;
    public float groundCheckOffset = -2.5f; // Matches your character pivot

    [Header("Jump")]
    public float jumpForce = 15f;
    public float jumpCooldown = 0.2f; // Minimum time between jumps
    public float jumpStartupDelay = 0.10f; // ✅ เวลาย่อก่อนโดด (Startup frame)
    public bool CanMove = true;
    public bool jumpOnAnyLayer = true;
    public LayerMask groundLayer;
    public LayerMask universalIgnoreMask; // Layers to ignore when jumpOnAnyLayer is true

    [Header("References")]
    public Rigidbody rb;
    public Animator anim;

    [Header("Control Lock")]
    public bool canControl = true;

    [Header("Animator Params")]
    public string yVelocityParam = "YVelocity";
    public string groundedParam = "Grounded";
    public string airborneParam = "Airborne";
    public string jumpStartTrigger = "Jump";
    public float yVelDeadZone = 0.1f;

    private int yVelHash;
    private int groundedHash;
    private int airborneHash;

    [Header("Stair Climbing")]
    public float maxStepHeight = 0.5f;
    public float stepSmoothness = 0.2f;
    public float stepDetectionDistance = 0.6f;

    [Header("Emote")]
    public bool lockMovementWhileEmote = true;
    public float emoteLockSeconds = 1.2f;

    // Ground / jump state
    private bool isGrounded;
    private float lastJumpTime;

    // ✅ กันโดดซ้ำกลางอากาศ
    [Header("Jump Anti-Double")]
    [SerializeField] private float jumpGroundLockTime = 0.15f; // หลังโดด บังคับไม่ grounded ชั่วคราว
    [SerializeField] private float groundedConfirmTime = 0.06f; // ต้อง grounded ต่อเนื่องเท่านี้ถึงจะรีเซ็ต jump
    private bool jumpConsumed = false;
    private float groundedTimer = 0f;

    // ✅ startup -> jump จริง
    private bool jumpQueued = false;
    private float jumpQueueStartTime = 0f;

    // Emote state
    private bool isEmoting;
    private Coroutine emoteRoutine;

    private System.Collections.Generic.HashSet<string> animatorParameters =
        new System.Collections.Generic.HashSet<string>();

    void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();

        yVelHash = Animator.StringToHash(yVelocityParam);
        groundedHash = Animator.StringToHash(groundedParam);
        airborneHash = Animator.StringToHash(airborneParam);

        if (anim != null)
        {
            foreach (AnimatorControllerParameter param in anim.parameters)
                animatorParameters.Add(param.name);
        }
        else
        {
            Debug.LogError("Animator component not found on the player GameObject.");
        }

        rb.freezeRotation = true;
        rb.constraints &= ~RigidbodyConstraints.FreezePositionY;
        rb.useGravity = true;

        // Default ignore mask (Ignore Raycast, Water, Player)
        if (universalIgnoreMask == 0)
        {
            universalIgnoreMask = (1 << 2) | (1 << 4) | (1 << gameObject.layer);

            // ถ้า Player อยู่ Default (0) ต้องไม่ ignore default ไม่งั้นไม่เจอพื้น
            if (gameObject.layer == 0)
            {
                Debug.LogWarning("[PlayerController] Player is on Default Layer! Removing Default from Ignore Mask so you can jump on floor.");
                universalIgnoreMask &= ~(1 << 0);
            }
        }
    }

    void Update()
    {
        CheckGrounded();

        // ---- Ground confirm (รีเซ็ต jump เมื่อแตะพื้นจริง) ----
        if (isGrounded && rb.linearVelocity.y <= 0.01f)
        {
            groundedTimer += Time.deltaTime;
            if (groundedTimer >= groundedConfirmTime)
                jumpConsumed = false;
        }
        else
        {
            groundedTimer = 0f;
        }

        // ---- ส่งค่าแนวตั้งให้ Animator ----
        if (anim != null)
        {
            float yVel = rb.linearVelocity.y;
            if (Mathf.Abs(yVel) < yVelDeadZone) yVel = 0f;

            if (animatorParameters.Contains(yVelocityParam)) anim.SetFloat(yVelHash, yVel);
            if (animatorParameters.Contains(groundedParam)) anim.SetBool(groundedHash, isGrounded);

            // ✅ Falling frame ใช้ Airborne = true เมื่อไม่ติดพื้น
            if (animatorParameters.Contains(airborneParam))
                anim.SetBool(airborneHash, !isGrounded);
        }

        // ---- Movement input ----
        float x = 0f;
        float y = 0f;

        bool allowInput = CanMove && canControl && !isEmoting;

        if (allowInput && !jumpQueued) // ระหว่าง startup ให้ไม่บังคับเพิ่ม
        {
            x = Input.GetAxis("Horizontal");
            y = Input.GetAxis("Vertical");
        }

        // ---- Jump input: กดแล้วเข้า Startup ก่อน ----
        bool jumpPressed = allowInput && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space));

        bool canStartJump = isGrounded
                            && !jumpConsumed
                            && !jumpQueued
                            && (Time.time >= lastJumpTime + jumpCooldown);

        if (jumpPressed && canStartJump)
        {
            // ✅ 1) เล่น Startup frame
            if (anim != null && animatorParameters.Contains(jumpStartTrigger))
            {
                anim.ResetTrigger(jumpStartTrigger);
                anim.SetTrigger(jumpStartTrigger);
            }

            // ✅ 2) คิวไว้ แล้วค่อยโดดจริงหลังดีเลย์
            jumpQueued = true;
            jumpQueueStartTime = Time.time;

            // หยุดความเร็วแนวนอนระหว่างย่อ (กันลื่น)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            // กันโดดซ้ำ
            jumpConsumed = true;
        }

        // ---- Execute real jump after startup delay ----
        if (jumpQueued && (Time.time - jumpQueueStartTime) >= jumpStartupDelay)
        {
            jumpQueued = false;

            // โดดจริง
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);

            isGrounded = false;
            lastJumpTime = Time.time;
        }

        // ---- Apply horizontal movement (physics normal, no snap) ----
        Vector3 moveDir = transform.right * x + transform.forward * y;
        rb.linearVelocity = new Vector3(moveDir.x * moveSpeed, rb.linearVelocity.y, moveDir.z * moveSpeed);

        // ---- Animation movement params ----
        if (anim != null)
        {
            if (animatorParameters.Contains("Direction")) anim.SetFloat("Direction", x);
            if (animatorParameters.Contains("Vertical")) anim.SetFloat("Vertical", y);
            if (animatorParameters.Contains("Speed")) anim.SetFloat("Speed", new Vector2(x, y).magnitude);
        }

        // ---- Step Climb ----
        // ทำเฉพาะตอนติดพื้น + ไม่กระโดด + ไม่ลอย
        if (isGrounded && rb.linearVelocity.y <= 0.01f && !jumpQueued)
            StepClimb();
    }

    // -------- Ground Check (Robust Self-Filtering) --------
    void CheckGrounded()
    {
        float radius = 0.3f;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        float castDistance = 10f;

        LayerMask mask = jumpOnAnyLayer ? ~universalIgnoreMask : groundLayer;

        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            radius,
            Vector3.down,
            castDistance,
            mask,
            QueryTriggerInteraction.Ignore
        );

        // Find the closest valid hit (not self)
        RaycastHit closestHit = new RaycastHit();
        bool validHitFound = false;
        float closestDist = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.distance < closestDist)
            {
                closestDist = hit.distance;
                closestHit = hit;
                validHitFound = true;
            }
        }

        bool groundedNow = validHitFound;

        if (groundedNow)
        {
            // ตัวนี้คือ "ระยะที่ยอมรับว่าแตะพื้น"
            float maxAcceptableDist = (0.5f - groundCheckOffset) + groundCheckDistance;
            groundedNow = closestHit.distance <= maxAcceptableDist;
        }

        // ✅ หลังโดด: บังคับไม่ grounded ชั่วคราวกัน double-jump
        if (Time.time - lastJumpTime < jumpGroundLockTime)
            groundedNow = false;

        isGrounded = groundedNow;
    }

    public void SetControl(bool value)
    {
        canControl = value;
    }

    // -------- Emote --------
    public void PlayEmote(string triggerName)
    {
        if (anim == null) return;

        if (emoteRoutine != null)
            StopCoroutine(emoteRoutine);

        emoteRoutine = StartCoroutine(EmoteRoutine(triggerName));
    }

    IEnumerator EmoteRoutine(string triggerName)
    {
        isEmoting = true;

        if (lockMovementWhileEmote)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        anim.ResetTrigger(triggerName);
        anim.SetTrigger(triggerName);

        yield return new WaitForSeconds(emoteLockSeconds);

        isEmoting = false;
        emoteRoutine = null;
    }

    public bool IsGrounded => isGrounded;

    [ContextMenu("Auto-Calibrate Ground Offset")]
    public void CalibrateOffset()
    {
        RaycastHit hit;
        LayerMask mask = jumpOnAnyLayer ? ~universalIgnoreMask : groundLayer;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 10f, mask))
        {
            groundCheckOffset = 0.5f - hit.distance;
            Debug.Log($"[PlayerController] Calibrated! New Ground Check Offset: {groundCheckOffset:F2}");
        }
        else
        {
            Debug.LogError("[PlayerController] Calibration failed: No ground detected below player. Stand on a floor and try again!");
        }
    }

    void StepClimb()
    {
        RaycastHit hitLower;
        RaycastHit hitUpper;

        Vector3 feetPos = transform.position + Vector3.up * groundCheckOffset;
        Vector3 lowerRayPos = feetPos + Vector3.up * 0.1f;
        Vector3 upperRayPos = feetPos + Vector3.up * maxStepHeight;

        Vector3 moveDir = rb.linearVelocity;
        moveDir.y = 0;

        if (moveDir.magnitude < 0.1f)
            moveDir = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");

        if (moveDir.magnitude < 0.1f)
            moveDir = transform.forward;

        moveDir.Normalize();

        LayerMask mask = jumpOnAnyLayer ? ~universalIgnoreMask : groundLayer;

        if (Physics.Raycast(lowerRayPos, moveDir, out hitLower, stepDetectionDistance, mask))
        {
            if (!Physics.Raycast(upperRayPos, moveDir, out hitUpper, stepDetectionDistance + 0.1f, mask))
            {
                rb.position += Vector3.up * stepSmoothness;
            }
        }
    }
}
