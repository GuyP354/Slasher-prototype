using UnityEngine;
using UnityEngine.AI;

public partial class EnemyCombat : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string objectiveTag = "Target";
    [SerializeField] private string obstacleTag = "Obstacle";

    [Header("Detection (planar / horizontal priority)")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private LayerMask obstacleLayers = ~0; // set to an "Obstacles" layer for best performance
    [SerializeField] private float scanInterval = 0.15f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [SerializeField] private int damagePerHit = 10;

    private NavMeshAgent agent;
    private Transform objective;
    private Transform currentObstacle;

    private float nextScanTime;
    private float nextAttackTime;

    // Non-alloc scan buffer (increase if you expect many obstacles clustered)
    private readonly Collider[] hits = new Collider[24];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        CacheObjective();
        if (objective != null)
            agent.SetDestination(objective.position);
    }

    private void Update()
    {
        if (objective == null) CacheObjective();
        if (objective == null) return;

        if (ObjectiveReached())
        {
            agent.isStopped = true;
            return;
        }

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            currentObstacle = FindBestObstacleInRange();
        }

        if (currentObstacle != null)
        {
            float planarToObstacle = PlanarDistance(transform.position, currentObstacle.position);

            if (planarToObstacle <= attackRange)
            {
                agent.isStopped = true;
                TryAttack(currentObstacle);
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(currentObstacle.position);
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(objective.position);
        }
    }

    private void CacheObjective()
    {
        GameObject obj = GameObject.FindGameObjectWithTag(objectiveTag);
        objective = obj ? obj.transform : null;
    }

    private bool ObjectiveReached()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath) return false;
        return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
    }

    // Horizontal/planar distance (ignores Y)
    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private Transform FindBestObstacleInRange()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, hits, obstacleLayers);

        Transform best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            Transform t = c.transform;

            // Allow obstacle colliders on child objects
            if (!t.CompareTag(obstacleTag))
            {
                Transform p = t.parent;
                if (p == null || !p.CompareTag(obstacleTag))
                    continue;

                t = p;
            }

            float d = PlanarDistance(transform.position, t.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    private void TryAttack(Transform target)
    {
        if (attacksPerSecond <= 0f) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + (1f / attacksPerSecond);

        // Deal damage if the obstacle has Health (below). If not, nothing happens.
        Health hp = target.GetComponentInParent<Health>();
        if (hp != null)
            hp.TakeDamage(damagePerHit);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
        Gizmos.DrawSphere(transform.position, detectionRange);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        Gizmos.DrawSphere(transform.position, attackRange);
    }
}