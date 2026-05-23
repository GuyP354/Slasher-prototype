using UnityEngine;

/// <summary>
/// Planning music between wave elements / before waves; wave music while a wave element is running.
/// Uses one Audio Source so only one track plays at a time.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameMusicController : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip planningMusicClip;
    [SerializeField] private AudioClip waveMusicClip;

    [Header("Volume")]
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;

    private AudioSource source;
    private LevelManager levelManager;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        SubscribeToWaves();

        levelManager = GetComponentInParent<LevelManager>();
        if (levelManager == null)
            levelManager = LevelManager.Instance;

        if (levelManager != null)
            levelManager.WaveCompleted += OnWaveCompleted;
    }

    private void OnDisable()
    {
        UnsubscribeFromWaves();

        if (levelManager != null)
            levelManager.WaveCompleted -= OnWaveCompleted;
    }

    private void Start()
    {
        PlayClip(planningMusicClip);
    }

    private void Update()
    {
        if (source == null || !source.isPlaying)
            return;

        source.volume = musicVolume * GetMasterVolume();
    }

    private void SubscribeToWaves()
    {
        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].WaveElementStarted += OnWaveElementStarted;
            waves[i].WaveElementEnded += OnWaveElementEnded;
        }
    }

    private void UnsubscribeFromWaves()
    {
        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        for (int i = 0; i < waves.Length; i++)
        {
            waves[i].WaveElementStarted -= OnWaveElementStarted;
            waves[i].WaveElementEnded -= OnWaveElementEnded;
        }
    }

    private void OnWaveElementStarted()
    {
        PlayClip(waveMusicClip);
    }

    private void OnWaveElementEnded(bool waveFullyComplete)
    {
        PlayClip(planningMusicClip);
    }

    private void OnWaveCompleted()
    {
        PlayClip(planningMusicClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (source == null || clip == null)
            return;

        if (source.clip == clip && source.isPlaying)
            return;

        source.Stop();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = musicVolume * GetMasterVolume();

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        source.Play();
    }

    private static float GetMasterVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
    }
}
