using UnityEngine;
using UnityEngine.UI;
using System;

public class LevelButton : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button button;
    [SerializeField] private Image levelImage;
    [SerializeField] private Text levelNumber;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private GameObject completedOverlay;

    [Header("Star Rating")]
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite filledStar;
    [SerializeField] private Sprite emptyStar;

    public event Action OnLevelSelected;

    private int levelId;
    private bool isUnlocked;
    private bool isCompleted;
    private int starRating;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleLevelClick);
    }

    public void SetLevelData(int id)
    {
        levelId = id;

        // Update level number
        if (levelNumber != null)
            levelNumber.text = id.ToString();

        // Update level state
        UpdateLevelState();
    }

    private void UpdateLevelState()
    {
        // Haal level state op uit save data
        int currentRegion = PlayerPrefs.GetInt("CurrentRegion", 1);
        string levelKey = $"Region_{currentRegion}_Level_{levelId}";

        isCompleted = PlayerPrefs.GetInt($"{levelKey}_Completed", 0) == 1;
        starRating = PlayerPrefs.GetInt($"{levelKey}_Stars", 0);
        isUnlocked = levelId == 1 || PlayerPrefs.GetInt($"Region_{currentRegion}_Level_{levelId - 1}_Completed", 0) == 1;

        // Update UI
        button.interactable = isUnlocked;

        if (lockOverlay != null)
            lockOverlay.SetActive(!isUnlocked);

        if (completedOverlay != null)
            completedOverlay.SetActive(isCompleted);

        UpdateStarDisplay();
    }

    private void UpdateStarDisplay()
    {
        if (stars == null) return;

        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] != null)
            {
                stars[i].sprite = i < starRating ? filledStar : emptyStar;
                stars[i].gameObject.SetActive(isCompleted);
            }
        }
    }

    private void HandleLevelClick()
    {
        if (isUnlocked)
        {
            OnLevelSelected?.Invoke();
        }
    }
}
