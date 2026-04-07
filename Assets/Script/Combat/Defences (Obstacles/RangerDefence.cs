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
    [SerializeField] private float attacksPerSecond = 1f;
    [SerializeField] private int damagePerHit = 10;
    [SerializeField] private LayerMask overlapLayers = ~0;

    private Transform currentTarget;
    private float nextAttackTime;

    private void Update()
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
            AcquireTarget();
        }

        if (currentTarget == null || Time.time < nextAttackTime)
            return;

        if (!IsTargetInAttackRange(currentTarget))
            return;

        nextAttackTime = Time.time + (attacksPerSecond > 0f ? 1f / attacksPerSecond : 0f);
        DealDamageToCurrentTarget();
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
