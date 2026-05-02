using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;     // dialed down from 6 — player was overpowered
    public float jumpForce = 12f;    // dialed down from 15 — peak reach now ~2.44 vs rim 2.10, dunks need tighter timing
    [Range(0f, 1f)] public float shortHopMultiplier = 0.4f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;

    [Header("Steal")]
    public Transform opponent;
    public BasketballBall ball;
    [Tooltip("Local offset from player position to the 'arm/hand' point used for steal contact. X is mirrored by facing.")]
    public Vector2 armReachOffset = new Vector2(0.55f, 0.2f);
    [Tooltip("Ball must be within this radius of the arm point to be stolen.")]
    public float armReachRadius = 0.5f;

    private Rigidbody2D rb;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private readonly Collider2D[] groundHits = new Collider2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        UpdateGrounded();

        var kb = Keyboard.current;
        if (kb == null) return;

        float horizontal = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) horizontal -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) horizontal += 1f;

        rb.linearVelocity = new Vector2(horizontal * moveSpeed, rb.linearVelocity.y);

        bool jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame;
        bool jumpReleased = kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame;

        if (isGrounded && jumpPressed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortHopMultiplier);
        }

        // Q = steal (must be touching opponent who is holding the ball)
        if (kb.qKey.wasPressedThisFrame) TryStealFromOpponent();

        // Sprite facing
        if (Mathf.Abs(horizontal) > 0.01f)
        {
            var v = transform.Find("Visuals");
            if (v != null)
            {
                var s = v.localScale;
                s.x = horizontal > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                v.localScale = s;
            }
        }
    }

    void TryStealFromOpponent()
    {
        if (ball == null || !ball.IsHeld) return;
        if (ball.holder == transform) return;
        if (opponent == null || ball.holder != opponent) return;
        if (Vector2.Distance(GetArmPosition(), ball.transform.position) > armReachRadius) return;

        ball.Steal(transform);
        Debug.Log("[BTierHoops] Player stole the ball.");
    }

    Vector2 GetArmPosition()
    {
        var visuals = transform.Find("Visuals");
        float facing = (visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(armReachOffset.x * facing, armReachOffset.y);
    }

    void UpdateGrounded()
    {
        isGrounded = false;
        if (groundCheck == null) return;

        int count = Physics2D.OverlapCircleNonAlloc(groundCheck.position, groundCheckRadius, groundHits);
        for (int i = 0; i < count; i++)
        {
            var c = groundHits[i];
            if (c == null) continue;
            if (c.transform == transform || c.transform.IsChildOf(transform)) continue;
            isGrounded = true;
            return;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.cyan;
        var visuals = transform.Find("Visuals");
        float facing = (Application.isPlaying && visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        Vector3 arm = transform.position + new Vector3(armReachOffset.x * facing, armReachOffset.y, 0f);
        Gizmos.DrawWireSphere(arm, armReachRadius);
    }
}
