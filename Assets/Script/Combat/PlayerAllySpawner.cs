using UnityEngine;

/// <summary>
/// Attach to the Player. Spend blood to spawn allies: Human Shield (Melee) costs 3, Ranger costs 4.
/// </summary>
public class PlayerAllySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject humanShieldMeleePrefab;
    [SerializeField] private GameObject rangerPrefab;

    [Header("Costs")]
    [SerializeField] private int humanShieldBloodCost = 3;
    [SerializeField] private int rangerBloodCost = 4;

    [Header("Spawn")]
    [SerializeField] private float spawnForwardOffset = 2f;
    [SerializeField] private float spawnHeightOffset;

    [Header("Input")]
    [SerializeField] private KeyCode spawnHumanShieldKey = KeyCode.F1;
    [SerializeField] private KeyCode spawnRangerKey = KeyCode.F2;

    private void Update()
    {
        if (Input.GetKeyDown(spawnHumanShieldKey))
            TrySpawn(humanShieldMeleePrefab, humanShieldBloodCost);
        if (Input.GetKeyDown(spawnRangerKey))
            TrySpawn(rangerPrefab, rangerBloodCost);
    }

    private void TrySpawn(GameObject prefab, int cost)
    {
        if (prefab == null) return;
        if (BloodInventory.Instance == null) return;
        if (!BloodInventory.Instance.TrySpendBlood(cost)) return;

        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
        flatForward.Normalize();

        Vector3 pos = transform.position + flatForward * spawnForwardOffset + Vector3.up * spawnHeightOffset;
        Quaternion rot = Quaternion.LookRotation(flatForward, Vector3.up);
        Instantiate(prefab, pos, rot);
    }
}
