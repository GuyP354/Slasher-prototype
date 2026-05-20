using UnityEngine;
using System.Collections.Generic;

public class Wave : MonoBehaviour
{
    public List<WaveEvent> events = new();
    public event System.Action WaveElementStarted;
    /// <summary>False between multi-step wave segments; true when this was the last segment (whole wave done).</summary>
    public event System.Action<bool> WaveElementEnded;
    private bool advanceAllowed;
    private bool isPlaying;
    private bool waitingForNextEvent;
    private int totalEvents;
    private int completedEvents;

    public void SetAdvanceAllowed(bool allowed) => advanceAllowed = allowed;

    public bool IsWaitingForNextEvent => isPlaying && waitingForNextEvent;

    /// <summary>Remaining spawn quota for the active element (sum of SpawnInfo.amount).</summary>
    public int GetCurrentElementRemainingSpawns()
    {
        if (!isPlaying || waitingForNextEvent || events.Count == 0)
            return 0;

        return events[0].GetRemainingSpawnCount();
    }

    /// <summary>Enemies spawned by the active element that are still alive.</summary>
    public int GetCurrentElementAliveEnemies()
    {
        if (!isPlaying || waitingForNextEvent || events.Count == 0)
            return 0;

        return events[0].GetAliveSpawnedCount();
    }

    private void FinishWave()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        waitingForNextEvent = false;
        advanceAllowed = false;
        LevelManager.Instance.EndWave();
    }

    public void StartWave()
    {
        isPlaying = true;
        waitingForNextEvent = false;
        totalEvents = events.Count;
        completedEvents = 0;
        if (events.Count == 0)
        {
            FinishWave();
            return;
        }
        waitingForNextEvent = true;
    }

    /// <summary>Begins the current waiting segment (used for the first E at the lever; later segments still use E in Update).</summary>
    public bool TryBeginWaitingEvent()
    {
        if (!isPlaying || !waitingForNextEvent || !advanceAllowed)
            return false;

        waitingForNextEvent = false;
        if (events.Count == 0)
        {
            FinishWave();
            return false;
        }

        events[0].StartEvent();
        WaveElementStarted?.Invoke();
        return true;
    }

    private void Update()
    {
        if (!isPlaying) return;

        if (waitingForNextEvent)
        {
            if (Input.GetKeyDown(KeyCode.E) && advanceAllowed)
                TryBeginWaitingEvent();
            return;
        }

        if (events.Count == 0)
        {
            FinishWave();
            return;
        }

        if (!events[0].RunEvent())
        {
            Debug.Log("End Event");
            completedEvents++;
            events.RemoveAt(0);
            bool waveFullyComplete = events.Count == 0;
            WaveElementEnded?.Invoke(waveFullyComplete);
            if (waveFullyComplete)
                FinishWave();
            else
            {
                waitingForNextEvent = true;
                Debug.Log("Press E to start next event");
            }
        }
    }

    public string GetEventInfo()
    {
        if (totalEvents <= 0)
            return "0";

        if (waitingForNextEvent)
            return completedEvents + "/" + totalEvents;

        if (events.Count == 0)
            return totalEvents + "/" + totalEvents;

        return (completedEvents + 1) + "/" + totalEvents;
    }

    [System.Serializable]
    public class WaveEvent
    {
        public List<SpawnInfo> spawnInfos = new();
        private readonly List<GameObject> spawnedThisEvent = new();

        public void StartEvent()
        {
            spawnedThisEvent.Clear();
            foreach (SpawnInfo info in spawnInfos)
                info.ResetForEvent();
        }

        public int GetRemainingSpawnCount()
        {
            int remaining = 0;
            for (int i = 0; i < spawnInfos.Count; i++)
                remaining += spawnInfos[i].RemainingAmount;
            return remaining;
        }

        public int GetAliveSpawnedCount()
        {
            PruneSpawnedEnemies();
            return spawnedThisEvent.Count;
        }

        /// <summary>Returns true while the element is still running; false when it is complete.</summary>
        public bool RunEvent()
        {
            for (int i = 0; i < spawnInfos.Count; i++)
            {
                GameObject spawned = spawnInfos[i].ReadyToSpawn();
                if (spawned != null)
                    spawnedThisEvent.Add(spawned);

                if (spawnInfos[i].RemainingAmount <= 0)
                {
                    spawnInfos.RemoveAt(i);
                    i--;
                }
            }

            PruneSpawnedEnemies();

            if (spawnInfos.Count > 0)
                return true;

            return spawnedThisEvent.Count > 0;
        }

        private void PruneSpawnedEnemies()
        {
            for (int i = spawnedThisEvent.Count - 1; i >= 0; i--)
            {
                if (!IsSpawnedEnemyAlive(spawnedThisEvent[i]))
                    spawnedThisEvent.RemoveAt(i);
            }
        }

        private static bool IsSpawnedEnemyAlive(GameObject enemy)
        {
            if (enemy == null)
                return false;

            Health health = enemy.GetComponent<Health>();
            if (health != null)
                return health.CurrentHealth > 0;

            return enemy.activeInHierarchy;
        }

        [System.Serializable]
        public class SpawnInfo
        {
            public int spawnPointIndex = 0;
            public int spawnPrefabIndex = 0;
            public int amount = 10;
            public float interval = 1.0f;

            private int initialAmount;
            private float lastTime;

            public int RemainingAmount => amount;

            public void ResetForEvent()
            {
                if (initialAmount <= 0)
                    initialAmount = amount;

                amount = initialAmount;
                lastTime = Time.time;
            }

            public GameObject ReadyToSpawn()
            {
                if (amount <= 0)
                    return null;

                if (Time.time - lastTime < interval)
                    return null;

                if (SpawnManager.Instance == null)
                    return null;

                GameObject spawned = SpawnManager.Instance.Spawn(spawnPrefabIndex, spawnPointIndex);
                lastTime = Time.time;
                amount--;
                return spawned;
            }
        }
    }
}
