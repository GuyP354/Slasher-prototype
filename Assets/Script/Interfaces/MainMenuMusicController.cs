using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays main-menu music whenever the "Main Menu" scene is loaded; stops in all other scenes.
/// Put this on a root-level object with an Audio Source (clip assigned, Play On Awake off, Loop on).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MainMenuMusicController : MonoBehaviour
{
    public static MainMenuMusicController Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 1f;

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;

        if (menuMusicClip != null)
            source.clip = menuMusicClip;
        else if (source.clip == null)
            Debug.LogWarning("[MainMenuMusic] Assign the .mp3 clip on Menu Music Clip or on Audio Source.", this);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (sceneName == mainMenuSceneName)
            Play();
        else
            Stop();
    }

    private void Play()
    {
        if (source == null)
            return;

        if (source.clip == null)
        {
            Debug.LogWarning("[MainMenuMusic] No AudioClip — assign it on Audio Source or Menu Music Clip.", this);
            return;
        }

        if (FindFirstObjectByType<AudioListener>() == null)
        {
            Debug.LogWarning("[MainMenuMusic] No Audio Listener in scene (add one to Main Camera).", this);
            return;
        }

        source.spatialBlend = 0f;
        source.loop = true;
        ApplySourceVolume();

        if (GetMasterVolume() < 0.01f)
            Debug.LogWarning("[MainMenuMusic] MasterVolume is 0 in PlayerPrefs — use Options volume slider or delete that key.", this);

        if (!source.isPlaying)
        {
            if (source.clip.loadState == AudioDataLoadState.Unloaded)
                source.clip.LoadAudioData();
            source.Play();
        }
    }

    private void Stop()
    {
        if (source != null && source.isPlaying)
            source.Stop();
    }

    private void Update()
    {
        if (source == null || !source.isPlaying)
            return;

        ApplySourceVolume();
    }

    private void ApplySourceVolume()
    {
        source.volume = musicVolume * GetMasterVolume();
    }

    private static float GetMasterVolume()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat("MasterVolume", 1f));
    }
}
