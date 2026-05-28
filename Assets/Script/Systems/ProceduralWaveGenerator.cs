using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds wave events (spawn infos) from wave number and procedural rules.
/// </summary>
public static class ProceduralWaveGenerator
{
    public const int MaxSpawnPointIndex = 3;
    public const int MaxSpawnInfosPerEvent = 4;
    public const int MaxSpawnInfosBeforeWave12 = 3;
    public const int MaxAmountPerSpawnInfo = 20;
    public const int MaxTotalEnemiesPerEvent = 80;
    public const int MaxConcurrentAlive = 15;
    public const float MinInterval = 4f;
    public const float MaxIntervalLate = 22f;
    public const float MaxIntervalEarly = 12f;

    public static Wave.WaveEvent GenerateWaveEvent(int waveNumber, System.Random rng)
    {
        if (waveNumber <= 1)
            return GenerateWaveOne(rng);

        int totalEnemies = GetTotalEnemyRange(waveNumber, rng);
        int maxInfos = waveNumber >= 12 ? MaxSpawnInfosPerEvent : MaxSpawnInfosBeforeWave12;
        int infoCount = rng.Next(1, maxInfos + 1);
        float maxInterval = waveNumber >= 6 ? MaxIntervalLate : MaxIntervalEarly;
        float strongChance = waveNumber >= 13 ? 0.22f : waveNumber >= 5 ? 0.15f : 0f;

        return BuildEvent(totalEnemies, infoCount, maxInterval, strongChance, rng);
    }

    /// <summary>Wave 1: exactly 1 enemy total (prefab 0), random spawn point 0–3.</summary>
    public static Wave.WaveEvent GenerateWaveOne(System.Random rng)
    {
        var spawnInfos = new List<Wave.WaveEvent.SpawnInfo>
        {
            CreateSpawnInfo(
                rng.Next(0, MaxSpawnPointIndex + 1),
                0,
                1,
                RandomInterval(MaxIntervalEarly, rng),
                0f)
        };

        return new Wave.WaveEvent { spawnInfos = spawnInfos };
    }

    private static Wave.WaveEvent BuildEvent(
        int totalEnemies,
        int infoCount,
        float maxInterval,
        float strongChance,
        System.Random rng)
    {
        totalEnemies = Mathf.Clamp(totalEnemies, 1, MaxTotalEnemiesPerEvent);
        infoCount = Mathf.Clamp(infoCount, 1, MaxSpawnInfosPerEvent);

        int[] amounts = SplitTotal(totalEnemies, infoCount, rng);
        int[] points = PickUniqueSpawnPoints(infoCount, rng);
        float[] intervals = PickUniqueIntervals(infoCount, maxInterval, rng);
        var spawnInfos = new List<Wave.WaveEvent.SpawnInfo>();
        for (int i = 0; i < infoCount; i++)
        {
            int amount = amounts[i];
            if (amount <= 0)
                continue;

            spawnInfos.Add(CreateSpawnInfo(points[i], 0, amount, intervals[i], strongChance));
        }

        return new Wave.WaveEvent { spawnInfos = spawnInfos };
    }

    private static Wave.WaveEvent.SpawnInfo CreateSpawnInfo(
        int spawnPointIndex,
        int spawnPrefabIndex,
        int amount,
        float interval,
        float strongChance)
    {
        return new Wave.WaveEvent.SpawnInfo
        {
            spawnPointIndex = spawnPointIndex,
            spawnPrefabIndex = spawnPrefabIndex,
            amount = amount,
            interval = interval,
            usePerSpawnPrefabRoll = strongChance > 0f,
            strongSpawnChance = strongChance
        };
    }

    private static int GetTotalEnemyRange(int waveNumber, System.Random rng)
    {
        // Wave 1 is handled separately (exactly 1 enemy).
        if (waveNumber <= 3)
            return RollInclusive(rng, 2, 3);      // waves 2–3

        if (waveNumber == 4)
            return RollInclusive(rng, 1, 4);

        if (waveNumber == 5)
            return RollInclusive(rng, 2, 5);

        if (waveNumber <= 7)
            return RollInclusive(rng, 3, 6);      // waves 6–7

        if (waveNumber == 8)
            return RollInclusive(rng, 1, 4);

        if (waveNumber == 9)
            return RollInclusive(rng, 5, 6);

        if (waveNumber == 10)
            return RollInclusive(rng, 7, 9);

        if (waveNumber <= 13)
            return RollInclusive(rng, 6, 12);     // waves 11–13

        if (waveNumber <= 16)
            return RollInclusive(rng, 8, 12);     // waves 14–16

        if (waveNumber <= 23)
            return RollInclusive(rng, 7, 15);     // waves 17–23

        // 24+: same band as 17–23, scaling up every 2 waves.
        int bonus = (waveNumber - 24) / 2;
        return RollInclusive(rng, 7 + bonus, 15 + bonus);
    }

    private static int RollInclusive(System.Random rng, int min, int max)
    {
        min = Mathf.Max(1, min);
        max = Mathf.Max(min, max);
        return rng.Next(min, max + 1);
    }

    private static int[] SplitTotal(int total, int parts, System.Random rng)
    {
        total = Mathf.Clamp(total, 1, MaxTotalEnemiesPerEvent);
        parts = Mathf.Clamp(parts, 1, MaxSpawnInfosPerEvent);

        if (parts == 1)
            return new[] { total };

        // Cannot assign at least 1 to every part when total < parts — only one slot gets enemies.
        if (total < parts)
        {
            int[] sparse = new int[parts];
            sparse[rng.Next(0, parts)] = total;
            return sparse;
        }

        int[] amounts = new int[parts];
        int remaining = total;

        for (int i = 0; i < parts - 1; i++)
        {
            int slotsLeft = parts - i;
            int minForThis = 1;
            int maxForThis = Mathf.Min(MaxAmountPerSpawnInfo, remaining - (slotsLeft - 1));

            if (maxForThis < minForThis)
                maxForThis = minForThis;

            int amount = rng.Next(minForThis, maxForThis + 1);
            amounts[i] = amount;
            remaining -= amount;
        }

        amounts[parts - 1] = Mathf.Clamp(remaining, 0, MaxAmountPerSpawnInfo);

        int sum = 0;
        for (int i = 0; i < amounts.Length; i++)
            sum += amounts[i];

        if (sum != total)
            amounts[parts - 1] = Mathf.Clamp(amounts[parts - 1] + (total - sum), 0, MaxAmountPerSpawnInfo);

        return amounts;
    }

    private static int[] PickUniqueSpawnPoints(int count, System.Random rng)
    {
        var pool = new List<int> { 0, 1, 2, 3 };
        Shuffle(pool, rng);

        int[] points = new int[count];
        for (int i = 0; i < count; i++)
            points[i] = pool[i % pool.Count];

        return points;
    }

    private static float[] PickUniqueIntervals(int count, float maxInterval, System.Random rng)
    {
        float[] intervals = new float[count];
        var used = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            float interval;
            int attempts = 0;
            do
            {
                interval = RandomInterval(maxInterval, rng);
                attempts++;
            } while (attempts < 50 && !used.Add(Mathf.RoundToInt(interval * 10f)));

            intervals[i] = interval;
        }

        return intervals;
    }

    private static float RandomInterval(float maxInterval, System.Random rng)
    {
        float max = Mathf.Max(MinInterval, maxInterval);
        return Mathf.Round((float)(MinInterval + rng.NextDouble() * (max - MinInterval)) * 10f) / 10f;
    }

    private static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
