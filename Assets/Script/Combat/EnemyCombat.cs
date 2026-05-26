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
    [Tooltip("Optional. Uses this sprite/child for drop position; otherwise the first living sprite child.")]
    [SerializeField] private Transform bloodDropAnchor;
    [Tooltip("Raycast straight down from the sprite to find floor height.")]
    [SerializeField] private bool snapBloodToGround = true;
    [SerializeField] private float bloodDropHeightAboveGround = 0.14f;
    [SerializeField] private float bloodDropRayStartHeight = 2f;
    [SerializeField] private float bloodDropRayDistance = 6f;
    [SerializeField] private LayerMask bloodDropGroundMask = ~0;

    private NavMeshAgent agent;
    private Transform objective;
    private Transform currentObstacle;
    private SpriteRenderer livingSprite;

    private float nextScanTime;
    private float nextAttackTime;
    private Health health;
    private PossessedEnemy possessedCached;
    private CombatRangeAnimator combatRangeAnimator;

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
        combatRangeAnimator = GetComponent<CombatRangeAnimator>();
        CacheLivingSprite();
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
        Instantiate(bloodResourcePrefab, GetBloodSpawnPosition(), Quaternion.identity);
    }

    private void CacheLivingSprite()
    {
        livingSprite = null;

        if (bloodDropAnchor != null)
        {
            livingSprite = bloodDropAnchor.GetComponent<SpriteRenderer>();
            if (livingSprite == null)
                livingSprite = bloodDropAnchor.GetComponentInChildren<SpriteRenderer>(true);
            return;
        }

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < sprites.Length; i++)
        {
            SpriteRenderer sr = sprites[i];
            if (sr == null || IsDeathVisualTransform(sr.transform))
                continue;

            livingSprite = sr;
            return;
        }
    }

    private bool IsDeathVisualTransform(Transform t)
    {
        while (t != null)
        {
            if (t.name.IndexOf("Death", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }

        return false;
    }

    private Vector3 GetBloodSpawnPosition()
    {
        Vector3 anchor = GetBloodAnchorPoint();
        anchor += bloodDropOffset;

        if (snapBloodToGround && TryGetGroundY(anchor.x, anchor.z, anchor.y, out float groundY))
            return new Vector3(anchor.x, groundY, anchor.z);

        return new Vector3(anchor.x, anchor.y + bloodDropHeightAboveGround, anchor.z);
    }

    private Vector3 GetBloodAnchorPoint()
    {
        if (livingSprite != null)
        {
            Bounds bounds = livingSprite.bounds;
            return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        if (bloodDropAnchor != null)
            return bloodDropAnchor.position;

        return transform.position;
    }

    private bool TryGetGroundY(float x, float z, float referenceY, out float groundY)
    {
        Vector3 origin = new Vector3(x, referenceY + bloodDropRayStartHeight, z);
        float maxDistance = bloodDropRayStartHeight + bloodDropRayDistance;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, maxDistance, bloodDropGroundMask, QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        bool found = false;
        groundY = referenceY + bloodDropHeightAboveGround;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider col = hits[i].collider;
            if (col == null || IsSelfCollider(col))
                continue;

            if (hits[i].distance >= bestDistance)
                continue;

            bestDistance = hits[i].distance;
            groundY = hits[i].point.y + bloodDropHeightAboveGround;
            found = true;
        }

        return found;
    }

    private bool IsSelfCollider(Collider col)
    {
        Transform hitTransform = col.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
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

        UpdateAttackAnimation();
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

        UpdateAttackAnimation();
    }

    private void UpdateAttackAnimation()
    {
        if (combatRangeAnimator == null)
            return;

        bool engaged = currentObstacle != null
            && PlanarDistance(transform.position, currentObstacle.position) <= attackRange;

        combatRangeAnimator.SetEngaged(engaged);
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