using UnityEngine;

public class LevelManager : MonoSingleton<LevelManager>
{
    private int lifePoint = 10;
    public void EnemyCrossed()
    {
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
}
