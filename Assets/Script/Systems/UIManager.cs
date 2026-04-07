using UnityEngine;
using TMPro;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI Containers")]
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;

    public override void Init()
    {
        waveInfoPanel.SetActive(true);
    }

    public void UpdateWaveDisplay(int enemyCount)
    {
        currentWaveText.text = LevelManager.Instance.GetWaveInfo();
        enemiesAliveText.text = enemyCount.ToString();
    }
}
