using UnityEngine;

/// <summary>
/// Ranger defence: acquires one Enemy-tagged target in range, attacks it until it dies, then picks a new target.
/// </summary>
public class RangerDefence : MonoBehaviour
{
    [Header("Who to attack")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Combat")]
    [SerializeField] private float attackRange = 8f;
    [SerializeField] private float attacksPerSecond = 1f; // fallback if cooldown is 0
    [SerializeField] private float attackCooldownSeconds = 9f;
    [SerializeField] private int damagePerHit = 10;
    [SerializeField] private LayerMask overlapLayers = ~0;

    [Header("Animation")]
    [Tooltip("How long the looping attack animation plays before returning to standstill for the damage cooldown.")]
    [SerializeField] private float attackLoopDuration = 1f;

    private Transform currentTarget;
    private float nextAttackTime;
    private float attackVisualEndTime;
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
        UpdateCombatAnimation(targetInRange);

        if (!targetInRange)
            return;

        if (Time.time < attackVisualEndTime)
            return;

        if (Time.time < nextAttackTime)
            return;

        float fallbackInterval = attacksPerSecond > 0f ? 1f / attacksPerSecond : 0f;
        float cooldown = attackCooldownSeconds > 0f ? attackCooldownSeconds : fallbackInterval;
        attackVisualEndTime = Time.time + Mathf.Max(0.05f, attackLoopDuration);
        nextAttackTime = Time.time + cooldown;
        SetAttackAnimation(true);
        DealDamageToCurrentTarget();
    }

    private void UpdateCombatAnimation(bool targetInRange)
    {
        if (!targetInRange)
        {
            SetAttackAnimation(false);
            return;
        }

        if (Time.time < attackVisualEndTime)
        {
            SetAttackAnimation(true);
            return;
        }

        if (Time.time < nextAttackTime)
        {
            SetAttackAnimation(false);
            return;
        }

        SetAttackAnimation(false);
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
