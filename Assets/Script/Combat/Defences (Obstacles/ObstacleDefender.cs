using UnityEngine;

public class ObstacleDefense : MonoBehaviour
{
    [Header("Who to attack")]
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Attack settings")]
    [SerializeField] private float attackRange = 2.0f;      // melee radius
    [SerializeField] private float attacksPerSecond = 1.0f; // 1 hit per second
    [SerializeField] private int damagePerHit = 10;

    [Header("Placement (Tower preview)")]
    [SerializeField] private float minSeparationFromOtherDefences = 20f;

    private float nextAttackTime;

    public float MinSeparationFromOtherDefences => minSeparationFromOtherDefences;

    private void Update()
    {
        if (Time.time < nextAttackTime) return;

        Collider target = FindEnemyInRange();
        if (target != null)
        {
            nextAttackTime = Time.time + 1f / attacksPerSecond;
            DealDamage(target);
        }
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
        {
            hp.TakeDamage(damagePerHit);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}