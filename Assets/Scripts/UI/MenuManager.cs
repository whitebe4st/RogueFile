using UnityEngine;
using System.Collections.Generic;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance { get; private set; }

    [Header("UI Panels")]
    [Tooltip("Assign your UI Panels here (Investigation Board, Inventory, etc.)")]
    public List<GameObject> allPanels = new List<GameObject>();

    [Header("Behavior")]
    public bool closeOthersOnOpen = true;
    public GameObject defaultPanel; // Optional: keep for direct OpenPanel() calls from code


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Key handling is intentionally NOT here.
    // - Tab/I shortcuts  →  GameHUD.cs
    // - Escape priority  →  EscapePriorityManager.cs

    /// <summary>
    /// Toggles a specific panel. If opening, can optionally close all others.
    /// </summary>
    public void TogglePanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        bool isCurrentlyActive = targetPanel.activeSelf;

        if (!isCurrentlyActive && closeOthersOnOpen)
        {
            CloseAllPanels();
        }

        targetPanel.SetActive(!isCurrentlyActive);
        
        // Handle cursor lock/unlock if needed
        UpdateCursorState();
    }

    public void OpenPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;
        if (closeOthersOnOpen) CloseAllPanels();
        targetPanel.SetActive(true);
        UpdateCursorState();
    }

    public void CloseAllPanels()
    {
        foreach (var p in allPanels)
        {
            if (p != null) p.SetActive(false);
        }
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        // Simple logic: if any panel is open, show cursor. If all closed, lock it.
        bool anyOpen = false;
        foreach (var p in allPanels)
        {
            if (p != null && p.activeInHierarchy)
            {
                anyOpen = true;
                break;
            }
        }

        if (anyOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Optional: return to game lock state
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
        }
    }
}
