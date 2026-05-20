using System.Collections;
using UnityEngine;

/// <summary>
/// Alternates Backgroundstandstill (idle) and Background creature (plays once), never showing both at once.
/// </summary>
public class BackgroundCreatureCycle : MonoBehaviour
{
    [Header("Objects (same position)")]
    [SerializeField] private GameObject standstillObject;
    [SerializeField] private GameObject creatureObject;

    [Header("Timing")]
    [SerializeField] private float minStandstillSeconds = 5f;
    [SerializeField] private float maxStandstillSeconds = 10f;

    [Header("Creature animation")]
    [SerializeField] private int creatureAnimatorLayer;
    [SerializeField] private string creatureAnimatorStateName;
    [SerializeField] private float maxCreaturePlaySeconds = 30f;

    private Coroutine cycleRoutine;

    private void Awake()
    {
        ResolveObjects();
    }

    private void OnEnable()
    {
        if (standstillObject == null || creatureObject == null)
            ResolveObjects();

        cycleRoutine = StartCoroutine(CycleRoutine());
    }

    private void OnDisable()
    {
        if (cycleRoutine != null)
        {
            StopCoroutine(cycleRoutine);
            cycleRoutine = null;
        }
    }

    private void ResolveObjects()
    {
        if (standstillObject != null && creatureObject != null)
            return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            if (standstillObject == null && child.name.Contains("standstill"))
                standstillObject = child.gameObject;

            if (creatureObject == null && child.name.Contains("creature"))
                creatureObject = child.gameObject;
        }
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            ShowStandstill();
            float wait = Random.Range(minStandstillSeconds, maxStandstillSeconds);
            yield return new WaitForSeconds(wait);

            ShowCreature();
            yield return PlayCreatureOnce();

            ShowStandstill();
        }
    }

    private void ShowStandstill()
    {
        if (creatureObject != null)
            creatureObject.SetActive(false);

        if (standstillObject == null)
            return;

        standstillObject.SetActive(true);
        RestartAnimation(standstillObject);
    }

    private void ShowCreature()
    {
        if (standstillObject != null)
            standstillObject.SetActive(false);

        if (creatureObject == null)
            return;

        creatureObject.SetActive(true);
    }

    private IEnumerator PlayCreatureOnce()
    {
        if (creatureObject == null)
            yield break;

        Animator animator = creatureObject.GetComponent<Animator>();
        if (animator != null)
        {
            yield return PlayAnimatorOnce(animator);
            yield break;
        }

        UIImageFrameAnimator frameAnimator = creatureObject.GetComponent<UIImageFrameAnimator>();
        if (frameAnimator != null)
        {
            yield return PlayFrameAnimatorOnce(frameAnimator);
            yield break;
        }

        yield return new WaitForSeconds(Random.Range(minStandstillSeconds, maxStandstillSeconds));
    }

    private IEnumerator PlayAnimatorOnce(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);

        if (!string.IsNullOrEmpty(creatureAnimatorStateName))
            animator.Play(creatureAnimatorStateName, creatureAnimatorLayer, 0f);
        else
            animator.Play(0, creatureAnimatorLayer, 0f);

        yield return null;

        float elapsed = 0f;
        while (elapsed < maxCreaturePlaySeconds)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(creatureAnimatorLayer);
            if (state.normalizedTime >= 1f && !animator.IsInTransition(creatureAnimatorLayer))
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private static IEnumerator PlayFrameAnimatorOnce(UIImageFrameAnimator frameAnimator)
    {
        bool finished = false;
        void OnFinished() => finished = true;

        frameAnimator.OnNonLoopFinished += OnFinished;
        frameAnimator.SetLoop(false);
        frameAnimator.Play(true);

        float elapsed = 0f;
        const float timeout = 30f;
        while (!finished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        frameAnimator.OnNonLoopFinished -= OnFinished;
    }

    private static void RestartAnimation(GameObject target)
    {
        Animator animator = target.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            animator.Play(0, 0, 0f);
            return;
        }

        UIImageFrameAnimator frameAnimator = target.GetComponent<UIImageFrameAnimator>();
        if (frameAnimator != null)
        {
            frameAnimator.SetLoop(true);
            frameAnimator.Play(true);
        }
    }
}
