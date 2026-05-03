using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    private float _lastDribbleTime = -999f;
    private float _lastRimTime = -999f;

    public bool IsHeld => holder != null;

    [Header("Sfx")]
    public AudioClip dribbleClip;
    [Tooltip("Minimum seconds between dribble sounds.")]
    public float dribbleCooldown = 0.08f;
    [Tooltip("Minimum impact magnitude to trigger a dribble sound.")]
    public float dribbleMinImpact = 1.0f;
    [Tooltip("When held, play dribble periodically while holder is dribbling (seconds).")]
    public float heldDribbleInterval = 0.3f;
    [Tooltip("Volume for held dribble (0-1).")]
    public float heldDribbleVolume = 0.8f;
    [Header("Rim")]
    [Tooltip("Clip to play when the ball hits the rim.")]
    public AudioClip rimClip;
    [Tooltip("Minimum impact magnitude to trigger a rim hit sound.")]
    public float rimMinImpact = 1.0f;
    [Tooltip("Volume for rim hit (0-1)")]
    [Range(0f,1f)] public float rimVolume = 0.9f;
    [Tooltip("Minimum seconds between rim hit sounds to avoid repeats.")]
    public float rimCooldown = 0.06f;

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
        Vector3 newPos = holder.position + offset;
        if (!MathUtils.IsFinite(newPos))
        {
            Debug.LogWarning($"[BTierHoops] Skipping invalid ball position: {newPos}");
            return;
        }
        transform.position = newPos;

        // Play periodic dribble when the ball is held and the holder is in dribble motion
        if (dribbleClip != null && anim != null && anim.IsDribbling)
        {
            if (Time.time - _lastDribbleTime >= heldDribbleInterval)
            {
                SoundHandler.Instance?.PlayClip(dribbleClip, heldDribbleVolume);
                _lastDribbleTime = Time.time;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Only play dribble when ball is free (not held)
        if (IsHeld) return;

        // Ensure we have a clip
        if (dribbleClip == null) return;

        // Respect cooldown
        if (Time.time - _lastDribbleTime < dribbleCooldown) return;

        // Check contact normal to ensure a downward hit (ball hitting ground from above)
        if (collision.contactCount == 0) return;
        var contact = collision.GetContact(0);
        if (contact.normal.y < 0.5f) return;

        // Use relative velocity magnitude as impact strength
        float impact = collision.relativeVelocity.magnitude;
        if (impact < dribbleMinImpact) return;

        // Scale volume by impact (tweak divisor to taste)
        float volume = Mathf.Clamp01(impact / 6f);
        SoundHandler.Instance?.PlayClip(dribbleClip, volume);
        _lastDribbleTime = Time.time;

        // Rim hit detection: if we collided with something that is a child of a Hoop, play rim sound
        if (rimClip != null && collision.collider != null)
        {
            var hoop = collision.collider.GetComponentInParent<Hoop>();
            if (hoop != null)
            {
                float rimImpact = collision.relativeVelocity.magnitude;
                if (rimImpact >= rimMinImpact && Time.time - _lastRimTime >= rimCooldown)
                {
                    float rv = Mathf.Clamp01(rimImpact / 6f) * rimVolume;
                    Debug.Log($"[BTierHoops] Rim hit detected (impact={rimImpact:F2}, vol={rv:F2}) on {collision.collider.name}");
                    SoundHandler.Instance?.PlayClip(rimClip, rv);
                    _lastRimTime = Time.time;
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (dribbleClip == null)
        {
            string[] results = AssetDatabase.FindAssets("dribble t:AudioClip");
            if (results.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(results[0]);
                dribbleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
        if (rimClip == null)
        {
            string[] r = AssetDatabase.FindAssets("rimhit t:AudioClip");
            if (r.Length == 0) r = AssetDatabase.FindAssets("rim t:AudioClip");
            if (r.Length > 0)
            {
                string p = AssetDatabase.GUIDToAssetPath(r[0]);
                rimClip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
            }
        }
    }
#endif
}
