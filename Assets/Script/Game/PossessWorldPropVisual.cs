using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Keeps an Aseprite prop on frame 0 until <see cref="PlayOnceThenFrameZero"/> is called (e.g. possess ability).
/// </summary>
[DisallowMultipleComponent]
public class PossessWorldPropVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Tooltip("Aseprite clip/tag name. Leave empty to use the first clip on the controller.")]
    [SerializeField] private string playClipName;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        ShowFrameZero();
    }

    private void OnEnable()
    {
        ShowFrameZero();
    }

    public void ShowFrameZero()
    {
        AnimationClip clip = FindClip(ResolveClipName());
        if (clip == null)
        {
            StopAnimator();
            return;
        }

        StopAnimator();
        clip.SampleAnimation(GetSampleRoot(), 0f);
    }

    public IEnumerator PlayOnceThenFrameZero()
    {
        AnimationClip clip = FindClip(ResolveClipName());
        if (clip == null)
            yield break;

        StopAnimator();

        GameObject sampleRoot = GetSampleRoot();
        float duration = Mathf.Max(clip.length, 0.0001f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float time = Mathf.Min(elapsed, duration - 0.0001f);
            clip.SampleAnimation(sampleRoot, time);
            elapsed += Time.deltaTime;
            yield return null;
        }

        ShowFrameZero();
    }

    /// <summary>Detach from pickup, play once, hold frame 0, then destroy the visual root.</summary>
    public void PlayAbilityPresentationAndDestroy()
    {
        StartCoroutine(PlayOnceThenFrameZeroAndDestroy());
    }

    private IEnumerator PlayOnceThenFrameZeroAndDestroy()
    {
        yield return PlayOnceThenFrameZero();
        Destroy(GetSampleRoot());
    }

    public Transform GetVisualTransform()
    {
        return GetSampleRoot().transform;
    }

    private GameObject GetSampleRoot()
    {
        if (animator != null)
            return animator.gameObject;
        return gameObject;
    }

    private string ResolveClipName()
    {
        if (!string.IsNullOrEmpty(playClipName))
            return playClipName;

        if (animator == null || animator.runtimeAnimatorController == null)
            return null;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        if (clips == null || clips.Length == 0)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
                continue;

            if (clip.name.IndexOf("reverse", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;

            return clip.name;
        }

        return clips[0].name;
    }

    private AnimationClip FindClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName) || animator == null || animator.runtimeAnimatorController == null)
            return null;

        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
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

    private void StopAnimator()
    {
        if (animator == null)
            return;

        animator.speed = 0f;
        animator.enabled = false;
    }
}
