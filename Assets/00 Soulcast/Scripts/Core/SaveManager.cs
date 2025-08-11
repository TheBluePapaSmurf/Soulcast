using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SaveManager for pure ID-based procedural rune system
/// Handles save/load with proper loading order and migration from legacy saves
/// </summary>
public class SaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // 5 minutes

    [Header("File Settings")]
    public string saveFileName = "PlayerSave.es3";

    public const string SAVE_FILE = "PlayerSave.es3"; // Static reference for other scripts
    public const string CURRENT_SAVE_VERSION = "3.0"; // Updated for pure ID-based system

    private float autoSaveTimer = 0f;

    // Events
    public static event Action OnSaveStarted;
    public static event Action OnSaveCompleted;
    public static event Action OnLoadStarted;
    public static event Action OnLoadCompleted;
    public static event Action<string> OnMigrationStarted;
    public static event Action<string> OnMigrationCompleted;

    public static SaveManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("💾 SaveManager initialized for pure ID-based rune system");
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

    // ========== ENHANCED SAVE SYSTEM ==========

    /// <summary>
    /// Save all game data with proper ID-based rune system
    /// </summary>
    public void SaveGame()
    {
        try
        {
            OnSaveStarted?.Invoke();

            Debug.Log("💾 Starting game save with pure ID-based system...");

            // Save system metadata first
            SaveMetadata();

            // Save all managers in proper order
            SaveAllManagers();

            OnSaveCompleted?.Invoke();
            Debug.Log("✅ Game saved successfully with pure ID-based rune system!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to save game: {e.Message}");
        }
    }

    /// <summary>
    /// Save all manager data
    /// </summary>
    private void SaveAllManagers()
    {
        // Save currency first (no dependencies)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.SaveCurrency();
            Debug.Log("💰 Currency saved");
        }

        // Save procedural rune collection
        if (RuneCollectionManager.Instance != null)
        {
            RuneCollectionManager.Instance.SaveRuneCollection();
            Debug.Log("💎 Procedural rune collection saved");
        }

        // Save monster collection (depends on runes for equipped references)
        if (MonsterCollectionManager.Instance != null)
        {
            MonsterCollectionManager.Instance.SaveMonsterCollection();
            Debug.Log("🐉 Monster collection saved");
        }

        // Save battle progression
        if (BattleProgressionManager.Instance != null)
        {
            BattleProgressionManager.Instance.SaveProgression();
            Debug.Log("⚔️ Battle progression saved");
        }

        // Save other systems (check if they exist and have save methods)
        SaveOptionalSystems();
    }

    /// <summary>
    /// Save optional systems that may or may not exist
    /// </summary>
    private void SaveOptionalSystems()
    {
        // Try to save player inventory if it exists and has save methods
        try
        {
            var playerInventoryType = System.Type.GetType("PlayerInventory");
            if (playerInventoryType != null)
            {
                var instance = playerInventoryType.GetProperty("Instance")?.GetValue(null);
                if (instance != null)
                {
                    var saveMethod = playerInventoryType.GetMethod("SaveInventory");
                    if (saveMethod != null)
                    {
                        saveMethod.Invoke(instance, null);
                        Debug.Log("🎒 Player inventory saved");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Could not save player inventory: {e.Message}");
        }
    }

    // ========== ENHANCED LOAD SYSTEM ==========

    /// <summary>
    /// Load all game data with proper loading order for ID-based system
    /// </summary>
    public void LoadGame()
    {
        try
        {
            OnLoadStarted?.Invoke();

            Debug.Log("📂 Starting game load with pure ID-based system...");

            if (!ES3.FileExists(SAVE_FILE))
            {
                Debug.Log("📝 No save file found - initializing new game");
                InitializeNewGame();
                return;
            }

            // Check save version and migrate if needed
            string saveVersion = ES3.Load("SaveVersion", SAVE_FILE, "1.0");
            if (saveVersion != CURRENT_SAVE_VERSION)
            {
                Debug.Log($"🔄 Migrating save from version {saveVersion} to {CURRENT_SAVE_VERSION}...");
                MigrateSaveFile(saveVersion);
                return;
            }

            // Load metadata
            LoadMetadata();

            // Load all managers in proper order
            LoadAllManagersInOrder();

            OnLoadCompleted?.Invoke();
            Debug.Log("✅ Game loaded successfully with pure ID-based rune system!");
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to load game: {e.Message}");
            Debug.LogWarning("🔧 Falling back to new game initialization");
            InitializeNewGame();
        }
    }

    /// <summary>
    /// Load all managers in proper dependency order
    /// </summary>
    private void LoadAllManagersInOrder()
    {
        // STEP 1: Load currency first (no dependencies)
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.LoadCurrency();
            Debug.Log("💰 Currency loaded");
        }

        // STEP 2: Load procedural rune collection (must be loaded before monsters)
        if (RuneCollectionManager.Instance != null)
        {
            RuneCollectionManager.Instance.LoadRuneCollection();

            int runeCount = RuneCollectionManager.Instance.GetRuneCount();
            Debug.Log($"💎 Procedural rune collection loaded - {runeCount} runes available");

            if (runeCount == 0)
            {
                Debug.LogWarning("⚠️ No procedural runes loaded - monsters may lose equipped runes");
            }
        }
        else
        {
            Debug.LogError("❌ RuneCollectionManager.Instance is null!");
        }

        // STEP 3: Load monster collection (depends on runes being loaded first)
        if (MonsterCollectionManager.Instance != null)
        {
            MonsterCollectionManager.Instance.LoadMonsterCollection();
            Debug.Log("🐉 Monster collection loaded");
        }

        // STEP 4: Load battle progression
        if (BattleProgressionManager.Instance != null)
        {
            BattleProgressionManager.Instance.LoadProgression();
            Debug.Log("⚔️ Battle progression loaded");
        }

        // STEP 5: Load other optional systems
        LoadOptionalSystems();
    }

    /// <summary>
    /// Load optional systems that may or may not exist
    /// </summary>
    private void LoadOptionalSystems()
    {
        // Try to load player inventory if it exists and has load methods
        try
        {
            var playerInventoryType = System.Type.GetType("PlayerInventory");
            if (playerInventoryType != null)
            {
                var instance = playerInventoryType.GetProperty("Instance")?.GetValue(null);
                if (instance != null)
                {
                    var loadMethod = playerInventoryType.GetMethod("LoadInventory");
                    if (loadMethod != null)
                    {
                        loadMethod.Invoke(instance, null);
                        Debug.Log("🎒 Player inventory loaded");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚠️ Could not load player inventory: {e.Message}");
        }
    }

    // ========== MIGRATION SYSTEM ==========

    /// <summary>
    /// Migrate save file from older versions to current pure ID-based system
    /// </summary>
    private void MigrateSaveFile(string oldVersion)
    {
        try
        {
            OnMigrationStarted?.Invoke($"Migrating from version {oldVersion}");

            Debug.Log($"🔄 Starting migration from save version {oldVersion} to {CURRENT_SAVE_VERSION}");

            // Migrate based on old version
            switch (oldVersion)
            {
                case "1.0":
                    MigrateFromV1ToV3();
                    break;
                case "2.0":
                    MigrateFromV2ToV3();
                    break;
                default:
                    Debug.LogWarning($"⚠️ Unknown save version {oldVersion}, starting fresh");
                    DeleteSaveFile();
                    InitializeNewGame();
                    return;
            }

            // After migration, set new version and save
            ES3.Save("SaveVersion", CURRENT_SAVE_VERSION, SAVE_FILE);

            Debug.Log($"✅ Migration completed! Save upgraded to version {CURRENT_SAVE_VERSION}");
            OnMigrationCompleted?.Invoke($"Migration to version {CURRENT_SAVE_VERSION} completed");

            // Now load the migrated save
            LoadGame();
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Migration failed: {e.Message}");
            Debug.LogWarning("🔧 Starting fresh due to migration failure");
            DeleteSaveFile();
            InitializeNewGame();
        }
    }

    /// <summary>
    /// Migrate from version 1.0 (legacy mixed system) to 3.0 (pure ID-based)
    /// </summary>
    private void MigrateFromV1ToV3()
    {
        Debug.Log("🔄 Migrating from V1.0 - removing all legacy ScriptableObject runes");

        // Remove legacy rune data completely
        if (ES3.KeyExists("RuneCollection", SAVE_FILE))
        {
            ES3.DeleteKey("RuneCollection", SAVE_FILE);
            Debug.Log("🗑️ Removed legacy RuneCollection data");
        }

        // Clear any old equipped rune names in monster data
        MigrateMonsterEquippedRunes();

        // Keep currency and other data as-is
        Debug.Log("✅ V1.0 migration completed - legacy rune data removed");
    }

    /// <summary>
    /// Migrate from version 2.0 (hybrid system) to 3.0 (pure ID-based)
    /// </summary>
    private void MigrateFromV2ToV3()
    {
        Debug.Log("🔄 Migrating from V2.0 - converting to pure ID-based system");

        // Ensure RuneCollectionManager is available for migration
        if (RuneCollectionManager.Instance != null)
        {
            // Load existing procedural runes
            RuneCollectionManager.Instance.LoadRuneCollection();
            Debug.Log($"💎 Loaded {RuneCollectionManager.Instance.GetRuneCount()} procedural runes for migration");
        }

        // Migrate monster equipped runes to ID-only system
        MigrateMonsterEquippedRunes();

        // Remove any legacy rune keys that might exist
        string[] legacyKeys = { "RuneCollection", "LegacyRunes", "ScriptableObjectRunes" };
        foreach (string key in legacyKeys)
        {
            if (ES3.KeyExists(key, SAVE_FILE))
            {
                ES3.DeleteKey(key, SAVE_FILE);
                Debug.Log($"🗑️ Removed legacy key: {key}");
            }
        }

        Debug.Log("✅ V2.0 migration completed - converted to pure ID-based system");
    }

    /// <summary>
    /// Migrate monster equipped runes to pure ID system
    /// </summary>
    private void MigrateMonsterEquippedRunes()
    {
        try
        {
            // Load monster collection for migration
            if (ES3.KeyExists("MonsterCollection", SAVE_FILE))
            {
                var monsters = ES3.Load<List<CollectedMonster>>("MonsterCollection", SAVE_FILE, new List<CollectedMonster>());

                int migratedMonsters = 0;
                foreach (var monster in monsters)
                {
                    if (monster != null)
                    {
                        // Suppress obsolete warning for migration code only
#pragma warning disable CS0618 // Type or member is obsolete

                        // Clear any legacy equipped rune names (migration only)
                        if (monster.equippedRuneNames != null)
                        {
                            for (int i = 0; i < monster.equippedRuneNames.Length; i++)
                            {
                                monster.equippedRuneNames[i] = "";
                            }
                        }

#pragma warning restore CS0618 // Type or member is obsolete

                        // Ensure equipped rune IDs are initialized
                        if (monster.equippedRuneIDs == null || monster.equippedRuneIDs.Length != 6)
                        {
                            monster.equippedRuneIDs = new string[6];
                            for (int i = 0; i < 6; i++)
                            {
                                monster.equippedRuneIDs[i] = "";
                            }
                        }

                        // Clear rune slots to prevent legacy references
                        if (monster.runeSlots != null)
                        {
                            for (int i = 0; i < monster.runeSlots.Length; i++)
                            {
                                if (monster.runeSlots[i] != null)
                                {
                                    monster.runeSlots[i].equippedRune = null;
                                }
                            }
                        }

                        migratedMonsters++;
                    }
                }

                // Save migrated monster data
                ES3.Save("MonsterCollection", monsters, SAVE_FILE);
                Debug.Log($"🔄 Migrated {migratedMonsters} monsters to pure ID-based rune system");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to migrate monster rune data: {e.Message}");
        }
    }

    // ========== METADATA SYSTEM ==========

    private void SaveMetadata()
    {
        ES3.Save("SaveVersion", CURRENT_SAVE_VERSION, SAVE_FILE);
        ES3.Save("LastSaveTime", DateTime.Now.ToBinary(), SAVE_FILE);
        ES3.Save("TotalPlayTime", Time.time, SAVE_FILE);
        ES3.Save("RuneSystemType", "Pure_ID_Based", SAVE_FILE);

        // Save migration history
        var migrationHistory = ES3.Load("MigrationHistory", SAVE_FILE, new List<string>());
        if (!migrationHistory.Contains(CURRENT_SAVE_VERSION))
        {
            migrationHistory.Add($"{CURRENT_SAVE_VERSION}_{DateTime.Now:yyyy-MM-dd}");
            ES3.Save("MigrationHistory", migrationHistory, SAVE_FILE);
        }
    }

    private void LoadMetadata()
    {
        long lastSaveTime = ES3.Load("LastSaveTime", SAVE_FILE, DateTime.Now.ToBinary());
        float totalPlayTime = ES3.Load("TotalPlayTime", SAVE_FILE, 0f);
        string runeSystemType = ES3.Load("RuneSystemType", SAVE_FILE, "Unknown");

        Debug.Log($"📊 Save Info - Version: {CURRENT_SAVE_VERSION}, Rune System: {runeSystemType}");
        Debug.Log($"📊 Last saved: {DateTime.FromBinary(lastSaveTime):yyyy-MM-dd HH:mm:ss}");
        Debug.Log($"⏱️ Total play time: {totalPlayTime / 3600f:F1} hours");

        // Debug migration history
        var migrationHistory = ES3.Load("MigrationHistory", SAVE_FILE, new List<string>());
        if (migrationHistory.Count > 0)
        {
            Debug.Log($"🔄 Migration History: {string.Join(", ", migrationHistory)}");
        }
    }

    // ========== AUTO SAVE SYSTEM ==========

    public void AutoSave()
    {
        if (IsGameReadyForSave())
        {
            SaveGame();
            Debug.Log("🔄 Auto-saved game with pure ID-based rune system");
        }
        else
        {
            Debug.LogWarning("⚠️ Game not ready for auto-save - some managers missing");
        }
    }

    private bool IsGameReadyForSave()
    {
        bool ready = CurrencyManager.Instance != null &&
                    MonsterCollectionManager.Instance != null &&
                    RuneCollectionManager.Instance != null;

        if (!ready)
        {
            Debug.Log("🔍 Save readiness check:");
            Debug.Log($"   CurrencyManager: {(CurrencyManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"   MonsterCollectionManager: {(MonsterCollectionManager.Instance != null ? "✅" : "❌")}");
            Debug.Log($"   RuneCollectionManager: {(RuneCollectionManager.Instance != null ? "✅" : "❌")}");
        }

        return ready;
    }

    // ========== INITIALIZATION ==========

    private void InitializeNewGame()
    {
        Debug.Log("🆕 Initializing new game with pure ID-based rune system");

        // Save initial metadata
        SaveMetadata();

        // Initialize all managers with default values
        InitializeAllManagers();

        // Trigger events for UI updates
        OnLoadCompleted?.Invoke();

        Debug.Log("✅ New game initialized successfully!");
    }

    private void InitializeAllManagers()
    {
        // Initialize each manager if they exist
        if (CurrencyManager.Instance != null)
        {
            // Currency manager will initialize with default values
            Debug.Log("💰 CurrencyManager initialized with defaults");
        }

        if (RuneCollectionManager.Instance != null)
        {
            // Rune collection starts empty
            Debug.Log("💎 RuneCollectionManager initialized (empty collection)");
        }

        if (MonsterCollectionManager.Instance != null)
        {
            // Monster collection starts empty
            Debug.Log("🐉 MonsterCollectionManager initialized (empty collection)");
        }

        if (BattleProgressionManager.Instance != null)
        {
            // Battle progression starts at beginning
            Debug.Log("⚔️ BattleProgressionManager initialized with defaults");
        }
    }

    // ========== UTILITY METHODS ==========

    public void DeleteSaveFile()
    {
        if (ES3.FileExists(SAVE_FILE))
        {
            ES3.DeleteFile(SAVE_FILE);
            Debug.Log("🗑️ Save file deleted - starting fresh!");
            InitializeNewGame();
        }
        else
        {
            Debug.Log("📝 No save file to delete");
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
            string runeSystemType = ES3.Load("RuneSystemType", SAVE_FILE, "Unknown");

            return $"Version: {saveVersion}\nRune System: {runeSystemType}\nLast saved: {DateTime.FromBinary(lastSaveTime):yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading save file info: {e.Message}");
            return "Save file corrupted";
        }
    }

    public SaveFileStats GetSaveFileStats()
    {
        if (!HasSaveFile()) return null;

        try
        {
            var stats = new SaveFileStats
            {
                saveVersion = ES3.Load("SaveVersion", SAVE_FILE, "Unknown"),
                runeSystemType = ES3.Load("RuneSystemType", SAVE_FILE, "Unknown"),
                lastSaveTime = DateTime.FromBinary(ES3.Load("LastSaveTime", SAVE_FILE, DateTime.Now.ToBinary())),
                totalPlayTime = ES3.Load("TotalPlayTime", SAVE_FILE, 0f),
                migrationHistory = ES3.Load("MigrationHistory", SAVE_FILE, new List<string>())
            };

            return stats;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading save file stats: {e.Message}");
            return null;
        }
    }

    // ========== DEBUG AND VALIDATION ==========

    [ContextMenu("Validate Save File")]
    public void ValidateSaveFile()
    {
        if (!HasSaveFile())
        {
            Debug.Log("📝 No save file to validate");
            return;
        }

        Debug.Log("🔍 Validating save file integrity...");

        try
        {
            var stats = GetSaveFileStats();
            if (stats != null)
            {
                Debug.Log($"✅ Save file validation successful:");
                Debug.Log($"   Version: {stats.saveVersion}");
                Debug.Log($"   Rune System: {stats.runeSystemType}");
                Debug.Log($"   Last Save: {stats.lastSaveTime:yyyy-MM-dd HH:mm:ss}");
                Debug.Log($"   Play Time: {stats.totalPlayTime / 3600f:F1}h");
                Debug.Log($"   Migrations: {stats.migrationHistory.Count}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Save file validation failed: {e.Message}");
        }
    }

    [ContextMenu("Force Migration to V3.0")]
    public void ForceMigrationToV3()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("⚠️ No save file to migrate");
            return;
        }

        Debug.Log("🔄 Forcing migration to V3.0 (Pure ID-based system)");

        // Set old version to trigger migration
        ES3.Save("SaveVersion", "2.0", SAVE_FILE);

        // Reload to trigger migration
        LoadGame();
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

    [ContextMenu("Initialize New Game")]
    public void ManualInitializeNewGame() => InitializeNewGame();
}

// ========== SUPPORTING CLASSES ==========

/// <summary>
/// Statistics about the save file
/// </summary>
[System.Serializable]
public class SaveFileStats
{
    public string saveVersion;
    public string runeSystemType;
    public DateTime lastSaveTime;
    public float totalPlayTime;
    public List<string> migrationHistory;
}
