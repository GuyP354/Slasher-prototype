using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public partial class EnemyCombat : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string objectiveTag = "Target";
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private ArrayList obstacleTags = new ArrayList {"Obstacle", "Target"};

    [Header("Detection (planar / horizontal priority)")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private LayerMask obstacleLayers = ~0; // set to an "Obstacles" layer for best performance
    [SerializeField] private float scanInterval = 0.15f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.6f;
    [SerializeField] private float attacksPerSecond = 1.0f;
    [SerializeField] private int damagePerHit = 10;

    [Header("Loot")]
    [SerializeField] private GameObject bloodResourcePrefab;
    [SerializeField] private Vector3 bloodDropOffset;

    private NavMeshAgent agent;
    private Transform objective;
    private Transform currentObstacle;

    private float nextScanTime;
    private float nextAttackTime;
    private Health health;
    private PossessedEnemy possessedCached;

    // Non-alloc scan buffer (increase if you expect many obstacles clustered)
    private readonly Collider[] hits = new Collider[24];

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        health = GetComponent<Health>();
        possessedCached = GetComponent<PossessedEnemy>();
    }

    /// <summary>Call after <see cref="PossessedEnemy"/> is added at runtime so possession AI applies immediately.</summary>
    public void RefreshPossessedState()
    {
        possessedCached = GetComponent<PossessedEnemy>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += DropBloodOnDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= DropBloodOnDeath;
    }

    private void DropBloodOnDeath()
    {
        if (bloodResourcePrefab == null) return;
        Vector3 pos = transform.position + bloodDropOffset;
        Instantiate(bloodResourcePrefab, pos, Quaternion.identity);
    }

    private void Start()
    {
        CacheObjective();
        if (objective != null)
            agent.SetDestination(objective.position);
    }

    private void Update()
    {
        if (possessedCached != null)
        {
            UpdatePossessedEnemy();
            return;
        }

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
            currentObstacle = FindClosestPossessedEnemyInRange() ?? FindBestObstacleInRange();
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

    private void UpdatePossessedEnemy()
    {
        if (objective == null) CacheObjective();

        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + scanInterval;
            currentObstacle = FindNearestOtherEnemyTransform();
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
        else if (objective != null)
        {
            agent.isStopped = false;
            agent.SetDestination(objective.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private Transform FindClosestPossessedEnemyInRange()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, hits, obstacleLayers);

        Transform best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            PossessedEnemy pe = c.GetComponentInParent<PossessedEnemy>();
            if (pe == null || pe.gameObject == gameObject) continue;

            Transform t = pe.transform;
            float d = PlanarDistance(transform.position, t.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }

        return best;
    }

    private Transform FindNearestOtherEnemyTransform()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, hits, obstacleLayers);

        Transform best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            Transform enemyRoot = FindTaggedRootTransform(c.transform, enemyTag);
            if (enemyRoot == null || enemyRoot.gameObject == gameObject) continue;

            float d = PlanarDistance(transform.position, enemyRoot.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = enemyRoot;
            }
        }

        return best;
    }

    private static Transform FindTaggedRootTransform(Transform t, string tag)
    {
        Transform walk = t;
        while (walk != null)
        {
            if (walk.CompareTag(tag))
                return walk;
            walk = walk.parent;
        }

        return null;
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
            if (!obstacleTags.Contains(t.tag))
            {
                Transform p = t.parent;
                if (p == null || !obstacleTags.Contains(p.tag))
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

        GateHealth gateHp = target.GetComponentInParent<GateHealth>();
        if (gateHp != null)
        {
            gateHp.TakeDamage(damagePerHit);
            return;
        }

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