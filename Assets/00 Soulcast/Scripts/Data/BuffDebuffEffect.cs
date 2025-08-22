using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum EffectType
{
    Buff,
    Debuff,
    Neutral
}

[System.Serializable]
public enum EffectCategory
{
    StatModifier,     // ATK/DEF/SPD boosts/reductions
    StatusCondition,  // Poison, Burn, Stun, etc.
    Healing,         // Regeneration, healing over time
    Protection,      // Shields, immunity
    Control         // Stun, Sleep, Charm
}

[CreateAssetMenu(fileName = "New Buff Debuff Effect", menuName = "Monster System/Buff Debuff Effect")]
public class BuffDebuffEffect : ScriptableObject
{
    [Header("Basic Information")]
    public string effectName;
    [TextArea(2, 4)]
    public string description;
    public EffectType effectType;
    public EffectCategory category;

    [Header("Visual")]
    public Sprite icon;
    public Color effectColor = Color.white;
    public GameObject visualEffectPrefab;

    [Header("Duration")]
    public int duration = 3;
    public bool isPermanent = false;
    [Tooltip("If true, effect stacks when applied multiple times")]
    public bool canStack = false;

    [Header("Stat Modifications")]
    public List<StatModifierData> statModifiers = new List<StatModifierData>();

    [Header("Status Conditions")]
    public bool preventsAction = false; // Stun, Sleep

    [Header("Damage/Heal Over Time (Percentage of Max HP)")]
    [Range(0f, 50f)]
    [Tooltip("Percentage of target's MAX HP dealt as damage per turn (0-50%)")]
    public float damagePerTurnPercent = 0f;     // Poison, Burn - percentage of max HP

    [Range(0f, 25f)]
    [Tooltip("Percentage of target's MAX HP healed per turn (0-25%)")]
    public float healPerTurnPercent = 0f;       // Regeneration - percentage of max HP

    [Header("Flat Damage/Heal Over Time (Optional)")]
    [Tooltip("Fixed damage per turn (used if damagePerTurnPercent is 0)")]
    public int damagePerTurnFlat = 0;           // Fallback to flat damage

    [Tooltip("Fixed heal per turn (used if healPerTurnPercent is 0)")]
    public int healPerTurnFlat = 0;             // Fallback to flat healing


    [Header("Special Properties")]
    public bool clearsOnDamage = false;   // Sleep breaks on damage
    public bool clearsOnAction = false;   // Some effects clear when taking action
    public int maxStacks = 1;             // Maximum stacks if canStack is true

    [Header("Resistance & Immunity")]
    public List<ElementType> resistantElements = new List<ElementType>(); // ✅ FIXED: ElementType instead of Element
    public List<BuffDebuffEffect> preventsEffects = new List<BuffDebuffEffect>();

    /// <summary>
    /// Apply this effect to a target monster
    /// </summary>
    public void ApplyTo(Monster target)
    {
        if (target == null) return;

        // Check element resistance
        if (resistantElements.Contains(target.monsterData.element))
        {
            Debug.Log($"🛡️ {target.monsterData.monsterName} resists {effectName} due to {target.monsterData.element} element!");
            return;
        }

        // Add the effect to the target's active effects
        target.AddBuffDebuffEffect(this);

        Debug.Log($"✨ {effectName} applied to {target.monsterData.monsterName}");
    }

    /// <summary>
    /// Remove this effect from a target monster
    /// </summary>
    public void RemoveFrom(Monster target)
    {
        if (target == null) return;

        target.RemoveBuffDebuffEffect(this);

        Debug.Log($"💨 {effectName} removed from {target.monsterData.monsterName}");
    }

    /// <summary>
    /// Get display text for UI
    /// </summary>
    public string GetDisplayText()
    {
        List<string> effects = new List<string>();

        // Add stat modifications
        foreach (var modifier in statModifiers)
        {
            effects.Add(modifier.GetDisplayText());
        }

        // Add status conditions with percentage display
        if (damagePerTurnPercent > 0)
            effects.Add($"{damagePerTurnPercent:F1}% max HP damage per turn");
        else if (damagePerTurnFlat > 0)
            effects.Add($"{damagePerTurnFlat} damage per turn");

        if (healPerTurnPercent > 0)
            effects.Add($"{healPerTurnPercent:F1}% max HP healing per turn");
        else if (healPerTurnFlat > 0)
            effects.Add($"{healPerTurnFlat} healing per turn");

        if (preventsAction)
            effects.Add("Cannot act");

        return string.Join(", ", effects);
    }
}

    [System.Serializable]
public class StatModifierData
{
    public StatType statType;
    public int modifierAmount;
    [Tooltip("If true, modifierAmount is a percentage. If false, it's a flat amount.")]
    public bool isPercentage = false;

    /// <summary>
    /// Calculate the actual modifier value for given base stat
    /// </summary>
    public int CalculateModifierValue(int baseStat)
    {
        if (isPercentage)
        {
            return Mathf.RoundToInt(baseStat * (modifierAmount / 100f));
        }
        else
        {
            return modifierAmount;
        }
    }

    /// <summary>
    /// Get display text for UI
    /// </summary>
    public string GetDisplayText()
    {
        string prefix = modifierAmount >= 0 ? "+" : "";
        string suffix = isPercentage ? "%" : "";
        string statName = GetStatDisplayName();

        return $"{prefix}{modifierAmount}{suffix} {statName}";
    }

    private string GetStatDisplayName()
    {
        return statType switch
        {
            StatType.Attack => "ATK",
            StatType.Defense => "DEF",
            StatType.Speed => "SPD",
            StatType.HP => "HP",
            StatType.Energy => "Energy",
            _ => statType.ToString()
        };
    }
}
