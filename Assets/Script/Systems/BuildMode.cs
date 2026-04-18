using UnityEngine;
using System.Collections.Generic;

public class BuildMode : MonoBehaviour
{
    private const float PREVIEW_DISTANCE_FROM_PLAYER = 3.0f;

    public const string RangerAreaTag = "RangerArea";
    public const string UnplaceableTag = "Unplaceable";
    public const string ObstacleTag = "Obstacle";

    public List<GameObject> defencePrefabs;

    [Header("Preview materials")]
    [Tooltip("Material applied to preview renderers while building.")]
    public Material previewMaterial;

    [Header("Placement overlap")]
    [Tooltip("Ignored layers apply to normal solid geometry (e.g. ground). Tagged volumes: RangerArea — only prefabs with RangerDefence may be placed (Space). Unplaceable — prefabs tagged Obstacle cannot be placed. Use colliders with those tags, or PlacementRestrictionZone (no collider) with the same tags.")]
    [SerializeField] private LayerMask overlapIgnoreLayers;

    [Header("Defence spacing")]
    [Tooltip("Minimum gap between this preview and any placed defence (center footprint + padding). Preview snaps sideways until valid.")]
    [SerializeField] private float minSeparationBetweenDefences = 8f;

    [Header("Auto-snap when overlapping")]
    [Tooltip("If the default preview spot overlaps, search this far on the ground (XZ) for a valid position.")]
    [SerializeField] private float validPlacementSearchMaxRadius = 10f;
    [SerializeField] private float validPlacementSearchStep = 0.5f;

    [Header("Blood cost (same order as defence prefabs)")]
    [Tooltip("Blood spent when placing each defence. Index matches defencePrefabs. If missing, defaultBloodCost is used.")]
    [SerializeField] private List<int> defenceBloodCosts = new List<int>();
    [SerializeField] private int defaultBloodCost = 1;

    private GameObject spawnPreview;
    private Vector3 previewDirection = Vector3.left;
    private bool isActive;
    private int prefabIndex;
    private Renderer[] previewRenderers;

    private void Start()
    {
        if (defencePrefabs == null || defencePrefabs.Count == 0)
        {
            Debug.LogWarning("BuildMode: assign defence prefabs.");
            return;
        }

        prefabIndex = 0;
        for (int i = 0; i < defencePrefabs.Count; i++)
        {
            if (defencePrefabs[i] != null)
            {
                prefabIndex = i;
                break;
            }
        }

        if (defencePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning("BuildMode: no valid prefab in list.");
            return;
        }

        RebuildSpawnPreview();
    }

    private void Update()
    {
        if (GamePauseMenu.IsPaused)
            return;
        PoolInput();
        if (!isActive || spawnPreview == null)
            return;

        MoveSpawnPreview();
        SnapPreviewToValidNearbyIfOverlapping();
        UpdatePreviewDirectionInput();
    }

    private void UpdatePreviewDirectionInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            CyclePrefab(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            CyclePrefab(1);

        if (Input.GetKeyDown(KeyCode.A))
            previewDirection = -transform.right;
        else if (Input.GetKeyDown(KeyCode.D))
            previewDirection = transform.right;
        else if (Input.GetKeyDown(KeyCode.W))
            previewDirection = transform.forward;
        else if (Input.GetKeyDown(KeyCode.S))
            previewDirection = -transform.forward;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (PreviewPlacementBlocked())
                return;
            int cost = GetBloodCostForCurrentPrefab();
            if (BloodInventory.Instance == null || !BloodInventory.Instance.TrySpendBlood(cost))
                return;
            if (defencePrefabs != null && prefabIndex >= 0 && prefabIndex < defencePrefabs.Count && defencePrefabs[prefabIndex] != null)
                Instantiate(defencePrefabs[prefabIndex], spawnPreview.transform.position, spawnPreview.transform.rotation);
        }
    }

    private void MoveSpawnPreview()
    {
        Vector3 previewPosition = transform.position + (previewDirection * PREVIEW_DISTANCE_FROM_PLAYER);
        spawnPreview.transform.position = previewPosition;
    }

    /// <summary>If the default build spot overlaps blockers or is too close to another defence, slide the preview on XZ.</summary>
    private void SnapPreviewToValidNearbyIfOverlapping()
    {
        if (spawnPreview == null) return;
        if (!PreviewPlacementBlocked())
            return;

        Vector3 basePos = spawnPreview.transform.position;
        Quaternion rot = spawnPreview.transform.rotation;

        float step = Mathf.Max(0.1f, validPlacementSearchStep);
        float maxR = Mathf.Max(step, validPlacementSearchMaxRadius);

        for (float r = step; r <= maxR; r += step)
        {
            int segments = Mathf.Max(8, Mathf.CeilToInt((2f * Mathf.PI * r) / step));
            for (int i = 0; i < segments; i++)
            {
                float ang = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * r;
                Vector3 test = basePos + offset;
                if (IsValidPlacementAt(test, rot))
                {
                    spawnPreview.transform.position = test;
                    return;
                }
            }
        }
    }

    private bool IsValidPlacementAt(Vector3 position, Quaternion rotation)
    {
        Vector3 prevPos = spawnPreview.transform.position;
        Quaternion prevRot = spawnPreview.transform.rotation;
        spawnPreview.transform.SetPositionAndRotation(position, rotation);
        bool blocked = PreviewPlacementBlocked();
        spawnPreview.transform.SetPositionAndRotation(prevPos, prevRot);
        return !blocked;
    }

    private void CyclePrefab(int delta)
    {
        if (defencePrefabs == null || defencePrefabs.Count == 0) return;

        int start = prefabIndex;
        for (int i = 0; i < defencePrefabs.Count; i++)
        {
            prefabIndex = (prefabIndex + delta + defencePrefabs.Count) % defencePrefabs.Count;
            if (defencePrefabs[prefabIndex] != null)
            {
                RebuildSpawnPreview();
                return;
            }
        }

        prefabIndex = start;
    }

    private void RebuildSpawnPreview()
    {
        if (defencePrefabs == null || defencePrefabs.Count == 0) return;
        if (defencePrefabs[prefabIndex] == null) return;

        if (spawnPreview != null)
        {
            Destroy(spawnPreview);
            spawnPreview = null;
        }

        spawnPreview = Instantiate(defencePrefabs[prefabIndex]);
        foreach (var c in spawnPreview.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        foreach (var od in spawnPreview.GetComponentsInChildren<ObstacleDefense>(true))
            od.enabled = false;

        previewRenderers = spawnPreview.GetComponentsInChildren<Renderer>(true);
        spawnPreview.SetActive(isActive);

        MoveSpawnPreview();
        SnapPreviewToValidNearbyIfOverlapping();
        ApplyPreviewMaterial();
    }

    private int GetBloodCostForCurrentPrefab()
    {
        if (defenceBloodCosts != null && prefabIndex >= 0 && prefabIndex < defenceBloodCosts.Count)
            return Mathf.Max(0, defenceBloodCosts[prefabIndex]);
        return Mathf.Max(0, defaultBloodCost);
    }

    private bool CanAffordCurrentDefence()
    {
        int cost = GetBloodCostForCurrentPrefab();
        if (cost <= 0) return true;
        if (BloodInventory.Instance == null) return false;
        return BloodInventory.Instance.CurrentBlood >= cost;
    }

    /// <summary>Physics overlap with world, placement zones, or too close to an existing placed defence.</summary>
    private bool PreviewPlacementBlocked()
    {
        if (PreviewOverlapsBlockingCollider()) return true;
        if (PreviewOverlapsPlacementZones()) return true;
        return ViolatesDefenceSeparation();
    }

    /// <summary>Returns true if preview footprint overlaps any collider we care about (blocked placement).</summary>
    private bool PreviewOverlapsBlockingCollider()
    {
        if (spawnPreview == null || !TryGetFootprintBounds(spawnPreview, out Bounds b))
            return true;

        Quaternion rot = spawnPreview.transform.rotation;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, rot, ~0, QueryTriggerInteraction.Collide);

        bool rangerPrefab = CurrentPrefabHasRangerDefence();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider h = hits[i];
            if (h == null) continue;
            if (h.transform.IsChildOf(spawnPreview.transform)) continue;
            if (h.CompareTag("Player")) continue;

            bool onIgnoredLayer = ((1 << h.gameObject.layer) & overlapIgnoreLayers.value) != 0;

            if (TransformHasTag(h.transform, RangerAreaTag))
            {
                if (!rangerPrefab)
                    return true;
                continue;
            }

            if (TransformHasTag(h.transform, UnplaceableTag))
            {
                if (CurrentPrefabHasObstacleTag())
                    return true;
                continue;
            }

            if (h.isTrigger)
                continue;

            if (onIgnoredLayer)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>Tag may be on a parent while the collider is on a child.</summary>
    private static bool TransformHasTag(Transform t, string tag)
    {
        while (t != null)
        {
            if (t.CompareTag(tag)) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>Build footprint intersects a tagged <see cref="PlacementRestrictionZone"/> (no collider on the zone).</summary>
    private bool PreviewOverlapsPlacementZones()
    {
        if (spawnPreview == null || !TryGetFootprintBounds(spawnPreview, out Bounds candidateBounds))
            return true;

        bool rangerPrefab = CurrentPrefabHasRangerDefence();

        PlacementRestrictionZone[] zones = FindObjectsByType<PlacementRestrictionZone>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            PlacementRestrictionZone z = zones[i];
            if (z == null || !z.isActiveAndEnabled) continue;
            if (!candidateBounds.Intersects(z.GetWorldBounds())) continue;

            if (TransformHasTag(z.transform, RangerAreaTag))
            {
                if (!rangerPrefab)
                    return true;
                continue;
            }

            if (TransformHasTag(z.transform, UnplaceableTag))
            {
                if (CurrentPrefabHasObstacleTag())
                    return true;
                continue;
            }
        }

        return false;
    }

    /// <summary>True when the selected prefab or any child is tagged Obstacle (blocked in Unplaceable zones).</summary>
    private bool CurrentPrefabHasObstacleTag()
    {
        if (defencePrefabs == null || prefabIndex < 0 || prefabIndex >= defencePrefabs.Count)
            return false;
        GameObject p = defencePrefabs[prefabIndex];
        if (p == null) return false;
        foreach (Transform t in p.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.CompareTag(ObstacleTag))
                return true;
        }
        return false;
    }

    /// <summary>True when the selected defence prefab has a RangerDefence (allowed inside RangerArea).</summary>
    private bool CurrentPrefabHasRangerDefence()
    {
        if (defencePrefabs == null || prefabIndex < 0 || prefabIndex >= defencePrefabs.Count)
            return false;
        GameObject p = defencePrefabs[prefabIndex];
        return p != null && p.GetComponentInChildren<RangerDefence>(true) != null;
    }

    private bool ViolatesDefenceSeparation()
    {
        if (spawnPreview == null || !TryGetFootprintBounds(spawnPreview, out Bounds candidateBounds))
            return true;

        float pad = Mathf.Max(0f, minSeparationBetweenDefences);

        ObstacleDefense[] obstacles = FindObjectsOfType<ObstacleDefense>();
        for (int i = 0; i < obstacles.Length; i++)
        {
            ObstacleDefense od = obstacles[i];
            if (od == null) continue;
            if (IsPartOfSpawnPreview(od.transform)) continue;
            if (!TryGetFootprintBounds(od.gameObject, out Bounds other)) continue;
            if (BoundsTooClose(candidateBounds, other, pad)) return true;
        }

        RangerDefence[] rangers = FindObjectsOfType<RangerDefence>();
        for (int i = 0; i < rangers.Length; i++)
        {
            RangerDefence rd = rangers[i];
            if (rd == null) continue;
            if (IsPartOfSpawnPreview(rd.transform)) continue;
            if (!TryGetFootprintBounds(rd.gameObject, out Bounds other)) continue;
            if (BoundsTooClose(candidateBounds, other, pad)) return true;
        }

        return false;
    }

    private bool IsPartOfSpawnPreview(Transform t)
    {
        if (spawnPreview == null || t == null) return false;
        if (t == spawnPreview.transform) return true;
        return t.IsChildOf(spawnPreview.transform);
    }

    /// <summary>True if candidate intersects other footprint expanded by min gap (keeps defences side-by-side with space).</summary>
    private static bool BoundsTooClose(Bounds candidate, Bounds other, float minSeparation)
    {
        Vector3 ext = other.extents + Vector3.one * (minSeparation * 0.5f);
        Bounds expanded = new Bounds(other.center, ext * 2f);
        return candidate.Intersects(expanded);
    }

    private static bool TryGetFootprintBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        BoxCollider[] boxes = root.GetComponentsInChildren<BoxCollider>(true);
        if (boxes.Length > 0)
        {
            bounds = boxes[0].bounds;
            for (int j = 1; j < boxes.Length; j++)
                bounds.Encapsulate(boxes[j].bounds);
            return true;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return false;
        bounds = renderers[0].bounds;
        for (int j = 1; j < renderers.Length; j++)
            bounds.Encapsulate(renderers[j].bounds);
        return true;
    }

    private void ApplyPreviewMaterial()
    {
        if (previewRenderers == null || previewMaterial == null) return;
        foreach (var r in previewRenderers)
        {
            if (r != null) r.sharedMaterial = previewMaterial;
        }
    }

    private void PoolInput()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (isActive)
                DisableBuildMode();
            else
                ActivateBuildMode();
        }
    }

    private void ActivateBuildMode()
    {
        if (spawnPreview == null) return;
        isActive = true;
        spawnPreview.SetActive(true);
        ApplyPreviewMaterial();
    }

    private void DisableBuildMode()
    {
        isActive = false;
        if (spawnPreview != null)
            spawnPreview.SetActive(false);
    }
}
