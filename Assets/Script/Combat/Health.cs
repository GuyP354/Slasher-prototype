using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private bool destroyGameObjectOnDeath = true;
    private int current;

    [SerializeField] private UnityEvent onDeath;

    [Header("Death animation (optional)")]
    [Tooltip("When set, plays the death child animation for Death Display Seconds before destroying (overrides instant destroy).")]
    [SerializeField] private bool playDeathAnimationOnDeath;
    [SerializeField] private float deathDisplaySeconds = 2f;
    [Tooltip("Drag the Death animations child here, or leave empty and use Death Child Name.")]
    [SerializeField] private GameObject deathVisualObject;
    [SerializeField] private string deathChildName = "Death animations";
    [SerializeField] private string deathClipName = "Death animations";

    private Transform deathVisualRoot;
    private Animator deathAnimator;
    private bool deathSequenceStarted;

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => current;
    public bool IsDead => current <= 0;

    private void Awake()
    {
        current = maxHealth;
        OnHealthChanged?.Invoke(current, maxHealth);
        CacheDeathVisual();
    }

    public void TakeDamage(int amount)
    {
        if (current <= 0)
            return;

        if (amount <= 0)
            return;

        current = Mathf.Max(0, current - amount);
        OnHealthChanged?.Invoke(current, maxHealth);
        if (current <= 0)
            HandleDeath();
    }

    private void HandleDeath()
    {
        OnDeath?.Invoke();
        onDeath?.Invoke();

        if (TryBeginDeathPresentation())
            return;

        if (destroyGameObjectOnDeath)
            Destroy(gameObject);
    }

    /// <summary>Optional On Death () target: shows the death visual (Health also drives the full sequence).</summary>
    public void ActivateDeathVisual()
    {
        CacheDeathVisual();
        if (deathVisualRoot != null)
            deathVisualRoot.gameObject.SetActive(true);
    }

    private bool TryBeginDeathPresentation()
    {
        if (deathSequenceStarted)
            return true;

        if (!playDeathAnimationOnDeath)
            return false;

        CacheDeathVisual();
        if (deathVisualRoot == null)
            return false;

        deathSequenceStarted = true;
        StartCoroutine(DeathPresentationThenDestroy());
        return true;
    }

    private IEnumerator DeathPresentationThenDestroy()
    {
        DisableLivingGameplay();
        deathVisualRoot.gameObject.SetActive(true);

        AnimationClip clip = FindDeathClip();
        float elapsed = 0f;
        float clipLength = clip != null ? Mathf.Max(clip.length, 0.0001f) : 0f;
        float totalDuration = Mathf.Max(deathDisplaySeconds, clipLength);

        if (clip != null)
            StopDeathAnimator();

        while (elapsed < totalDuration)
        {
            if (clip != null)
            {
                float sampleTime = Mathf.Min(elapsed, clipLength - 0.0001f);
                clip.SampleAnimation(deathVisualRoot.gameObject, sampleTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (clip != null)
            clip.SampleAnimation(deathVisualRoot.gameObject, Mathf.Max(0f, clipLength - 0.0001f));

        Destroy(gameObject);
    }

    private void DisableLivingGameplay()
    {
        gameObject.tag = "Untagged";

        EnemyCombat combat = GetComponent<EnemyCombat>();
        if (combat != null)
            combat.enabled = false;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        CombatRangeAnimator rangeAnimator = GetComponent<CombatRangeAnimator>();
        if (rangeAnimator != null)
            rangeAnimator.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null)
                continue;
            if (deathVisualRoot != null && c.transform.IsChildOf(deathVisualRoot))
                continue;
            c.enabled = false;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null)
                continue;
            if (deathVisualRoot != null && r.transform.IsChildOf(deathVisualRoot))
                continue;
            r.enabled = false;
        }
    }

    private void CacheDeathVisual()
    {
        if (deathVisualRoot != null)
            return;

        if (deathVisualObject != null)
        {
            deathVisualRoot = deathVisualObject.transform;
            deathAnimator = deathVisualRoot.GetComponentInChildren<Animator>(true);
            deathVisualRoot.gameObject.SetActive(false);
            return;
        }

        if (string.IsNullOrEmpty(deathChildName))
            return;

        Transform found = transform.Find(deathChildName);
        if (found == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == deathChildName)
                {
                    found = children[i];
                    break;
                }
            }
        }

        if (found == null)
            return;

        deathVisualRoot = found;
        deathAnimator = deathVisualRoot.GetComponentInChildren<Animator>(true);
        deathVisualRoot.gameObject.SetActive(false);
    }

    private AnimationClip FindDeathClip()
    {
        string clipName = ResolveClipName();
        if (string.IsNullOrEmpty(clipName) || deathAnimator == null || deathAnimator.runtimeAnimatorController == null)
            return null;

        AnimationClip[] clips = deathAnimator.runtimeAnimatorController.animationClips;
        if (clips == null)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip != null && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase))
                return clip;
        }

        return null;
    }

    private string ResolveClipName()
    {
        if (!string.IsNullOrEmpty(deathClipName))
            return deathClipName;

        if (deathAnimator == null || deathAnimator.runtimeAnimatorController == null)
            return null;

        AnimationClip[] clips = deathAnimator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return null;

        return clips[0].name;
    }

    private void StopDeathAnimator()
    {
        if (deathAnimator == null)
            return;

        deathAnimator.speed = 0f;
        deathAnimator.enabled = false;
    }
}
