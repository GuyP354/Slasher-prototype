using System.Collections;
using UnityEngine;

public class LeverPlatform : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private Transform interactionCenter;
    [SerializeField] private Vector3 interactionSize = new Vector3(2f, 2f, 2f);

    [Header("Standstill")]
    [SerializeField] private float standstillSeconds = 1f;

    private Transform playerTransform;
    private PlayerController playerController;
    private Rigidbody playerRb;
    private bool isInteracting;

    private void Update()
    {
        if (playerTransform == null)
            CachePlayer();

        if (playerTransform == null || isInteracting) return;
        if (!IsPlayerInInteractionArea()) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (playerController == null)
            playerController = playerTransform.GetComponent<PlayerController>();

        if (playerRb == null)
            playerRb = playerTransform.GetComponent<Rigidbody>();

        if (playerController == null) return;

        StartCoroutine(StandstillCoroutine());
    }

    private void CachePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null) return;

        playerTransform = player.transform;
        playerController = player.GetComponent<PlayerController>();
        playerRb = player.GetComponent<Rigidbody>();
    }

    private bool IsPlayerInInteractionArea()
    {
        Transform center = interactionCenter != null ? interactionCenter : transform;
        Vector3 local = center.InverseTransformPoint(playerTransform.position);
        Vector3 half = interactionSize * 0.5f;

        return Mathf.Abs(local.x) <= half.x
            && Mathf.Abs(local.y) <= half.y
            && Mathf.Abs(local.z) <= half.z;
    }

    private IEnumerator StandstillCoroutine()
    {
        isInteracting = true;

        bool wasEnabled = playerController.enabled;
        playerController.enabled = false;

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        yield return new WaitForSeconds(standstillSeconds);

        if (playerController != null)
            playerController.enabled = wasEnabled;

        isInteracting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Transform center = interactionCenter != null ? interactionCenter : transform;
        Gizmos.color = Color.cyan;
        Gizmos.matrix = center.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, interactionSize);
    }
}

