using System.Collections;
using UnityEngine;

public class rot : MonoBehaviour
{
    [SerializeField] private float minSecondsBetweenDecay = 100f;
    [SerializeField] private float maxSecondsBetweenDecay = 300f;
    [SerializeField] private int damagePerDecayTick = 5;
    [SerializeField] private float expiredPreviewSeconds = 0.5f;
    [SerializeField] private Sprite heartExpiredSprite;

    private Health health;
    private SpawnedHealthHeartUI heartUi;

    public void Configure(
        Health targetHealth,
        SpawnedHealthHeartUI targetHeartUi,
        Sprite expiredSprite,
        float minSeconds,
        float maxSeconds,
        int damageAmount,
        float previewSeconds)
    {
        health = targetHealth;
        heartUi = targetHeartUi;
        heartExpiredSprite = expiredSprite;
        minSecondsBetweenDecay = minSeconds;
        maxSecondsBetweenDecay = maxSeconds;
        damagePerDecayTick = damageAmount;
        expiredPreviewSeconds = previewSeconds;
    }

    private void Start()
    {
        if (health == null)
            health = GetComponentInChildren<Health>(true);
        if (heartUi == null)
            heartUi = GetComponent<SpawnedHealthHeartUI>();

        if (health != null)
            StartCoroutine(DecayLoop());
    }

    private IEnumerator DecayLoop()
    {
        while (health != null && health.CurrentHealth > 0)
        {
            float minS = Mathf.Max(0.1f, minSecondsBetweenDecay);
            float maxS = Mathf.Max(minS, maxSecondsBetweenDecay);
            yield return new WaitForSeconds(Random.Range(minS, maxS));

            if (health == null || health.CurrentHealth <= 0)
                yield break;

            if (heartUi != null && heartExpiredSprite != null)
                heartUi.PreviewNextHeartExpired(heartExpiredSprite, expiredPreviewSeconds);

            if (expiredPreviewSeconds > 0f)
                yield return new WaitForSeconds(expiredPreviewSeconds);

            if (health == null || health.CurrentHealth <= 0)
                yield break;

            health.TakeDamage(Mathf.Max(1, damagePerDecayTick));
        }
    }
}
