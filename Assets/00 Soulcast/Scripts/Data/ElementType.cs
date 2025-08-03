// ElementType.cs
using UnityEngine;

[System.Serializable]
public enum ElementType
{
    Fire,
    Water,
    Earth,
    Light,
    Dark
}

[System.Serializable]
public static class ElementalSystem
{
    public const float ADVANTAGE_MULTIPLIER = 1.2f;
    public const float DISADVANTAGE_MULTIPLIER = 0.8f;
    public const float NEUTRAL_MULTIPLIER = 1.0f;

    public static float GetElementalAdvantage(ElementType attacker, ElementType defender)
    {
        // Fire beats Earth
        if (attacker == ElementType.Fire && defender == ElementType.Earth)
            return ADVANTAGE_MULTIPLIER;
        if (attacker == ElementType.Earth && defender == ElementType.Fire)
            return DISADVANTAGE_MULTIPLIER;

        // Earth beats Water
        if (attacker == ElementType.Earth && defender == ElementType.Water)
            return ADVANTAGE_MULTIPLIER;
        if (attacker == ElementType.Water && defender == ElementType.Earth)
            return DISADVANTAGE_MULTIPLIER;

        // Water beats Fire
        if (attacker == ElementType.Water && defender == ElementType.Fire)
            return ADVANTAGE_MULTIPLIER;
        if (attacker == ElementType.Fire && defender == ElementType.Water)
            return DISADVANTAGE_MULTIPLIER;

        // Light and Dark counter each other
        if (attacker == ElementType.Light && defender == ElementType.Dark)
            return ADVANTAGE_MULTIPLIER;
        if (attacker == ElementType.Dark && defender == ElementType.Light)
            return ADVANTAGE_MULTIPLIER;

        // All other combinations are neutral
        return NEUTRAL_MULTIPLIER;
    }

    public static Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return new Color(1f, 0.3f, 0.1f); // Red-Orange
            case ElementType.Water: return new Color(0.1f, 0.5f, 1f); // Blue
            case ElementType.Earth: return new Color(0.6f, 0.4f, 0.2f); // Brown
            case ElementType.Light: return new Color(1f, 1f, 0.8f); // Light Yellow
            case ElementType.Dark: return new Color(0.3f, 0.1f, 0.4f); // Dark Purple
            default: return Color.white;
        }
    }

    public static string GetElementAdvantageText(ElementType attacker, ElementType defender)
    {
        float multiplier = GetElementalAdvantage(attacker, defender);

        if (multiplier > NEUTRAL_MULTIPLIER)
            return "Super Effective!";
        else if (multiplier < NEUTRAL_MULTIPLIER)
            return "Not Very Effective...";
        else
            return "";
    }
}
