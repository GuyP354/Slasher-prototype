using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a heart bar for a spawned defence on front UI and updates by Health changes.
/// </summary>
public class SpawnedHealthHeartUI : MonoBehaviour
{
    private const int HealthPerHeart = 5;

    [SerializeField] private GameObject heartIconPrefab;
    [SerializeField] private Sprite heartSprite;
    [SerializeField] private Sprite boxSprite;
    [SerializeField] private Color heartColor = Color.white;
    [SerializeField] private Color boxColor = Color.white;
    [SerializeField] private float worldYOffset = 0.12f;
    [SerializeField] private float heartPixelSize = 15f;
    [SerializeField] private float spacingPixels = 2f;
    [SerializeField] private Vector2 boxPaddingPixels = new Vector2(10f, 6f);

    private Health health;
    private RectTransform uiRootRect;
    private RectTransform heartBarRect;
    private readonly List<GameObject> heartIcons = new List<GameObject>();
    private readonly List<Image> heartImages = new List<Image>();
    private Camera cam;
    private int currentActiveHearts;

    public static SpawnedHealthHeartUI CreateFor(GameObject target, Health targetHealth, GameObject heartPrefab, Sprite heart, Sprite box)
    {
        if (target == null || targetHealth == null)
            return null;
        if (heartPrefab == null && heart == null)
            return null;

        SpawnedHealthHeartUI ui = target.AddComponent<SpawnedHealthHeartUI>();
        ui.health = targetHealth;
        ui.heartIconPrefab = heartPrefab;
        ui.heartSprite = heart;
        ui.boxSprite = box;
        ui.Build();
        return ui;
    }

