using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Strong enemy only: on death, deals flat damage to nearby enemies and obstacle defences.
/// </summary>
[DisallowMultipleComponent]
public class StrongEnemyDeathBurst : MonoBehaviour
{
    [SerializeField] private int burstDamage = 15;
    [SerializeField] private float burstRadius = 6f;
    [SerializeField] private LayerMask affectLayers = ~0;
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private string obstacleTag = "Obstacle";
    [SerializeField] private string targetTag = "Target";
    [Header("Explosion VFX")]
    [Tooltip("Optional aseprite visual root used as the death explosion animation.")]
    [SerializeField] private Transform explosionVisualSource;
    [SerializeField] private bool playExplosionVfx = true;
    [SerializeField] private float explosionLifetimeSeconds = 0.8f;
    [Tooltip("Scales the spawned explosion so it roughly matches burst radius.")]
    [SerializeField] private float explosionRadiusScaleMultiplier = 1f;

    private Health health;
    private readonly Collider[] overlapBuffer = new Collider[32];

    private void Awake()
    {
        health = GetComponent<Health>();
        if (explosionVisualSource == null)
            explosionVisualSource = FindExplosionSource();
    }

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += OnDeathBurst;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= OnDeathBurst;
    }

    private void OnDeathBurst()
    {
        if (burstDamage <= 0 || burstRadius <= 0f)
            return;

        SpawnExplosionVfx();

        Vector3 center = transform.position;
        int count = Physics.OverlapSphereNonAlloc(
            center,
            burstRadius,
            overlapBuffer,
            affectLayers,
            QueryTriggerInteraction.Collide);

        var damagedHealth = new HashSet<Health>();

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];
            if (hit == null)
                continue;

            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                continue;

            if (!IsEnemyOrObstacleTarget(hit.transform))
                continue;

            Health targetHealth = hit.GetComponentInParent<Health>();
            if (targetHealth == null || targetHealth == health || targetHealth.IsDead)
                continue;

            if (!damagedHealth.Add(targetHealth))
                continue;

            targetHealth.TakeDamage(burstDamage);
        }
    }

    private void SpawnExplosionVfx()
    {
        if (!playExplosionVfx || explosionVisualSource == null)
            return;

        GameObject instance = Instantiate(explosionVisualSource.gameObject, transform.position, Quaternion.identity);
        instance.SetActive(true);

        float diameter = Mathf.Max(0.01f, burstRadius * 2f * Mathf.Max(0.01f, explosionRadiusScaleMultiplier));
        float sourceDiameter = Mathf.Max(0.01f, EstimateSourceDiameter(explosionVisualSource.gameObject));
        float scale = diameter / sourceDiameter;
        instance.transform.localScale = Vector3.one * scale;

        float life = Mathf.Max(0.05f, explosionLifetimeSeconds);
        Destroy(instance, life);
    }

    private Transform FindExplosionSource()
    {
        Transform direct = transform.Find("Death animations");
        if (direct != null)
            return direct;

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && t.name == "Death animations")
                return t;
        }

        return null;
    }

    private static float EstimateSourceDiameter(GameObject go)
    {
        SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
            return 1f;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return Mathf.Max(b.size.x, b.size.z, b.size.y);
    }

    private bool IsEnemyOrObstacleTarget(Transform t)
    {
        Transform walk = t;
        while (walk != null)
        {
            if (walk.CompareTag(enemyTag))
                return true;

            if (walk.CompareTag(obstacleTag) || walk.CompareTag(targetTag))
                return true;

            if (walk.GetComponent<ObstacleDefense>() != null)
                return true;

            walk = walk.parent;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0.1f, 0.35f);
        Gizmos.DrawSphere(transform.position, burstRadius);
    }
}
