using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

public class BuildMode : MonoBehaviour
{
    private const float PREVIEW_DISTANCE_FROM_PLAYER = 3.0f;
    public List<GameObject> defencePrefabs;
    private Transform playerTransform;
    private Transform cameraTransform;
    private GameObject spawnPreview;

    private bool isActive = false;

    private void Start()
    {
        //Create the Defence preview
        spawnPreview = Instantiate(defencePrefabs[0]) as GameObject;
        spawnPreview.active = false;
    }

    private void Update()
    {
        PoolInput();
        if(!isActive)
            return;
        MoveSpawnPreview ();
    }

    private void MoveSpawnPreview()
    {
        Vector3 previewPosition = (transform.position) + (-transform.right * PREVIEW_DISTANCE_FROM_PLAYER);
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
        spawnPreview.active = true;
    }

    private void DisableBuildMode()
    {
        isActive = false;
        spawnPreview.active = false;
    }
}
