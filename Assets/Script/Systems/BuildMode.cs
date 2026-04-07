using UnityEngine;
using System.Collections.Generic;

public class BuildMode : MonoBehaviour
{
    private const float PREVIEW_DISTANCE_FROM_PLAYER = 3.0f;

    public List<GameObject> defencePrefabs;

    [Header("Preview materials")]
    [Tooltip("Material when placement is valid.")]
    public Material previewMaterial;
    [Tooltip("Material when preview overlaps something (e.g. red).")]
    public Material invalidPreviewMaterial;

    [Header("Placement overlap")]
    [Tooltip("Hits on these layers are ignored (e.g. Ground / Terrain) so the preview can sit on the floor.")]
    [SerializeField] private LayerMask overlapIgnoreLayers;

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
    private bool lastOverlapInvalid = true;

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
        PoolInput();
        if (!isActive || spawnPreview == null)
            return;

        MoveSpawnPreview();
        SnapPreviewToValidNearbyIfOverlapping();
        UpdatePreviewDirectionInput();

        bool invalid = IsPlacementInvalid();
        if (invalid != lastOverlapInvalid)
        {
            lastOverlapInvalid = invalid;
            ApplyPreviewVisuals(invalid);
        }
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
            if (PreviewOverlapsBlockingCollider())
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

    /// <summary>If the default build spot overlaps blockers, slide the preview on XZ to the nearest valid position.</summary>
    private void SnapPreviewToValidNearbyIfOverlapping()
    {
        if (spawnPreview == null) return;
        if (!PreviewOverlapsBlockingCollider())
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
        bool blocked = PreviewOverlapsBlockingCollider();
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
        bool inv = IsPlacementInvalid();
        lastOverlapInvalid = inv;
        ApplyPreviewVisuals(inv);
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

    /// <summary>True when overlap, unaffordable blood cost, or no inventory.</summary>
    private bool IsPlacementInvalid()
    {
        if (PreviewOverlapsBlockingCollider()) return true;
        return !CanAffordCurrentDefence();
    }

    /// <summary>Returns true if preview footprint overlaps any collider we care about (blocked placement).</summary>
    private bool PreviewOverlapsBlockingCollider()
    {
        if (spawnPreview == null || !TryGetFootprintBounds(spawnPreview, out Bounds b))
            return true;

        Quaternion rot = spawnPreview.transform.rotation;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, rot, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider h = hits[i];
            if (h == null) continue;
            if (h.transform.IsChildOf(spawnPreview.transform)) continue;
            if (h.CompareTag("Player")) continue;
            if (((1 << h.gameObject.layer) & overlapIgnoreLayers.value) != 0) continue;
            return true;
        }

        return false;
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

    private void ApplyPreviewVisuals(bool invalid)
    {
        if (previewRenderers == null) return;

        if (invalid)
        {
            if (invalidPreviewMaterial != null)
            {
                foreach (var r in previewRenderers)
                {
                    if (r != null) r.sharedMaterial = invalidPreviewMaterial;
                }
            }
            return;
        }

        if (previewMaterial != null)
        {
            foreach (var r in previewRenderers)
            {
                if (r != null) r.sharedMaterial = previewMaterial;
            }
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
        bool inv = IsPlacementInvalid();
        lastOverlapInvalid = inv;
        ApplyPreviewVisuals(inv);
    }

    private void DisableBuildMode()
    {
        isActive = false;
        if (spawnPreview != null)
            spawnPreview.SetActive(false);
    }
}
