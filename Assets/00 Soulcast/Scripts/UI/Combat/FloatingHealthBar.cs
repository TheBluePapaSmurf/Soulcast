using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public Image fillImage;
    public Image backgroundImage;

    [Header("Level Display")]
    public TextMeshProUGUI levelText;              // Text component for level
    public Image levelBackgroundImage;             // Background image for level text

    [Header("Buff/Debuff Icons")]
    public Transform buffDebuffContainer;           // Container for buff/debuff icons
    public GameObject buffDebuffIconPrefab;         // Prefab for individual icons
    public int maxVisibleIcons = 6;                // Maximum icons to show
    public float iconSpacing = 30f;                // Spacing between icons

    [Header("Settings")]
    public bool faceCamera = true;
    public Vector3 offset = Vector3.up * 2.5f;
    public float smoothTime = 0.1f;

    [Header("Scale Settings")]
    [Range(0.01f, 2f)]
    public float healthBarScale = 0.2f; // Editable scale in inspector

    [Header("Colors")]
    public Color healthyColor = Color.green;
    public Color lowHealthColor = Color.red;
    public Color criticalHealthColor = Color.red;
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.3f;
    [Range(0f, 1f)]
    public float criticalHealthThreshold = 0.15f;

    private Monster targetMonster;
    private Camera mainCamera;
    private Canvas canvas;
    private Vector3 velocity;
    private float lastScale = -1f; // Track scale changes

    // Buff/Debuff tracking
    private Dictionary<BuffDebuffEffect, GameObject> activeIcons = new Dictionary<BuffDebuffEffect, GameObject>();
    private List<ActiveBuffDebuffEffect> lastKnownEffects = new List<ActiveBuffDebuffEffect>();

    [System.Obsolete]
    void Start()
    {
        // Get references
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindAnyObjectByType<Camera>();
        }

        targetMonster = GetComponentInParent<Monster>();
        canvas = GetComponent<Canvas>();

        // Setup canvas for world space
        SetupCanvas();

        // Apply initial scale
        ApplyScale();

        // Initialize position
        if (targetMonster != null)
        {
            transform.position = targetMonster.transform.position + offset;
            UpdateHealthBar();
            UpdateBuffDebuffIcons();
        }

        Debug.Log($"FloatingHealthBar initialized for {targetMonster?.monsterData?.monsterName ?? "Unknown"}");
    }

    void SetupCanvas()
    {
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = mainCamera;

            // Set canvas size for better scaling
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080); // Keep original size, scale with transform
        }
    }

    void Update()
    {
        if (targetMonster == null) return;

        // Update position to follow monster
        Vector3 targetPosition = targetMonster.transform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Check if scale changed in editor
        if (lastScale != healthBarScale)
        {
            ApplyScale();
        }

        // Update health values
        UpdateHealthBar();

        // Update buff/debuff icons
        UpdateBuffDebuffIcons();
    }

    void LateUpdate()
    {
        // Face camera with Y-axis rotation only
        if (faceCamera && mainCamera != null)
        {
            FaceCameraYAxisOnly();
        }
    }

    void FaceCameraYAxisOnly()
    {
        // Calculate direction to camera
        Vector3 directionToCamera = mainCamera.transform.position - transform.position;

        // Zero out Y component to get horizontal direction only
        directionToCamera.y = 0;

        // Only rotate if we have a valid horizontal direction
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            // Create rotation that only rotates around Y-axis
            float yRotation = Mathf.Atan2(directionToCamera.x, directionToCamera.z) * Mathf.Rad2Deg;

            // Apply rotation with X=0, Z=0, only Y rotation
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void ApplyScale()
    {
        transform.localScale = Vector3.one * healthBarScale;
        lastScale = healthBarScale;
    }

    public void UpdateHealthBar()
    {
        if (targetMonster == null || targetMonster.monsterData == null) return;

        int currentHP = targetMonster.currentHP;
        int maxHP = targetMonster.monsterData.baseHP;
        float healthPercent = (float)currentHP / maxHP;

        // Update slider
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        // Update text
        if (healthText != null)
        {
            healthText.text = $"{currentHP}/{maxHP}";
        }

        // ✅ NEW: Update level display
        UpdateLevelDisplay();

        // Update colors based on health percentage
        UpdateHealthBarColor(healthPercent);

        // Hide health bar if monster is dead
        if (!targetMonster.isAlive)
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateLevelDisplay()
    {
        if (targetMonster == null || targetMonster.monsterData == null) return;

        // Get monster level
        int monsterLevel = GetMonsterLevel();

        // Update level text (always white)
        if (levelText != null)
        {
            levelText.text = $"{monsterLevel}";
            levelText.color = Color.white;
        }

        // Update level background with custom element color
        if (levelBackgroundImage != null)
        {
            Color elementColor = GetCustomElementColor(targetMonster.monsterData.element);
            levelBackgroundImage.color = elementColor;
        }

        Debug.Log($"Updated level display: Lv.{monsterLevel} with {targetMonster.monsterData.element} color");
    }

    private Color GetCustomElementColor(ElementType element)
    {
        return element switch
        {
            ElementType.Fire => Color.red,                                    // 🔥 Rood
            ElementType.Water => Color.blue,                                  // 💧 Blauw  
            ElementType.Earth => new Color(0.8f, 0.6f, 0.2f, 0.9f),         // 🌍 Bruin/geel
            ElementType.Dark => new Color(0.3f, 0.1f, 0.4f, 0.9f),          // 🌑 Donker paars
            ElementType.Light => new Color(0.8f, 0.8f, 0.8f, 0.9f),         // ✨ Licht grijs
            _ => Color.white
        };
    }


    // ✅ NEW: Get Monster Level from Different Sources
    private int GetMonsterLevel()
    {
        // Try to get level from CollectedMonster (for player monsters)
        if (targetMonster.isPlayerControlled && BattleDataManager.Instance != null)
        {
            var selectedTeam = BattleDataManager.Instance.GetSelectedTeam();
            var collectedMonster = selectedTeam.FirstOrDefault(m =>
                m.monsterData.monsterName == targetMonster.monsterData.monsterName);

            if (collectedMonster != null)
            {
                return collectedMonster.currentLevel;
            }
        }

        // For enemy monsters, try to extract level from name
        string monsterName = targetMonster.monsterData.monsterName;
        if (monsterName.Contains("Lv."))
        {
            // Extract level from name like "Fire Monster Lv.5 (Rare)"
            int lvIndex = monsterName.IndexOf("Lv.") + 3;
            int spaceIndex = monsterName.IndexOf(" ", lvIndex);
            if (spaceIndex == -1) spaceIndex = monsterName.IndexOf("(", lvIndex);
            if (spaceIndex == -1) spaceIndex = monsterName.Length;

            string levelStr = monsterName.Substring(lvIndex, spaceIndex - lvIndex).Trim();
            if (int.TryParse(levelStr, out int extractedLevel))
            {
                return extractedLevel;
            }
        }

        // Fallback to default level
        return targetMonster.monsterData.defaultLevel;
    }


    // ===== NEW: BUFF/DEBUFF ICON SYSTEM =====

    public void UpdateBuffDebuffIcons()
    {
        if (targetMonster == null || buffDebuffContainer == null) return;

        var currentEffects = targetMonster.activeBuffDebuffEffects;

        // Check if effects have changed
        if (EffectsChanged(currentEffects))
        {
            RefreshBuffDebuffIcons(currentEffects);
            lastKnownEffects = new List<ActiveBuffDebuffEffect>(currentEffects);
        }
        else
        {
            // Just update the turn counters
            UpdateIconTurnCounters(currentEffects);
        }
    }

    private bool EffectsChanged(List<ActiveBuffDebuffEffect> currentEffects)
    {
        if (currentEffects.Count != lastKnownEffects.Count)
            return true;

        // Check if any effects are different
        foreach (var current in currentEffects)
        {
            bool found = lastKnownEffects.Any(last =>
                last.effect == current.effect);
            if (!found)
                return true;
        }

        return false;
    }

    private void RefreshBuffDebuffIcons(List<ActiveBuffDebuffEffect> effects)
    {
        // Clear existing icons
        ClearAllIcons();

        // Create new icons (limit to maxVisibleIcons)
        int iconsToShow = Mathf.Min(effects.Count, maxVisibleIcons);

        for (int i = 0; i < iconsToShow; i++)
        {
            var activeEffect = effects[i];
            CreateBuffDebuffIcon(activeEffect, i);
        }

        // If there are more effects than we can show, create an overflow indicator
        if (effects.Count > maxVisibleIcons)
        {
            CreateOverflowIndicator(effects.Count - maxVisibleIcons);
        }
    }

    private void CreateBuffDebuffIcon(ActiveBuffDebuffEffect activeEffect, int index)
    {
        if (buffDebuffIconPrefab == null) return;

        GameObject iconGO = Instantiate(buffDebuffIconPrefab, buffDebuffContainer);
        BuffDebuffIconUI iconUI = iconGO.GetComponent<BuffDebuffIconUI>();

        if (iconUI != null)
        {
            iconUI.Setup(activeEffect);
        }

        // Position the icon
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        if (iconRect != null)
        {
            float xPosition = index * iconSpacing;
            iconRect.anchoredPosition = new Vector2(xPosition, 0);
        }

        // Track the icon
        activeIcons[activeEffect.effect] = iconGO;
    }

    private void CreateOverflowIndicator(int hiddenCount)
    {
        if (buffDebuffIconPrefab == null) return;

        GameObject iconGO = Instantiate(buffDebuffIconPrefab, buffDebuffContainer);
        BuffDebuffIconUI iconUI = iconGO.GetComponent<BuffDebuffIconUI>();

        if (iconUI != null)
        {
            iconUI.SetupAsOverflow(hiddenCount);
        }

        // Position at the end
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        if (iconRect != null)
        {
            float xPosition = (maxVisibleIcons - 1) * iconSpacing;
            iconRect.anchoredPosition = new Vector2(xPosition, 0);
        }
    }

    private void UpdateIconTurnCounters(List<ActiveBuffDebuffEffect> effects)
    {
        foreach (var effect in effects)
        {
            if (activeIcons.TryGetValue(effect.effect, out GameObject iconGO))
            {
                BuffDebuffIconUI iconUI = iconGO.GetComponent<BuffDebuffIconUI>();
                if (iconUI != null)
                {
                    iconUI.UpdateTurnCounter(effect.remainingTurns);
                }
            }
        }
    }

    private void ClearAllIcons()
    {
        foreach (var icon in activeIcons.Values)
        {
            if (icon != null)
                Destroy(icon);
        }
        activeIcons.Clear();
    }

    // ===== EXISTING METHODS =====

    private void UpdateHealthBarColor(float healthPercent)
    {
        if (fillImage == null) return;

        Color targetColor;

        if (healthPercent <= criticalHealthThreshold)
        {
            // Critical health - pulsing red
            targetColor = criticalHealthColor;
            StartPulsingEffect();
        }
        else if (healthPercent <= lowHealthThreshold)
        {
            // Low health - orange/red
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor,
                (healthPercent - criticalHealthThreshold) / (lowHealthThreshold - criticalHealthThreshold));
        }
        else
        {
            // Healthy - green to yellow
            targetColor = Color.Lerp(lowHealthColor, healthyColor,
                (healthPercent - lowHealthThreshold) / (1f - lowHealthThreshold));
        }

        fillImage.color = targetColor;
    }

    private void StartPulsingEffect()
    {
        // Simple pulsing effect for critical health
        if (fillImage != null)
        {
            float pulse = (Mathf.Sin(Time.time * 5f) + 1f) / 2f; // 0 to 1
            Color baseColor = criticalHealthColor;
            baseColor.a = Mathf.Lerp(0.5f, 1f, pulse);
            fillImage.color = baseColor;
        }
    }

    public void SetTarget(Monster monster)
    {
        targetMonster = monster;
        UpdateHealthBar(); // This now includes level display
        UpdateBuffDebuffIcons();
    }

    // Method to manually trigger health bar update
    public void OnHealthChanged()
    {
        UpdateHealthBar(); // This now includes level display
    }


    // ✅ NEW: Method to trigger buff/debuff icon update
    public void OnBuffDebuffChanged()
    {
        UpdateBuffDebuffIcons();
    }

    // Method to change scale at runtime
    public void SetScale(float newScale)
    {
        healthBarScale = Mathf.Clamp(newScale, 0.01f, 2f);
        ApplyScale();
    }

    // Cleanup
    private void OnDestroy()
    {
        ClearAllIcons();
    }

    // Editor helper methods
#if UNITY_EDITOR
    void OnValidate()
    {
        // Apply scale changes immediately in editor
        if (Application.isPlaying)
        {
            ApplyScale();
        }
    }
#endif
}
