using UnityEngine;

public class ObstacleDefense : MonoBehaviour
{
    [Header("Who to attack")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Attack settings")]
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [SerializeField] private float attackCooldownSeconds = 12f;
    [SerializeField] private int damagePerHit = 10;

    [Header("Animation")]
    [Tooltip("How long the looping attack animation plays before returning to standstill for the damage cooldown.")]
    [SerializeField] private float attackLoopDuration = 1f;

    [Header("Placement (Tower preview)")]
    [SerializeField] private float minSeparationFromOtherDefences = 20f;

    private float nextAttackTime;
    private float attackVisualEndTime;
    private CombatRangeAnimator combatAnimator;

    public float MinSeparationFromOtherDefences => minSeparationFromOtherDefences;

    private void Awake()
    {
        combatAnimator = GetComponent<CombatRangeAnimator>();
    }

    private void Update()
    {
        bool targetInRange = FindEnemyInRange() != null;
        UpdateCombatAnimation(targetInRange);

        if (!targetInRange)
            return;

        if (Time.time < attackVisualEndTime)
            return;

        if (Time.time < nextAttackTime)
            return;

        Collider target = FindEnemyInRange();
        if (target == null)
            return;

        float fallbackInterval = attacksPerSecond > 0f ? 1f / attacksPerSecond : 0f;
        float cooldown = attackCooldownSeconds > 0f ? attackCooldownSeconds : fallbackInterval;
        attackVisualEndTime = Time.time + Mathf.Max(0.05f, attackLoopDuration);
        nextAttackTime = Time.time + cooldown;
        SetAttackAnimation(true);
        DealDamage(target);
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
