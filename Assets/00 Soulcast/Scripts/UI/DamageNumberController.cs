// This script goes on the DamageNumberManager GameObject in the scene
using UnityEngine;
using CartoonFX;

public class DamageNumberController : MonoBehaviour
{
    [Header("Prefab Settings")]
    public GameObject damageNumberPrefab; // Assign your DamageNumber prefab here

    [Header("Animation Settings")]
    public float floatHeight = 2f;
    public float floatDuration = 1.5f;

    [Header("Colors")]
    public Color normalDamageColor = Color.red;
    public Color criticalDamageColor = Color.yellow;
    public Color healColor = Color.green;

    // Singleton for easy access
    public static DamageNumberController Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowDamageNumber(Vector3 worldPosition, int damage, bool isCritical = false)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogError("DamageNumber prefab not assigned to DamageNumberController!");
            return;
        }

        StartCoroutine(CreateDamageNumber(worldPosition, damage, isCritical));
    }

    private System.Collections.IEnumerator CreateDamageNumber(Vector3 position, int damage, bool isCritical)
    {
        // Instantiate at the hit position
        Vector3 spawnPos = position + Vector3.up * 1.5f;
        GameObject damageObj = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity);

        // Get the CFXR component
        CFXR_ParticleText particleText = damageObj.GetComponent<CFXR_ParticleText>();

        if (particleText != null)
        {
            // Set text and colors
            string displayText = isCritical ? $"CRIT! {damage}" : damage.ToString();
            Color textColor = isCritical ? criticalDamageColor : normalDamageColor;
            float size = isCritical ? 1.3f : 1f;

            particleText.UpdateText(displayText, size, textColor, textColor * 0.8f);
        }

        // Animate upward movement
        Vector3 startPos = damageObj.transform.position;
        Vector3 endPos = startPos + Vector3.up * floatHeight;
        float elapsedTime = 0f;

        while (elapsedTime < floatDuration)
        {
            float t = elapsedTime / floatDuration;
            damageObj.transform.position = Vector3.Lerp(startPos, endPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Clean up
        Destroy(damageObj);
    }
}
