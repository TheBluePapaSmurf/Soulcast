using UnityEngine;
using System.Collections;

public class ModelController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;

    [Header("Movement Animation")]
    public bool isMoving = false;

    [Header("Visual Effects")]
    public ParticleSystem elementalEffect;
    public Material damageMaterial;

    [Header("Health Bar")]
    public GameObject healthBarPrefab;

    [Header("Damage Numbers")]
    public DamageNumberController damageNumberController;

    [Header("Model Settings")]
    public bool lookAtCamera = false;
    public bool bobAnimation = false;
    public float bobSpeed = 1f;
    public float bobHeight = 0.1f;

    [Header("Combat Visual Feedback")]
    public float damageFlashDuration = 0.5f;
    public Color damageFlashColor = Color.red;

    [Header("Attack Animation Settings")]
    public Transform attackOriginPoint;
    public bool showAttackEffects = true;

    [Header("Audio")]
    public AudioSource audioSource;

    private Vector3 originalPosition;
    private Camera mainCamera;
    private Monster parentMonster;
    private Renderer[] modelRenderers;
    private Material[] originalMaterials;
    private FloatingHealthBar floatingHealthBar;
    private bool isAnimating = false;

    [Header("Performance Settings")]
    public float updateRate = 30f; // Updates per second instead of every frame
    private float lastUpdateTime = 0f;

    [System.Obsolete]
    void Start()
    {
        // Get references
        mainCamera = Camera.main;
        parentMonster = GetComponentInParent<Monster>();
        originalPosition = transform.localPosition;

        // Store original materials for damage flash
        modelRenderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[modelRenderers.Length];
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            originalMaterials[i] = modelRenderers[i].material;
        }

        // Auto-find components if not assigned
        if (animator == null)
            animator = GetComponent<Animator>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Setup attack points
        SetupAttackPoints();

        // Setup elemental effects
        SetupElementalEffects();

        // Create floating health bar
        CreateFloatingHealthBar();
    }

    void Update()
    {
        // Limit update frequency to reduce CPU usage
        if (Time.time - lastUpdateTime < 1f / updateRate) return;
        lastUpdateTime = Time.time;

        // Only update if this monster is active or visible
        if (!ShouldUpdate()) return;

        // Look at camera (only when not moving or animating)
        if (lookAtCamera && mainCamera != null && !isAnimating && !isMoving)
        {
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0;
            if (directionToCamera != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
            }
        }

        // Bob animation (only when not moving or animating)
        if (bobAnimation && !isAnimating && !isMoving)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = originalPosition + Vector3.up * bobOffset;
        }
    }

    private bool ShouldUpdate()
    {
        // Only update if monster is alive and within camera view
        if (parentMonster != null && !parentMonster.isAlive) return false;

        // Check if object is within camera frustum (basic culling)
        if (mainCamera != null)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
            Bounds bounds = GetComponent<Collider>()?.bounds ?? new Bounds(transform.position, Vector3.one);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        return true;
    }


    public void TriggerMovementAnimation(bool moving)
    {
        isMoving = moving;

        if (animator != null)
        {
            animator.SetBool("IsMoving", moving);
        }

        Debug.Log($"{gameObject.name} movement animation: {(moving ? "Started" : "Stopped")}");
    }


    void SetupAttackPoints()
    {
        // Create attack origin point if not assigned
        if (attackOriginPoint == null)
        {
            GameObject originPoint = new GameObject("AttackOrigin");
            originPoint.transform.SetParent(transform);
            originPoint.transform.localPosition = Vector3.up * 1.5f; // Chest height
            attackOriginPoint = originPoint.transform;
        }
    }

    #region Health Bar Management
    private void CreateFloatingHealthBar()
    {
        if (parentMonster == null) return;

        GameObject healthBarObj;

        // Use prefab if assigned, otherwise create one
        if (healthBarPrefab != null)
        {
            healthBarObj = Instantiate(healthBarPrefab, transform);
        }
        else
        {
            // Create health bar programmatically
            healthBarObj = HealthBarFactory.CreateFloatingHealthBarPrefab();
            healthBarObj.transform.SetParent(transform);
        }

        // Position the health bar
        healthBarObj.transform.localPosition = Vector3.up * 3f;
        healthBarObj.name = "FloatingHealthBar";

        // Get the FloatingHealthBar component
        floatingHealthBar = healthBarObj.GetComponent<FloatingHealthBar>();
        if (floatingHealthBar != null)
        {
            floatingHealthBar.SetTarget(parentMonster);
            Debug.Log($"Created floating health bar for {parentMonster.monsterData.monsterName}");
        }
        else
        {
            Debug.LogError("FloatingHealthBar component not found on health bar prefab!");
        }
    }

    public void OnMonsterHealthChanged()
    {
        if (floatingHealthBar != null)
        {
            floatingHealthBar.OnHealthChanged();
        }
    }
    #endregion

    #region Simple Animation System
    public void TriggerAttackAnimation(MonsterAction action, Monster target = null)
    {
        if (isAnimating) return; // Prevent overlapping animations

        StartCoroutine(PlayAttackAnimation(action, target));
    }

    private IEnumerator PlayAttackAnimation(MonsterAction action, Monster target)
    {
        isAnimating = true;

        // 1. Face target if it exists (only for ranged attacks, melee already faces target)
        if (target != null && action.IsRangedAttack)
        {
            LookAtTarget(target.transform);
        }

        // 2. Play sound effect
        if (action.soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(action.soundEffect);
        }

        // 3. Trigger animator
        string animationTrigger = GetAnimationTrigger(action);
        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
        {
            animator.SetTrigger(animationTrigger);
        }

        // 4. Wait for windup
        float windupTime = GetWindupTime(action);
        yield return new WaitForSeconds(windupTime);

        // 5. Spawn visual effects
        if (showAttackEffects)
        {
            SpawnAttackEffects(action, target);
        }

        // 6. Create projectile for ranged attacks only
        if (action.IsRangedAttack && target != null)
        {
            yield return StartCoroutine(FireProjectile(action, target));
        }

        // 7. Wait for animation to complete
        float animationDuration = GetAnimationDuration(action);
        yield return new WaitForSeconds(animationDuration - windupTime);

        // 8. Return to idle (only for ranged attacks)
        if (action.IsRangedAttack)
        {
            yield return StartCoroutine(ReturnToIdle());
        }

        isAnimating = false;
        Debug.Log($"Completed attack animation for {action.actionName}");
    }

    private string GetAnimationTrigger(MonsterAction action)
    {
        switch (action.type)
        {
            case ActionType.Attack:
                switch (action.category)
                {
                    case ActionCategory.NormalAttack:
                        return "Attack";
                    case ActionCategory.SpecialAttack:
                        return "SpecialAttack";
                    case ActionCategory.Ultimate:
                        return "Ultimate";
                    default:
                        return "Attack";
                }

            case ActionType.Buff:
            case ActionType.Debuff:
            case ActionType.Heal:
                return "Ability";

            default:
                return "Attack";
        }
    }

    private bool IsRangedAttack(MonsterAction action)
    {
        return action.IsRangedAttack; // Use the helper method from MonsterAction
    }

    private void LookAtTarget(Transform target)
    {
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0; // Keep horizontal

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void SpawnAttackEffects(MonsterAction action, Monster target)
    {
        // Spawn action-specific visual effects
        if (action.effectPrefab != null)
        {
            Vector3 spawnPosition = attackOriginPoint.position;
            GameObject effect = Instantiate(action.effectPrefab, spawnPosition, Quaternion.identity);

            // Force start any looping particle systems
            ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in particleSystems)
            {
                if (!ps.isPlaying)
                {
                    ps.Play();
                }
            }

            // Orient effect toward target
            if (target != null)
            {
                Vector3 directionToTarget = target.transform.position - spawnPosition;
                effect.transform.LookAt(spawnPosition + directionToTarget);
            }

            // Auto-destroy after 3 seconds
            Destroy(effect, 3f);

            Debug.Log($"Spawned effect for {action.actionName}");
        }
        else
        {
            // Create default elemental effect
            SpawnDefaultElementalEffect(action, target);
        }
    }

    private void SpawnDefaultElementalEffect(MonsterAction action, Monster target = null)
    {
        if (parentMonster == null || parentMonster.monsterData == null) return;

        ElementType element = parentMonster.monsterData.element;
        Color elementColor = ElementalSystem.GetElementColor(element);

        // Create simple particle effect
        GameObject effectObj = new GameObject($"{action.actionName}_Effect");
        effectObj.transform.position = attackOriginPoint.position;

        // Calculate direction to target
        Vector3 directionToTarget = Vector3.forward;
        if (target != null)
        {
            directionToTarget = (target.transform.position - attackOriginPoint.position).normalized;
        }

        ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();

        // Main settings
        var main = particles.main;
        main.startColor = elementColor;
        main.startLifetime = 1f;
        main.startSpeed = 5f;
        main.maxParticles = 30;

        // Shape settings - cone pointing toward target
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;
        shape.radius = 0.1f;

        // Point the cone toward the target
        effectObj.transform.LookAt(attackOriginPoint.position + directionToTarget);

        // Emission settings
        var emission = particles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 15)
        });

        // Auto-destroy
        Destroy(effectObj, 2f);
    }

    private IEnumerator FireProjectile(MonsterAction action, Monster target)
    {
        if (target == null) yield break;

        // Use object pool instead of CreatePrimitive
        GameObject projectile = ObjectPool.Instance?.SpawnFromPool("Projectile", attackOriginPoint.position, Quaternion.identity);

        if (projectile == null)
        {
            // Fallback to original method if pool not available
            projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = attackOriginPoint.position;
        }

        projectile.transform.position = attackOriginPoint.position;
        projectile.transform.localScale = Vector3.one * 0.3f;
        projectile.name = $"{action.actionName}_Projectile";

        // Remove collider
        Destroy(projectile.GetComponent<Collider>());

        // Color based on element
        Renderer projRenderer = projectile.GetComponent<Renderer>();
        if (parentMonster != null && parentMonster.monsterData != null)
        {
            Color elementColor = ElementalSystem.GetElementColor(parentMonster.monsterData.element);
            projRenderer.material.color = elementColor;
            projRenderer.material.SetFloat("_Metallic", 0.8f);
            projRenderer.material.SetFloat("_Smoothness", 0.9f);
        }

        // Add trail effect
        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.material = projRenderer.material;
        trail.time = 0.5f;
        trail.startWidth = 0.2f;
        trail.endWidth = 0.05f;

        // Move projectile to target
        Vector3 startPos = projectile.transform.position;
        Vector3 targetPos = target.transform.position + Vector3.up * 1.5f;
        float travelTime = 0.8f;
        float elapsedTime = 0f;

        while (elapsedTime < travelTime)
        {
            float t = elapsedTime / travelTime;
            projectile.transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Final position
        projectile.transform.position = targetPos;

        // Impact effect
        SpawnImpactEffect(targetPos, action);

        // Cleanup
        Destroy(projectile);
    }

    private void SpawnImpactEffect(Vector3 position, MonsterAction action)
    {
        if (parentMonster == null || parentMonster.monsterData == null) return;

        ElementType element = parentMonster.monsterData.element;
        Color elementColor = ElementalSystem.GetElementColor(element);

        GameObject effectObj = new GameObject($"{action.actionName}_Impact");
        effectObj.transform.position = position;

        ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = elementColor;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.maxParticles = 20;

        var emission = particles.emission;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0.0f, 20)
        });
        emission.rateOverTime = 0;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        Destroy(effectObj, 1f);
    }

    private IEnumerator ReturnToIdle()
    {
        // Don't reset rotation for ranged attacks - preserve the monster's spawn rotation
        // Get the parent monster's original rotation
        Monster parentMonster = GetComponentInParent<Monster>();
        if (parentMonster != null)
        {
            Quaternion startRot = transform.rotation;
            Quaternion targetRot = parentMonster.transform.rotation; // Use monster's current rotation, not identity

            float returnTime = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < returnTime)
            {
                float t = elapsedTime / returnTime;
                transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            transform.rotation = targetRot;
        }
    }


    private float GetWindupTime(MonsterAction action)
    {
        switch (action.category)
        {
            case ActionCategory.NormalAttack:
                return 0.3f;
            case ActionCategory.SpecialAttack:
                return 0.5f;
            case ActionCategory.Ultimate:
                return 0.8f;
            default:
                return 0.3f;
        }
    }

    private float GetAnimationDuration(MonsterAction action)
    {
        switch (action.category)
        {
            case ActionCategory.NormalAttack:
                return 1f;
            case ActionCategory.SpecialAttack:
                return 1.5f;
            case ActionCategory.Ultimate:
                return 2f;
            default:
                return 1f;
        }
    }

    // Legacy animation methods (for backward compatibility)
    public void TriggerAttackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
        Debug.Log($"Triggered basic attack animation for {gameObject.name}");
    }
    #endregion

    #region Other Animation Methods
    public void TriggerHitAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hit");
            animator.SetBool("IsMoving", false);
        }

        // Enhanced damage flash effect
        StartCoroutine(EnhancedDamageFlash());

        // Screen shake effect
        StartCoroutine(HitShakeEffect());

        // Update health bar
        OnMonsterHealthChanged();

        Debug.Log($"Triggered hit animation for {gameObject.name}");
    }


    public void TriggerDeathAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Death");

        // Start death effects
        StartCoroutine(DeathSequence());

        Debug.Log($"Triggered death animation for {gameObject.name}");
    }

    public void SetActiveIndicator(bool isActive)
    {
        // Show visual indicator when it's this monster's turn
        if (isActive)
        {
            CreateActiveGlow();
        }
        else
        {
            RemoveActiveGlow();
        }
    }
    #endregion

    #region Visual Effects
    private void SetupElementalEffects()
    {
        if (parentMonster == null || parentMonster.monsterData == null) return;

        ElementType element = parentMonster.monsterData.element;

        // Create elemental light
        GameObject lightObj = new GameObject("Elemental_Light");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.up * 2f;

        Light elementLight = lightObj.AddComponent<Light>();
        elementLight.type = LightType.Point;
        elementLight.color = ElementalSystem.GetElementColor(element);
        elementLight.intensity = 0.3f;
        elementLight.range = 3f;

        // Create element indicator above model
        CreateElementIndicator(element);
    }

    private void CreateElementIndicator(ElementType element)
    {
        // Create a simple floating icon above the monster
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = Vector3.up * 4f; // Above health bar
        indicator.transform.localScale = Vector3.one * 0.3f;
        indicator.name = "Element_Indicator";

        // Remove collider
        Destroy(indicator.GetComponent<Collider>());

        // Color it
        Renderer renderer = indicator.GetComponent<Renderer>();
        renderer.material.color = ElementalSystem.GetElementColor(element);
        renderer.material.SetFloat("_Metallic", 0.8f);
        renderer.material.SetFloat("_Smoothness", 0.9f);

        // Add simple rotation using a basic component
        RotatingObject rotator = indicator.AddComponent<RotatingObject>();
        rotator.rotationSpeed = Vector3.up * 30f; // 30 degrees per second around Y axis
    }

    private void CreateActiveGlow()
    {
        // Create a simple glow ring around the active monster
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glow.transform.SetParent(transform);
        glow.transform.localPosition = Vector3.zero;
        glow.transform.localScale = new Vector3(3f, 0.1f, 3f);
        glow.name = "Active_Glow";

        // Remove collider
        Destroy(glow.GetComponent<Collider>());

        // Make it glow
        Renderer renderer = glow.GetComponent<Renderer>();
        renderer.material.color = Color.yellow;
        renderer.material.SetFloat("_Metallic", 0f);
        renderer.material.SetFloat("_Smoothness", 1f);

        // Add simple pulsing using a basic animation component
        SimpleColorPulse pulse = glow.AddComponent<SimpleColorPulse>();
        pulse.baseColor = Color.yellow;
        pulse.pulseSpeed = 2f;
    }

    public void ShowParticleDamageNumber(int damage, bool isCritical = false)
    {
        if (DamageNumberController.Instance != null)
        {
            // Use the monster's world position
            Vector3 damagePosition = transform.position;
            DamageNumberController.Instance.ShowDamageNumber(damagePosition, damage, isCritical);
        }
        else
        {
            Debug.LogWarning("DamageNumberController.Instance not found! Make sure DamageNumberManager exists in scene.");
        }
    }

    public void ShowHealNumber(int healAmount)
    {
        if (DamageNumberController.Instance != null)
        {
            Vector3 healPosition = transform.position;
            // You can extend the manager to handle heal type
            DamageNumberController.Instance.ShowDamageNumber(healPosition, healAmount, false);
        }
    }

    private void RemoveActiveGlow()
    {
        Transform glow = transform.Find("Active_Glow");
        if (glow != null)
        {
            Destroy(glow.gameObject);
        }
    }

    private IEnumerator EnhancedDamageFlash()
    {
        int flashCount = 3; // Number of flashes
        float flashDuration = 0.1f; // Duration of each flash

        for (int i = 0; i < flashCount; i++)
        {
            // Flash to damage color
            for (int j = 0; j < modelRenderers.Length; j++)
            {
                if (modelRenderers[j] != null)
                {
                    modelRenderers[j].material.color = damageFlashColor;
                }
            }

            yield return new WaitForSeconds(flashDuration);

            // Return to original color
            for (int j = 0; j < modelRenderers.Length; j++)
            {
                if (modelRenderers[j] != null && j < originalMaterials.Length)
                {
                    modelRenderers[j].material.color = originalMaterials[j].color;
                }
            }

            yield return new WaitForSeconds(flashDuration);
        }
    }

    private IEnumerator HitShakeEffect()
    {
        Vector3 originalPos = transform.localPosition;
        float shakeIntensity = 0.1f;
        float shakeDuration = 0.3f;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float x = Random.Range(-shakeIntensity, shakeIntensity);
            float z = Random.Range(-shakeIntensity, shakeIntensity);

            transform.localPosition = originalPos + new Vector3(x, 0, z);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    private IEnumerator DeathSequence()
    {
        // Hide health bar
        if (floatingHealthBar != null)
        {
            floatingHealthBar.gameObject.SetActive(false);
        }

        // Fade out over time
        float fadeTime = 2f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            float alpha = 1f - (elapsedTime / fadeTime);

            for (int i = 0; i < modelRenderers.Length; i++)
            {
                if (modelRenderers[i] != null)
                {
                    Color color = modelRenderers[i].material.color;
                    color.a = alpha;
                    modelRenderers[i].material.color = color;
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Disable the model
        gameObject.SetActive(false);
    }
    #endregion
}
