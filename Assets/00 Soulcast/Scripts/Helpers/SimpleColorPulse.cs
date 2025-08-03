using UnityEngine;

public class SimpleColorPulse : MonoBehaviour
{
    public Color baseColor = Color.white;
    public float pulseSpeed = 2f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;

    private Renderer objectRenderer;
    private Material material;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            material = objectRenderer.material;
        }
    }

    void Update()
    {
        if (material == null) return;

        float alpha = Mathf.Lerp(minAlpha, maxAlpha,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);

        Color newColor = baseColor;
        newColor.a = alpha;
        material.color = newColor;
    }
}
