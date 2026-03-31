using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CutsceneController : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Sprite image;
        [TextArea(3, 8)] public string text;
        public float charsPerSecond = 40f;
        public float holdAfterDone = 0.3f; // หน่วงนิดก่อนให้กดไปต่อ
    }

    [Header("UI Refs")]
    public Image backgroundImage;
    public TMP_Text storyText;
    public GameObject continueHint; // optional

    [Header("Slides")]
    public Slide[] slides;

    [Header("Controls")]
    public KeyCode nextKey = KeyCode.E;
    public KeyCode skipKey = KeyCode.Space;

    [Header("Next Scene")]
    public string nextSceneName = "Game";

    int index = 0;
    bool isTyping = false;
    bool canContinue = false;
    Coroutine typingRoutine;

    void Start()
    {
        if (continueHint) continueHint.SetActive(false);
        ShowSlide(0);
    }

    void Update()
    {
        // กดข้ามทั้งหมดไปเข้าเกม
        if (Input.GetKeyDown(skipKey))
        {
            LoadNextScene();
            return;
        }

        // กดไปต่อ / หรือเร่งพิมพ์ให้จบทันที
        if (Input.GetKeyDown(nextKey))
        {
            if (isTyping)
            {
                // เร่ง: แสดงข้อความเต็มทันที
                FinishTypingInstant();
            }
            else if (canContinue)
            {
                NextSlide();
            }
        }
    }

    void ShowSlide(int i)
    {
        if (slides == null || slides.Length == 0) return;
        i = Mathf.Clamp(i, 0, slides.Length - 1);

        index = i;
        canContinue = false;

        if (continueHint) continueHint.SetActive(false);
        if (backgroundImage) backgroundImage.sprite = slides[i].image;

        // เริ่มพิมพ์ข้อความ
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeText(slides[i].text, slides[i].charsPerSecond, slides[i].holdAfterDone));
    }

    IEnumerator TypeText(string fullText, float cps, float holdAfterDone)
    {
        isTyping = true;
        storyText.text = "";

        cps = Mathf.Max(1f, cps);
        float delay = 1f / cps;

        for (int c = 0; c < fullText.Length; c++)
        {
            storyText.text += fullText[c];
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(holdAfterDone);

        isTyping = false;
        canContinue = true;
        if (continueHint) continueHint.SetActive(true);
    }

    void FinishTypingInstant()
    {
        if (slides == null || slides.Length == 0) return;

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        storyText.text = slides[index].text;

        isTyping = false;
        canContinue = true;
        if (continueHint) continueHint.SetActive(true);
    }

    void NextSlide()
    {
        int next = index + 1;
        if (next >= slides.Length)
        {
            LoadNextScene();
            return;
        }
        ShowSlide(next);
    }

    void LoadNextScene()
    {
        // กัน error ชื่อซีนผิด: ให้แน่ใจว่า add ใน Build Settings แล้ว
        SceneManager.LoadScene(nextSceneName);
    }
}
