using System.Collections;
using UnityEngine;

public class WaveGate : MonoBehaviour
{
    [System.Serializable]
    private class GateVisualPair
    {
        public GameObject planningVisual;
        public GameObject reverseVisual;
        public GateFrameAnimator planningAnimator;
        public GateFrameAnimator reverseAnimator;
    }

    [Header("Wave")]
    [SerializeField] private Wave waveToStart;

    [Header("Gate passage (child Box Collider)")]
    [Tooltip("Disabled = open (passable). Enabled = closed (blocks passage).")]
    [SerializeField] private BoxCollider gateBoxCollider;

    [Header("Blocking (optional extras)")]
    [SerializeField] private Collider[] blockingColliders;
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;

    [Header("Gate visuals")]
    [SerializeField] private Transform gateVisualsRoot;
    [SerializeField] private GateVisualPair primaryGate = new();
    [SerializeField] private GateVisualPair secondaryGate = new();

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform playerTransform;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Collider interactionArea;

    private Transform player;
    private Coroutine gateVisualRoutine;

    private void Awake()
    {
        ResolveGateBoxCollider();
        ResolveGateVisuals();
        ApplyOpenState();
        SetPlanningVisible(true);
    }

    private IEnumerator Start()
    {
        yield return null;
        PlayOpenAnimations();
    }

    private void OnEnable()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.WaveStarted += OnWaveStarted;
            LevelManager.Instance.WaveCompleted += OnWaveCompleted;
        }

        if (waveToStart == null)
            return;

        waveToStart.WaveElementStarted += OnWaveElementStarted;
        waveToStart.WaveElementEnded += OnWaveElementEnded;
    }

    private void OnDisable()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.WaveStarted -= OnWaveStarted;
            LevelManager.Instance.WaveCompleted -= OnWaveCompleted;
        }

        if (waveToStart == null)
            return;

        waveToStart.WaveElementStarted -= OnWaveElementStarted;
        waveToStart.WaveElementEnded -= OnWaveElementEnded;
    }

    private void Update()
    {
        if (player == null)
            CachePlayer();

        bool playerInArea = IsPlayerInInteractionArea();

        if (waveToStart != null)
            waveToStart.SetAdvanceAllowed(playerInArea);

        if (!playerInArea)
            return;

        if (!Input.GetKeyDown(interactKey) || waveToStart == null || LevelManager.Instance == null)
            return;

        if (LevelManager.Instance.CanStartWaveFromInteraction())
            LevelManager.Instance.TryStartWaveFromPlayerInteraction();
    }

    private void ResolveGateVisuals()
    {
        if (gateVisualsRoot == null)
        {
            Transform found = transform.Find("Gate");
            if (found != null)
                gateVisualsRoot = found;
        }

        if (gateVisualsRoot == null)
            return;

        if (primaryGate.planningVisual == null)
            primaryGate.planningVisual = FindChildByName(gateVisualsRoot, "Gate");

        if (primaryGate.reverseVisual == null)
            primaryGate.reverseVisual = FindChildByName(gateVisualsRoot, "Gate Reverse");

        if (secondaryGate.planningVisual == null)
            secondaryGate.planningVisual = FindChildByName(gateVisualsRoot, "Gate second side");

        if (secondaryGate.reverseVisual == null)
            secondaryGate.reverseVisual = FindChildByName(gateVisualsRoot, "Gate second side reverse");

        primaryGate.planningAnimator = GetAnimator(primaryGate.planningVisual);
        primaryGate.reverseAnimator = GetAnimator(primaryGate.reverseVisual);
        secondaryGate.planningAnimator = GetAnimator(secondaryGate.planningVisual);
        secondaryGate.reverseAnimator = GetAnimator(secondaryGate.reverseVisual);
    }

    private static GateFrameAnimator GetAnimator(GameObject visual)
    {
        return visual != null ? visual.GetComponent<GateFrameAnimator>() : null;
    }

    private static GameObject FindChildByName(Transform root, string exactName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && child.name == exactName)
                return child.gameObject;
        }

        return null;
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

    private void SetPlanningVisible(bool planningVisible)
    {
        SetPairVisible(primaryGate, planningVisible);
        SetPairVisible(secondaryGate, planningVisible);
    }

    private static void SetPairVisible(GateVisualPair pair, bool planningVisible)
    {
        if (pair.planningVisual != null)
            pair.planningVisual.SetActive(planningVisible);

        if (pair.reverseVisual != null)
            pair.reverseVisual.SetActive(!planningVisible);
    }

    private void PlayOpenAnimations()
    {
        StopGateVisualRoutine();
        SetPlanningVisible(true);
        gateVisualRoutine = StartCoroutine(PlayBoth(
            primaryGate.planningAnimator,
            secondaryGate.planningAnimator));
    }

    private void PlayCloseAnimations()
    {
        StopGateVisualRoutine();
        SetPlanningVisible(false);
        gateVisualRoutine = StartCoroutine(PlayBoth(
            primaryGate.reverseAnimator,
            secondaryGate.reverseAnimator));
    }

    private void StopGateVisualRoutine()
    {
        if (gateVisualRoutine == null)
            return;

        StopCoroutine(gateVisualRoutine);
        gateVisualRoutine = null;
    }

    private IEnumerator PlayBoth(GateFrameAnimator primary, GateFrameAnimator secondary)
    {
        int pending = 0;

        if (RunIfActive(primary, ref pending))
            StartCoroutine(WaitThenDone(primary.PlayOnce(), () => pending--));

        if (RunIfActive(secondary, ref pending))
            StartCoroutine(WaitThenDone(secondary.PlayOnce(), () => pending--));

        while (pending > 0)
            yield return null;

        gateVisualRoutine = null;
    }

    private static bool RunIfActive(GateFrameAnimator gateAnimator, ref int pending)
    {
        if (gateAnimator == null || !gateAnimator.gameObject.activeInHierarchy)
            return false;

        pending++;
        return true;
    }

    private static IEnumerator WaitThenDone(IEnumerator routine, System.Action onDone)
    {
        yield return routine;
        onDone();
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

    private void OnWaveStarted()
    {
        ApplyClosedState();
        SetPlanningVisible(false);
    }

    private void OnWaveElementStarted()
    {
        ApplyClosedState();
        PlayCloseAnimations();
    }

    private void OnWaveElementEnded(bool waveFullyComplete)
    {
        ApplyOpenState();
        PlayOpenAnimations();
    }

    private void OnWaveCompleted()
    {
        ApplyOpenState();
    }
}
