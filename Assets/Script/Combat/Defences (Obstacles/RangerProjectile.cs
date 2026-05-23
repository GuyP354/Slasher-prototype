using UnityEngine;

/// <summary>
/// Flies toward a target and applies damage when it reaches the enemy.
/// </summary>
public class RangerProjectile : MonoBehaviour
{
    private Transform target;
    private int damage;
    private float speed;
    private float hitRadius;
    private Vector3 targetOffset;
    private Quaternion spawnRotation;

    public void Launch(Transform enemyTarget, int damageAmount, float travelSpeed, float arrivalRadius, Vector3 aimOffset)
    {
        target = enemyTarget;
        damage = damageAmount;
        speed = Mathf.Max(0.01f, travelSpeed);
        hitRadius = Mathf.Max(0.05f, arrivalRadius);
        targetOffset = aimOffset;
        spawnRotation = transform.rotation;
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 goal = target.position + targetOffset;
        transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.deltaTime);
        transform.rotation = spawnRotation;

        if ((goal - transform.position).sqrMagnitude <= hitRadius * hitRadius)
        {
            ApplyDamage();
            Destroy(gameObject);
        }
    }

    private void ApplyDamage()
    {
        if (target == null)
            return;

        Health hp = target.GetComponentInParent<Health>();
        if (hp == null)
            hp = target.GetComponent<Health>();

        if (hp != null)
            hp.TakeDamage(damage);
    }
}
