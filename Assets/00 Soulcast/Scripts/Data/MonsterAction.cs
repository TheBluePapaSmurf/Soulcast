// MonsterAction.cs
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public enum ActionCategory
{
    NormalAttack,
    SpecialAttack,
    Ultimate
}

[System.Serializable]
public enum ActionType
{
    Attack,
    Buff,
    Debuff,
    Heal
}

[System.Serializable]
public enum TargetType
{
    Single,
    AllEnemies,
    AllAllies,
    Self,
    Random
}

[System.Serializable]
public enum AttackRange
{
    Melee,
    Ranged,
    None  // For non-attack actions like buffs/heals
}

[CreateAssetMenu(fileName = "New Monster Action", menuName = "Monster System/Monster Action")]
public class MonsterAction : ScriptableObject
{
    [Header("Basic Information")]
    public string actionName;
    [TextArea(2, 4)]
    public string description;
    public ActionCategory category;
    public ActionType type;

    [Header("Target Settings")]
    public TargetType targetType;
    public bool requiresTarget = true;
    public AttackRange attackRange = AttackRange.None;

    [Header("Resource Costs")]
    public int energyCost = 10;
    public int cooldownTurns = 0;

    [Header("Damage/Healing")]
    public int basePower = 0;
    public bool usesAttackStat = true;
    public bool ignoresDefense = false;

    [Header("Multi-Hit Settings")]
    public int hitCount = 1; // Hoeveel keer de attack raakt
    public float timeBetweenHits = 0.3f; // Tijd tussen elke hit in seconden
    [Tooltip("Als true, wordt de schade per hit verdeeld. Als false, doet elke hit volledige schade")]
    public bool divideDamagePerHit = true; // Of de schade verdeeld wordt over hits

    [Header("Status Effects")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    [Header("Stat Modifiers")]
    public List<StatModifier> statModifiers = new List<StatModifier>();

    [Header("Visual/Audio")]
    public GameObject effectPrefab;
    public AudioClip soundEffect;
    public Sprite icon;

    [Header("Special Properties")]
    public bool healsUser = false;
    public int healAmount = 0;
    public float criticalChance = 0.1f;
    public float criticalMultiplier = 1.5f;

    // Helper methods
    public bool IsMeleeAttack => type == ActionType.Attack && attackRange == AttackRange.Melee;
    public bool IsRangedAttack => type == ActionType.Attack && attackRange == AttackRange.Ranged;
    public bool IsAttack => type == ActionType.Attack && attackRange != AttackRange.None;
}

[System.Serializable]
public class StatusEffect
{
    public string effectName;
    public int duration;
    public int damagePerTurn; // For poison/burn
    public int healPerTurn;   // For regeneration
    public bool preventAction; // For stun/sleep
}

[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public int modifierAmount;
    public int duration;
    public bool isPermanent = false;
}

[System.Serializable]
public enum StatType
{
    Attack,
    Defense,
    Speed,
    HP,
    Energy
}
