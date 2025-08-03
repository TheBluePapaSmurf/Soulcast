using UnityEngine;

public class PulsingGlow : MonoBehaviour
{
    [Header("Pulsing Settings")]
    public float pulseSpeed = 2f;
    public float minIntensity = 0.3f;
    public float maxIntensity = 1f;
    public bool pulseAlpha = true;
    public bool pulseEmission = false;

    [Header("Color Settings")]
    public Color baseColor = Color.yellow;
    public bool useRandomColor = false;

    private Renderer objectRenderer;
    private Material material;
    private Color originalColor;
    private Color originalEmission;

    void Start()
    {
        // Get renderer and material
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            material = objectRenderer.material;
            originalColor = material.color;

            // Set base color
            if (useRandomColor)
            {
                baseColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f);
            }

            material.color = baseColor;

            // Store original emission if using emission pulsing
            if (pulseEmission && material.HasProperty("_EmissionColor"))
            {
                originalEmission = material.GetColor("_EmissionColor");
            }
        }
    }

    void Update()
    {
        if (material == null) return;

        // Calculate pulse value using sine wave
        float pulseValue = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

        // Apply pulsing to alpha
        if (pulseAlpha)
        {
            Color newColor = baseColor;
            newColor.a = pulseValue;
            material.color = newColor;
        }

        // Apply pulsing to emission (if material supports it)
        if (pulseEmission && material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = baseColor * pulseValue;
            material.SetColor("_EmissionColor", emissionColor);

            // Enable emission keyword for URP
            material.EnableKeyword("_EMISSION");
        }
    }

    void OnDestroy()
    {
        // Restore original color if possible
        if (material != null)
        {
            material.color = originalColor;

            if (pulseEmission && material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", originalEmission);
            }
        }
    }

    // Public methods to control pulsing
    public void SetPulseSpeed(float speed)
    {
        pulseSpeed = speed;
    }

    public void SetPulseRange(float min, float max)
    {
        minIntensity = Mathf.Clamp01(min);
        maxIntensity = Mathf.Clamp01(max);
    }

    public void SetBaseColor(Color color)
    {
        baseColor = color;
        if (material != null)
        {
            material.color = color;
        }
    }
}
