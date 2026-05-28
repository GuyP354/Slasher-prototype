using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class MenuBackgroundVideoPlayer : MonoBehaviour
{
    [Header("Video Source")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private string streamingAssetsMp4FileName = "menu-background.mp4";

    [Header("UI Target")]
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private RenderTexture targetTexture;

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool muteAudio = true;

    private RenderTexture runtimeTexture;
    private bool prepared;

    private void Awake()
    {
        EnsureVideoPlayer();
        ConfigureVideoPlayer();
        ConfigureRenderTarget();
    }

    private void OnEnable()
    {
        if (!playOnEnable || videoPlayer == null)
            return;

        if (prepared)
        {
            videoPlayer.Play();
            return;
        }

        videoPlayer.Prepare();
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
            videoPlayer.Pause();
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepareCompleted;
            videoPlayer.errorReceived -= HandleErrorReceived;
        }

        if (runtimeTexture != null)
            runtimeTexture.Release();
    }

    private void EnsureVideoPlayer()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
    }

    private void ConfigureVideoPlayer()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;

        if (muteAudio)
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        if (videoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = videoClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = BuildStreamingAssetsVideoPath();
        }

        videoPlayer.prepareCompleted -= HandlePrepareCompleted;
        videoPlayer.prepareCompleted += HandlePrepareCompleted;
        videoPlayer.errorReceived -= HandleErrorReceived;
        videoPlayer.errorReceived += HandleErrorReceived;
    }

    private void ConfigureRenderTarget()
    {
        if (targetRawImage == null)
            targetRawImage = GetComponent<RawImage>();

        if (targetRawImage == null)
            return;

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        if (targetTexture != null)
        {
            videoPlayer.targetTexture = targetTexture;
            targetRawImage.texture = targetTexture;
            return;
        }

        int width = Mathf.Max(1280, Screen.width);
        int height = Mathf.Max(720, Screen.height);
        runtimeTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        runtimeTexture.name = "MenuBackgroundVideoRT";
        runtimeTexture.Create();

        videoPlayer.targetTexture = runtimeTexture;
        targetRawImage.texture = runtimeTexture;
    }

    private string BuildStreamingAssetsVideoPath()
    {
        if (string.IsNullOrWhiteSpace(streamingAssetsMp4FileName))
            return string.Empty;

        string fileName = streamingAssetsMp4FileName.Trim();
        string fullPath = Path.Combine(Application.streamingAssetsPath, fileName);
        return fullPath;
    }

    private void HandlePrepareCompleted(VideoPlayer source)
    {
        prepared = true;
        if (playOnEnable && source != null && gameObject.activeInHierarchy)
            source.Play();
    }

    private static void HandleErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"MenuBackgroundVideoPlayer: video playback error: {message}", source);
    }
}
