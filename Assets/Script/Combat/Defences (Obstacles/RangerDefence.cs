using UnityEngine;

/// <summary>
/// Ranger defence: acquires one Enemy-tagged target in range, wind-up, fires a visible projectile, then cooldown.
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
    [Tooltip("Seconds after an enemy is in range before the first shot and between cooldown and the next shot.")]
    [SerializeField] private float damageWindUpSeconds = 3f;
    [SerializeField] private int damagePerHit = 10;
    [SerializeField] private LayerMask overlapLayers = ~0;

    [Header("Projectile")]
    [Tooltip("Child used as the flying bolt visual (e.g. Ranger Projectile). Stays hidden on the tower; copies fly to targets.")]
    [SerializeField] private Transform projectileSpawn;
    [SerializeField] private float projectileSpeed = 35f;
    [SerializeField] private float projectileHitRadius = 0.35f;
    [SerializeField] private Vector3 projectileAimOffset = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Fires the bolt this many seconds before wind-up ends (attack anim keeps playing).")]
    [SerializeField] private float projectileFireLeadSeconds = 0.25f;

    private GameObject projectileTemplate;
    private Transform currentTarget;
    private float nextDamageAllowedTime;
    private float pendingDamageTime;
    private float projectileFireTime;
    private bool firedProjectileThisWindUp;
    private CombatRangeAnimator combatAnimator;

    private void Awake()
    {
        combatAnimator = GetComponent<CombatRangeAnimator>();
        ResolveProjectileSpawn();
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
            firedProjectileThisWindUp = false;
            UpdateCombatAnimation(false);
            return;
        }

        if (Time.time >= nextDamageAllowedTime)
        {
            if (pendingDamageTime <= 0f)
            {
                float windUp = Mathf.Max(0f, damageWindUpSeconds);
                pendingDamageTime = Time.time + windUp;
                float lead = Mathf.Clamp(projectileFireLeadSeconds, 0f, Mathf.Max(0f, windUp - 0.05f));
                projectileFireTime = pendingDamageTime - lead;
                firedProjectileThisWindUp = false;
            }

            if (!firedProjectileThisWindUp && Time.time >= projectileFireTime)
            {
                firedProjectileThisWindUp = true;
                nextDamageAllowedTime = Time.time + GetCooldownDuration();
                FireProjectileAtCurrentTarget();
            }

            if (Time.time >= pendingDamageTime)
                pendingDamageTime = 0f;
        }

        UpdateCombatAnimation(true);
    }

    private void FireProjectileAtCurrentTarget()
    {
        if (currentTarget == null)
            return;

        if (projectileTemplate == null)
        {
            DealDamageToCurrentTarget();
            return;
        }

        Vector3 spawnPos = projectileSpawn != null ? projectileSpawn.position : transform.position;
        GameObject bolt = Instantiate(projectileTemplate, spawnPos, projectileTemplate.transform.rotation);
        bolt.SetActive(true);

        RangerProjectile flight = bolt.GetComponent<RangerProjectile>();
        if (flight == null)
            flight = bolt.AddComponent<RangerProjectile>();

        flight.Launch(currentTarget, damagePerHit, projectileSpeed, projectileHitRadius, projectileAimOffset);
    }

    private void ResolveProjectileSpawn()
    {
        if (projectileSpawn == null)
        {
            projectileSpawn = transform.Find("Ranger Projectile");
            if (projectileSpawn == null)
            {
                Transform[] children = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].name == "Ranger Projectile")
                    {
                        projectileSpawn = children[i];
                        break;
                    }
                }
            }
        }

        if (projectileSpawn == null)
            return;

        projectileTemplate = projectileSpawn.gameObject;
        projectileTemplate.SetActive(false);
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
