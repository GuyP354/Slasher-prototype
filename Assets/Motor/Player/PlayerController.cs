using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed;
    [SerializeField] private float forwardBackwardSpeedMultiplier = 1.2f;
    public float groundDist;
    [Header("Animation")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string moveUpStateName = "MoveUp";
    [SerializeField] private string moveDownStateName = "MoveDown";
    [SerializeField] private string moveLeftStateName = "MoveLeft";
    [SerializeField] private string moveRightStateName = "MoveRight";
    [SerializeField] private float idleReturnDelaySeconds = 3f;

    public LayerMask terrainLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    private string currentAnimState;
    private float lastMovementTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()

    {
        rb = gameObject.GetComponent<Rigidbody>();
        lastMovementTime = Time.time;
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
        float x = 0f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;

        float y = 0f;
        if (Input.GetKey(KeyCode.S)) y -= 1f;
        if (Input.GetKey(KeyCode.W)) y += 1f;

        float horizontalSpeed = speed;
        float verticalSpeed = y > 0f ? speed * forwardBackwardSpeedMultiplier : speed;
        bool movingHorizontal = !Mathf.Approximately(x, 0f);
        bool movingVertical = !Mathf.Approximately(y, 0f);

        float chosenSpeed;
        if (movingHorizontal && movingVertical)
            chosenSpeed = Mathf.Min(horizontalSpeed, verticalSpeed);
        else if (movingVertical)
            chosenSpeed = verticalSpeed;
        else
            chosenSpeed = horizontalSpeed;

        Vector3 moveDir = new Vector3(x, 0, y).normalized;
        rb.linearVelocity = moveDir * chosenSpeed;
        UpdateMovementAnimation();
        if (x != 0 && x < 0)
        {
            sr.flipX = false;
        }
        else if (x != 0 && x > 0)
        {
            sr.flipY = false;
        }
    }

    private void UpdateMovementAnimation()
    {
        if (characterAnimator == null)
            return;

        string targetState = null;
        if (Input.GetKey(KeyCode.W))
            targetState = moveUpStateName;
        else if (Input.GetKey(KeyCode.S))
            targetState = moveDownStateName;
        else if (Input.GetKey(KeyCode.A))
            targetState = moveLeftStateName;
        else if (Input.GetKey(KeyCode.D))
            targetState = moveRightStateName;

        if (!string.IsNullOrEmpty(targetState))
        {
            lastMovementTime = Time.time;
            characterAnimator.speed = 1f;
            PlayAnimState(targetState, true);
            return;
        }

        // No movement key: freeze current movement pose, then return to idle after delay.
        if (currentAnimState != idleStateName)
        {
            if (Time.time - lastMovementTime >= idleReturnDelaySeconds)
            {
                characterAnimator.speed = 1f;
                PlayAnimState(idleStateName, true);
            }
            else
            {
                characterAnimator.speed = 0f;
            }
            return;
        }

        // Ensure idle remains playing normally.
        characterAnimator.speed = 1f;
    }

    private void PlayAnimState(string stateName, bool restart)
    {
        if (string.IsNullOrEmpty(stateName))
            return;

        if (restart || currentAnimState != stateName)
        {
            characterAnimator.Play(stateName, 0, 0f);
            currentAnimState = stateName;
        }
    }
}
