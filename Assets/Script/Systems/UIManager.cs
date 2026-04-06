using UnityEngine;
using TMPro; // Use TextMeshPro for better quality

public class UIManager : MonoSingleton<UIManager>, IGameUI
{
    [Header("UI Containers")]
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;

    public override void Init()
    {
        // Ensure the panel is active when the game starts
        waveInfoPanel.SetActive(true);
    }

    public void UpdateWaveDisplay(int enemyCount)
    {
        ILevelWaveInfo level = LevelManager.Instance;
        currentWaveText.text = level != null ? level.GetWaveInfo() : "0/0";
        enemiesAliveText.text = enemyCount.ToString();
    }
}