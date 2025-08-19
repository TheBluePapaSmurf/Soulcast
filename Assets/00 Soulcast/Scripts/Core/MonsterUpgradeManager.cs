using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterUpgradeManager : MonoBehaviour
{
    public static MonsterUpgradeManager Instance { get; private set; }

    [Header("Upgrade Panel UI")]
    public MonsterUpgradePanel upgradePanel;

    [Header("Audio")]
    public AudioClip upgradeSuccessSound;
    public AudioClip upgradeFailedSound;
    [Range(0f, 1f)]
    public float audioVolume = 0.8f;

    private AudioSource audioSource;

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

    void Start()
    {
        SetupAudioSource();

        if (upgradePanel != null)
        {
            upgradePanel.gameObject.SetActive(false);
        }
    }

    void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = audioVolume;
    }

    /// <summary>
    /// Open the monster upgrade panel
    /// </summary>
    public void OpenUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.OpenPanel();
        }
        else
        {
            Debug.LogError("❌ MonsterUpgradePanel is not assigned!");
        }
    }

    /// <summary>
    /// Called when upgrade panel is closed
    /// </summary>
    public void CloseUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.ClosePanel();
        }

        // Notify altar that UI is closed
        var upgradeAltars = FindObjectsByType<AltarInteraction>(FindObjectsSortMode.None);
        foreach (var altar in upgradeAltars)
        {
            if (altar.altarType == AltarType.Upgrade)
            {
                altar.OnSystemUIClosed();
            }
        }
    }

    /// <summary>
    /// Get required materials for star upgrade
    /// ✨ UPDATED: Simplified requirements text
    /// </summary>
    public UpgradeRequirement GetUpgradeRequirement(int currentStarLevel)
    {
        return new UpgradeRequirement
        {
            requiredStarLevel = currentStarLevel,
            requiredCount = currentStarLevel,
            canUpgrade = currentStarLevel >= 1 && currentStarLevel <= 5
        };
    }


    /// <summary>
    /// Check if monster can be upgraded
    /// </summary>
    public bool CanUpgradeMonster(CollectedMonster monster)
    {
        if (monster == null) return false;
        if (monster.currentStarLevel >= 6) return false;
        if (monster.currentLevel < GetMaxLevelForStar(monster.currentStarLevel)) return false;

        return true;
    }

    /// <summary>
    /// Get maximum level for a specific star level
    /// </summary>
    public int GetMaxLevelForStar(int starLevel)
    {
        switch (starLevel)
        {
            case 1: return 20;
            case 2: return 30;
            case 3: return 40;
            case 4: return 50;
            case 5: return 60;
            case 6: return 60;
            default: return 20;
        }
    }

    /// <summary>
    /// Get available material monsters for upgrade
    /// ✨ SIMPLIFIED: Only requires same star level (no max level requirement)
    /// </summary>
    public List<CollectedMonster> GetAvailableMaterials(int requiredStarLevel)
    {
        if (MonsterCollectionManager.Instance == null) return new List<CollectedMonster>();

        var allMonsters = MonsterCollectionManager.Instance.GetAllMonsters();

        return allMonsters.Where(monster =>
            monster.currentStarLevel == requiredStarLevel &&
            // ✅ REMOVED: Max level requirement
            !monster.isInBattleTeam
        ).ToList();
    }


    /// <summary>
    /// Perform monster star upgrade
    /// </summary>
    public bool UpgradeMonster(CollectedMonster targetMonster, List<CollectedMonster> materialMonsters)
    {
        if (!CanUpgradeMonster(targetMonster))
        {
            Debug.LogWarning("⚠️ Target monster cannot be upgraded!");
            PlayUpgradeFailedSound();
            return false;
        }

        var requirement = GetUpgradeRequirement(targetMonster.currentStarLevel);

        if (materialMonsters.Count != requirement.requiredCount)
        {
            Debug.LogWarning($"⚠️ Incorrect number of materials! Required: {requirement.requiredCount}, Provided: {materialMonsters.Count}");
            PlayUpgradeFailedSound();
            return false;
        }

        // Validate all material monsters
        foreach (var material in materialMonsters)
        {
            if (material.currentStarLevel != targetMonster.currentStarLevel)
            {
                Debug.LogWarning($"⚠️ Material monster has wrong star level! Required: {targetMonster.currentStarLevel}, Got: {material.currentStarLevel}");
                PlayUpgradeFailedSound();
                return false;
            }

            // ✅ REMOVED: Max level validation for materials

            if (material.isInBattleTeam)
            {
                Debug.LogWarning("⚠️ Cannot use monster in battle team as material!");
                PlayUpgradeFailedSound();
                return false;
            }
        }


        // Perform the upgrade
        ExecuteUpgrade(targetMonster, materialMonsters);
        PlayUpgradeSuccessSound();
        return true;
    }

    /// <summary>
    /// Execute the actual upgrade
    /// </summary>
    private void ExecuteUpgrade(CollectedMonster targetMonster, List<CollectedMonster> materialMonsters)
    {
        // Upgrade star level
        targetMonster.currentStarLevel++;

        // Reset level to 1
        targetMonster.currentLevel = 1;
        targetMonster.currentExperience = 0;

        // Update base stats based on new star level
        UpdateBaseStatsForStarLevel(targetMonster);

        // Refresh current stats
        targetMonster.RefreshStats();

        // Remove material monsters from collection
        foreach (var material in materialMonsters)
        {
            MonsterCollectionManager.Instance.RemoveMonster(material);
        }

        // ✅ FIXED: Use SaveMonsterCollection() instead of SaveCollection()
        MonsterCollectionManager.Instance.SaveMonsterCollection();

        Debug.Log($"✅ Successfully upgraded {targetMonster.GetDisplayName()} to {targetMonster.currentStarLevel} stars!");
    }

    /// <summary>
    /// Update base stats when star level increases
    /// </summary>
    private void UpdateBaseStatsForStarLevel(CollectedMonster monster)
    {
        if (monster.monsterData == null || monster.baseStats == null) return;

        // Apply star level multiplier to base stats
        float starMultiplier = monster.monsterData.GetStarLevelMultiplier(monster.currentStarLevel);

        monster.baseStats.health = Mathf.RoundToInt(monster.monsterData.baseHP * starMultiplier);
        monster.baseStats.attack = Mathf.RoundToInt(monster.monsterData.baseATK * starMultiplier);
        monster.baseStats.defense = Mathf.RoundToInt(monster.monsterData.baseDEF * starMultiplier);
        monster.baseStats.speed = Mathf.RoundToInt(monster.monsterData.baseSPD * starMultiplier);
        monster.baseStats.energy = Mathf.RoundToInt(monster.monsterData.baseEnergy * starMultiplier);

        // Update combat stats
        monster.baseStats.criticalRate = monster.monsterData.baseCriticalRate + (monster.currentStarLevel - 1) * 3f;
        monster.baseStats.criticalDamage = monster.monsterData.baseCriticalDamage + (monster.currentStarLevel - 1) * 15f;
        monster.baseStats.accuracy = monster.monsterData.baseAccuracy + (monster.currentStarLevel - 1) * 2f;
        monster.baseStats.resistance = monster.monsterData.baseResistance + (monster.currentStarLevel - 1) * 2f;
    }

    /// <summary>
    /// Play upgrade success sound
    /// </summary>
    public void PlayUpgradeSuccessSound()
    {
        if (upgradeSuccessSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(upgradeSuccessSound);
        }
    }

    /// <summary>
    /// Play upgrade failed sound
    /// </summary>
    public void PlayUpgradeFailedSound()
    {
        if (upgradeFailedSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(upgradeFailedSound);
        }
    }
}

[System.Serializable]
public class UpgradeRequirement
{
    public int requiredStarLevel;
    public int requiredCount;
    public bool canUpgrade;

    public string GetRequirementText()
    {
        if (!canUpgrade) return "Cannot upgrade";

        // ✅ SIMPLIFIED: Remove "Max Level" from requirement text
        return $"Requires: {requiredCount}x {requiredStarLevel}⭐ Monsters";
    }
}
