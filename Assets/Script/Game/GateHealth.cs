using System;
using UnityEngine;

public class GateHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 400;

    [Header("Lose UI")]
    [SerializeField] private GameObject loseGameCanvas;
    [SerializeField] private bool pauseOnDefeat = true;

    private int current;
    private bool isDefeated;

    public event Action<int, int> OnHealthChanged;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => current;
    public bool IsDefeated => isDefeated;

    private void Awake()
    {
        current = maxHealth;

        if (loseGameCanvas == null)
        {
            GameObject found = GameObject.Find("Lose Game Canvas");
            if (found != null)
                loseGameCanvas = found;
        }

        if (loseGameCanvas != null)
            loseGameCanvas.SetActive(false);

        OnHealthChanged?.Invoke(current, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDefeated || current <= 0)
            return;

        if (amount <= 0)
            return;

        current = Mathf.Max(0, current - amount);
        OnHealthChanged?.Invoke(current, maxHealth);

        if (current <= 0)
            TriggerDefeat();
    }

    private void TriggerDefeat()
    {
        if (isDefeated)
            return;

        isDefeated = true;

        if (loseGameCanvas != null)
            loseGameCanvas.SetActive(true);

        if (pauseOnDefeat)
            Time.timeScale = 0f;
    }
}
