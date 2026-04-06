using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class UIManager : MonoSingleton<UIManager>, IGameUI
{
    [Header("UI Containers")]
    [SerializeField] private GameObject waveInfoPanel;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentWaveText;
    [SerializeField] private TextMeshProUGUI enemiesAliveText;

    [Header("Wave info — readable in-game UI")]
    [Tooltip("Screen Space Overlay draws HUD on top without URP camera/post stack tinting text.")]
    [SerializeField] private bool forceScreenSpaceOverlay = true;
    [SerializeField] private int waveCanvasSortOrder = 100;
    [Tooltip("Rebuild text materials with TMP Mobile Distance Field (UI) copied from the font atlas — not lit URP surface.")]
    [SerializeField] private bool useMobileDistanceFieldForWaveText = true;
    [SerializeField] private float waveTextOutlineWidth = 0.2f;

    private readonly Dictionary<int, Material> _waveFontMaterials = new Dictionary<int, Material>();

    public override void Init()
    {
        waveInfoPanel.SetActive(true);

        if (waveInfoPanel != null && forceScreenSpaceOverlay)
        {
            Canvas canvas = waveInfoPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = waveCanvasSortOrder;
            }
        }

        if (waveInfoPanel != null)
        {
            foreach (TextMeshProUGUI tmp in waveInfoPanel.GetComponentsInChildren<TextMeshProUGUI>(true))
                ApplyWaveTextStyle(tmp);
        }
    }

    private void OnDestroy()
    {
        foreach (Material m in _waveFontMaterials.Values)
        {
            if (m != null)
                Destroy(m);
        }

        _waveFontMaterials.Clear();
    }

    private void ApplyWaveTextStyle(TextMeshProUGUI tmp)
    {
        if (tmp == null || tmp.font == null)
            return;

        if (useMobileDistanceFieldForWaveText)
        {
            Shader mobile = Shader.Find("TextMeshPro/Mobile/Distance Field");
            if (mobile == null)
                mobile = Shader.Find("TextMeshPro/Distance Field");

            if (mobile != null)
            {
                int id = tmp.font.GetInstanceID();
                if (!_waveFontMaterials.TryGetValue(id, out Material mat) || mat == null)
                {
                    mat = new Material(mobile);
                    mat.CopyPropertiesFromMaterial(tmp.font.material);
                    mat.name = "WaveUI_" + tmp.font.name;
                    _waveFontMaterials[id] = mat;
                }

                tmp.fontSharedMaterial = mat;
            }
        }

        tmp.enableVertexGradient = false;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.faceColor = new Color32(255, 255, 255, 255);
        tmp.outlineWidth = waveTextOutlineWidth;
        tmp.outlineColor = new Color32(15, 15, 15, 240);
    }

    public void UpdateWaveDisplay(int enemyCount)
    {
        ILevelWaveInfo level = LevelManager.Instance;
        currentWaveText.text = level != null ? level.GetWaveInfo() : "0/0";
        enemiesAliveText.text = enemyCount.ToString();
    }
}