using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoSingleton<SpawnManager>
{
    public List<Transform> spawnPoint = new List<Transform>();
    public List<GameObject> spawnPrefabs = new List<GameObject>();
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    public GameObject Spawn(int spawnPrefabIndex)
    {
        return Spawn(spawnPrefabIndex, 0);
    }
    public GameObject Spawn(int spawnPrefabIndex, int spawnPointIndex)
    {
        // 1. Store the newly created enemy in a variable
        GameObject newEnemy = Instantiate(spawnPrefabs[spawnPrefabIndex],
            spawnPoint[spawnPointIndex].position,
            spawnPoint[spawnPointIndex].rotation
        );

        // 2. Add that enemy to your tracking list
        activeEnemies.Add(newEnemy);
        return newEnemy;
    }

    public void DestroyEnemy(GameObject go)
    {
        activeEnemies.Remove(go);
        Destroy(go);
    }

    public int GetEnemiesLeft()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
                activeEnemies.RemoveAt(i);
        }

        return activeEnemies.Count;
    }
    // Temporary
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Spawn(0);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            Spawn(1);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            Spawn(0, 2);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            Spawn(1, 1);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            Spawn(0,1);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            Spawn(1, 2);
        
        //Debug.Log(GetEnemiesLeft());
    }
}