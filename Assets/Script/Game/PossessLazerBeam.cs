using System;
using UnityEngine;

/// <summary>
/// Pickup / carrier for the possess ability: E toggles carry, Space fires one beam then despawns.
/// </summary>
public class PossessLazerBeam : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.R;
    [SerializeField] private KeyCode abilityKey = KeyCode.Space;
    [SerializeField] private float pickupRadius = 2.5f;

    [Header("Carry")]
    [Tooltip("Side offset while held (X = left/right, Z = forward/back) relative to player facing. Height stays at pickup/spawn level.")]
    [SerializeField] private Vector3 holdLocalOffset = new Vector3(-1.2f, 0f, 0f);
    [Tooltip("Small lift applied while carried (on top of spawn/pickup height).")]
    [SerializeField] private float holdExtraHeight = 0.25f;

    [Header("Idle float")]
    [SerializeField] private float levitateAmplitude = 0.25f;
    [SerializeField] private float levitateSpeed = 2f;

    [Header("Possess shot")]
    [SerializeField] private float beamLength = 30f;
    [SerializeField] private float beamHalfWidth = 2.5f;
    [SerializeField] private float beamHalfHeight = 2f;
    [SerializeField] private LayerMask beamHitMask = ~0;
    private const string enemyTag = "Enemy";

    private PossessRoundCoordinator coordinator;
    private Transform spawnerAnchor;
    private Transform player;
    private bool held;
    private bool consumed;
    private float holdHeightAbovePlayer;
    private Quaternion heldWorldRotation;
    private PossessWorldPropVisual worldPropVisual;

    public void Initialize(PossessRoundCoordinator owner, Transform spawner)
    {
        coordinator = owner;
        spawnerAnchor = spawner;
        ResolveWorldPropVisual();
        SnapToSpawnerIdlePose();
    }

    private void Awake()
    {
        ResolveWorldPropVisual();
    }

    private void OnDestroy()
    {
        if (coordinator != null)
            coordinator.NotifyBeamDestroyed(this);
    }

    private void Update()
    {
        if (consumed)
            return;

        if (player == null)
            CachePlayer();

        if (!held)
        {
            SnapToSpawnerIdlePose();

            if (player != null
                && Input.GetKeyDown(interactKey)
                && PlanarDistance(transform.position, player.position) <= pickupRadius)
            {
                transform.SetParent(null);
                heldWorldRotation = transform.rotation;
                holdHeightAbovePlayer = transform.position.y - player.position.y;
                held = true;
            }
        }
        else
        {
            if (player == null)
            {
                held = false;
                return;
            }

            Vector3 flatForward = player.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.01f)
                flatForward = Vector3.forward;
            flatForward.Normalize();

            Vector3 planarOffset = new Vector3(holdLocalOffset.x, 0f, holdLocalOffset.z);
            Vector3 worldOffset = Quaternion.LookRotation(flatForward, Vector3.up) * planarOffset;
            float bob = Mathf.Sin(Time.time * levitateSpeed) * (levitateAmplitude * 0.5f);
            Vector3 holdPosition = player.position + worldOffset;
            holdPosition.y = player.position.y + holdHeightAbovePlayer + holdExtraHeight + bob;
            transform.position = holdPosition;
            transform.rotation = heldWorldRotation;

            if (Input.GetKeyDown(interactKey))
            {
                held = false;
                if (spawnerAnchor != null)
                    transform.SetParent(spawnerAnchor);
                SnapToSpawnerIdlePose();
                return;
            }

            if (Input.GetKeyDown(abilityKey))
                FirePossessBeamAndConsume(flatForward);
        }
    }

    private void FirePossessBeamAndConsume(Vector3 flatForward)
    {
        if (player == null)
            return;

        consumed = true;

        Vector3 origin = player.position + Vector3.up * 1.2f;
        Quaternion orient = Quaternion.LookRotation(flatForward, Vector3.up);
        Vector3 halfExtents = new Vector3(beamHalfWidth, beamHalfHeight, 0.35f);

        RaycastHit[] hits = Physics.BoxCastAll(
            origin,
            halfExtents,
            flatForward,
            orient,
            beamLength,
            beamHitMask,
            QueryTriggerInteraction.Ignore);

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        GameObject firstEnemy = FindFirstEnemyRootAlongHits(hits);
        if (firstEnemy != null)
        {
            Transform nearestOther = FindNearestEnemyTransform(firstEnemy.transform, firstEnemy.transform.position);
            if (nearestOther != null)
            {
                if (firstEnemy.GetComponent<PossessedEnemy>() == null)
                    firstEnemy.AddComponent<PossessedEnemy>();
                EnemyCombat ec = firstEnemy.GetComponent<EnemyCombat>();
                if (ec != null)
                    ec.RefreshPossessedState();
            }
        }

        if (BloodInventory.Instance != null)
            BloodInventory.Instance.AbsorbAllBloodPickupsInScene();

        PlayDetachedWorldPropAnimation();
        Destroy(gameObject);
    }

    private void PlayDetachedWorldPropAnimation()
    {
        if (worldPropVisual == null)
            return;

        PossessWorldPropVisual runner = ResolveAnimationRunner();
        if (runner == null)
            return;

        runner.GetVisualTransform().SetParent(null);
        runner.PlayAbilityPresentationAndDestroy();
    }

    /// <summary>Animator lives on World props; runner must be on that object so it survives pickup destroy.</summary>
    private PossessWorldPropVisual ResolveAnimationRunner()
    {
        Transform visual = worldPropVisual.GetVisualTransform();
        PossessWorldPropVisual onVisual = visual.GetComponent<PossessWorldPropVisual>();
        if (onVisual != null)
            return onVisual;

        onVisual = visual.gameObject.AddComponent<PossessWorldPropVisual>();
        return onVisual;
    }

    private void ResolveWorldPropVisual()
    {
        if (worldPropVisual != null)
            return;

        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            worldPropVisual = anim.GetComponent<PossessWorldPropVisual>();
            if (worldPropVisual == null)
                worldPropVisual = anim.gameObject.AddComponent<PossessWorldPropVisual>();
            worldPropVisual.ShowFrameZero();
            return;
        }

        worldPropVisual = GetComponentInChildren<PossessWorldPropVisual>(true);
        if (worldPropVisual != null)
            return;

        Transform worldProp = transform.Find("World props");
        if (worldProp == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == "World props")
                {
                    worldProp = children[i];
                    break;
                }
            }
        }

        if (worldProp == null)
            return;

        worldPropVisual = worldProp.GetComponent<PossessWorldPropVisual>();
        if (worldPropVisual == null && worldProp.GetComponent<Animator>() != null)
            worldPropVisual = worldProp.gameObject.AddComponent<PossessWorldPropVisual>();

        worldPropVisual.ShowFrameZero();
    }

    private static GameObject FindFirstEnemyRootAlongHits(RaycastHit[] hits)
    {
        foreach (RaycastHit h in hits)
        {
            if (!h.collider)
                continue;
            if (TryGetEnemyTaggedRoot(h.collider.gameObject, out GameObject root))
                return root;
        }

        return null;
    }

    private static bool TryGetEnemyTaggedRoot(GameObject colliderGo, out GameObject root)
    {
        root = null;
        Transform t = colliderGo.transform;
        GameObject tagged = FindTaggedRoot(t, enemyTag);
        if (tagged == null)
            return false;
        root = tagged;
        return true;
    }

    private static GameObject FindTaggedRoot(Transform t, string tag)
    {
        Transform walk = t;
        while (walk != null)
        {
            if (walk.CompareTag(tag))
                return walk.gameObject;
            walk = walk.parent;
        }
        return null;
    }

    private Transform FindNearestEnemyTransform(Transform exclude, Vector3 from)
    {
        GameObject[] all = GameObject.FindGameObjectsWithTag(enemyTag);
        Transform best = null;
        float bestDist = float.PositiveInfinity;

        foreach (GameObject go in all)
        {
            if (go == null || go.transform == exclude)
                continue;
            float d = PlanarDistance(from, go.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = go.transform;
            }
        }

        return best;
    }

    private void SnapToSpawnerIdlePose()
    {
        if (spawnerAnchor == null)
            return;

        if (transform.parent != spawnerAnchor)
            transform.SetParent(spawnerAnchor);

        float bob = Mathf.Sin(Time.time * levitateSpeed) * levitateAmplitude;
        transform.localPosition = Vector3.up * bob;
        transform.localRotation = Quaternion.identity;
    }

    private void CachePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
            player = p.transform;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
