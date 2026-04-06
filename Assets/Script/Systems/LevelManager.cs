using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LevelManager : MonoSingleton<LevelManager>, ILevelWaveInfo
{
    [SerializeField] private int lifePoint = 10;
    private int currentWave;
    private int ammWave;

    private bool spawnActive = false;
    private bool waveActive = false;
    private List<Wave> waves = new List<Wave>();
    private IGameUI uiManager;

    
    public override void Init() 
    {
        
        waves.Clear();
        waves.AddRange(GetComponents<Wave>());
        currentWave = 0;
        ammWave = waves.Count;
        uiManager = FindFirstObjectByType<UIManager>();
    }

    private IEnumerator Start()
    {
        // Wait one frame so scene managers/UI are initialized before starting.
        yield return null;

        if (!waveActive && waves.Count > 0)
            StartWave();
    }

    private void Update()
    {
        if (waveActive)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (uiManager != null)
                uiManager.UpdateWaveDisplay(enemyCount);

            if (!spawnActive && enemyCount == 0)
            {
                waveActive = false;
                Debug.Log("Wave cleared");
                
                if (waves.Count == 0)
                    Victory();
            }
        }
    }

    private void StartWave()
    {
        if (waves.Count > 0)
        {
            currentWave++; // Increment the counter
            waves[0].StartWave();
            spawnActive = true;
            waveActive = true;
            
            // Initial UI Update
            if (uiManager != null)
                uiManager.UpdateWaveDisplay(0);
        }
    }

    public void EndWave()
    {
        Debug.Log("Ending Wave");
        
        Wave waveToDestroy = waves[0];
        waves.RemoveAt(0);
        Destroy(waveToDestroy);
        
        spawnActive = false;
    }

    public void EnemyCrossed()
    {
        lifePoint--;
        if (lifePoint <= 0) 
            Defeat();
    }

    public string GetWaveInfo()
    {
        if (waveActive && waves.Count > 0)
            return waves[0].GetEventInfo();

        return "0/0";
    }

    private void Victory() => Debug.Log("Level Cleared");

    private void Defeat() => Debug.Log("Defeat");
}