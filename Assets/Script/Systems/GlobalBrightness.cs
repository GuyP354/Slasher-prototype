using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a global brightness value using a fullscreen overlay and persists it across scenes.
/// </summary>
[DisallowMultipleComponent]
public class GlobalBrightness : MonoBehaviour
{
    public const string PlayerPrefsKey = "GlobalBrightness";

    private static GlobalBrightness instance;
    private Canvas overlayCanvas;
    private Image overlayImage;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExistsOnBoot()
    {
        EnsureInstance();
    }

    private static GlobalBrightness EnsureInstance()
    {
        if (instance != null)
            return instance;

        GlobalBrightness existing = FindFirstObjectByType<GlobalBrightness>();
        if (existing != null)
        {
            instance = existing;
            instance.InitializeIfNeeded();
            return instance;
        }

        GameObject go = new GameObject("GlobalBrightness");
        instance = go.AddComponent<GlobalBrightness>();
        instance.InitializeIfNeeded();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        DontDestroyOnLoad(gameObject);

        if (overlayCanvas == null)
            CreateOverlay();

        ApplyBrightness(PlayerPrefs.GetFloat(PlayerPrefsKey, 1f), save: false);
    }

    private void CreateOverlay()
    {
        GameObject canvasGO = new GameObject("Brightness Overlay");
        canvasGO.transform.SetParent(transform, false);

        overlayCanvas = canvasGO.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue;

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject imageGO = new GameObject("Brightness Darken");
        imageGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = imageGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayImage = imageGO.AddComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0f);
        overlayImage.raycastTarget = false;
    }

    public static float GetBrightness()
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(PlayerPrefsKey, 1f));
    }

    public static void SetBrightness(float brightness)
    {
        EnsureInstance().ApplyBrightness(brightness, save: true);
    }

    private void ApplyBrightness(float brightness, bool save)
    {
        float clamped = Mathf.Clamp01(brightness);

        if (overlayImage != null)
        {
            // 1.0 = no darkening, 0.0 = fully dark.
            float alpha = 1f - clamped;
            overlayImage.color = new Color(0f, 0f, 0f, alpha);
        }

        if (save)
        {
            PlayerPrefs.SetFloat(PlayerPrefsKey, clamped);
            PlayerPrefs.Save();
        }
    }
}
