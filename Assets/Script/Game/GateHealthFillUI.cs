using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GateHealthFillUI : MonoBehaviour
{
    [SerializeField] private DefeatZone defeatZone;
    [SerializeField] private bool autoFindDefeatZone = true;

    private Image foregroundImage;
    private Health gateHealth;

    private void Awake()
    {
        foregroundImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        TryBindHealth();
        UpdateFillFromHealth();
    }

    private void Update()
    {
        if (gateHealth == null)
        {
            TryBindHealth();
            UpdateFillFromHealth();
        }
    }

    private void OnDisable()
    {
        UnbindHealth();
    }

    private void TryBindHealth()
    {
        if (gateHealth != null)
            return;

        if (defeatZone == null && autoFindDefeatZone)
            defeatZone = FindFirstObjectByType<DefeatZone>();

        if (defeatZone == null)
            return;

        gateHealth = defeatZone.GetComponent<Health>();
        if (gateHealth == null)
            return;

        gateHealth.OnHealthChanged += HandleHealthChanged;
    }

    private void UnbindHealth()
    {
        if (gateHealth == null)
            return;

        gateHealth.OnHealthChanged -= HandleHealthChanged;
        gateHealth = null;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        if (foregroundImage == null)
            return;

        float fill = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        foregroundImage.fillAmount = Mathf.Clamp01(fill);
    }

    private void UpdateFillFromHealth()
    {
        if (gateHealth == null)
            return;

        HandleHealthChanged(gateHealth.CurrentHealth, gateHealth.MaxHealth);
    }
}
