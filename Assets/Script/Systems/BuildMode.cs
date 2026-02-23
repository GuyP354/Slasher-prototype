using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BuildMode : MonoBehaviour
{
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
