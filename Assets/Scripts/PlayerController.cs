using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 15f;
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

    [Header("Sfx")]
    [Tooltip("Clip to play when the player changes walking direction.")]
    public AudioClip squeakClip;
    [Tooltip("Minimum seconds between squeaks.")]
    public float squeakCooldown = 0.12f;
    float _lastSqueakTime = -999f;
    int _prevFacingSign = 1;

    #if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-assign squeak clip in editor if not set
        if (squeakClip == null)
        {
            string[] results = AssetDatabase.FindAssets("squeak t:AudioClip");
            if (results.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(results[0]);
                squeakClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
    }
    #endif

    void Start()
    {
        // Ensure SoundHandler exists
        if (SoundHandler.Instance == null)
        {
            var soundGO = new GameObject("SoundHandler");
            soundGO.AddComponent<SoundHandler>();
            Debug.Log("[BTierHoops] Created SoundHandler");
        }
    }

    private Rigidbody2D rb;
    private CharacterAnimationController anim;
    private bool isGrounded;
    public bool IsGrounded => isGrounded;
    private readonly Collider2D[] groundHits = new Collider2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<CharacterAnimationController>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        // initialize previous facing from Visuals localScale if present
        var v = transform.Find("Visuals");
        if (v != null) _prevFacingSign = v.localScale.x < 0f ? -1 : 1;
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
            if (anim != null) anim.TriggerJump();
        }

        if (jumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * shortHopMultiplier);
        }

        // Q = steal (must be touching opponent who is holding the ball)
        if (kb.qKey.wasPressedThisFrame) TryStealFromOpponent();

        // Sprite facing and squeak on direction change (inferred from keys pressed)
        int currentSign = horizontal > 0.01f ? 1 : (horizontal < -0.01f ? -1 : 0);
        if (currentSign != 0 && currentSign != _prevFacingSign)
        {
            // play squeak when changing direction
            Debug.Log($"[BTierHoops] Player changed direction: {_prevFacingSign} ? {currentSign}, squeakClip={squeakClip}");
            if (squeakClip != null && Time.time - _lastSqueakTime >= squeakCooldown)
            {
                Debug.Log("[BTierHoops] Playing squeak!");
                if (SoundHandler.Instance != null)
                {
                    SoundHandler.Instance.PlayClip(squeakClip);
                }
                else
                {
                    Debug.LogError("[BTierHoops] SoundHandler.Instance is null!");
                }
                _lastSqueakTime = Time.time;
            }
            else if (squeakClip == null)
            {
                Debug.LogError("[BTierHoops] squeakClip is NULL - please assign it in Inspector!");
            }
            _prevFacingSign = currentSign;
        }

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
        if (anim != null) anim.TriggerSteal();
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
