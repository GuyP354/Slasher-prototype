using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int current;

    private void Awake()
    {
        current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        current -= amount;
        if (current <= 0)
            Destroy(gameObject);
    }
}