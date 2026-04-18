using UnityEngine;
using TMPro;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI Containers")]
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;
    [SerializeField] private TextMeshProUGUI bloodCountText;

    private int _lastBlood = int.MinValue;

    public override void Init()
    {
        waveInfoPanel.SetActive(true);
        ForceUpdateBlood();
    }

    public void UpdateWaveDisplay(int enemyCount)
    {
        currentWaveText.text = LevelManager.Instance.GetWaveInfo();
        enemiesAliveText.text = enemyCount.ToString();
        ForceUpdateBlood();
    }

    private void Update()
    {
        UpdateBloodIfChanged();
    }

    private void UpdateBloodIfChanged()
    {
        if (bloodCountText == null) return;
        if (BloodInventory.Instance == null) return;

        int current = BloodInventory.Instance.CurrentBlood;
        if (current == _lastBlood) return;
        _lastBlood = current;
        bloodCountText.text = current.ToString();
    }

    private void ForceUpdateBlood()
    {
        _lastBlood = int.MinValue;
        UpdateBloodIfChanged();
    }
}
