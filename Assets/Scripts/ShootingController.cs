using UnityEngine;
using UnityEngine.InputSystem;

public class ShootingController : MonoBehaviour
{
    [Header("References")]
    public BasketballBall ball;
    public Transform targetHoop;
    [Tooltip("Which hoop this shooter targets. +1 = right, -1 = left. Auto-derived from targetHoop on Awake.")]
    public int targetSide = 1;

    [Header("Pickup")]
    public float pickupRange = 1.6f;
    [Tooltip("Pickup distance is measured from this offset below the player's center (toward the feet).")]
    public float pickupCheckYOffset = -0.6f;

    [Header("Charge")]
    public float chargeTime = 1.0f;
    [Range(0f, 1f)] public float perfectChargeMin = 0.55f;
    [Range(0f, 1f)] public float perfectChargeMax = 0.92f;

    [Header("Shot Physics")]
    [Tooltip("Time of flight for a 'perfect' shot, in seconds. Higher = taller arc, slower shot.")]
    public float shotFlightTime = 1.3f;
    [Tooltip("Velocity multiplier at zero charge — shot falls way short.")]
    public float minPowerMultiplier = 0.55f;
    [Tooltip("Velocity multiplier at full charge — shot flies over the backboard.")]
    public float maxPowerMultiplier = 1.75f;
    [Tooltip("Random velocity error added when timing is off (small).")]
    public float maxAimErrorVelocity = 0.6f;

    [Header("Dunk")]
    [Tooltip("Max horizontal distance from the rim to trigger a dunk attempt.")]
    public float dunkRange = 2.0f;
    [Tooltip("How far above the player's center their extended arm reaches during a dunk. Player.y + this must be >= rim.y for a dunk to count — prevents dunking from directly below the rim while grounded or low in the air.")]
    public float dunkReachHeight = 1.5f;
    [Tooltip("Extra upward impulse applied to the shooter on dunk for visual flair.")]
    public float dunkHopImpulse = 6f;
    [Tooltip("Downward velocity applied to the ball on dunk. Must clear Hoop.minDownwardSpeed to count.")]
    public float dunkBallSpeed = 10f;

    [Header("State (read-only)")]
    [Range(0f, 1f)] public float currentCharge;
    public bool isCharging;
    [Header("Sfx")]
    public AudioClip chargeupClip;
    [Tooltip("Loop volume for charge-up (0-1)")]
    [Range(0f,1f)] public float chargeupVolume = 0.9f;

    private PlayerController playerCtrl;
    private CharacterAnimationController anim;

