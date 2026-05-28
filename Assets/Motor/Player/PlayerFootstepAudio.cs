using UnityEngine;

/// <summary>
/// Loops footstep audio at the player while moving. Full 3D spatial audio with distance falloff from the AudioListener (usually on the camera).
/// </summary>
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Clip")]
    [SerializeField] private AudioClip footstepLoop;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private float minMoveSpeed = 0.15f;

    [Header("3D emit point")]
    [Tooltip("Optional feet empty on the player. If set, the AudioSource is created/used on this transform instead of this object.")]
    [SerializeField] private Transform footstepEmitPoint;

    [Header("3D distance")]
    [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 18f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField] [Range(0f, 360f)] private float spread = 45f;

    [Header("Reverb")]
    [SerializeField] private bool enableReverb = true;
    [SerializeField] private AudioReverbPreset reverbPreset = AudioReverbPreset.Forest;
    [SerializeField] [Range(-10000f, 0f)] private float customRoom = -1000f;
    [SerializeField] [Range(0f, 20f)] private float customDecayTime = 1.2f;

    private AudioSource source;
    private GameObject audioHost;
    private Vector3 lastEmitPosition;

    private void Awake()
    {
        audioHost = footstepEmitPoint != null ? footstepEmitPoint.gameObject : gameObject;

        source = audioHost.GetComponent<AudioSource>();
        if (source == null)
            source = audioHost.AddComponent<AudioSource>();

        source.clip = footstepLoop;
        source.loop = true;
        source.playOnAwake = false;
        source.dopplerLevel = 0f;

        Apply3DSettings();
        ApplyReverb();

        lastEmitPosition = audioHost.transform.position;
    }

    private void Update()
    {
        if (footstepLoop == null || source == null)
            return;

        source.volume = GameSfx.MasterVolume * volume;

        Vector3 emitPos = audioHost.transform.position;
        bool shouldPlay = ShouldPlayFootsteps(emitPos);

        if (shouldPlay)
        {
            if (!source.isPlaying)
                source.Play();
        }
        else if (source.isPlaying)
        {
            source.Stop();
        }
    }

    private void Apply3DSettings()
    {
        source.spatialBlend = spatialBlend;
        source.rolloffMode = rolloffMode;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.spread = spread;
    }

    private void ApplyReverb()
    {
        AudioReverbFilter filter = audioHost.GetComponent<AudioReverbFilter>();
        if (!enableReverb)
        {
            if (filter != null)
                filter.enabled = false;
            return;
        }

        if (filter == null)
            filter = audioHost.AddComponent<AudioReverbFilter>();

        filter.enabled = true;
        filter.reverbPreset = reverbPreset;

        if (reverbPreset == AudioReverbPreset.User)
        {
            filter.room = customRoom;
            filter.decayTime = customDecayTime;
        }
    }

    private bool ShouldPlayFootsteps(Vector3 emitPos)
    {
        if (GamePauseMenu.IsPaused)
            return false;

        if (!IsMovementKeyHeld())
            return false;

        Vector3 delta = emitPos - lastEmitPosition;
        lastEmitPosition = emitPos;
        delta.y = 0f;
        float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        return speed >= minMoveSpeed;
    }

    private static bool IsMovementKeyHeld()
    {
        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (source == null && audioHost != null)
            source = audioHost.GetComponent<AudioSource>();

        if (source != null)
            Apply3DSettings();
    }
}
