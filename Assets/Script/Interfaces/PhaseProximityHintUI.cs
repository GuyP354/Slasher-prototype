using UnityEngine;

/// <summary>
/// UI-only hint toggler: shows/hides a visual root based on player proximity and wave phase.
/// Attach to a world-space hint canvas (or any hint root); it will toggle its first child by default.
/// </summary>
[DisallowMultipleComponent]
public class PhaseProximityHintUI : MonoBehaviour
{
    public enum PhaseRule
    {
        Always,
        /// <summary>Gate lever hint — between waves or between multi-segment events.</summary>
        PlanningOnly,
        /// <summary>Possess pickup hint — only while an active beam exists at the spawner.</summary>
        PossessPickupAvailable
    }

    [Header("Proximity")]
    [SerializeField] private float showRadius = 3f;
    [SerializeField] private Transform distanceAnchor;
    [Tooltip("When set, player must be inside this collider (same idea as WaveGate interaction area).")]
    [SerializeField] private Collider proximityCollider;
    [SerializeField] private string playerTag = "Player";

    [Header("Phase")]
    [SerializeField] private PhaseRule phaseRule = PhaseRule.Always;

    [Header("Visuals")]
    [Tooltip("If null, the first child GameObject is used.")]
    [SerializeField] private GameObject visualRoot;

    private Transform player;
    private LevelManager levelManager;
    private PossessRoundCoordinator possessCoordinator;
    private bool subscribed;

    private void Awake()
    {
        if (distanceAnchor == null)
            distanceAnchor = transform;

        if (visualRoot == null && transform.childCount > 0)
            visualRoot = transform.GetChild(0).gameObject;
    }

    public GameObject GetVisualRoot() => visualRoot;

    public void ForceHideVisual()
    {
        ApplyVisible(false);
    }

    private void OnEnable()
    {
        CacheManagers();
        TrySubscribe();
        ApplyVisible(ShouldShow());
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (!subscribed)
            TrySubscribe();

        ApplyVisible(ShouldShow());
    }

    private void CacheManagers()
    {
        if (levelManager == null)
            levelManager = FindFirstObjectByType<LevelManager>();

        if (possessCoordinator == null)
            possessCoordinator = FindFirstObjectByType<PossessRoundCoordinator>();
    }

    private void TrySubscribe()
    {
        if (subscribed || levelManager == null)
            return;

        levelManager.WaveStarted += OnPhaseChanged;
        levelManager.WaveCompleted += OnPhaseChanged;
        subscribed = true;

        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] == null)
                continue;
            waves[i].WaveElementStarted += OnPhaseChanged;
            waves[i].WaveElementEnded += OnWaveElementEnded;
        }
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (levelManager != null)
        {
            levelManager.WaveStarted -= OnPhaseChanged;
            levelManager.WaveCompleted -= OnPhaseChanged;
        }

        Wave[] waves = FindObjectsByType<Wave>(FindObjectsSortMode.None);
        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i] == null)
                continue;
            waves[i].WaveElementStarted -= OnPhaseChanged;
            waves[i].WaveElementEnded -= OnWaveElementEnded;
        }

        subscribed = false;
    }

    private void OnPhaseChanged()
    {
        ApplyVisible(ShouldShow());
    }

    private void OnWaveElementEnded(bool _)
    {
        ApplyVisible(ShouldShow());
    }

    private bool ShouldShow()
    {
        if (visualRoot == null)
            return false;

        if (!PassesPhaseRule())
            return false;

        return IsPlayerInRange();
    }

    private bool PassesPhaseRule()
    {
        if (phaseRule == PhaseRule.Always)
            return true;

        CacheManagers();

        if (levelManager == null)
            return false;

        switch (phaseRule)
        {
            case PhaseRule.PlanningOnly:
                if (!GameHintsTutorial.IsGateHintUnlocked)
                    return false;
                return levelManager.ShouldShowGateStartHint();
            case PhaseRule.PossessPickupAvailable:
                return levelManager.IsWaveActive
                    && possessCoordinator != null
                    && possessCoordinator.HasActivePickup;
            default:
                return true;
        }
    }

    private bool IsPlayerInRange()
    {
        if (player == null)
            CachePlayer();

        if (player == null)
            return false;

        if (proximityCollider != null)
            return IsInsideInteractionCollider(proximityCollider, player, playerTag);

        if (distanceAnchor == null)
            return false;

        Vector3 a = distanceAnchor.position;
        Vector3 b = player.position;
        a.y = 0f;
        b.y = 0f;
        float r = Mathf.Max(0f, showRadius);
        return (a - b).sqrMagnitude <= r * r;
    }

    private void CachePlayer()
    {
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

    private static bool IsInsideInteractionCollider(Collider area, Transform playerTransform, string tag)
    {
        if (area == null || playerTransform == null)
            return false;

        Vector3 closest = area.ClosestPoint(playerTransform.position);
        if ((closest - playerTransform.position).sqrMagnitude < 0.0001f)
            return true;

        Bounds b = area.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, area.transform.rotation);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];
            if (hit == null)
                continue;

            if (hit.CompareTag(tag))
                return true;
            if (hit.transform.root != null && hit.transform.root.CompareTag(tag))
                return true;
        }

        return false;
    }

    private void ApplyVisible(bool visible)
    {
        if (visualRoot != null && visualRoot.activeSelf != visible)
            visualRoot.SetActive(visible);
    }
}
