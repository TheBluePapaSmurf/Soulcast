using UnityEngine;
using UnityEngine.UI;
using System;

public class RegionButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image regionImage;
    [SerializeField] private Text regionTitle;
    [SerializeField] private Text progressText;

    [Header("Region Data")]
    [SerializeField] private Sprite regionSprite;
    [SerializeField] private string regionName;

    public event Action OnRegionSelected;

    private int regionId;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleRegionClick);
    }

    public void SetRegionData(int id)
    {
        regionId = id;

        // Update UI
        if (regionTitle != null)
            regionTitle.text = $"Regio {id}";

        if (regionImage != null && regionSprite != null)
            regionImage.sprite = regionSprite;

        // Update progress (zou uit save data moeten komen)
        UpdateProgress();
    }

    private void UpdateProgress()
    {
        // Haal progress data op uit SaveManager of PlayerPrefs
        int completedLevels = PlayerPrefs.GetInt($"Region_{regionId}_CompletedLevels", 0);
        int totalLevels = 12;

        if (progressText != null)
            progressText.text = $"{completedLevels}/{totalLevels}";
    }

    private void HandleRegionClick()
    {
        OnRegionSelected?.Invoke();
    }
}
