using UnityEngine;
using TMPro; // Use TextMeshPro for better quality

public class UIManager : MonoSingleton<UIManager>
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
        // levelManager.GetWaveInfo() already provides "current/total", 
        // so we just format the label around it.
        currentWaveText.text = LevelManager.Instance.GetWaveInfo();
        enemiesAliveText.text = enemyCount.ToString();
    }
}