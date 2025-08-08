using UnityEngine;
using UnityEngine.UI;

public class HubNavigationManager : MonoBehaviour
{
    [Header("Navigation Buttons")]
    [SerializeField] private Button openBattleButton;
    [SerializeField] private Button missionsButton;
    [SerializeField] private Button monsterCollectionButton;
    [SerializeField] private Button shopButton;

    private void Start()
    {
        SetupNavigationButtons();
    }

    private void SetupNavigationButtons()
    {
        if (openBattleButton != null)
            openBattleButton.onClick.AddListener(OpenWorldMap);
    }

    private void OpenWorldMap()
    {
        SceneTransitionManager.Instance?.LoadWorldMap();
    }
}
