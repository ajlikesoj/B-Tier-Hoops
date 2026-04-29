using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 12f;
    [Range(0f, 1f)] public float shortHopMultiplier = 0.4f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.18f;

    private Rigidbody2D rb;
    private bool isGrounded;
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
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
