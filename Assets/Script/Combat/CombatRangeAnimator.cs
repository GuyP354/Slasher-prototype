using UnityEngine;



/// <summary>

/// Switches an Animator bool: false = standstill loop, true = attack loop.

/// Enemies: Drive Mode = Scan For Targets (uses Detection Range).

/// Defences: Driven By Combat Script (RangerDefence / ObstacleDefense call SetEngaged).

/// </summary>

[DisallowMultipleComponent]

public class CombatRangeAnimator : MonoBehaviour

{

    public enum DriveMode

    {

        ScanForTargets,

        DrivenByCombatScript

    }



    [SerializeField] private Animator animator;

    [SerializeField] private string engagedBoolParameter = "HasTarget";

    [SerializeField] private DriveMode driveMode = DriveMode.ScanForTargets;



    [Header("Detection (Scan For Targets only)")]

    [SerializeField] private float detectionRange = 6f;

    [SerializeField] private string[] targetTags = { "Enemy" };

    [SerializeField] private LayerMask detectionLayers = ~0;

    [SerializeField] private float scanInterval = 0.15f;

    [SerializeField] private bool usePlanarDistance = true;



    private Health health;

    private int engagedBoolHash;

    private float nextScanTime;

    private bool isEngaged;

    private bool loggedMissingParameter;

    private readonly Collider[] overlapBuffer = new Collider[24];



    private void Awake()

    {

        ResolveAnimator();

        health = GetComponent<Health>();

        engagedBoolHash = Animator.StringToHash(engagedBoolParameter);



        if (GetComponent<RangerDefence>() != null || GetComponent<ObstacleDefense>() != null)

            driveMode = DriveMode.DrivenByCombatScript;

    }



    private void Start()

    {

        ValidateAnimatorParameter();

    }



    private void ResolveAnimator()

    {

        Animator onSelf = GetComponent<Animator>();

        if (onSelf == null)

        {

            animator = null;

            return;

        }



        if (animator == null || animator.gameObject != gameObject)

            animator = onSelf;

    }



    private void OnEnable()

    {

        if (health != null)

            health.OnDeath += HandleDeath;



        SetEngaged(false, forceAnimatorUpdate: true);

        nextScanTime = 0f;

    }



    private void OnDisable()

    {

        if (health != null)

            health.OnDeath -= HandleDeath;

    }



    private void Update()

    {

        if (driveMode != DriveMode.ScanForTargets)

            return;



        if (animator == null || !animator.isActiveAndEnabled)

            return;



        if (Time.time < nextScanTime)

            return;



        nextScanTime = Time.time + scanInterval;

        SetEngaged(HasTargetInRange());

    }



    public void SetEngaged(bool engaged)

    {

        SetEngaged(engaged, forceAnimatorUpdate: false);

    }



    private void SetEngaged(bool engaged, bool forceAnimatorUpdate)

    {

        if (!forceAnimatorUpdate && isEngaged == engaged)

            return;



        if (animator == null || !animator.isActiveAndEnabled)

            return;



        if (!AnimatorHasBoolParameter())

            return;



        isEngaged = engaged;

        animator.SetBool(engagedBoolHash, isEngaged);

        animator.Update(0f);

    }



    private bool AnimatorHasBoolParameter()

    {

        if (animator == null)

            return false;



        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)

        {

            AnimatorControllerParameter p = parameters[i];

            if (p.type == AnimatorControllerParameterType.Bool && p.name == engagedBoolParameter)

                return true;

        }



        if (!loggedMissingParameter)

        {

            loggedMissingParameter = true;

            Debug.LogWarning(

                $"[{name}] Animator Controller is missing Bool parameter \"{engagedBoolParameter}\". " +

                "Add it in the Animator window (Parameters tab) and wire transitions to standstill/attack.",

                this);

        }



        return false;

    }



    private void ValidateAnimatorParameter()

    {

        if (animator == null)

        {

            Debug.LogWarning($"[{name}] CombatRangeAnimator needs an Animator on the same object.", this);

            return;

        }



        if (animator.runtimeAnimatorController == null)

        {

            Debug.LogWarning($"[{name}] Animator has no Controller assigned.", this);

            return;

        }



        AnimatorHasBoolParameter();

    }



    private bool HasTargetInRange()

    {

        if (targetTags == null || targetTags.Length == 0)

            return false;



        int count = Physics.OverlapSphereNonAlloc(

            transform.position,

            detectionRange,

            overlapBuffer,

            detectionLayers,

            QueryTriggerInteraction.Ignore);



        for (int i = 0; i < count; i++)

        {

            Collider hit = overlapBuffer[i];

            if (hit == null)

                continue;



            if (!ColliderMatchesAnyTag(hit))

                continue;



            if (usePlanarDistance)

            {

                if (PlanarDistance(transform.position, hit.transform.position) <= detectionRange)

                    return true;

            }

            else if ((hit.transform.position - transform.position).sqrMagnitude <= detectionRange * detectionRange)

            {

                return true;

            }

        }



        return false;

    }



    private bool ColliderMatchesAnyTag(Collider hit)

    {

        Transform walk = hit.transform;

        while (walk != null)

        {

            for (int t = 0; t < targetTags.Length; t++)

            {

                string tag = targetTags[t];

                if (!string.IsNullOrEmpty(tag) && walk.CompareTag(tag))

                    return true;

            }



            walk = walk.parent;

        }



        return false;

    }



    private static float PlanarDistance(Vector3 a, Vector3 b)

    {

        a.y = 0f;

        b.y = 0f;

        return Vector3.Distance(a, b);

    }



    private void HandleDeath()

    {

        enabled = false;



        if (animator != null)

            animator.enabled = false;

    }



    private void OnDrawGizmosSelected()

    {

        if (driveMode != DriveMode.ScanForTargets)

            return;



        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);

        Gizmos.DrawSphere(transform.position, detectionRange);

    }

}


