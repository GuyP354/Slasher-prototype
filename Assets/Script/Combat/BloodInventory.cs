using UnityEngine;

public class BloodInventory : MonoSingleton<BloodInventory>
{
    [SerializeField] private int startingBlood = 5;

    public int CurrentBlood { get; private set; }

    public override void Init()
    {
        CurrentBlood = Mathf.Max(0, startingBlood);
    }

    public void AddBlood(int amount)
    {
        if (amount <= 0) return;
        CurrentBlood += amount;
    }

    /// <summary>Returns true if blood was spent.</summary>
    public bool TrySpendBlood(int cost)
    {
        if (cost <= 0) return true;
        if (CurrentBlood < cost) return false;
        CurrentBlood -= cost;
        return true;
    }
}
