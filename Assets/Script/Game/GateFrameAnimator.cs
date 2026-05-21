using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Plays one Aseprite tag once and holds the last frame.
/// Uses the clip from the Animator controller; the Animator stays off during play (no looping).
/// Set Play State Name to the clip/tag name (e.g. Opening, Reverse).
/// </summary>
[DisallowMultipleComponent]
public class GateFrameAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string playStateName;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        StopAnimator();
    }

    private void OnEnable()
    {
        StopAnimator();
    }

    public IEnumerator PlayOnce()
    {
        AnimationClip clip = FindClip(ResolveClipName());
        if (clip == null)
            yield break;

        StopAnimator();

        float duration = Mathf.Max(clip.length, 0.0001f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float time = Mathf.Min(elapsed, duration - 0.0001f);
            clip.SampleAnimation(gameObject, time);
            elapsed += Time.deltaTime;
            yield return null;
        }

        clip.SampleAnimation(gameObject, duration - 0.0001f);
    }

    private string ResolveClipName()
    {
        if (!string.IsNullOrEmpty(playStateName))
            return playStateName;

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
