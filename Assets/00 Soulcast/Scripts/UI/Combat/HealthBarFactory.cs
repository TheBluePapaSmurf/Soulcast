using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class HealthBarFactory
{
    public static GameObject CreateFloatingHealthBarPrefab()
    {
        // Create root Canvas GameObject
        GameObject healthBarRoot = new GameObject("FloatingHealthBar");

        // Add Canvas component
        Canvas canvas = healthBarRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        // Add CanvasScaler
        CanvasScaler scaler = healthBarRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Add GraphicRaycaster
        healthBarRoot.AddComponent<GraphicRaycaster>();

        // Set canvas size
        RectTransform canvasRect = healthBarRoot.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 50);

        // Create background panel
        GameObject background = new GameObject("Background");
        background.transform.SetParent(healthBarRoot.transform, false);

        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black

        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create health slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(healthBarRoot.transform, false);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 100;

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.1f, 0.3f);
        sliderRect.anchorMax = new Vector2(0.9f, 0.7f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Create slider background
        GameObject sliderBg = new GameObject("Background");
        sliderBg.transform.SetParent(sliderObj.transform, false);

        Image sliderBgImage = sliderBg.AddComponent<Image>();
        sliderBgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.offsetMin = Vector2.zero;
        sliderBgRect.offsetMax = Vector2.zero;

        slider.targetGraphic = sliderBgImage;

        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);

        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        // Create health text
        GameObject textObj = new GameObject("HealthText");
        textObj.transform.SetParent(healthBarRoot.transform, false);

        TextMeshProUGUI healthText = textObj.AddComponent<TextMeshProUGUI>();
        healthText.text = "100/100";
        healthText.fontSize = 12;
        healthText.color = Color.white;
        healthText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.7f);
        textRect.anchorMax = new Vector2(1, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Add FloatingHealthBar component
        FloatingHealthBar floatingHealthBar = healthBarRoot.AddComponent<FloatingHealthBar>();
        floatingHealthBar.healthSlider = slider;
        floatingHealthBar.healthText = healthText;
        floatingHealthBar.fillImage = fillImage;
        floatingHealthBar.backgroundImage = bgImage;

        return healthBarRoot;
    }
}
