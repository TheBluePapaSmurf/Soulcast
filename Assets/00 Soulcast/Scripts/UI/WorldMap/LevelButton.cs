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

    [Header("Battle Sequence Menu")]
    [SerializeField] private BattleSequenceMenu battleSequenceMenu;

    public event Action OnLevelSelected;

    private int levelId;
    private int currentRegion;
    private bool isUnlocked;
    private bool isCompleted;
    private int starRating;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(HandleLevelClick);

        // Find BattleSequenceMenu if not assigned
        if (battleSequenceMenu == null)
            battleSequenceMenu = FindFirstObjectByType<BattleSequenceMenu>();
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

    public void SetRegionData(int regionId)
    {
        currentRegion = regionId;
        UpdateLevelState(); // Refresh state with new region
    }

    private void UpdateLevelState()
    {
        // Haal level state op uit save data
        // Gebruik currentRegion als het is ingesteld, anders fallback naar PlayerPrefs
        int regionToUse = currentRegion > 0 ? currentRegion : PlayerPrefs.GetInt("CurrentRegion", 1);
        string levelKey = $"Region_{regionToUse}_Level_{levelId}";

        isCompleted = PlayerPrefs.GetInt($"{levelKey}_Completed", 0) == 1;
        starRating = PlayerPrefs.GetInt($"{levelKey}_Stars", 0);
        isUnlocked = levelId == 1 || PlayerPrefs.GetInt($"Region_{regionToUse}_Level_{levelId - 1}_Completed", 0) == 1;

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
        if (!isUnlocked) return;

        Debug.Log($"Level {levelId} clicked in region {currentRegion}");

        // Trigger BattleSequenceMenu horizontaal slide-in van rechts
        if (battleSequenceMenu != null)
        {
            int regionToUse = currentRegion > 0 ? currentRegion : PlayerPrefs.GetInt("CurrentRegion", 1);
            battleSequenceMenu.ShowMenu(regionToUse, levelId);
        }
        else
        {
            Debug.LogError("BattleSequenceMenu not found! Make sure it's assigned or exists in the scene.");
            // Fallback to old behavior
            OnLevelSelected?.Invoke();
        }
    }

    // Public properties voor external access
    public int LevelId => levelId;
    public int CurrentRegion => currentRegion;
    public bool IsUnlocked => isUnlocked;
    public bool IsCompleted => isCompleted;
    public int StarRating => starRating;

    // Method om battle sequence menu reference te updaten
    public void SetBattleSequenceMenu(BattleSequenceMenu menu)
    {
        battleSequenceMenu = menu;
    }

    // Context menu voor testing
    [ContextMenu("Test Level Click")]
    private void TestLevelClick()
    {
        if (Application.isPlaying)
        {
            Debug.Log($"Testing level {levelId} click");
            HandleLevelClick();
        }
    }

    // Method voor WorldMapManager om region te setten
    public void InitializeLevel(int regionId, int levelIndex, BattleSequenceMenu menuReference = null)
    {
        currentRegion = regionId;
        SetLevelData(levelIndex);

        if (menuReference != null)
            battleSequenceMenu = menuReference;
    }
}
