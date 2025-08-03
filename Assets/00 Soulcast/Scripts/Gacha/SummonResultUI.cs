using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SummonResultUI : MonoBehaviour
{
    [Header("Result Display")]
    public Transform monsterDisplayParent;
    public GameObject inventoryMonsterCardPrefab; // CHANGED: Use InventoryMonsterCard prefab
    public TextMeshProUGUI resultHeaderText;
    public Button continueButton;

    [Header("Animation Settings")]
    public float cardAnimationDelay = 0.2f;
    public float cardAnimationDuration = 0.5f;

    [Header("Rarity Colors")]
    public Color commonColor = Color.white;
    public Color uncommonColor = Color.green;
    public Color rareColor = Color.blue;
    public Color epicColor = Color.magenta;
    public Color legendaryColor = Color.yellow;

    [Header("Gacha Result Styling")]
    public Color gachaResultCardColor = new Color(1f, 1f, 1f, 0.9f); // Slightly transparent
    public bool hideSelectionBorder = true; // Hide selection border in gacha results
    public bool hideDuplicateInfo = true; // Hide duplicate info in gacha results

    private List<InventoryMonsterCard> currentCards = new List<InventoryMonsterCard>(); // CHANGED: Use InventoryMonsterCard
    private GachaUI gachaUI;

    [System.Obsolete]
    void Start()
    {
        gachaUI = FindAnyObjectByType<GachaUI>();

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    public void DisplayResults(GachaSummonResult result)
    {
        // Ensure this GameObject is active before starting coroutine
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("Cannot start coroutine on inactive GameObject!");
            return;
        }

        StartCoroutine(AnimateResults(result));
    }

    IEnumerator AnimateResults(GachaSummonResult result)
    {
        // Clear previous results
        ClearResults();

        // Set header text
        if (resultHeaderText != null)
        {
            bool isMulti = result.summonedMonsters.Count > 1;
            resultHeaderText.text = isMulti ? $"Summoned {result.summonedMonsters.Count} Monsters!" : "Summoned Monster!";
        }

        // Create and animate cards
        for (int i = 0; i < result.summonedMonsters.Count; i++)
        {
            var gachaMonster = result.summonedMonsters[i];

            // Create card
            GameObject cardObj = Instantiate(inventoryMonsterCardPrefab, monsterDisplayParent);
            InventoryMonsterCard card = cardObj.GetComponent<InventoryMonsterCard>();

            if (card != null)
            {
                // Convert GachaMonster to CollectedMonster for display
                CollectedMonster displayMonster = ConvertToCollectedMonster(gachaMonster);

                // Setup card (pass null for inventoryUI since we don't need inventory functionality)
                card.Setup(displayMonster, null);

                // Apply gacha result styling
                ApplyGachaResultStyling(card, gachaMonster.rarity);

                currentCards.Add(card);

                // Start with card invisible
                cardObj.transform.localScale = Vector3.zero;

                // Animate card appearance
                StartCoroutine(AnimateCardAppearance(cardObj, i * cardAnimationDelay));
            }

            yield return new WaitForSeconds(cardAnimationDelay);
        }

        // Show continue button after all cards are displayed
        yield return new WaitForSeconds(cardAnimationDuration);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
        }
    }

    // NEW: Convert GachaMonster to CollectedMonster for display purposes
    CollectedMonster ConvertToCollectedMonster(GachaMonster gachaMonster)
    {
        // Create a temporary CollectedMonster for display
        CollectedMonster displayMonster = new CollectedMonster(gachaMonster.monsterData);

        // Set appropriate values for gacha result display
        displayMonster.level = 1; // New monsters start at level 1
        displayMonster.currentStarLevel = gachaMonster.monsterData.defaultStarLevel;

        return displayMonster;
    }

    void ApplyGachaResultStyling(InventoryMonsterCard card, MonsterRarity rarity)
    {
        // Use selection border color instead of background
        if (card.selectionBorder != null)
        {
            Image borderImage = card.selectionBorder.GetComponent<Image>();
            if (borderImage != null)
            {
                Color rarityColor = GetRarityColor(rarity);
                borderImage.color = rarityColor;
                card.selectionBorder.gameObject.SetActive(true); // Show border for rarity
            }
        }

        // Hide duplicate indicator for gacha results
        if (hideDuplicateInfo && card.duplicateIndicator != null)
        {
            card.duplicateIndicator.SetActive(false);
        }
    }



    IEnumerator AnimateCardAppearance(GameObject cardObj, float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsedTime = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 targetScale = Vector3.one;

        while (elapsedTime < cardAnimationDuration)
        {
            float t = elapsedTime / cardAnimationDuration;
            // Use easing for smooth animation
            float easedT = Mathf.Sin(t * Mathf.PI * 0.5f);

            cardObj.transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cardObj.transform.localScale = targetScale;
    }

    Color GetRarityColor(MonsterRarity rarity)
    {
        switch (rarity)
        {
            case MonsterRarity.Common: return commonColor;
            case MonsterRarity.Uncommon: return uncommonColor;
            case MonsterRarity.Rare: return rareColor;
            case MonsterRarity.Epic: return epicColor;
            case MonsterRarity.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }

    void ClearResults()
    {
        foreach (var card in currentCards)
        {
            if (card != null && card.gameObject != null)
            {
                Destroy(card.gameObject);
            }
        }
        currentCards.Clear();

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
        }
    }

    void OnContinueClicked()
    {
        if (gachaUI != null)
        {
            gachaUI.ReturnToMainPanel();
        }
    }
}
