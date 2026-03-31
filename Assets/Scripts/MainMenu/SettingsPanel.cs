using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("ลาก SettingsPanel (ตัวพาเนลหลัก) มาใส่ ถ้าไม่ใส่จะใช้ gameObject ของสคริปนี้")]
    public GameObject panelRoot;

    [Header("Buttons")]
    public Button closeButton;

    [Header("Audio")]
    public Slider volumeSlider;
    public TMP_Text volumeText;

    [Header("Display")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private Resolution[] uniqueResolutions;

    // กันไม่ให้ event ยิงซ้ำตอนเราตั้งค่าในโค้ด
    private bool isInitializing;

    void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        // ผูกปุ่มปิด
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        // ผูก slider ให้ % อัปเดตแน่นอน
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);

        // ผูก dropdown events
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
    }

    void Start()
    {
        SetupResolutions();
        SetupScreenModesIfEmpty();
        LoadSettings();
    }

    // ถ้าเปิด/ปิดพาเนลหลายรอบ อยากให้มัน sync ค่าเสมอ
    void OnEnable()
    {
        // กัน Start() ยังไม่ทันทำงาน
        if (resolutionDropdown == null && screenModeDropdown == null && volumeSlider == null) return;

        // รีเฟรชเฉยๆแบบไม่ยิง event แปลกๆ
        LoadSettings();
    }

    void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        // ทำ list แบบ unique (กันซ้ำ width/height)
        var list = new List<Resolution>();
        var seen = new HashSet<string>();

        foreach (var r in Screen.resolutions)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key)) list.Add(r);
        }

        uniqueResolutions = list.ToArray();

        resolutionDropdown.ClearOptions();
        var options = new List<string>();

        int currentIndex = 0;
        int currW = Screen.width;
        int currH = Screen.height;

        for (int i = 0; i < uniqueResolutions.Length; i++)
        {
            var r = uniqueResolutions[i];
            options.Add($"{r.width} x {r.height}");

            if (r.width == currW && r.height == currH)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);

        // ตั้งค่าเริ่มต้นแบบไม่ยิง event
        isInitializing = true;
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
        isInitializing = false;
    }

    void SetupScreenModesIfEmpty()
    {
        if (screenModeDropdown == null) return;

        // ถ้ายังไม่มี options ใน Inspector ให้เติมให้
        if (screenModeDropdown.options == null || screenModeDropdown.options.Count == 0)
        {
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(new List<string>
            {
                "Exclusive Fullscreen",
                "Windowed",
                "Borderless (Fullscreen Window)"
            });
            screenModeDropdown.RefreshShownValue();
        }
    }

    //  Volume
    public void SetVolume(float value)
    {
        if (isInitializing) return;

        AudioListener.volume = value;

        if (volumeText != null)
            volumeText.text = $"{Mathf.RoundToInt(value * 100f)}%";

        PlayerPrefs.SetFloat("Volume", value);
        PlayerPrefs.Save();
    }

    // Resolution
    public void SetResolution(int index)
    {
        if (isInitializing) return;
        if (uniqueResolutions == null || uniqueResolutions.Length == 0) return;

        index = Mathf.Clamp(index, 0, uniqueResolutions.Length - 1);
        var r = uniqueResolutions[index];

        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }

    // Screen Mode
    public void SetScreenMode(int modeIndex)
    {
        if (isInitializing) return;

        FullScreenMode mode = FullScreenMode.FullScreenWindow;

        switch (modeIndex)
        {
            case 0: mode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: mode = FullScreenMode.Windowed; break;
            case 2: mode = FullScreenMode.FullScreenWindow; break;
        }

        Screen.fullScreenMode = mode;

        PlayerPrefs.SetInt("ScreenMode", modeIndex);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        isInitializing = true;

        // Volume
        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.value = savedVolume;

            AudioListener.volume = savedVolume;
            if (volumeText != null)
                volumeText.text = $"{Mathf.RoundToInt(savedVolume * 100f)}%";
        }

        // Resolution
        if (resolutionDropdown != null && uniqueResolutions != null && uniqueResolutions.Length > 0)
        {
            int savedRes = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
            savedRes = Mathf.Clamp(savedRes, 0, uniqueResolutions.Length - 1);
            resolutionDropdown.value = savedRes;
            resolutionDropdown.RefreshShownValue();
        }

        // Screen Mode
        if (screenModeDropdown != null)
        {
            int savedMode = PlayerPrefs.GetInt("ScreenMode", 2); // ค่า default แนะนำให้เป็น Borderless
            savedMode = Mathf.Clamp(savedMode, 0, 2);
            screenModeDropdown.value = savedMode;
            screenModeDropdown.RefreshShownValue();

            // Apply mode ให้ตรงตอนโหลด
            FullScreenMode mode = FullScreenMode.FullScreenWindow;
            if (savedMode == 0) mode = FullScreenMode.ExclusiveFullScreen;
            else if (savedMode == 1) mode = FullScreenMode.Windowed;
            else mode = FullScreenMode.FullScreenWindow;

            Screen.fullScreenMode = mode;
        }

        isInitializing = false;
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        else gameObject.SetActive(false);
    }

    //เปิดพาเนลจากปุ่ม Settings
    public void Open()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        else gameObject.SetActive(true);

        LoadSettings();
    }
}
