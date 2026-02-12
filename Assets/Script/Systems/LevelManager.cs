using UnityEngine;

public class LevelManager : MonoSingleton<LevelManager>
{
    private int lifePoint = 10;
    public void EnemyCrossed()
    {
        Debug.Log("EnemyCrossed");
        lifePoint--;
        if (lifePoint <= 0)
            Defeat();
    }
    private void Defeat()
    {
        // Wipe all the enemies
        // clean the level
        Debug.Log("Defeat");
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            GetComponent<Wave>().StartWave();
    }
}
