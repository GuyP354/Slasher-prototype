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
        // If we finished an event, wait for E to start the next one
        if (waitingForNextEvent)
        {
            if (Input.GetKeyDown(KeyCode.E) && advanceAllowed)
                TryBeginWaitingEvent();
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
            foreach (var info in spawnInfos)
                info.Initialize();
        }
        public bool RunEvent()
        {
            if (spawnInfos.Count == 0)
                return false;

            // Keep running until all spawnInfos are depleted and their spawned enemies are gone.
            for (int i = 0; i < spawnInfos.Count; i++)
            {
                GameObject spawned = spawnInfos[i].ReadyToSpawn();
                if (spawned != null)
                    spawnedThisEvent.Add(spawned);
                if (spawnInfos[i].amount <= 0)
                {
                    spawnInfos.RemoveAt(i);
                    i--;
                }
            }

            // Spawning finished: end only when enemies spawned by THIS event are all gone.
            if (spawnInfos.Count == 0)
            {
                for (int i = spawnedThisEvent.Count - 1; i >= 0; i--)
                {
                    if (spawnedThisEvent[i] == null)
                        spawnedThisEvent.RemoveAt(i);
                }

                if (spawnedThisEvent.Count == 0)
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
            public GameObject ReadyToSpawn()
            {
                if (amount <= 0) return null;
                if (Time.time - lastTime >= interval)
                {
                    if (SpawnManager.Instance == null)
                        return null;
                    GameObject spawned = SpawnManager.Instance.Spawn(spawnPrefabIndex, spawnPointIndex);
                    lastTime = Time.time;
                    amount--;
                    return spawned;
                }
                return null;
            }
        }
    }
}