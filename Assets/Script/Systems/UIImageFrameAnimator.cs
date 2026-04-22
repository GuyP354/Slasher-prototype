using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIImageFrameAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 10f;
    [SerializeField] private bool loop = true;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = false;

    private Image targetImage;
    private int frameIndex;
    private float timer;
    private bool playing;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        ApplyFrame();
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play(true);
    }

    private void Update()
    {
        if (!playing) return;
        if (frames == null || frames.Length == 0) return;

        float fps = Mathf.Max(0.01f, framesPerSecond);
        float frameDuration = 1f / fps;
        timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        while (timer >= frameDuration)
        {
            timer -= frameDuration;
            frameIndex++;

            if (frameIndex >= frames.Length)
            {
                if (loop)
                {
                    frameIndex = 0;
                }
                else
                {
                    frameIndex = frames.Length - 1;
                    playing = false;
                }
            }

            ApplyFrame();
        }
    }

    public void Play(bool restart = false)
    {
        if (frames == null || frames.Length == 0) return;

        if (restart)
        {
            frameIndex = 0;
            timer = 0f;
            ApplyFrame();
        }

        playing = true;
    }

    public void Stop()
    {
        playing = false;
    }

    private void ApplyFrame()
    {
        if (targetImage == null) return;
        if (frames == null || frames.Length == 0) return;
        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        targetImage.sprite = frames[frameIndex];
    }
}
