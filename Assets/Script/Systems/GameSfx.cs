using UnityEngine;

/// <summary>
/// One-shot SFX that respects Master Volume. Does not affect music AudioSources.
/// </summary>
public static class GameSfx
{
    public const float Default3DMinDistance = 1f;
    public const float Default3DMaxDistance = 30f;

    public static float MasterVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));

    public static void Play2D(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        GameObject temp = new GameObject("OneShotSfx2D");
        AudioSource source = temp.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.playOnAwake = false;
        source.volume = MasterVolume * Mathf.Max(0f, volumeScale);
        source.PlayOneShot(clip);
        Object.Destroy(temp, clip.length + 0.05f);
    }

    /// <summary>World-space one-shot at a position (defences, enemies).</summary>
    public static void Play3D(
        AudioClip clip,
        Vector3 position,
        float volumeScale = 1f,
        float minDistance = Default3DMinDistance,
        float maxDistance = Default3DMaxDistance)
    {
        if (clip == null)
            return;

        GameObject temp = new GameObject("OneShotSfx3D");
        temp.transform.position = position;
        AudioSource source = temp.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = Mathf.Max(0.01f, minDistance);
        source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
        source.playOnAwake = false;
        source.volume = MasterVolume * Mathf.Max(0f, volumeScale);
        source.PlayOneShot(clip);
        Object.Destroy(temp, clip.length + 0.05f);
    }
}
