using UnityEngine;
using UnityEngine.UI;

public class StarConfig : MonoBehaviour
{
    [Header("Star Settings")]
    public Sprite filledStarSprite;
    public Color filledStarColor = Color.yellow;

    public Image imageComponent;

    void Awake()
    {
        if (imageComponent == null)
        {
            imageComponent = GetComponent<Image>();
        }
    }

    public void SetFilled(bool filled)
    {
        if (imageComponent == null) return;

        // Always use the same sprite
        imageComponent.sprite = filledStarSprite;

        if (filled)
        {
            // Filled star: full opacity
            imageComponent.color = filledStarColor;
        }
        else
        {
            // Empty star: completely transparent
            imageComponent.color = new Color(filledStarColor.r, filledStarColor.g, filledStarColor.b, 0f);
        }
    }
}
