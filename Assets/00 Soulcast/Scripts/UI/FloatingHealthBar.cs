using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("UI Components")]
    public Slider healthSlider;
    public TextMeshProUGUI healthText;
    public Image fillImage;
    public Image backgroundImage;

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

        // Update colors based on health percentage
        UpdateHealthBarColor(healthPercent);

        // Hide health bar if monster is dead
        if (!targetMonster.isAlive)
        {
            gameObject.SetActive(false);
        }
    }

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
        UpdateHealthBar();
    }

    // Method to manually trigger health bar update
    public void OnHealthChanged()
    {
        UpdateHealthBar();
    }

    // Method to change scale at runtime
    public void SetScale(float newScale)
    {
        healthBarScale = Mathf.Clamp(newScale, 0.01f, 2f);
        ApplyScale();
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
