using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GateHealthFillUI : MonoBehaviour
{
    [SerializeField] private GateHealth gateHealth;
    [SerializeField] private bool autoFindGateHealth = true;
    [SerializeField] private TextMeshProUGUI healthNumberText;
    [SerializeField] private bool autoFindHealthNumberText = true;

    private Image foregroundImage;

    private void Awake()
    {
        foregroundImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        TryBindHealth();
        UpdateDisplayFromHealth();
    }

    private void Update()
    {
        if (gateHealth == null)
        {
            TryBindHealth();
            UpdateDisplayFromHealth();
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

        if (autoFindGateHealth)
        {
            DefeatZone defeatZone = FindFirstObjectByType<DefeatZone>();
            if (defeatZone != null)
                gateHealth = defeatZone.GetComponent<GateHealth>();
        }

        if (gateHealth == null)
            gateHealth = FindFirstObjectByType<GateHealth>();

        if (gateHealth == null)
            return;

        gateHealth.OnHealthChanged += HandleHealthChanged;

        if (healthNumberText == null && autoFindHealthNumberText)
            ResolveHealthNumberText();
    }

    private void ResolveHealthNumberText()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null || texts[i].gameObject == gameObject)
                continue;

            healthNumberText = texts[i];
            return;
        }
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
        if (foregroundImage != null)
        {
            float fill = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
            foregroundImage.fillAmount = Mathf.Clamp01(fill);
        }

        if (healthNumberText != null)
            healthNumberText.text = currentHealth.ToString();
    }

    private void UpdateDisplayFromHealth()
    {
        if (gateHealth == null)
            return;

        HandleHealthChanged(gateHealth.CurrentHealth, gateHealth.MaxHealth);
    }
}
