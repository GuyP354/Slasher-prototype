using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoSingleton<LevelManager>
{
    [SerializeField] private int lifePoint = 10;
    private int currentWave;
    private int ammWave;

    private bool spawnActive = false;
    private bool waveActive = false;
    private List<Wave> waves = new List<Wave>();

    
    public override void Init() 
    {
        
        waves.Clear();
        waves.AddRange(GetComponents<Wave>());
        currentWave = 0;
        ammWave = waves.Count;
    }

    private void Update()
    {
        if (!waveActive)
        {
            if (Input.GetKeyDown(KeyCode.K) && waves.Count > 0)
                StartWave();
        }
        else
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            UIManager.Instance.UpdateWaveDisplay(enemyCount);
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
            UIManager.Instance.UpdateWaveDisplay(0);
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
        return currentWave + "/" + ammWave;
    }

    private void Victory() => Debug.Log("Level Cleared");

    private void Defeat() => Debug.Log("Defeat");
}