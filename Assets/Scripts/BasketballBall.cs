using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BasketballBall : MonoBehaviour
{
    [Header("Hold")]
    public Vector2 holdOffset = new Vector2(0.45f, 0.05f);
    public float pickupCooldown = 0.4f;

    [HideInInspector] public Transform holder;
    [HideInInspector] public int lastShotPoints = 2;
    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float lastReleasedTime = -999f;

    public bool IsHeld => holder != null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
    }

    public void Pickup(Transform newHolder)
    {
        holder = newHolder;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
    }

    public void Release(Vector2 velocity)
    {
        holder = null;
        rb.bodyType = RigidbodyType2D.Dynamic;
        col.enabled = true;
        rb.linearVelocity = velocity;
        rb.angularVelocity = -velocity.x * 30f;
        lastReleasedTime = Time.time;
    }

    /// <summary>Transfer possession instantly without re-enabling physics. Caller must already hold the ball before this is taken.</summary>
    public void Steal(Transform newHolder)
    {
        if (newHolder == null) return;
        holder = newHolder;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        lastReleasedTime = -999f;
    }

    public bool CanBePickedUp()
    {
        if (IsHeld) return false;
        if (Time.time - lastReleasedTime <= pickupCooldown) return false;
        // Block all pickups between a make and the scheduled court reset — otherwise an AI standing
        // by the rim after a dunk grabs the ball back inside the 1s reset window and dunks again,
        // pushing resetAtTime further out each time and locking the game into a scoring loop.
        if (GameManager.Instance != null && !GameManager.Instance.BallInPlay) return false;
        return true;
    }

    void LateUpdate()
    {
        if (holder == null) return;
        var visuals = holder.Find("Visuals");
        float facing = (visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        Vector2 localOffset = new Vector2(holdOffset.x, holdOffset.y);
        var anim = holder.GetComponent<CharacterAnimationController>();
        if (anim != null)
            localOffset = anim.GetBallHoldOffset(localOffset, facing);
        else
            localOffset.x *= facing;

        Vector3 offset = new Vector3(localOffset.x, localOffset.y, -0.02f);
        transform.position = holder.position + offset;
    }
}
