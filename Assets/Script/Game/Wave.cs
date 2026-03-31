using UnityEngine;
using System.Collections.Generic;
public class Wave : MonoBehaviour
{
    public List<WaveEvent> events = new();
    private bool isPlaying;
    private bool waitingForNextEvent;
    private int totalEvents;
    private int completedEvents;
    private void FinishWave()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        waitingForNextEvent = false;
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
            if (Input.GetKeyDown(KeyCode.E))
            {
                waitingForNextEvent = false;
                if (events.Count == 0)
                    FinishWave();
                else
                    events[0].StartEvent();
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
            if (duration == 0.0f && spawnInfos.Count == 0)
                return false;
            if (duration != 0.0f && Time.time - startTime > duration)
                return false;
            for (int i = 0; i < spawnInfos.Count; i++)
            {
                spawnInfos[i].ReadyToSpawn();
                if (spawnInfos[i].amount <= 0)
                {
                    spawnInfos.RemoveAt(i);
                    i--;
                }
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