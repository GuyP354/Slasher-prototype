using UnityEngine;
using System.Collections.Generic;
public class Wave : MonoBehaviour
{
    public List<WaveEvent> events = new();
    public event System.Action WaveElementStarted;
    public event System.Action WaveElementEnded;
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
            WaveElementEnded?.Invoke();
            completedEvents++;
            events.RemoveAt(0);
            if (events.Count == 0)
            {
                FinishWave();
            }
            else
            {
                // Don't auto-start next event; wait for E
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
        public List<SpawnInfo> spawnInfos = new();
        public void StartEvent()
        {
            foreach (var info in spawnInfos)
                info.Initialize();
        }
        public bool RunEvent()
        {
            // No spawn entries means this event can never satisfy completion conditions.
            // Keep it running forever.
            if (spawnInfos.Count == 0)
                return true;

            // Keep running until every configured spawn info has finished its amount.
            bool allSpawnsCompleted = true;
            for (int i = 0; i < spawnInfos.Count; i++)
            {
                spawnInfos[i].ReadyToSpawn();
                if (!spawnInfos[i].IsCompleted())
                    allSpawnsCompleted = false;
            }

            // Once all enemies are spawned, only end when there are no remaining active enemies.
            if (allSpawnsCompleted)
            {
                int enemyCount = SpawnManager.Instance.GetEnemiesLeft();
                if (enemyCount == 0)
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
            private int remainingAmount;
            private bool initialized;
            public void Initialize()
            {
                lastTime = Time.time;
                remainingAmount = amount;
                initialized = true;
            }
            public void ReadyToSpawn()
            {
                if (!initialized)
                    Initialize();

                if (remainingAmount <= 0) return;
                if (Time.time - lastTime >= interval)
                {
                    SpawnManager.Instance.Spawn(spawnPrefabIndex, spawnPointIndex);
                    lastTime = Time.time;
                    remainingAmount--;
                }
            }
            public bool IsCompleted()
            {
                if (!initialized)
                    return amount <= 0;

                return remainingAmount <= 0;
            }
        }
    }
}