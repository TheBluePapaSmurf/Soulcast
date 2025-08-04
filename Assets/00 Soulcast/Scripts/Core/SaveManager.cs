// ✅ NEW: SaveManager.cs - Helper script voor Easy Save 3 management
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [Header("Auto Save Settings")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5 minutes

    private float autoSaveTimer = 0f;

    public static SaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (autoSaveEnabled)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                AutoSave();
                autoSaveTimer = 0f;
            }
        }
    }

    public void AutoSave()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.SaveGame();
            Debug.Log("🔄 Auto-saved game");
        }
    }

    public void ManualSave()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.SaveGame();
            Debug.Log("💾 Manual save completed");
        }
    }

    public void LoadGame()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.LoadGame();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveEnabled)
        {
            AutoSave(); // Save when app is paused
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && autoSaveEnabled)
        {
            AutoSave(); // Save when app loses focus
        }
    }

    // For UI buttons
    [ContextMenu("Save Game")]
    public void SaveGameButton()
    {
        ManualSave();
    }

    [ContextMenu("Load Game")]
    public void LoadGameButton()
    {
        LoadGame();
    }

    [ContextMenu("Delete Save")]
    public void DeleteSaveButton()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.DeleteSaveFile();
        }
    }
}
