using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class WavePreview : MonoBehaviour
{
    [Header("Wave Info")]
    [SerializeField] private TextMeshProUGUI waveNumberText;
    [SerializeField] private TextMeshProUGUI waveTitleText;
    [SerializeField] private TextMeshProUGUI enemyCountText;

    [Header("Enemy Preview")]
    [SerializeField] private Transform enemyIconContainer;
    [SerializeField] private GameObject enemyIconPrefab; // Simple prefab with Image and Text
    [SerializeField] private int maxEnemyIcons = 6;

    [Header("Wave Stats")]
    [SerializeField] private TextMeshProUGUI totalHPText;
    [SerializeField] private TextMeshProUGUI averageLevelText;
    [SerializeField] private TextMeshProUGUI waveTimeText;

    [Header("Difficulty Indicator")]
    [SerializeField] private Image difficultyBar;
    [SerializeField] private TextMeshProUGUI difficultyText;
    [SerializeField] private Color[] difficultyColors = { Color.green, Color.yellow, Color.orange, Color.red };

    private WaveConfiguration waveConfig;
    private int waveNumber;

    public void Setup(WaveConfiguration wave, int number)
    {
        waveConfig = wave;
        waveNumber = number;

        if (wave == null)
        {
            Debug.LogWarning("WavePreview: Invalid wave configuration");
            return;
        }

        UpdateWaveDisplay();
        CreateEnemyIcons();
        CalculateWaveStats();
    }

    private void UpdateWaveDisplay()
    {
        // Wave number and title
        if (waveNumberText != null)
            waveNumberText.text = $"Wave {waveNumber}";

        if (waveTitleText != null)
        {
            string title = string.IsNullOrEmpty(waveConfig.waveName) ? $"Wave {waveNumber}" : waveConfig.waveName;
            waveTitleText.text = title;
        }

        // Enemy count
        if (enemyCountText != null)
            enemyCountText.text = $"{waveConfig.TotalEnemies} Enemies";

        // Wave time limit
        if (waveTimeText != null && waveConfig.maxWaveTime > 0)
            waveTimeText.text = $"Time: {waveConfig.maxWaveTime:F0}s";
    }

    private void CreateEnemyIcons()
    {
        if (enemyIconContainer == null || enemyIconPrefab == null) return;

        // Clear existing icons
        foreach (Transform child in enemyIconContainer)
        {
            if (child != enemyIconContainer)
                Destroy(child.gameObject);
        }

        // Group enemies by type for cleaner display
        var enemyGroups = waveConfig.enemySpawns
            .Where(spawn => spawn.monsterData != null)
            .GroupBy(spawn => spawn.monsterData)
            .Take(maxEnemyIcons)
            .ToList();

        foreach (var group in enemyGroups)
        {
            var iconObj = Instantiate(enemyIconPrefab, enemyIconContainer);
            var card = iconObj.GetComponent<UniversalMonsterCard>();

            if (card != null)
            {
                var totalCount = group.Sum(spawn => spawn.spawnCount);
                var highestLevel = group.Max(spawn => spawn.monsterLevel);
                var highestStars = group.Max(spawn => spawn.starLevel);

                // Use the new wave preview setup method
                card.SetupForWavePreview(group.Key, highestLevel, highestStars, totalCount);
            }
        }

        // Add "..." indicator if there are more enemy types
        if (waveConfig.enemySpawns.Count > maxEnemyIcons)
        {
            // Create simple text indicator or modify last card
            Debug.Log($"Wave has {waveConfig.enemySpawns.Count - maxEnemyIcons} more enemy types not shown");
        }
    }


    private void SetupSimpleEnemyIcon(GameObject iconObj, MonsterData monsterData, System.Linq.IGrouping<MonsterData, EnemySpawn> group)
    {
        // Get components from the simple prefab
        var image = iconObj.GetComponent<Image>();
        var texts = iconObj.GetComponentsInChildren<TextMeshProUGUI>();

        // Set monster icon
        if (image != null && monsterData.icon != null)
        {
            image.sprite = monsterData.icon;
        }

        // Calculate group stats
        var totalCount = group.Sum(spawn => spawn.spawnCount);
        var highestLevel = group.Max(spawn => spawn.monsterLevel);
        var highestStars = group.Max(spawn => spawn.starLevel);

        // Setup text elements (assuming first is name/count, second is level)
        if (texts.Length > 0)
        {
            if (totalCount > 1)
                texts[0].text = $"{monsterData.monsterName} x{totalCount}";
            else
                texts[0].text = monsterData.monsterName;
        }

        if (texts.Length > 1)
        {
            texts[1].text = $"Lv.{highestLevel} • {highestStars}★";
        }

        // Color coding based on threat level
        if (image != null)
        {
            if (highestStars >= 5 || highestLevel >= 50)
                image.color = new Color(1f, 0.5f, 0.5f); // Light red for boss
            else if (highestStars >= 4 || highestLevel >= 30)
                image.color = new Color(1f, 1f, 0.5f); // Light yellow for elite
            else
                image.color = Color.white; // Normal
        }
    }

    private void CalculateWaveStats()
    {
        if (waveConfig.enemySpawns.Count == 0) return;

        int totalHP = 0;
        int totalEnemies = 0;
        int levelSum = 0;
        float difficultyScore = 0f;

        foreach (var spawn in waveConfig.enemySpawns)
        {
            if (spawn.monsterData == null) continue;

            var stats = spawn.GetEffectiveStats();
            int spawnCount = spawn.spawnCount;

            totalHP += stats.health * spawnCount;
            totalEnemies += spawnCount;
            levelSum += spawn.monsterLevel * spawnCount;

            // Calculate difficulty based on level and stars
            difficultyScore += (spawn.monsterLevel + spawn.starLevel * 5) * spawnCount;
        }

        // Display stats
        if (totalHPText != null)
            totalHPText.text = $"Total HP: {totalHP:N0}";

        if (averageLevelText != null && totalEnemies > 0)
        {
            float avgLevel = (float)levelSum / totalEnemies;
            averageLevelText.text = $"Avg Lv: {avgLevel:F1}";
        }

        // Difficulty assessment
        UpdateDifficultyDisplay(difficultyScore / totalEnemies);
    }

    private void UpdateDifficultyDisplay(float difficultyScore)
    {
        // Normalize difficulty score to 0-1 range
        float normalizedDifficulty = Mathf.Clamp01(difficultyScore / 50f); // Adjust divisor as needed

        if (difficultyBar != null)
        {
            difficultyBar.fillAmount = normalizedDifficulty;

            // Color based on difficulty
            int colorIndex = Mathf.FloorToInt(normalizedDifficulty * (difficultyColors.Length - 1));
            colorIndex = Mathf.Clamp(colorIndex, 0, difficultyColors.Length - 1);
            difficultyBar.color = difficultyColors[colorIndex];
        }

        if (difficultyText != null)
        {
            string[] difficultyLabels = { "Easy", "Normal", "Hard", "Extreme" };
            int labelIndex = Mathf.FloorToInt(normalizedDifficulty * (difficultyLabels.Length - 1));
            labelIndex = Mathf.Clamp(labelIndex, 0, difficultyLabels.Length - 1);
            difficultyText.text = difficultyLabels[labelIndex];
        }
    }

    // Public properties for external access
    public WaveConfiguration WaveConfig => waveConfig;
    public int WaveNumber => waveNumber;
    public int TotalEnemies => waveConfig?.TotalEnemies ?? 0;
}
