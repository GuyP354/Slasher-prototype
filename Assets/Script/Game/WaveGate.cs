using UnityEngine;

public class WaveGate : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Wave waveToStart;

    [Header("Mesh Collider")]
    [SerializeField] private MeshCollider gateMeshCollider;

    [Header("Blocking")]
    [SerializeField] private Collider[] blockingColliders; // solid colliders to toggle
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;   // optional

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactionArea;

    private bool waveStarted;
    private bool idleIsTrigger;

    private Transform player;
    private bool isClosed;

    private void Awake()
    {
        if (gateMeshCollider == null)
            gateMeshCollider = GetComponent<MeshCollider>();

        // Per requirement: collider is a trigger before/after an element runs.
        idleIsTrigger = true;

        // Start with the gate open and non-blocking until a wave element begins.
        Open();
    }

    private void OnEnable()
    {
        if (waveToStart == null) return;

        waveToStart.WaveElementStarted += HandleWaveElementStarted;
        waveToStart.WaveElementEnded += HandleWaveElementEnded;
    }

    private void OnDisable()
    {
        if (waveToStart == null) return;

        waveToStart.WaveElementStarted -= HandleWaveElementStarted;
        waveToStart.WaveElementEnded -= HandleWaveElementEnded;
    }

    //private void OnEnable()
   // {
        //if (LevelManager.Instance != null)
            //LevelManager.Instance.WaveEnds += Open;
   // }

    //private void OnDisable()
    //{
        //if (LevelManager.Instance != null)
           // LevelManager.Instance.if WaveEnds -= Open;
   // }

    private void Update()
    {
        if (player == null)
            CachePlayer();

        bool playerInArea = IsPlayerInInteractionArea();

        if (waveToStart != null)
            waveToStart.SetAdvanceAllowed(playerInArea);

        // Start the wave only once (Wave itself handles "Press E to start next event").
        if (waveStarted) return;

        if (!playerInArea) return;

        if (Input.GetKeyDown(interactKey) && waveToStart != null)
        {
            waveStarted = true;
            waveToStart.StartWave();
        }
    }

    private void CachePlayer()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo != null)
            player = playerGo.transform;
    }

    private bool IsPlayerInInteractionArea()
    {
        if (interactionArea == null)
            return false;

        Bounds b = interactionArea.bounds;
        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            interactionArea.transform.rotation
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null) continue;
            if (hit.CompareTag(playerTag)) return true;

            if (hit.transform.root != null && hit.transform.root.CompareTag(playerTag))
                return true;
        }

        return false;
    }

    private void Close()
    {
        if (isClosed) return;
        isClosed = true;

        for (int i = 0; i < blockingColliders.Length; i++)
            if (blockingColliders[i] != null) blockingColliders[i].enabled = true;

        if (navObstacle != null) navObstacle.enabled = true;

        if (gateMeshCollider != null)
            gateMeshCollider.isTrigger = false;
    }

    private void Open()
    {
        isClosed = false;

        for (int i = 0; i < blockingColliders.Length; i++)
            if (blockingColliders[i] != null) blockingColliders[i].enabled = false;

        if (navObstacle != null) navObstacle.enabled = false;

        if (gateMeshCollider != null)
            gateMeshCollider.isTrigger = idleIsTrigger;
    }

    private void HandleWaveElementStarted()
    {
        // Element is running: block the gate.
        Close();
    }

    private void HandleWaveElementEnded()
    {
        // Element finished (enemy count reached zero): reopen the gate.
        Open();
    }
}