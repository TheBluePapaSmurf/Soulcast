using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffDebuffIconUI : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public TextMeshProUGUI turnCounterText;
    public Image backgroundImage;

    [Header("Visual Settings")]
    public Color buffBackgroundColor = new Color(0.2f, 0.8f, 0.2f, 0.8f);    // Green for buffs
    public Color debuffBackgroundColor = new Color(0.8f, 0.2f, 0.2f, 0.8f);  // Red for debuffs
    public Color neutralBackgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Gray for neutral
    public Color overflowBackgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Dark gray for overflow

    private ActiveBuffDebuffEffect currentEffect;
    private bool isOverflowIndicator = false;

    /// <summary>
    /// Setup the icon for a specific buff/debuff effect
    /// </summary>
    public void Setup(ActiveBuffDebuffEffect activeEffect)
    {
        currentEffect = activeEffect;
        isOverflowIndicator = false;

        if (activeEffect?.effect == null) return;

        BuffDebuffEffect effect = activeEffect.effect;

        // Set icon
        if (iconImage != null && effect.icon != null)
        {
            iconImage.sprite = effect.icon;
            iconImage.color = effect.effectColor;
        }

        // Set background color based on effect type
        if (backgroundImage != null)
        {
            backgroundImage.color = GetBackgroundColor(effect.effectType);
        }

        // Set turn counter
        UpdateTurnCounter(activeEffect.remainingTurns);
    }

    /// <summary>
    /// Setup as overflow indicator (shows "+" with count)
    /// </summary>
    public void SetupAsOverflow(int hiddenCount)
    {
        isOverflowIndicator = true;

        // Set icon to "+" symbol or overflow icon
        if (iconImage != null)
        {
            iconImage.sprite = null; // Or set to a "+" icon if you have one
            iconImage.color = Color.white;
        }

        // Set background
        if (backgroundImage != null)
        {
            backgroundImage.color = overflowBackgroundColor;
        }

        // Show hidden count
        if (turnCounterText != null)
        {
            turnCounterText.text = $"+{hiddenCount}";
            turnCounterText.color = Color.white;
        }
    }

    /// <summary>
    /// Update the turn counter display
    /// </summary>
    public void UpdateTurnCounter(int remainingTurns)
    {
        if (turnCounterText == null || isOverflowIndicator) return;

        if (remainingTurns < 0) // Permanent effect
        {
            turnCounterText.text = "∞";
            turnCounterText.color = Color.yellow;
        }
        else if (remainingTurns == 0)
        {
            turnCounterText.text = "0";
            turnCounterText.color = Color.red;
        }
        else
        {
            turnCounterText.text = remainingTurns.ToString();

            // Color based on remaining time
            if (remainingTurns <= 1)
                turnCounterText.color = Color.red;
            else if (remainingTurns <= 2)
                turnCounterText.color = Color.yellow;
            else
                turnCounterText.color = Color.white;
        }
    }

    private Color GetBackgroundColor(EffectType effectType)
    {
        return effectType switch
        {
            EffectType.Buff => buffBackgroundColor,
            EffectType.Debuff => debuffBackgroundColor,
            EffectType.Neutral => neutralBackgroundColor,
            _ => neutralBackgroundColor
        };
    }
}
