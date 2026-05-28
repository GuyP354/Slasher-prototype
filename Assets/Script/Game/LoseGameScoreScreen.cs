using TMPro;
using UnityEngine;

/// <summary>
/// Populates the lose/score canvas with current wave and top-4 leaderboard.
/// Layout is controlled entirely in the scene (Canvas Scaler handles screen size).
/// </summary>
[DisallowMultipleComponent]
public class LoseGameScoreScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI yourScoreText;
    [SerializeField] private TextMeshProUGUI rank1Text;
    [SerializeField] private TextMeshProUGUI rank2Text;
    [SerializeField] private TextMeshProUGUI rank3Text;
    [SerializeField] private TextMeshProUGUI rank4Text;
    [SerializeField] private Color playerOnBoardColor = new Color(0.5f, 1f, 0.99f, 0.96f);

    private TextMeshProUGUI[] rankTexts;
    private Color[] defaultRankColors;

    private void Awake()
    {
        ResolveReferences();
        CacheDefaultColors();
    }

    private void OnEnable()
    {
        RefreshScoreboard();
    }

    private void OnDisable()
    {
        RestoreDefaultRankColors();
        WaveLeaderboard.ClearSessionHighlight();
    }

    public void RefreshScoreboard()
    {
        ResolveReferences();

        int waveReached = 0;
        if (LevelManager.Instance != null)
            waveReached = LevelManager.Instance.CurrentWave;

        if (yourScoreText != null)
            yourScoreText.text = waveReached.ToString();

        int highlightRank = WaveLeaderboard.SubmitRun(waveReached);
        int[] topFour = WaveLeaderboard.LoadTopFour();

        for (int i = 0; i < rankTexts.Length; i++)
        {
            TextMeshProUGUI text = rankTexts[i];
            if (text == null)
                continue;

            text.text = topFour[i].ToString();
            text.color = i == highlightRank ? playerOnBoardColor : defaultRankColors[i];
        }
    }

    private void ResolveReferences()
    {
        if (yourScoreText == null)
        {
            Transform scoreRoot = transform.Find("Your Score number");
            if (scoreRoot != null)
                yourScoreText = scoreRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (rank1Text == null)
            rank1Text = FindRankText("Rank 1 number");
        if (rank2Text == null)
            rank2Text = FindRankText("Rank 2 number");
        if (rank3Text == null)
            rank3Text = FindRankText("Rank 3 number");
        if (rank4Text == null)
            rank4Text = FindRankText("Rank 4 number");

        rankTexts = new[] { rank1Text, rank2Text, rank3Text, rank4Text };
    }

    private TextMeshProUGUI FindRankText(string objectName)
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t != null && t.name == objectName)
                return t.GetComponent<TextMeshProUGUI>();
        }

        return null;
    }

    private void CacheDefaultColors()
    {
        if (rankTexts == null || rankTexts.Length == 0)
            rankTexts = new[] { rank1Text, rank2Text, rank3Text, rank4Text };

        defaultRankColors = new Color[rankTexts.Length];
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null)
                defaultRankColors[i] = rankTexts[i].color;
        }
    }

    private void RestoreDefaultRankColors()
    {
        if (rankTexts == null || defaultRankColors == null)
            return;

        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null)
                rankTexts[i].color = defaultRankColors[i];
        }
    }
}
