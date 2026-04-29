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

    public bool CanBePickedUp() => !IsHeld && Time.time - lastReleasedTime > pickupCooldown;

    void LateUpdate()
    {
        if (holder == null) return;
        var visuals = holder.Find("Visuals");
        float facing = (visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        Vector3 offset = new Vector3(holdOffset.x * facing, holdOffset.y, -0.02f);
        transform.position = holder.position + offset;
    }
}
