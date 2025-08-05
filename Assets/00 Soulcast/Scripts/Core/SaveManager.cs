// Core/SaveManager.cs
using UnityEngine;
using System;

public class SaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5 minutes

    [Header("File Settings")]
    public string saveFileName = "PlayerSave.es3";

    public const string SAVE_FILE = "PlayerSave.es3"; // Static reference for other scripts

    private float autoSaveTimer = 0f;

    // Events
    public static event Action OnSaveStarted;
    public static event Action OnSaveCompleted;
    public static event Action OnLoadStarted;
    public static event Action OnLoadCompleted;

    public static SaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("💾 SaveManager initialized and made persistent");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Auto-load game on start
        LoadGame();
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

    // ========== SAVE SYSTEM ==========

    public void SaveGame()
    {
        try
        {
            OnSaveStarted?.Invoke();

            Debug.Log("💾 Starting game save...");

            // Save all managers
            CurrencyManager.Instance?.SaveCurrency();
            MonsterCollectionManager.Instance?.SaveMonsterCollection();
            RuneCollectionManager.Instance?.SaveRuneCollection();

            // Save metadata
            SaveMetadata();

            OnSaveCompleted?.Invoke();
            Debug.Log("✅ Game saved successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        try
        {
            OnLoadStarted?.Invoke();

            Debug.Log("📂 Starting game load...");

            if (!ES3.FileExists(SAVE_FILE))
            {
                Debug.Log("📝 No save file found - initializing with default values");
                InitializeNewGame();
                return;
            }

            // Check save version for migration
            string saveVersion = ES3.Load("SaveVersion", SAVE_FILE, "1.0");
            if (saveVersion != "2.0")
            {
                Debug.Log("🔄 Migrating old save format...");
                MigrateOldSave();
                return;
            }

            // Load all managers
            CurrencyManager.Instance?.LoadCurrency();
            MonsterCollectionManager.Instance?.LoadMonsterCollection();
            RuneCollectionManager.Instance?.LoadRuneCollection();

            LoadMetadata();

            OnLoadCompleted?.Invoke();
            Debug.Log("✅ Game loaded successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load game: {e.Message}");
            InitializeNewGame();
        }
    }

    private void SaveMetadata()
    {
        ES3.Save("SaveVersion", "2.0", SAVE_FILE);
        ES3.Save("LastSaveTime", DateTime.Now.ToBinary(), SAVE_FILE);
        ES3.Save("TotalPlayTime", Time.time, SAVE_FILE);
    }

    private void LoadMetadata()
    {
        long lastSaveTime = ES3.Load("LastSaveTime", SAVE_FILE, DateTime.Now.ToBinary());
        float totalPlayTime = ES3.Load("TotalPlayTime", SAVE_FILE, 0f);

        Debug.Log($"📊 Last saved: {DateTime.FromBinary(lastSaveTime):yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"⏱️ Total play time: {totalPlayTime / 3600f:F1} hours");
    }

    // ========== AUTO SAVE ==========

    public void AutoSave()
    {
        if (IsGameReadyForSave())
        {
            SaveGame();
            Debug.Log("🔄 Auto-saved game");
        }
    }

    private bool IsGameReadyForSave()
    {
        return CurrencyManager.Instance != null &&
               MonsterCollectionManager.Instance != null &&
               RuneCollectionManager.Instance != null;
    }

    // ========== INITIALIZATION ==========

    private void InitializeNewGame()
    {
        Debug.Log("🆕 Initializing new game with default values");

        // Trigger events for UI updates
        OnLoadCompleted?.Invoke();
    }

    private void MigrateOldSave()
    {
        Debug.Log("🔄 Migration from old save format completed");

        // Set new version and save
        ES3.Save("SaveVersion", "2.0", SAVE_FILE);
        SaveGame();
    }

    // ========== UTILITY ==========

    public void DeleteSaveFile()
    {
        if (ES3.FileExists(SAVE_FILE))
        {
            ES3.DeleteFile(SAVE_FILE);
            Debug.Log("🗑️ Save file deleted!");
            InitializeNewGame();
        }
    }

    public bool HasSaveFile()
    {
        return ES3.FileExists(SAVE_FILE);
    }

    public string GetSaveFileInfo()
    {
        if (!HasSaveFile()) return "No save file found";

        try
        {
            long lastSaveTime = ES3.Load("LastSaveTime", SAVE_FILE, 0);
            string saveVersion = ES3.Load("SaveVersion", SAVE_FILE, "Unknown");
            return $"Version: {saveVersion}, Last saved: {DateTime.FromBinary(lastSaveTime):yyyy-MM-dd HH:mm:ss}";
        }
        catch
        {
            return "Save file corrupted";
        }
    }

    // ========== EVENT HANDLERS ==========

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveEnabled)
        {
            AutoSave();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && autoSaveEnabled)
        {
            AutoSave();
        }
    }

    // ========== MANUAL CONTROLS ==========

    [ContextMenu("Save Game")]
    public void ManualSave() => SaveGame();

    [ContextMenu("Load Game")]
    public void ManualLoad() => LoadGame();

    [ContextMenu("Delete Save")]
    public void ManualDeleteSave() => DeleteSaveFile();
}
