using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int current;
    
    [SerializeField] private UnityEvent onDeath;

    private void Awake()
    {
        current = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        Debug.Log("TakeDamange: "+amount);
        if (amount <= 0) return;

        Debug.Log("Current Health: " + current);
        current -= amount;
        if (current <= 0)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}