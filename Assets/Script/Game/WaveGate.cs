using UnityEngine;

public class WaveGate : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private Wave waveToStart;

    [Header("Blocking")]
    [SerializeField] private Collider[] blockingColliders; // solid colliders to toggle
    [SerializeField] private UnityEngine.AI.NavMeshObstacle navObstacle;   // optional

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerInTrigger;
    private Transform player;
    private bool isClosed;

    //private void OnEnable()
   // {
        //if (LevelManager.Instance != null)
            //LevelManager.Instance.WaveEnds += Open;
   // }

    //private void OnDisable()
    //{
        //if (LevelManager.Instance != null)
           // LevelManager.Instance.if WaveEnds -= Open;
   // }

    private void Update()
    {
        if (!playerInTrigger || player == null)
            return;

        if (!IsPlayerOnRightSide(player))
            return;

        if (Input.GetKeyDown(interactKey))
        {
            Close();
            if (waveToStart != null)
                waveToStart.StartWave();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInTrigger = true;
        player = other.transform;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInTrigger = false;
        player = null;
    }

    private bool IsPlayerOnRightSide(Transform playerTransform)
    {
        // "Right side" is this object's local +X side.
        Vector3 local = transform.InverseTransformPoint(playerTransform.position);
        return local.x > 0f;
    }

    private void Close()
    {
        if (isClosed) return;
        isClosed = true;

        for (int i = 0; i < blockingColliders.Length; i++)
            if (blockingColliders[i] != null) blockingColliders[i].enabled = true;

        if (navObstacle != null) navObstacle.enabled = true;
    }

    private void Open()
    {
        isClosed = false;

        for (int i = 0; i < blockingColliders.Length; i++)
            if (blockingColliders[i] != null) blockingColliders[i].enabled = false;

        if (navObstacle != null) navObstacle.enabled = false;
    }
}