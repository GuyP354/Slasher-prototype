using UnityEngine;
using TMPro; // Use TextMeshPro for better quality

public class UIManager : MonoSingleton<UIManager>
{
    [Header("UI Containers")]
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;

    public override void Init()
    {
        // No more GetComponentsInChildren needed! 
        // Just ensure you assign the slots in the Unity Inspector.
        base.Init();
    }

    /// <summary>
    /// Updates the wave display. Pass in the actual values!
    /// </summary>
    public void UpdateWaveDisplay(int waveIndex, int enemyCount)
    {
        // Using string interpolation ($"") is cleaner than "+"
        currentWaveText.text = $"Current Wave: {waveIndex}";
        enemiesAliveText.text = $"Enemies Alive: {enemyCount}";
    }
}
