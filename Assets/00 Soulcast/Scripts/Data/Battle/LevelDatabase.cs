using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "BattleDatabase", menuName = "Soulcast/Battle Database")]
public class LevelDatabase : ScriptableObject
{
    [Header("All Combat Templates")]
    public List<CombatTemplate> allCombats = new List<CombatTemplate>();

    [Header("Level Organization")]
    public List<RegionData> regions = new List<RegionData>();

    public CombatTemplate GetBattleConfiguration(int regionId, int levelId, int combatTemplateID)
    {
        var region = regions.FirstOrDefault(r => r.regionId == regionId);
        if (region == null) return null;

        var level = region.levels.FirstOrDefault(l => l.levelId == levelId);
        if (level == null) return null;

        if (combatTemplateID <= 0 || combatTemplateID > level.combats.Count)
            return null;

        return level.combats[combatTemplateID - 1];
    }

    public List<CombatTemplate> GetLevelBattles(int regionId, int levelId)
    {
        var region = regions.FirstOrDefault(r => r.regionId == regionId);
        if (region == null) return new List<CombatTemplate>();

        var level = region.levels.FirstOrDefault(l => l.levelId == levelId);
        if (level == null) return new List<CombatTemplate>();

        return level.combats;
    }

    public CombatTemplate GetRandomBattle(BattleDifficulty difficulty)
    {
        var filteredBattles = allCombats.Where(b => b.difficulty == difficulty).ToList();
        if (filteredBattles.Count == 0) return null;

        return filteredBattles[Random.Range(0, filteredBattles.Count)];
    }
}

[System.Serializable]
public class RegionData
{
    public int regionId;
    public string regionName;
    public List<LevelData> levels = new List<LevelData>();
}

[System.Serializable]
public class LevelData
{
    public int levelId;
    public string levelName;
    public List<CombatTemplate> combats = new List<CombatTemplate>();
}
