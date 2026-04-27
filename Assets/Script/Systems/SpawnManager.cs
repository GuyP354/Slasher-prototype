using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoSingleton<SpawnManager>
{
    public List<Transform> spawnPoint = new List<Transform>();
    public List<GameObject> spawnPrefabs = new List<GameObject>();
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    public void Spawn(int spawnPrefabIndex)
    {
        Spawn(spawnPrefabIndex, 0);
    }
    public void Spawn(int spawnPrefabIndex, int spawnPointIndex)
    {
        // 1. Store the newly created enemy in a variable
        GameObject newEnemy = Instantiate(spawnPrefabs[spawnPrefabIndex],
            spawnPoint[spawnPointIndex].position,
            spawnPoint[spawnPointIndex].rotation
        );

        // 2. Add that enemy to your tracking list
        activeEnemies.Add(newEnemy);
    }

    public void DestroyEnemy(GameObject go)
    {
        activeEnemies.Remove(go);
        Destroy(go);
    }

    public int GetEnemiesLeft()
    {
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