    void Awake()
    {
        playerCtrl = GetComponent<PlayerController>();
        anim = GetComponent<CharacterAnimationController>();
        if (targetHoop != null)
        {
            var hoop = targetHoop.GetComponent<Hoop>();
            if (hoop != null) targetSide = hoop.side;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (chargeupClip == null)
        {
            string[] results = UnityEditor.AssetDatabase.FindAssets("chargeup t:AudioClip");
            if (results.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(results[0]);
                chargeupClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
    }
#endif

    void Update()
    {
        if (ball == null || targetHoop == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        TryAutoPickup();

        if (!ball.IsHeld || ball.holder != transform)
        {
            // Ball was stolen / released / never-held — kill the charge-up loop so it doesn't keep
            // playing after we've lost possession.
            if (isCharging) SoundHandler.Instance?.StopLoop();
            isCharging = false;
            currentCharge = 0f;
            if (anim != null) anim.SetCharge(0f);
            return;
        }

        bool fDown = kb.fKey.wasPressedThisFrame;
        bool fHeld = kb.fKey.isPressed;
        bool fUp = kb.fKey.wasReleasedThisFrame;

        if (fDown)
        {
            isCharging = true;
            currentCharge = 0f;
            if (anim != null) anim.SetCharge(0f);
            // start charge-up loop
            if (chargeupClip != null)
                SoundHandler.Instance?.PlayLoop(chargeupClip, chargeupVolume);
        }

        if (isCharging && fHeld)
        {
            currentCharge = Mathf.Clamp01(currentCharge + Time.deltaTime / chargeTime);
            if (anim != null) anim.SetCharge(currentCharge);
        }

        if (fUp && isCharging)
        {
            // stop charge-up loop before shooting
            SoundHandler.Instance?.StopLoop();
            Shoot();
            isCharging = false;
            currentCharge = 0f;
            if (anim != null) anim.SetCharge(0f);
        }
    }

    void TryAutoPickup()
    {
        if (ball.IsHeld) return;
        if (!ball.CanBePickedUp()) return;
        Vector2 checkPos = (Vector2)transform.position + Vector2.up * pickupCheckYOffset;
        float dist = Vector2.Distance(checkPos, ball.transform.position);
        if (dist <= pickupRange)
        {
            ball.Pickup(transform);
        }
    }

    void Shoot()
    {
        // Ensure any charge loop is stopped when shooting
        SoundHandler.Instance?.StopLoop();
        if (TryDunk())
        {
            if (anim != null) anim.TriggerDunk();
            return;
        }

        // No shot cancellation — the under-rim one-way barrier physically blocks balls fired from
        // beneath the rim, so the ball bounces off the underside instead of scoring.

        Vector2 from = ball.transform.position;
        Vector2 to = targetHoop.position;
        float ballGravityScale = ball.GetComponent<Rigidbody2D>().gravityScale;
        Vector2 baseVel = CalculateLaunchVelocity(from, to, shotFlightTime, ballGravityScale);

        float powerMul = ChargeToPowerMultiplier(currentCharge);
        float timing = TimingError(currentCharge);
        Vector2 error = (Vector2)Random.insideUnitCircle * (timing * maxAimErrorVelocity);

        Vector2 finalVel = baseVel * powerMul + error;

        // 3-point detection: shooter in their own half (opposite side from their target hoop)
        bool fromOwnHalf = (transform.position.x * targetSide) < 0f;
        ball.lastShotPoints = fromOwnHalf ? 3 : 2;

        ball.Release(finalVel);
        if (anim != null) anim.TriggerShoot(currentCharge);
    }

    bool TryDunk()
    {
        bool airborne = playerCtrl != null && !playerCtrl.IsGrounded;
        if (!airborne) return false;
        // Horizontal proximity to the rim
        if (Mathf.Abs(transform.position.x - targetHoop.position.x) > dunkRange) return false;
        // Arm-at-or-above-rim — no dunks from directly below the rim
        if (transform.position.y + dunkReachHeight < targetHoop.position.y) return false;

        DoDunk();
        return true;
    }


    void DoDunk()
    {
        // Small upward hop on the player for visual flair
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(rb.linearVelocity.x, dunkHopImpulse);

        // Two-hand throwdown visual: spawn the ball clearly above the rim center, then hammer it down.
        ball.lastShotPoints = 2;
        ball.Release(new Vector2(0f, -Mathf.Max(12f, dunkBallSpeed)));
        Vector3 dunkPos = (Vector3)targetHoop.position + new Vector3(0f, 0.42f, 0f);
        dunkPos = MathUtils.ClampFinite(dunkPos, ball.transform.position);
        if (dunkPos != (Vector3)targetHoop.position + new Vector3(0f, 0.42f, 0f))
            Debug.LogWarning($"[BTierHoops] Replaced invalid player dunk position with {dunkPos}");
        ball.transform.position = dunkPos;
        SoundHandler.Instance?.PlayDunkBoom();
        Debug.Log("[BTierHoops] DUNK by player!");
    }

    float ChargeToPowerMultiplier(float charge)
    {
        if (charge >= perfectChargeMin && charge <= perfectChargeMax)
            return 1.0f;
        if (charge < perfectChargeMin)
        {
            float t = charge / Mathf.Max(0.001f, perfectChargeMin);
            return Mathf.Lerp(minPowerMultiplier, 1.0f, t);
        }
        float tt = (charge - perfectChargeMax) / Mathf.Max(0.001f, 1f - perfectChargeMax);
        return Mathf.Lerp(1.0f, maxPowerMultiplier, tt);
    }

    float TimingError(float charge)
    {
        if (charge >= perfectChargeMin && charge <= perfectChargeMax) return 0f;
        if (charge < perfectChargeMin)
            return 1f - Mathf.InverseLerp(0f, perfectChargeMin, charge);
        return Mathf.InverseLerp(perfectChargeMax, 1f, charge);
    }

    Vector2 CalculateLaunchVelocity(Vector2 from, Vector2 to, float T, float gravityScale)
    {
        Vector2 delta = to - from;
        float g = Physics2D.gravity.y * gravityScale;
        float vx = delta.x / T;
        float vy = (delta.y - 0.5f * g * T * T) / T;
        return new Vector2(vx, vy);
    }
}
