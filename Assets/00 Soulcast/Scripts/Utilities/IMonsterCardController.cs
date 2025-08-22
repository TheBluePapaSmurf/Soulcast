/// <summary>
/// Interface for objects that can control UniversalMonsterCard behavior
/// </summary>
public interface IMonsterCardController
{
    void OnMonsterCardClicked(CollectedMonster monster);
    void RefreshCurrentMonsterStats();
    CollectedMonster GetCurrentSelectedMonster();
}
