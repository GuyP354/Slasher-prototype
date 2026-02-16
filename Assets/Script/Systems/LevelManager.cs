using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoSingleton<LevelManager>
{
    [SerializeField] private int lifePoint = 10;

    private bool spawnActive = false;
    private bool waveActive = false;
    private List<Wave> waves = new List<Wave>();

    
    public override void Init() 
    {
        
        waves.Clear();
        waves.AddRange(GetComponents<Wave>());
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
         
            if (!spawnActive && GameObject.FindGameObjectWithTag("Enemy") == null)
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
            Debug.Log("Wave Start");
            waves[0].StartWave();
            spawnActive = true;
            waveActive = true;
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

    private void Victory() => Debug.Log("Level Cleared");

    private void Defeat() => Debug.Log("Defeat");
}