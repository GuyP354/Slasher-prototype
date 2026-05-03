using System;
using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private bool destroyGameObjectOnDeath = true;
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
        if (current <= 0)
            return;

        Debug.Log("TakeDamange: "+amount);
        if (amount <= 0) return;

        Debug.Log("Current Health: " + current);
        current = Mathf.Max(0, current - amount);
        OnHealthChanged?.Invoke(current, maxHealth);
        if (current <= 0)
        {
            OnDeath?.Invoke();
            onDeath?.Invoke();
            if (destroyGameObjectOnDeath)
                Destroy(gameObject);
        }
    }
}