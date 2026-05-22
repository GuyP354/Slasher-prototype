using UnityEngine;

/// <summary>
/// Ranger defence: acquires one Enemy-tagged target in range, wind-up, damage, then cooldown before the next hit.
/// </summary>
public class RangerDefence : MonoBehaviour
{
    [Header("Who to attack")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Combat")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float attacksPerSecond = 1f;
    [Tooltip("Seconds after each hit before another wind-up can begin. Used when greater than 0.")]
    [SerializeField] private float attackCooldownSeconds = 9f;
    [Tooltip("Seconds after an enemy is in range before the first hit and between cooldown and the next hit.")]
    [SerializeField] private float damageWindUpSeconds = 3f;
    [SerializeField] private int damagePerHit = 10;
    [SerializeField] private LayerMask overlapLayers = ~0;

    private Transform currentTarget;
    private float nextDamageAllowedTime;
    private float pendingDamageTime;
    private CombatRangeAnimator combatAnimator;

    private void Awake()
    {
        combatAnimator = GetComponent<CombatRangeAnimator>();
    }

    private void Update()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            AcquireTarget();
        }

        bool targetInRange = currentTarget != null && IsTargetInAttackRange(currentTarget);

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
                DealDamageToCurrentTarget();
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

    private void AcquireTarget()
    {
        Collider best = FindClosestEnemyColliderInRange();
        currentTarget = best != null ? best.transform : null;
        if (currentTarget != null)
            pendingDamageTime = 0f;
    }

    private Collider FindClosestEnemyColliderInRange()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, overlapLayers, QueryTriggerInteraction.Ignore);
        Collider best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider h = hits[i];
            if (h == null) continue;
            if (!h.CompareTag(enemyTag)) continue;

            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = h;
            }
        }

        return best;
    }

    private bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;
        float d = Vector3.Distance(transform.position, target.position);
        return d <= attackRange + 0.05f;
    }

    private void DealDamageToCurrentTarget()
    {
        if (currentTarget == null) return;

        Health hp = currentTarget.GetComponentInParent<Health>();
        if (hp == null)
            hp = currentTarget.GetComponent<Health>();

        if (hp != null)
            hp.TakeDamage(damagePerHit);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}
