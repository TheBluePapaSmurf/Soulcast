using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class MonsterRoleUtility
{
    // Get role icon/color
    public static Color GetRoleColor(MonsterRole role)
    {
        switch (role)
        {
            case MonsterRole.Tank: return new Color(0.2f, 0.5f, 0.8f); // Blue
            case MonsterRole.DPS: return new Color(0.8f, 0.2f, 0.2f); // Red
            case MonsterRole.Support: return new Color(0.2f, 0.8f, 0.2f); // Green
            case MonsterRole.Healer: return new Color(0.8f, 0.8f, 0.2f); // Yellow
            case MonsterRole.Assassin: return new Color(0.6f, 0.2f, 0.8f); // Purple
            case MonsterRole.Balanced: return new Color(0.7f, 0.7f, 0.7f); // Gray
            default: return Color.white;
        }
    }

    // Check if team composition is balanced
    public static bool IsBalancedTeam(List<MonsterData> team)
    {
        if (team == null || team.Count == 0) return false;

        var roleCount = team.GroupBy(m => m.role).ToDictionary(g => g.Key, g => g.Count());

        // Basic balanced team rules
        bool hasTank = roleCount.ContainsKey(MonsterRole.Tank);
        bool hasDamage = roleCount.ContainsKey(MonsterRole.DPS) || roleCount.ContainsKey(MonsterRole.Assassin);
        bool hasSupport = roleCount.ContainsKey(MonsterRole.Support) || roleCount.ContainsKey(MonsterRole.Healer);

        return hasTank && hasDamage && (team.Count <= 2 || hasSupport);
    }

    // Get team composition rating
    public static float GetTeamSynergyRating(List<MonsterData> team)
    {
        if (team == null || team.Count <= 1) return 1f;

        float synergyScore = 0f;
        int synergyCount = 0;

        for (int i = 0; i < team.Count; i++)
        {
            for (int j = i + 1; j < team.Count; j++)
            {
                if (team[i].synergyRoles.Contains(team[j].role) ||
                    team[j].synergyRoles.Contains(team[i].role))
                {
                    synergyScore += 1f;
                }
                synergyCount++;
            }
        }

        return synergyCount > 0 ? (synergyScore / synergyCount) : 0f;
    }

    // Get recommended monsters for role
    public static List<MonsterData> GetMonstersByRole(List<MonsterData> allMonsters, MonsterRole role)
    {
        return allMonsters.Where(m => m.role == role).ToList();
    }
}
