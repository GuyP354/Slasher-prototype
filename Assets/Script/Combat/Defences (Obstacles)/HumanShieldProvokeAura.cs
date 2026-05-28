using UnityEngine;

/// <summary>
/// On each Human Shield in the level: periodically pulls nearby enemies off the gate to attack this shield.
/// </summary>
public class HumanShieldProvokeAura : MonoBehaviour
{
    [Header("Placement")]
    [SerializeField] private AudioClip placementSound;
    [SerializeField] [Range(0f, 4f)] private float placementSoundVolume = 2.5f;
    [SerializeField] private float placementSoundMinDistance = 8f;
    [SerializeField] private float placementSoundMaxDistance = 50f;

    [SerializeField] private float pulseIntervalSeconds = 20f;
    [SerializeField] private float provokeRadius = 25f;
    [SerializeField] private float provokeDurationSeconds = 5f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private float nextPulseTime;
    private readonly Collider[] overlapBuffer = new Collider[32];

    private void Start()
    {
        PlayPlacementSound();
        nextPulseTime = Time.time + Random.Range(0f, pulseIntervalSeconds);
    }

    private void Update()
    {
        if (Time.time < nextPulseTime)
            return;

        nextPulseTime = Time.time + pulseIntervalSeconds;
        PulseProvoke();
    }

    private void PulseProvoke()
    {
        if (provokeRadius <= 0f || provokeDurationSeconds <= 0f)
            return;

        Health selfHealth = GetComponent<Health>();
        if (selfHealth != null && selfHealth.IsDead)
            return;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            provokeRadius,
            overlapBuffer,
            enemyLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null)
                continue;

            EnemyCombat combat = GetEnemyCombat(col);
            if (combat != null)
                combat.ProvokeTowardHumanShield(transform, provokeDurationSeconds, provokeRadius);
        }
    }

    private void PlayPlacementSound()
    {
        if (placementSound == null)
            return;

        GameSfx.Play3D(
            placementSound,
            transform.position,
            placementSoundVolume,
            placementSoundMinDistance,
            placementSoundMaxDistance);
    }

    private static EnemyCombat GetEnemyCombat(Collider col)
    {
        Transform walk = col.transform;
        while (walk != null)
        {
            if (walk.CompareTag("Enemy"))
            {
                EnemyCombat combat = walk.GetComponent<EnemyCombat>();
                if (combat != null)
                    return combat;
            }

            walk = walk.parent;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, provokeRadius);
    }
}
