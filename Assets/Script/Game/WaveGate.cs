using UnityEngine;

public class WaveGate : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Wave waveToStart;

    [Header("Passage (never destroyed)")]
    [Tooltip("Assign a BoxCollider child, or leave empty to auto-create \"PassageBarrier\". Only this collider's isTrigger follows the wave.")]
    [SerializeField] private BoxCollider passageBarrier;
    [SerializeField] private Vector3 passageBarrierLocalSize = new Vector3(3f, 4f, 0.35f);
    [SerializeField] private Vector3 passageBarrierLocalCenter = Vector3.zero;

    [Header("Optional visuals (not driven by wave)")]
    [SerializeField] private MeshCollider gateMeshCollider;

    [Header("Blocking")]
    [SerializeField] private Collider[] blockingColliders;
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform playerTransform;
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

        EnsurePassageBarrier();

        idleIsTrigger = true;
        Open();
    }

    private void EnsurePassageBarrier()
    {
        if (passageBarrier != null)
            return;

        Transform existing = transform.Find("PassageBarrier");
        if (existing != null)
        {
            passageBarrier = existing.GetComponent<BoxCollider>();
            if (passageBarrier != null)
                return;
        }

        var go = new GameObject("PassageBarrier");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = passageBarrierLocalCenter;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = gameObject.layer;

        passageBarrier = go.AddComponent<BoxCollider>();
        passageBarrier.size = passageBarrierLocalSize;
        passageBarrier.center = Vector3.zero;
        passageBarrier.isTrigger = true;
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

    private void Update()
    {
        if (player == null)
            CachePlayer();

        bool playerInArea = IsPlayerInInteractionArea();

        if (waveToStart != null)
            waveToStart.SetAdvanceAllowed(playerInArea);

        if (waveStarted) return;

        if (!playerInArea) return;

        if (Input.GetKeyDown(interactKey) && waveToStart != null)
        {
            waveStarted = true;
            Close();
            waveToStart.StartWave();
        }
    }

    private void CachePlayer()
    {
        if (playerTransform != null)
        {
            player = playerTransform;
            return;
        }

        GameObject playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo != null)
        {
            player = playerGo.transform;
            return;
        }

        PlayerController controller = FindFirstObjectByType<PlayerController>();
        if (controller != null)
            player = controller.transform;
    }

    private bool IsPlayerInInteractionArea()
    {
        if (interactionArea == null || player == null)
            return false;

        Vector3 closest = interactionArea.ClosestPoint(player.position);
        if ((closest - player.position).sqrMagnitude < 0.0001f)
            return true;

        Bounds b = interactionArea.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, interactionArea.transform.rotation);
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

        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
                if (blockingColliders[i] != null) blockingColliders[i].enabled = true;
        }

        if (navObstacle != null) navObstacle.enabled = true;

        SetPassageSolid(true);
    }

    private void Open()
    {
        isClosed = false;

        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
                if (blockingColliders[i] != null) blockingColliders[i].enabled = false;
        }

        if (navObstacle != null) navObstacle.enabled = false;

        SetPassageSolid(false);
    }

    private void SetPassageSolid(bool solid)
    {
        if (passageBarrier == null)
            return;
        passageBarrier.isTrigger = !solid && idleIsTrigger;
    }

    private void HandleWaveElementStarted()
    {
        Close();
    }

    private void HandleWaveElementEnded(bool waveFullyComplete)
    {
        Open();
    }
}
