using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Persistent top-4 wave leaderboard (Rank 1 = highest).
/// </summary>
public static class WaveLeaderboard
{
    private const string KeyPrefix = "Slasher_WaveLeaderboard_";
    private const int SlotCount = 4;

    private static int sessionHighlightRank = -1;

    public static int SessionHighlightRank => sessionHighlightRank;

    public static void ClearSessionHighlight()
    {
        sessionHighlightRank = -1;
    }

    public static int[] LoadTopFour()
    {
        int[] scores = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++)
            scores[i] = PlayerPrefs.GetInt(KeyPrefix + i, 0);
        return scores;
    }

    /// <summary>
    /// Inserts this run into the saved top four. Returns rank index (0–3) if it made the board, otherwise -1.
    /// </summary>
    public static int SubmitRun(int waveReached)
    {
        waveReached = Mathf.Max(0, waveReached);
        int[] previous = LoadTopFour();

        var entries = new List<Entry>(SlotCount + 1);
        for (int i = 0; i < previous.Length; i++)
            entries.Add(new Entry(previous[i], false));

        entries.Add(new Entry(waveReached, true));
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));

        int highlightRank = -1;
        int[] topFour = new int[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            if (i < entries.Count)
            {
                topFour[i] = entries[i].Score;
                if (entries[i].IsPlayerRun)
                    highlightRank = i;
            }
            else
            {
                topFour[i] = 0;
            }
        }

        for (int i = 0; i < SlotCount; i++)
            PlayerPrefs.SetInt(KeyPrefix + i, topFour[i]);

        PlayerPrefs.Save();
        sessionHighlightRank = highlightRank;
        return highlightRank;
    }

    private struct Entry
    {
        public int Score;
        public bool IsPlayerRun;

        public Entry(int score, bool isPlayerRun)
        {
            Score = score;
            IsPlayerRun = isPlayerRun;
        }
    }
}