    private void Build()
    {
        Canvas targetCanvas = FindTargetCanvas();
        if (targetCanvas == null)
            return;

        uiRootRect = targetCanvas.GetComponent<RectTransform>();
        cam = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : targetCanvas.worldCamera;
        if (cam == null) cam = Camera.main;
        if (cam == null)
        {
            Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != null && cams[i].isActiveAndEnabled)
                {
                    cam = cams[i];
                    break;
                }
            }
        }
        if (uiRootRect == null || cam == null)
            return;

        int maxHeartCount = Mathf.Max(1, Mathf.CeilToInt(health.MaxHealth / (float)HealthPerHeart));
        float heartsWidth = maxHeartCount * heartPixelSize + Mathf.Max(0f, maxHeartCount - 1) * spacingPixels;
        float totalWidth = heartsWidth + boxPaddingPixels.x * 2f;
        float totalHeight = heartPixelSize + boxPaddingPixels.y * 2f;

        GameObject root = new GameObject("HealthHeartUI", typeof(RectTransform));
        heartBarRect = root.GetComponent<RectTransform>();
        heartBarRect.SetParent(uiRootRect, false);
        heartBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        heartBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        heartBarRect.pivot = new Vector2(0.5f, 0.5f);
        heartBarRect.sizeDelta = new Vector2(totalWidth, totalHeight);

        if (boxSprite != null)
            CreateBackground(root.transform, heartBarRect.sizeDelta);

        RectTransform heartsRow = new GameObject("Hearts", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
        heartsRow.SetParent(root.transform, false);
        heartsRow.anchorMin = new Vector2(0.5f, 0.5f);
        heartsRow.anchorMax = new Vector2(0.5f, 0.5f);
        heartsRow.pivot = new Vector2(0.5f, 0.5f);
        heartsRow.sizeDelta = new Vector2(heartsWidth, heartPixelSize);

        HorizontalLayoutGroup layout = heartsRow.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = spacingPixels;

        for (int i = 0; i < maxHeartCount; i++)
        {
            GameObject icon = CreateHeartIcon(heartsRow, i);
            heartIcons.Add(icon);
            heartImages.Add(icon != null ? icon.GetComponentInChildren<Image>(true) : null);
        }

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;
        HandleHealthChanged(health.CurrentHealth, health.MaxHealth);
    }

    private Canvas FindTargetCanvas()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.name == "UIRoot")
                return c;
            if (fallback == null && (c.renderMode == RenderMode.ScreenSpaceOverlay || c.renderMode == RenderMode.ScreenSpaceCamera))
                fallback = c;
        }
        return fallback;
    }

    private GameObject CreateHeartIcon(Transform parent, int index)
    {
        if (heartIconPrefab != null)
        {
            GameObject heart = Instantiate(heartIconPrefab, parent);
            heart.name = "Heart_" + index;
            RectTransform rt = heart.GetComponent<RectTransform>();
            if (rt == null)
            {
                // Non-UI prefab assigned; fallback to built-in UI icon.
                Destroy(heart);
                return CreateSpriteHeartIcon(parent, index);
            }
            rt.localScale = Vector3.one;
            // Keep prefab hearts free to animate their own visible size per frame.
            LayoutElement existingLayout = heart.GetComponent<LayoutElement>();
            if (existingLayout != null)
                existingLayout.ignoreLayout = true;
            return heart;
        }

        return CreateSpriteHeartIcon(parent, index);
    }

    private GameObject CreateSpriteHeartIcon(Transform parent, int index)
    {
        GameObject icon = new GameObject("Heart_" + index, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        icon.transform.SetParent(parent, false);
        Image image = icon.GetComponent<Image>();
        image.sprite = heartSprite;
        image.color = heartColor;
        image.preserveAspect = false;
        LayoutElement le = icon.GetComponent<LayoutElement>();
        le.preferredWidth = heartPixelSize;
        le.preferredHeight = heartPixelSize;
        return icon;
    }

    private void CreateBackground(Transform parent, Vector2 size)
    {
        GameObject bg = new GameObject("HeartBox", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        Image image = bg.GetComponent<Image>();
        image.sprite = boxSprite;
        image.color = boxColor;
        image.type = Image.Type.Sliced;
    }

    private void HandleHealthChanged(int currentHealth, int maxHealth)
    {
        int activeHearts = Mathf.Max(0, Mathf.CeilToInt(currentHealth / (float)HealthPerHeart));
        currentActiveHearts = activeHearts;
        for (int i = 0; i < heartIcons.Count; i++)
        {
            if (heartIcons[i] != null)
                heartIcons[i].SetActive(i < activeHearts);
        }
    }

    public void PreviewNextHeartExpired(Sprite expiredSprite, float previewSeconds)
    {
        if (expiredSprite == null || heartImages.Count == 0 || currentActiveHearts <= 0)
            return;
        int index = Mathf.Clamp(currentActiveHearts - 1, 0, heartImages.Count - 1);
        StartCoroutine(PreviewExpiredRoutine(index, expiredSprite, Mathf.Max(0f, previewSeconds)));
    }

    private IEnumerator PreviewExpiredRoutine(int heartIndex, Sprite expiredSprite, float previewSeconds)
    {
        if (heartIndex < 0 || heartIndex >= heartImages.Count)
            yield break;

        Image img = heartImages[heartIndex];
        if (img == null || !img.gameObject.activeInHierarchy)
            yield break;

        Sprite original = img.sprite;
        img.sprite = expiredSprite;

        if (previewSeconds > 0f)
            yield return new WaitForSeconds(previewSeconds);

        if (img != null)
            img.sprite = original;
    }

    private void HandleDeath()
    {
        if (heartBarRect != null)
            Destroy(heartBarRect.gameObject);
    }

    private void LateUpdate()
    {
        if (heartBarRect == null || uiRootRect == null || cam == null)
            return;

        Vector3 screen = cam.WorldToScreenPoint(GetWorldAnchorPoint());
        bool visible = screen.z > 0f;
        if (heartBarRect.gameObject.activeSelf != visible)
            heartBarRect.gameObject.SetActive(visible);
        if (!visible) return;

        Camera uiCam = uiRootRect.GetComponentInParent<Canvas>()?.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRootRect, screen, uiCam, out Vector2 localPos);
        heartBarRect.anchoredPosition = localPos;
    }

    private Vector3 GetWorldAnchorPoint()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return transform.position + Vector3.down * worldYOffset;

        bool hasAny = false;
        Bounds bounds = default;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null) continue;
            if (!hasAny)
            {
                bounds = r.bounds;
                hasAny = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (!hasAny)
            return transform.position + Vector3.down * worldYOffset;

        return new Vector3(bounds.center.x, bounds.min.y - worldYOffset, bounds.center.z);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }

        if (heartBarRect != null)
            Destroy(heartBarRect.gameObject);
    }
}
