using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BloodPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private int bloodValue = 1;

    private void Reset()
    {
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (BloodInventory.Instance != null)
            BloodInventory.Instance.AddBlood(bloodValue);
        Destroy(gameObject);
    }
}
