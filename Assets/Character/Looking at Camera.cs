using UnityEngine;

public class LookingatCamera : MonoBehaviour
{
    [SerializeField]
    private Camera _mainCamera;

    private void LateUpdate()
    {
        //Get the camera position
        Vector3 cameraPosition
            = _mainCamera.transform.position;
        //We only want to rotate on Y axis:
        cameraPosition.y
            = transform.position.y;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
