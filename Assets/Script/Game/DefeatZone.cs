using UnityEngine;

[RequireComponent(typeof(GateHealth))]
public class DefeatZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Enemy"))
            LevelManager.Instance.EnemyCrossed();
    }
}
