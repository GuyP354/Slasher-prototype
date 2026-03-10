using UnityEngine;
using System.Collections.Generic;

public class BuildMode : MonoBehaviour
{
    private const float PREVIEW_DISTANCE_FROM_PLAYER = 3.0f;

    public List<GameObject> defencePrefabs;

    [Header("Preview")]
    public Material previewMaterial; // assign in Inspector (optional)
    private GameObject spawnPreview;

    // This is the Vector you asked for (changes with WASD / Arrow keys)
    private Vector3 previewDirection = Vector3.left;

    private bool isActive = false;

    private void Start()
    {
        // Create the Defence preview (same prefab as before)
        spawnPreview = Instantiate(defencePrefabs[0]);
        ApplyPreviewMaterial(spawnPreview);
        spawnPreview.SetActive(false);
    }

    private void Update()
    {
        PoolInput();
        if (!isActive)
            return;

        UpdatePreviewDirectionInput();
        MoveSpawnPreview();
    }

    private void UpdatePreviewDirectionInput()
    {
        // Only 4 directions (no diagonals). Uses KeyDown so it "snaps" direction.
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            previewDirection = -transform.right;      // left of player
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            previewDirection = transform.right;       // right of player
        else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            previewDirection = transform.forward;    // behind player
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            previewDirection = -transform.forward;     // in front of player
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Instantiate(defencePrefabs[0], spawnPreview.transform.position, spawnPreview.transform.rotation);
        }
    }
    

    private void MoveSpawnPreview()
    {
        Vector3 previewPosition = transform.position + (previewDirection * PREVIEW_DISTANCE_FROM_PLAYER);
        spawnPreview.transform.position = previewPosition;
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
        isActive = true;
        spawnPreview.SetActive(true);
    }

    private void DisableBuildMode()
    {
        isActive = false;
        spawnPreview.SetActive(false);
    }

    private void ApplyPreviewMaterial(GameObject previewObj)
    {
        if (previewMaterial == null || previewObj == null)
            return;

        // Apply to all renderers on the preview object (and children)
        var renderers = previewObj.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = previewMaterial;
       
    }
}