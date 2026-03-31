using UnityEngine;

public class EmoteWheelController : MonoBehaviour
{
    [Header("Refs")]
    public GameObject wheelPanel;         // Panel วงแอ็คชั่น (UI)
    public PlayerController player;       // ลาก PlayerController
    public Animator anim;                 // Animator ของ Player

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.T;

    private bool isOpen;

    void Start()
    {
        if (wheelPanel) wheelPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (isOpen) Close();
            else Open();
        }

        // ปิดด้วย Esc ได้ด้วย (ถ้าต้องการ)
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    void Open()
    {
        isOpen = true;
        if (wheelPanel)
        {
            wheelPanel.SetActive(true);
            wheelPanel.transform.SetAsLastSibling(); // กันโดน UI อื่นบัง
        }

        if (player) player.SetControl(false);

        // ให้คลิกเลือกท่าได้
        //Cursor.visible = true;
        //Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        isOpen = false;
        if (wheelPanel) wheelPanel.SetActive(false);

        if (player) player.SetControl(true);

        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Locked;
    }

    // เรียกจากปุ่มในวงแอ็คชั่น
    public void PlayEmote(string triggerName)
    {
        if (player) player.PlayEmote(triggerName); // ✅ ให้ Player จัดการ lock เอง
        else if (anim) anim.SetTrigger(triggerName);

        Close();
    }

}
