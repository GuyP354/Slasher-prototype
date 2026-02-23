using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]
public class DefeatZone : MonoBehaviour
{
    private Health health;
    private void OnTriggerEnter (Collider col)
    {
        Debug.Log(col.name);
        if(col.tag == "Enemy")
        {
            LevelManager.Instance.EnemyCrossed();
            //SpawnManager.Instance.DestroyEnemy(col.gameObject);
        }
    }

    public void OnGateDestroy()
    {
        
    }
}
