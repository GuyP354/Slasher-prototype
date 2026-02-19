using UnityEngine;

public class UnitAI : MonoBehaviour
{
    public enum State { Idle, Moving, Attacking, Stunned, Feared }
    public State currentState = State.Idle;

    [Header("Stats")]
    public float agility = 0.5f; // Delay when switching states
    public float aggression = 1.0f; // Speed between attacks
    public float valor = 100f;
    public float attackRange = 1.5f;
    public float detectionRange = 10f;

    private Transform target;
    private float stateTimer;

    void Update()
    {
        if (stateTimer > 0) {
            stateTimer -= Time.deltaTime;
            return; 
        }

        // 1. Priority: Reaction (Stun) - Handled via external TakeDamage()
        if (currentState == State.Stunned) return;

        // 2. Priority: Engagement & Detection
        CheckForTargets();

        // 3. State Execution
        switch (currentState)
        {
            case State.Idle:
                MoveTowardsObjective();
                break;
            case State.Moving:
                MoveToTarget();
                break;
            case State.Attacking:
                PerformAttack();
                break;
        }
    }

    void CheckForTargets()
    {
        // Target Lock: Stay on current target until dead or out of range
        if (target != null && Vector2.Distance(transform.position, target.position) <= detectionRange) return;

        // Find closest enemy
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, detectionRange);
        float closestDist = Mathf.Infinity;
        
        foreach (var enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy")) {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist) {
                    closestDist = dist;
                    target = enemy.transform;
                }
            }
        }

        if (target != null) {
            // Apply Agility Delay (The "???" moment)
            stateTimer = 1.0f - agility; 
            currentState = (closestDist <= attackRange) ? State.Attacking : State.Moving;
        } else {
            currentState = State.Idle;
        }
    }

    void MoveToTarget() { /* Move code here */ }
    void PerformAttack() { /* Attack code here */ }
    void MoveTowardsObjective() { /* Move Right/Left code here */ }
}