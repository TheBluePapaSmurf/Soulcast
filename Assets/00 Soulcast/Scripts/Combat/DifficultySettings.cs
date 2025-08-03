using UnityEngine;

[CreateAssetMenu(fileName = "New Difficulty", menuName = "Combat/Difficulty Settings")]
public class DifficultySettings : ScriptableObject
{
    [Header("Difficulty Info")]
    public string difficultyName;
    public string description;

    [Header("Enemy Stat Modifiers")]
    [Range(0.5f, 3.0f)] public float hpMultiplier = 1.0f;
    [Range(0.5f, 3.0f)] public float damageMultiplier = 1.0f;
    [Range(0.5f, 2.0f)] public float speedMultiplier = 1.0f;
    [Range(0.5f, 2.0f)] public float energyMultiplier = 1.0f;

    [Header("AI Intelligence")]
    [Range(0, 100)] public int strategicThinkingChance = 50; // % chance to make smart decisions
    [Range(0, 100)] public int targetPriorityChance = 50;   // % chance to use target priority
    [Range(0, 100)] public int energyManagementChance = 50; // % chance to manage energy wisely

    [Header("Combat Advantages")]
    public bool canUseAdvancedAttacks = false;
    public bool hasBetterCritChance = false;
    [Range(1, 4)] public int maxEnemiesInCombat = 2;

    [Header("Player Disadvantages")]
    [Range(0.5f, 1.0f)] public float playerEnergyMultiplier = 1.0f;
    public bool limitPlayerHealing = false;
}
