using UnityEngine;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float speed;
    public float forwardBackwardSpeedMultiplier = 1.5f;
    public float groundDist;

    public LayerMask terrainLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    private readonly List<KeyCode> movementKeyOrder = new List<KeyCode>();
    private static readonly KeyCode[] movementKeys = { KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()

    {
        rb = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Vector3 castPos = transform.position;
        castPos.y += 1;
        if (Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, terrainLayer))
        {
            if (hit.collider != null)
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + groundDist;
                transform.position = movePos;
            }
        }
        UpdateMovementKeyOrder();
        Vector2 moveInput = GetPrioritizedMovementInput();
        float x = moveInput.x;
        float y = moveInput.y;
        Vector3 moveVelocity = new Vector3(
            x * speed,
            rb.linearVelocity.y,
            y * speed * forwardBackwardSpeedMultiplier
        );
        rb.linearVelocity = moveVelocity;
        if (x != 0 && x < 0)
        {
            sr.flipX = false;
        }
        else if (x != 0 && x > 0)
        {
            sr.flipY = false;
        }
    }

    void UpdateMovementKeyOrder()
    {
        for (int i = 0; i < movementKeys.Length; i++)
        {
            KeyCode key = movementKeys[i];
            if (Input.GetKeyDown(key))
            {
                movementKeyOrder.Remove(key);
                movementKeyOrder.Add(key);
            }

            if (Input.GetKeyUp(key))
            {
                movementKeyOrder.Remove(key);
            }
        }
    }

    Vector2 GetPrioritizedMovementInput()
    {
        while (movementKeyOrder.Count > 0 && !Input.GetKey(movementKeyOrder[0]))
        {
            movementKeyOrder.RemoveAt(0);
        }

        if (movementKeyOrder.Count == 0)
        {
            return Vector2.zero;
        }

        KeyCode activeKey = movementKeyOrder[0];
        if (activeKey == KeyCode.W) return Vector2.up;
        if (activeKey == KeyCode.S) return Vector2.down;
        if (activeKey == KeyCode.A) return Vector2.left;
        if (activeKey == KeyCode.D) return Vector2.right;
        return Vector2.zero;
    }
}
