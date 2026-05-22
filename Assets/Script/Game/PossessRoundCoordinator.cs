using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns the possess laser at <see cref="PossessSpawner"/> a random 10–50s after each wave event starts.
/// The beam is removed when the player fires it, when a new event segment begins, or when the whole wave finishes.
/// </summary>
public class PossessRoundCoordinator : MonoBehaviour
{
    [SerializeField] private Transform possessSpawner;
    [SerializeField] private GameObject lazerBeamPrefab;
    [SerializeField] private float minDelayAfterEventStart = 10f;
    [SerializeField] private float maxDelayAfterEventStart = 50f;

    private int waveEpoch;
    private Coroutine spawnRoutine;
    private PossessLazerBeam activeBeam;

    private void Awake()
    {
        Transform sceneMarker = FindScenePossessSpawner();
        if (sceneMarker != null)
            possessSpawner = sceneMarker;
        else if (possessSpawner == null)
        {
            Transform found = transform.Find("PossessSpawner");
            if (found != null)
                possessSpawner = found;
        }
    }

    /// <summary>Uses a scene-placed PossessSpawner (not parented under this LevelManager).</summary>
    private Transform FindScenePossessSpawner()
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != "PossessSpawner")
                continue;
            if (t.IsChildOf(transform))
                continue;
            return t;
        }

        return null;
    }

    private void OnEnable()
    {
        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        foreach (Wave w in waves)
        {
            w.WaveElementStarted += OnWaveElementStarted;
            w.WaveElementEnded += OnWaveElementEnded;
        }
    }

    private void OnDisable()
    {
        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        foreach (Wave w in waves)
        {
            w.WaveElementStarted -= OnWaveElementStarted;
            w.WaveElementEnded -= OnWaveElementEnded;
        }

        StopSpawnRoutine();
        DespawnActiveBeam();
    }

    private void OnWaveElementStarted()
    {
        waveEpoch++;
        int epoch = waveEpoch;
        StopSpawnRoutine();
        DespawnActiveBeam();
        spawnRoutine = StartCoroutine(SpawnAfterDelay(epoch));
    }

    private void OnWaveElementEnded(bool waveFullyComplete)
    {
        StopSpawnRoutine();
        // Only clear the beam when the entire wave is done — not when a single segment ends
        // (segments often finish seconds after the beam spawns because all enemies were killed).
        if (waveFullyComplete)
        {
            waveEpoch++;
            DespawnActiveBeam();
        }
    }

    private IEnumerator SpawnAfterDelay(int epoch)
    {
        float wait = Random.Range(minDelayAfterEventStart, maxDelayAfterEventStart);
        yield return new WaitForSeconds(wait);
        if (epoch != waveEpoch)
            yield break;
        if (lazerBeamPrefab == null || possessSpawner == null)
            yield break;

        GameObject go = Instantiate(lazerBeamPrefab, possessSpawner);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        activeBeam = go.GetComponent<PossessLazerBeam>();
        if (activeBeam != null)
            activeBeam.Initialize(this, possessSpawner);
    }

    private void StopSpawnRoutine()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    internal void NotifyBeamDestroyed(PossessLazerBeam beam)
    {
        if (activeBeam == beam)
            activeBeam = null;
    }

    private void DespawnActiveBeam()
    {
        if (activeBeam == null)
            return;
        Destroy(activeBeam.gameObject);
        activeBeam = null;
    }
}
