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
        Debug.Log("Press E to start wave");
    }
    private void Update()
    {
        if (!isPlaying) return;
        // If we finished an event, wait for E to start the next one
        if (waitingForNextEvent)
        {
            if (Input.GetKeyDown(KeyCode.E) && advanceAllowed)
            {
                waitingForNextEvent = false;
                if (events.Count == 0)
                    FinishWave();
                else
                {
                    events[0].StartEvent();
                    WaveElementStarted?.Invoke();
                }
            }
            return;
        }
        // Run current event
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
            return "0/0";

        if (events.Count == 0)
            return totalEvents + "/" + totalEvents;

        return (completedEvents + 1) + "/" + totalEvents;
    }
    [System.Serializable]
    public class WaveEvent
    {
        public float duration = 15.0f;
        public List<SpawnInfo> spawnInfos = new();
        private float startTime;
        public void StartEvent()
        {
            startTime = Time.time;
            foreach (var info in spawnInfos)
                info.Initialize();
        }
        public bool RunEvent()
        {
            // If this element doesn't spawn anything, fall back to time duration.
            if (spawnInfos.Count == 0)
            {
                if (duration == 0.0f)
                    return false;
                if (duration != 0.0f && Time.time - startTime > duration)
                    return false;
                return true;
            }

            // Spawning elements: keep running until all spawnInfos are depleted.
            for (int i = 0; i < spawnInfos.Count; i++)
            {
                spawnInfos[i].ReadyToSpawn();
                if (spawnInfos[i].amount <= 0)
                {
                    spawnInfos.RemoveAt(i);
                    i--;
                }
            }

            // Spawning finished: require no tagged enemies AND none left in SpawnManager (untagged spawned enemies no longer end the wave early).
            if (spawnInfos.Count == 0)
            {
                int tagged = GameObject.FindGameObjectsWithTag("Enemy").Length;
                int spawner = SpawnManager.Instance != null ? SpawnManager.Instance.GetEnemiesLeft() : 0;
                if (Mathf.Max(tagged, spawner) == 0)
                    return false;
            }

            return true;
        }
        [System.Serializable]
        public class SpawnInfo
        {
            public int spawnPointIndex = 0;
            public int spawnPrefabIndex = 0;
            public int amount = 10;
            public float interval = 1.0f;
            private float lastTime;
            public void Initialize()
            {
                lastTime = Time.time;
            }
            public void ReadyToSpawn()
            {
                if (amount <= 0) return;
                if (Time.time - lastTime >= interval)
                {
                    SpawnManager.Instance.Spawn(spawnPrefabIndex, spawnPointIndex);
                    lastTime = Time.time;
                    amount--;
                }
            }
        }
    }
}