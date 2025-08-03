using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class RuneTypeFilterButton : MonoBehaviour
{
    [Header("UI References")]
    public Image runeSetIcon;
    public TextMeshProUGUI runeSetName;
    public TextMeshProUGUI runeCount;
    public Button button;

    [Header("Available Rune Sets")]
    public RuneSetData[] availableRuneSets = new RuneSetData[0];

    private RuneSetData selectedRuneSet;
    private RunePanelUI runePanelUI;
    // REMOVED: private bool isAllSetsButton;

    void Start()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnButtonClicked);

        LoadAvailableRuneSets();
    }

    void LoadAvailableRuneSets()
    {
        var allRuneSets = Resources.LoadAll<RuneSetData>("");

        if (allRuneSets.Length == 0)
        {
            allRuneSets = Resources.FindObjectsOfTypeAll<RuneSetData>()
                .Where(set => set.name != "New Rune Set")
                .ToArray();
        }

        availableRuneSets = allRuneSets;
        Debug.Log($"Loaded {availableRuneSets.Length} rune sets for filter buttons");
    }

    // UPDATED: Removed isAllSetsButton parameter
    public void Initialize(RuneSetData runeSet, RunePanelUI panelUI)
    {
        selectedRuneSet = runeSet;
        runePanelUI = panelUI;

        SetupVisuals();
        UpdateRuneCount();
    }

    void SetupVisuals()
    {
        // REMOVED: All the isAllSetsButton logic
        if (selectedRuneSet != null)
        {
            // Specific rune set button
            if (runeSetName != null)
                runeSetName.text = selectedRuneSet.setName;

            if (runeSetIcon != null)
                runeSetIcon.sprite = selectedRuneSet.setIcon;

            // Optional: Apply set color to button background
            if (selectedRuneSet.setColor != Color.white)
            {
                var buttonImage = GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color tintedColor = selectedRuneSet.setColor;
                    tintedColor.a = 0.3f;
                    buttonImage.color = tintedColor;
                }
            }
        }
    }

    public void UpdateRuneCount()
    {
        if (runePanelUI == null || runeCount == null) return;

        int count = 0;

        // REMOVED: isAllSetsButton check - only count specific rune sets
        if (selectedRuneSet != null)
        {
            count = GetRuneCountBySet(selectedRuneSet);
        }

        runeCount.text = count.ToString();
    }

    int GetRuneCountBySet(RuneSetData runeSet)
    {
        if (PlayerInventory.Instance == null || runeSet == null)
            return 0;

        return PlayerInventory.Instance.ownedRunes
            .Where(rune => rune != null && rune.runeSet == runeSet)
            .Count();
    }

    void OnButtonClicked()
    {
        if (runePanelUI != null)
        {
            runePanelUI.OnRuneSetSelected(selectedRuneSet);
        }
    }

    public static RuneSetData[] GetAllRuneSets()
    {
        var resourceSets = Resources.LoadAll<RuneSetData>("");

        if (resourceSets.Length > 0)
        {
            return resourceSets;
        }

        return Resources.FindObjectsOfTypeAll<RuneSetData>()
            .Where(set => !string.IsNullOrEmpty(set.setName) && set.setName != "New Rune Set")
            .ToArray();
    }
}
