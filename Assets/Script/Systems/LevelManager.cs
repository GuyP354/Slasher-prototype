using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class LevelManager : MonoSingleton<LevelManager>
{
    [SerializeField] private int lifePoint = 10;
    [Header("Victory UI")]
    [SerializeField] private GameObject victoryCanvas;
    [SerializeField] private bool pauseOnVictory = true;

    private int currentWave;
    private int ammWave;

    private bool spawnActive = false;
    private bool waveActive = false;
    private bool levelEnded = false;
    private bool pendingFinalVictoryCheck = false;
    private List<Wave> waves = new List<Wave>();
    private UIManager uiManager;

    
    public override void Init() 
    {
        Time.timeScale = 1f;
        waves.Clear();
        waves.AddRange(GetComponents<Wave>());
        currentWave = 0;
        ammWave = waves.Count;
        uiManager = FindFirstObjectByType<UIManager>();
        levelEnded = false;
        pendingFinalVictoryCheck = false;

        if (victoryCanvas == null)
        {
            GameObject found = GameObject.Find("Victory Canvas");
            if (found != null)
                victoryCanvas = found;
        }

        if (victoryCanvas != null)
            victoryCanvas.SetActive(false);
    }

    private IEnumerator Start()
    {
        // Wait one frame so scene managers/UI are initialized before showing idle wave UI.
        yield return null;

        if (uiManager != null)
            uiManager.UpdateWaveDisplay(0);
    }

    private void Update()
    {
        if (levelEnded)
            return;

        if (waveActive)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (uiManager != null)
                uiManager.UpdateWaveDisplay(enemyCount);

            if (!spawnActive && enemyCount == 0)
            {
                waveActive = false;
                Debug.Log("Wave cleared");
                
                if (pendingFinalVictoryCheck)
                    TryTriggerVictory(enemyCount);
            }
        }

        if (pendingFinalVictoryCheck)
        {
            TryTriggerVictory(GameObject.FindGameObjectsWithTag("Enemy").Length);
        }
    }

    /// <summary>Called when the player presses E at the lever / wave gate. Starts round 1 and the first spawn segment.</summary>
    public void StartWaveFromPlayerInteraction()
    {
        if (waveActive || waves.Count == 0)
            return;

        StartWave();
        waves[0].TryBeginWaitingEvent();
    }

    private void StartWave()
    {
        if (waves.Count > 0)
        {
            currentWave++;
            waves[0].StartWave();
            spawnActive = true;
            waveActive = true;

            if (uiManager != null)
                uiManager.UpdateWaveDisplay(0);
        }
    }

    public void EndWave()
    {
        Debug.Log("Ending Wave");
        if (waves.Count == 0)
            return;

        Wave waveToDestroy = waves[0];
        waves.RemoveAt(0);
        Destroy(waveToDestroy);
        
        spawnActive = false;
        pendingFinalVictoryCheck = waves.Count == 0;
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

        return "0";
    }

    private void Defeat()
    {
        if (levelEnded)
            return;

        levelEnded = true;
        Debug.Log("Defeat");
    }

    private void TryTriggerVictory(int enemyCount)
    {
        if (levelEnded)
            return;

        // Victory only after all wave rounds are consumed and no enemies remain alive.
        if (waves.Count > 0)
            return;
        if (spawnActive)
            return;
        if (enemyCount > 0)
            return;

        pendingFinalVictoryCheck = false;
        Victory();
    }

    private void Victory()
    {
        if (levelEnded)
            return;

        levelEnded = true;
        Debug.Log("Level Cleared");

        if (victoryCanvas != null)
            victoryCanvas.SetActive(true);

        if (pauseOnVictory)
            Time.timeScale = 0f;
    }
}