using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    private int current;

    [SerializeField] private UnityEvent onDeath;

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => current;

    private void Awake()
    {
        current = maxHealth;
        OnHealthChanged?.Invoke(current, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        Debug.Log("TakeDamange: "+amount);
        if (amount <= 0) return;

        Debug.Log("Current Health: " + current);
        current = Mathf.Max(0, current - amount);
        OnHealthChanged?.Invoke(current, maxHealth);
        if (current <= 0)
        {
            OnDeath?.Invoke();
            onDeath?.Invoke();

            // Keep SpawnManager tracking in sync so wave completion works.
            if (CompareTag("Enemy") && SpawnManager.Instance != null)
                SpawnManager.Instance.DestroyEnemy(gameObject);
            else
                Destroy(gameObject);
        }
    }
}