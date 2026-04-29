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

    [Header("State (read-only)")]
    [Range(0f, 1f)] public float currentCharge;
    public bool isCharging;

    void Awake()
    {
        if (targetHoop != null)
        {
            var hoop = targetHoop.GetComponent<Hoop>();
            if (hoop != null) targetSide = hoop.side;
        }
    }

    void Update()
    {
        if (ball == null || targetHoop == null) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        TryAutoPickup();

        if (!ball.IsHeld || ball.holder != transform)
        {
            isCharging = false;
            currentCharge = 0f;
            return;
        }

        bool fDown = kb.fKey.wasPressedThisFrame;
        bool fHeld = kb.fKey.isPressed;
        bool fUp = kb.fKey.wasReleasedThisFrame;

        if (fDown)
        {
            isCharging = true;
            currentCharge = 0f;
        }

        if (isCharging && fHeld)
        {
            currentCharge = Mathf.Clamp01(currentCharge + Time.deltaTime / chargeTime);
        }

        if (fUp && isCharging)
        {
            Shoot();
            isCharging = false;
            currentCharge = 0f;
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
