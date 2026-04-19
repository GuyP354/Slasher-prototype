using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI Containers")]
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI[] additionalWaveTexts;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;
    [SerializeField] private TextMeshProUGUI bloodCountText;
    [SerializeField] private TextMeshProUGUI[] additionalBloodTexts;

    private int _lastBlood = int.MinValue;

    public override void Init()
    {
        waveInfoPanel.SetActive(true);
        ResolveAdditionalWaveTexts();
        ResolveAdditionalBloodTexts();
        ForceUpdateBlood();
    }

    public void UpdateWaveDisplay(int enemyCount)
    {
        string waveInfo = LevelManager.Instance.GetWaveInfo();
        ApplyWaveText(currentWaveText, waveInfo);

        if (additionalWaveTexts != null)
        {
            for (int i = 0; i < additionalWaveTexts.Length; i++)
                ApplyWaveText(additionalWaveTexts[i], waveInfo);
        }

        enemiesAliveText.text = enemyCount.ToString();
        ForceUpdateBlood();
    }

    private void ResolveAdditionalWaveTexts()
    {
        if (additionalWaveTexts != null && additionalWaveTexts.Length > 0)
            return;

        List<TextMeshProUGUI> foundTexts = new List<TextMeshProUGUI>();
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TextMeshProUGUI text = allTexts[i];
            if (text == null || text == currentWaveText)
                continue;

            if (text.name == "CurrentWaveNumber" || text.name == "CurrentWaveNumber (1)")
                foundTexts.Add(text);
        }

        additionalWaveTexts = foundTexts.ToArray();
    }

    private static void ApplyWaveText(TextMeshProUGUI text, string value)
    {
        if (text != null)
            text.text = value;
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
        string bloodValue = current.ToString();
        ApplyWaveText(bloodCountText, bloodValue);

        if (additionalBloodTexts != null)
        {
            for (int i = 0; i < additionalBloodTexts.Length; i++)
                ApplyWaveText(additionalBloodTexts[i], bloodValue);
        }
    }

    private void ForceUpdateBlood()
    {
        _lastBlood = int.MinValue;
        UpdateBloodIfChanged();
    }

    private void ResolveAdditionalBloodTexts()
    {
        if (additionalBloodTexts != null && additionalBloodTexts.Length > 0)
            return;

        List<TextMeshProUGUI> foundTexts = new List<TextMeshProUGUI>();
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        for (int i = 0; i < allTexts.Length; i++)
        {
            TextMeshProUGUI text = allTexts[i];
            if (text == null || text == bloodCountText)
                continue;

            if (text.name == "Blood Count Number" || text.name == "Blood Count Number (1)")
                foundTexts.Add(text);
        }

        additionalBloodTexts = foundTexts.ToArray();
    }
}
