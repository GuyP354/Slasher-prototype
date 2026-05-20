using UnityEngine;

public class WaveGate : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Wave waveToStart;

    [Header("Gate passage (child Box Collider)")]
    [Tooltip("Disabled = open (passable). Enabled = closed (blocks passage).")]
    [SerializeField] private BoxCollider gateBoxCollider;

    [Header("Blocking (optional extras)")]
    [SerializeField] private Collider[] blockingColliders;
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform playerTransform;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactionArea;

    private Transform player;

    /// <summary>True when the player can press E here to start a new wave round (gate open, cooldown done).</summary>
    public bool IsWaveStartInteractionAvailable =>
        LevelManager.Instance != null && LevelManager.Instance.CanStartWaveFromInteraction();

    private void Awake()
    {
        ResolveGateBoxCollider();
        ApplyOpenState();
    }

    private void OnEnable()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.WaveStarted += HandleWaveStarted;
            LevelManager.Instance.WaveCompleted += HandleWaveCompleted;
        }

        if (waveToStart == null) return;

        waveToStart.WaveElementStarted += HandleWaveElementStarted;
        waveToStart.WaveElementEnded += HandleWaveElementEnded;
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.WaveStarted -= HandleWaveStarted;
            LevelManager.Instance.WaveCompleted -= HandleWaveCompleted;
        }

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

        if (!playerInArea) return;

        if (!Input.GetKeyDown(interactKey) || waveToStart == null || LevelManager.Instance == null)
            return;

        if (LevelManager.Instance.CanStartWaveFromInteraction())
            LevelManager.Instance.TryStartWaveFromPlayerInteraction();
    }

    private void ResolveGateBoxCollider()
    {
        if (gateBoxCollider != null)
            return;

        Transform child = transform.Find("Gate Box Collider");
        if (child != null)
            gateBoxCollider = child.GetComponent<BoxCollider>();

        if (gateBoxCollider == null)
        {
            child = transform.Find("PassageBarrier");
            if (child != null)
                gateBoxCollider = child.GetComponent<BoxCollider>();
        }

        if (gateBoxCollider == null)
            gateBoxCollider = GetComponentInChildren<BoxCollider>(true);

        if (gateBoxCollider != null)
            gateBoxCollider.isTrigger = false;
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

    private void ApplyClosedState()
    {
        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
                if (blockingColliders[i] != null) blockingColliders[i].enabled = true;
        }

        if (navObstacle != null) navObstacle.enabled = true;

        if (gateBoxCollider != null)
            gateBoxCollider.enabled = true;
    }

    private void ApplyOpenState()
    {
        if (blockingColliders != null)
        {
            for (int i = 0; i < blockingColliders.Length; i++)
                if (blockingColliders[i] != null) blockingColliders[i].enabled = false;
        }

        if (navObstacle != null) navObstacle.enabled = false;

        if (gateBoxCollider != null)
            gateBoxCollider.enabled = false;
    }

    private void HandleWaveStarted()
    {
        ApplyClosedState();
    }

    private void HandleWaveElementStarted()
    {
        ApplyClosedState();
    }

    private void HandleWaveElementEnded(bool waveFullyComplete)
    {
        ApplyOpenState();
    }

    private void HandleWaveCompleted()
    {
        ApplyOpenState();
    }
}
