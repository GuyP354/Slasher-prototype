using UnityEngine;

public class ObstacleDefense : MonoBehaviour
{
    [Header("Who to attack")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Attack settings")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [Tooltip("Seconds after each hit before another wind-up can begin. Used when greater than 0.")]
    [SerializeField] private float attackCooldownSeconds = 12f;
    [Tooltip("Seconds after an enemy is in range before the first hit and between cooldown and the next hit.")]
    [SerializeField] private float damageWindUpSeconds = 3f;
    [SerializeField] private int damagePerHit = 10;

    [Header("Placement (Tower preview)")]
    [SerializeField] private float minSeparationFromOtherDefences = 20f;

    private float nextDamageAllowedTime;
    private float pendingDamageTime;
    private CombatRangeAnimator combatAnimator;

    public float MinSeparationFromOtherDefences => minSeparationFromOtherDefences;

    private void Awake()
    {
        combatAnimator = GetComponent<CombatRangeAnimator>();
    }

    private void Update()
    {
        Collider target = FindEnemyInRange();
        bool targetInRange = target != null;

        if (!targetInRange)
        {
            pendingDamageTime = 0f;
            UpdateCombatAnimation(false);
            return;
        }

        if (Time.time >= nextDamageAllowedTime)
        {
            if (pendingDamageTime <= 0f)
                pendingDamageTime = Time.time + Mathf.Max(0f, damageWindUpSeconds);

            if (Time.time >= pendingDamageTime)
            {
                pendingDamageTime = 0f;
                nextDamageAllowedTime = Time.time + GetCooldownDuration();
                DealDamage(target);
            }
        }

        UpdateCombatAnimation(true);
    }

    private void UpdateCombatAnimation(bool targetInRange)
    {
        if (!targetInRange)
        {
            SetAttackAnimation(false);
            return;
        }

        bool windingUp = pendingDamageTime > Time.time;
        SetAttackAnimation(windingUp);
    }

    private float GetCooldownDuration()
    {
        if (attackCooldownSeconds > 0f)
            return attackCooldownSeconds;
        if (attacksPerSecond > 0f)
            return 1f / attacksPerSecond;
        return 0f;
    }

    private void SetAttackAnimation(bool showAttackLoop)
    {
        if (combatAnimator != null)
            combatAnimator.SetEngaged(showAttackLoop);
    }

    private Collider FindEnemyInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        Collider best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (!hit.CompareTag(enemyTag)) continue;

            float d = Vector3.SqrMagnitude(hit.transform.position - transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = hit;
            }
        }

        return best;
    }

    private void DealDamage(Collider enemyCollider)
    {
        Health hp = enemyCollider.GetComponentInParent<Health>();
        if (hp != null)
            hp.TakeDamage(damagePerHit);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}
