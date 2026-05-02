using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AIController : MonoBehaviour
{
    public enum State { Idle, ChaseBall, Attack, Defend }
    public enum Tier { F, D, C, B, A, S }

    /// <summary>Visual + stat preset for one AI tier. Shared between AIController (stats) and CourtBuilder (visuals).</summary>
    public struct TierData
    {
        public string displayName;
        public Color skinColor;
        public Color hairColor;
        public float heightScale;
        public float widthScale;
        public float moveSpeed;
        public float reactionDelay;
        public float twoPointAccuracy;
        public float threePointAccuracy;
        public float shotWindupTime;
        public float optimalShotDistance;
        public float defenseSpeedMultiplier;
        public float defenseStandoff;
        public float stealAttemptCooldown;
        public Vector2 armReachOffset;
        public float armReachRadius;
        public float jumpBlockChance; // 0 = never jumps to block, 1 = always jumps when player starts a shot
        public bool canDunk;          // tier knows how to drive the rim and jump for a dunk
        public float dunkAttemptChance; // 0–1 per-possession chance to attempt a dunk drive (if canDunk)
    }

    public static TierData GetTierData(Tier t)
    {
        switch (t)
        {
            case Tier.F: // Afsar — slow, big, terrible shot
                return new TierData {
                    displayName = "AFSAR",
                    skinColor = new Color(0.36f, 0.24f, 0.16f),
                    hairColor = new Color(0.05f, 0.04f, 0.03f),
                    heightScale = 1.35f,
                    widthScale = 1.55f,
                    moveSpeed = 2.5f,
                    reactionDelay = 0.75f,
                    twoPointAccuracy = 0.30f,
                    threePointAccuracy = 0.18f,
                    shotWindupTime = 1.0f,
                    optimalShotDistance = 3.5f,
                    defenseSpeedMultiplier = 0.55f,
                    defenseStandoff = 3.0f,
                    stealAttemptCooldown = 2.5f,
                    armReachOffset = new Vector2(0.85f, 0.27f),
                    armReachRadius = 0.55f,
                    jumpBlockChance = 0.0f,        // not athletic — never jumps to block
                    canDunk = false,
                    dunkAttemptChance = 0f,
                };
            case Tier.D: // Praneel — tall + skinny, poor mobility, poor shooting
                return new TierData {
                    displayName = "PRANEEL",
                    skinColor = new Color(0.66f, 0.50f, 0.36f),
                    hairColor = new Color(0.10f, 0.07f, 0.05f),
                    heightScale = 1.20f,
                    widthScale = 0.85f,
                    moveSpeed = 3.2f,
                    reactionDelay = 0.55f,
                    twoPointAccuracy = 0.45f,
                    threePointAccuracy = 0.30f,
                    shotWindupTime = 0.80f,
                    optimalShotDistance = 4.0f,
                    defenseSpeedMultiplier = 0.70f,
                    defenseStandoff = 2.4f,
                    stealAttemptCooldown = 2.0f,
                    armReachOffset = new Vector2(0.47f, 0.24f),
                    armReachRadius = 0.50f,
                    jumpBlockChance = 0.10f,       // skinny, poor mobility — rarely tries to block
                    canDunk = false,
                    dunkAttemptChance = 0f,
                };
            case Tier.C: // Krish — average build, decent speed, excellent 3pt + decent 2pt, athletic
                return new TierData {
                    displayName = "KRISH",
                    skinColor = new Color(0.78f, 0.60f, 0.44f),
                    hairColor = new Color(0.10f, 0.07f, 0.05f),
                    heightScale = 1.00f,
                    widthScale = 1.00f,
                    moveSpeed = 4.5f,
                    reactionDelay = 0.35f,
                    twoPointAccuracy = 0.55f,
                    threePointAccuracy = 0.85f,
                    shotWindupTime = 0.35f,        // quicker release — less time for the player to bother him
                    optimalShotDistance = 8.0f,    // mid-range default; his 3pt skill kicks in only when defense pushes him back
                    defenseSpeedMultiplier = 0.85f,
                    defenseStandoff = 1.8f,
                    stealAttemptCooldown = 1.6f,
                    armReachOffset = new Vector2(0.55f, 0.20f),
                    armReachRadius = 0.50f,
                    jumpBlockChance = 0.45f,       // fairly athletic — meaningful block presence
                    canDunk = false,
                    dunkAttemptChance = 0f,
                };
            case Tier.B: // Vignesh — average build, quick + athletic, excellent 2pt + decent 3pt, good defense
                return new TierData {
                    displayName = "VIGNESH",
                    skinColor = new Color(0.50f, 0.35f, 0.22f),
                    hairColor = new Color(0.08f, 0.06f, 0.04f),
                    heightScale = 1.00f,
                    widthScale = 1.00f,
                    moveSpeed = 5.5f,              // quick — faster than Krish
                    reactionDelay = 0.25f,         // sharp reactions
                    twoPointAccuracy = 0.92f,      // excellent 2pt
                    threePointAccuracy = 0.65f,    // decent 3pt
                    shotWindupTime = 0.30f,        // quick release
                    optimalShotDistance = 5.5f,    // mid-range, comfortably in 2pt zone
                    defenseSpeedMultiplier = 1.00f,// athletic defense — keeps up
                    defenseStandoff = 1.4f,        // tighter than Krish — good defense
                    stealAttemptCooldown = 1.3f,
                    armReachOffset = new Vector2(0.55f, 0.20f),
                    armReachRadius = 0.50f,
                    jumpBlockChance = 0.65f,       // good defense — frequently contests
                    canDunk = true,
                    dunkAttemptChance = 0.20f,     // occasionally drives the rim
                };
            case Tier.A: // Ishaan — brown skin, decent height, athletic + intelligent, excellent 3pt + great 2pt, dunks, elite defense
                return new TierData {
                    displayName = "ISHAAN",
                    skinColor = new Color(0.60f, 0.45f, 0.30f),
                    hairColor = new Color(0.06f, 0.05f, 0.04f),
                    heightScale = 1.10f,           // decent height — taller than average
                    widthScale = 1.00f,            // average weight
                    moveSpeed = 6.0f,              // very quick
                    reactionDelay = 0.20f,         // sharpest brain
                    twoPointAccuracy = 0.85f,      // great 2pt
                    threePointAccuracy = 0.92f,    // excellent 3pt
                    shotWindupTime = 0.25f,        // very quick release
                    optimalShotDistance = 8.5f,    // versatile — borderline 2pt/3pt range
                    defenseSpeedMultiplier = 1.10f,// elite recovery speed
                    defenseStandoff = 1.0f,        // tight pressure defense
                    stealAttemptCooldown = 1.0f,   // frequent steals
                    armReachOffset = new Vector2(0.58f, 0.22f),
                    armReachRadius = 0.55f,
                    jumpBlockChance = 0.85f,       // elite shot-blocker
                    canDunk = true,
                    dunkAttemptChance = 0.50f,     // frequently attacks the rim
                };
            case Tier.S: // Jay — Asian, extremely tall + average weight, elite at everything
                return new TierData {
                    displayName = "JAY",
                    skinColor = new Color(0.95f, 0.80f, 0.65f),
                    hairColor = new Color(0.06f, 0.05f, 0.04f),
                    heightScale = 1.35f,           // extremely tall (matches Afsar but lean — different silhouette)
                    widthScale = 1.00f,            // average weight
                    moveSpeed = 6.5f,              // very fast
                    reactionDelay = 0.15f,         // sharpest brain
                    twoPointAccuracy = 0.95f,      // excellent
                    threePointAccuracy = 0.95f,    // excellent at every range
                    shotWindupTime = 0.20f,        // lightning release
                    optimalShotDistance = 8.5f,    // versatile mid/long
                    defenseSpeedMultiplier = 1.20f,// elite recovery
                    defenseStandoff = 0.7f,        // suffocating pressure defense
                    stealAttemptCooldown = 0.8f,   // frequent steals
                    armReachOffset = new Vector2(0.58f, 0.27f),  // height-scaled reach
                    armReachRadius = 0.60f,        // long arms
                    jumpBlockChance = 0.95f,       // almost always contests
                    canDunk = true,
                    dunkAttemptChance = 0.65f,     // frequent rim attacker
                };
            default: // safety fallback — should not be reached now that all 6 tiers are defined
                return new TierData {
                    displayName = "CPU",
                    skinColor = new Color(0.93f, 0.74f, 0.56f),
                    hairColor = new Color(0.10f, 0.07f, 0.05f),
                    heightScale = 1.0f,
                    widthScale = 1.0f,
                    moveSpeed = 5f,
                    reactionDelay = 0.3f,
                    twoPointAccuracy = 0.85f,
                    threePointAccuracy = 0.70f,
                    shotWindupTime = 0.45f,
                    optimalShotDistance = 5f,
                    defenseSpeedMultiplier = 0.95f,
                    defenseStandoff = 1.2f,
                    stealAttemptCooldown = 1.5f,
                    armReachOffset = new Vector2(0.55f, 0.2f),
                    armReachRadius = 0.5f,
                    jumpBlockChance = 0.70f,
                    canDunk = true,
                    dunkAttemptChance = 0.65f,
                };
        }
    }

    [Header("Identity")]
    public Tier tier = Tier.F;
    public string aiName = "CPU";

    [Header("References")]
    public BasketballBall ball;
    public Transform targetHoop;   // hoop AI shoots at
    public Transform opponent;     // player
    public Transform ownHoop;      // hoop AI defends
    [Tooltip("Side of target hoop. Auto-derived from targetHoop's Hoop component.")]
    public int targetSide = -1;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float pickupRange = 1.6f;
    public float pickupCheckYOffset = -0.6f;

    [Header("Decision")]
    [Tooltip("How often the AI re-evaluates its state, in seconds. Lower = sharper reaction.")]
    public float reactionDelay = 0.3f;

    [Header("Attack")]
    public float optimalShotDistance = 5f;
    public float optimalDistanceTolerance = 1.0f;
    public float shotWindupTime = 0.45f;
    [Range(0f, 1f)] public float twoPointAccuracy = 0.85f;
    [Range(0f, 1f)] public float threePointAccuracy = 0.70f;
    public float shotFlightTime = 1.3f;

    [Header("Defense")]
    [Range(0f, 1.2f)] public float defenseSpeedMultiplier = 0.95f;
    public float defenseDeadzone = 0.25f;
    [Tooltip("How many units the defender stands ahead of the opponent toward AI's own basket. Smaller = tighter defense. Tier-tunable: D=3.0, C=2.2, B=1.5, A=1.0, S=0.7.")]
    public float defenseStandoff = 1.2f;

    [Header("Steal")]
    public float stealAttemptCooldown = 1.5f;
    [Tooltip("Local offset from AI position to the 'arm/hand' point used for steal contact. X is mirrored by facing.")]
    public Vector2 armReachOffset = new Vector2(0.55f, 0.2f);
    [Tooltip("Ball must be within this radius of the arm point to be stolen.")]
    public float armReachRadius = 0.5f;

    [Header("Dunk")]
    [Tooltip("Max horizontal distance from the rim to trigger a dunk attempt. F/D AIs don't jump so this is dormant for them.")]
    public float dunkRange = 2.0f;
    [Tooltip("How far above the AI's center their extended arm reaches. AI.y + this must be >= rim.y for a dunk to count.")]
    public float dunkReachHeight = 1.5f;

    [Header("Dunk Drive (offensive)")]
    [Tooltip("If true, the AI can choose to drive the rim and jump for a dunk instead of taking a normal shot. Set per-tier via TierData.")]
    public bool canDunk = false;
    [Tooltip("Per-possession chance to attempt a dunk drive (rolled once when AI gains possession).")]
    [Range(0f, 1f)] public float dunkAttemptChance = 0f;
    [Tooltip("Drive until within this many units of the rim before jumping for the dunk.")]
    public float dunkDriveDistance = 1.5f;
    [Tooltip("Upward jump impulse for an offensive dunk attempt.")]
    public float dunkJumpForce = 13f;
    [Tooltip("Wait this long after jumping before triggering Shoot — gives the AI time to rise to rim height.")]
    public float dunkShotDelay = 0.25f;

    [Header("Ball-on-Head Recovery")]
    [Tooltip("If a loose ball is balanced on top of the AI for this long, jump to bounce it off and grab it.")]
    public float ballOnHeadHoldDuration = 0.4f;
    [Tooltip("Upward impulse applied to dislodge a ball stuck on the AI's head.")]
    public float ballOnHeadJumpImpulse = 9f;
    [Tooltip("Horizontal dart applied a moment after the jump — reverses the AI's direction so the ball slides off the front of their head.")]
    public float ballOnHeadDartSpeed = 6f;
    [Tooltip("Delay between the upward jump and the horizontal dart, in seconds.")]
    public float ballOnHeadDartDelay = 0.08f;
    [Tooltip("Min seconds between dislodge attempts so the AI doesn't spam jumps.")]
    public float ballOnHeadShakeCooldown = 0.7f;

    [Header("Block")]
    [Tooltip("0–1 chance the AI jumps when the player starts charging a shot. Tier-tuned via TierData.jumpBlockChance.")]
    [Range(0f, 1f)] public float jumpBlockChance = 0f;
    [Tooltip("Upward impulse for a block-jump.")]
    public float blockJumpForce = 12f;
    [Tooltip("Max horizontal distance from the ball-holder for the AI to bother attempting a block.")]
    public float blockJumpRange = 2.5f;
    [Tooltip("Min seconds between block-jump attempts.")]
    public float blockJumpCooldown = 1.5f;

    [Header("State (read-only)")]
    public State currentState = State.Idle;

    private Rigidbody2D rb;
    private BoxCollider2D bodyCollider;
    private float nextDecisionTime;
    private float shotWindupStart = -1f;
    private float lastStealAttemptTime = -999f;
    private float ballOnHeadStartTime = -1f;
    private float lastBallOnHeadShakeTime = -999f;
    private float pendingDartTime = -1f;
    private float pendingDartDir = 0f;
    private bool prevPlayerCharging = false;
    private float lastBlockJumpTime = -999f;
    private ShootingController opponentShooter;
    private bool dunkAttemptThisPossession = false;
    private float dunkJumpTime = -1f;
    private Transform prevBallHolder = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        bodyCollider = GetComponent<BoxCollider2D>();
        if (targetHoop != null)
        {
            var hoop = targetHoop.GetComponent<Hoop>();
            if (hoop != null) targetSide = hoop.side;
        }
        ApplyTierPreset();
    }

    /// <summary>Push the current tier's stats onto this controller. Call after changing `tier` from code.</summary>
    public void ApplyTierPreset()
    {
        var d = GetTierData(tier);
        aiName = d.displayName;
        moveSpeed = d.moveSpeed;
        reactionDelay = d.reactionDelay;
        twoPointAccuracy = d.twoPointAccuracy;
        threePointAccuracy = d.threePointAccuracy;
        shotWindupTime = d.shotWindupTime;
        optimalShotDistance = d.optimalShotDistance;
        defenseSpeedMultiplier = d.defenseSpeedMultiplier;
        defenseStandoff = d.defenseStandoff;
        stealAttemptCooldown = d.stealAttemptCooldown;
        armReachOffset = d.armReachOffset;
        armReachRadius = d.armReachRadius;
        jumpBlockChance = d.jumpBlockChance;
        canDunk = d.canDunk;
        dunkAttemptChance = d.dunkAttemptChance;
    }

    void Update()
    {
        if (ball == null || targetHoop == null) return;

        // Apply pending horizontal dart from a ball-on-head dislodge — slight delay after the jump
        // so the AI visibly leaps up first, then darts sideways out from under the ball.
        if (pendingDartTime > 0f && Time.time >= pendingDartTime)
        {
            rb.linearVelocity = new Vector2(pendingDartDir * ballOnHeadDartSpeed, rb.linearVelocity.y);
            pendingDartTime = -1f;
        }

        DetectPlayerChargeStart();
        DetectPossessionChange();

        if (Time.time >= nextDecisionTime)
        {
            DecideState();
            nextDecisionTime = Time.time + reactionDelay;
        }

        switch (currentState)
        {
            case State.ChaseBall: DoChaseBall(); break;
            case State.Attack: DoAttack(); break;
            case State.Defend: DoDefend(); break;
            default: rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); break;
        }

        TryAutoPickup();
        TryDislodgeBallFromHead();
        TryStealFromOpponent();
        ApplyFacing();
    }

    void DetectPlayerChargeStart()
    {
        if (opponent == null) return;
        if (opponentShooter == null) opponentShooter = opponent.GetComponent<ShootingController>();
        bool playerCharging = opponentShooter != null && opponentShooter.isCharging;
        if (playerCharging && !prevPlayerCharging) TryBlockJump();
        prevPlayerCharging = playerCharging;
    }

    void DetectPossessionChange()
    {
        Transform currentHolder = ball.holder;
        if (currentHolder == transform && prevBallHolder != transform)
        {
            // Just gained possession — roll once for whether this trip down is a dunk attempt
            dunkAttemptThisPossession = canDunk && Random.value < dunkAttemptChance;
            dunkJumpTime = -1f;
        }
        prevBallHolder = currentHolder;
    }

    void TryBlockJump()
    {
        if (currentState != State.Defend) return;
        if (Time.time - lastBlockJumpTime < blockJumpCooldown) return;
        if (rb.linearVelocity.y > 0.3f) return; // already airborne (mid-jump or being pushed up)
        if (Mathf.Abs(transform.position.x - opponent.position.x) > blockJumpRange) return;
        if (Random.value > jumpBlockChance) return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, blockJumpForce);
        lastBlockJumpTime = Time.time;
        Debug.Log($"[BTierHoops] {aiName} jumps to block!");
    }

    void TryDislodgeBallFromHead()
    {
        if (ball == null || ball.IsHeld)
        {
            ballOnHeadStartTime = -1f;
            return;
        }
        if (Time.time - lastBallOnHeadShakeTime < ballOnHeadShakeCooldown) return;

        Vector2 ballPos = ball.transform.position;
        Vector2 myPos = transform.position;
        float dy = ballPos.y - myPos.y;
        float dx = Mathf.Abs(ballPos.x - myPos.x);

        // "On head/shoulders" = ball above the AI's center by at least 0.6, and within roughly the body's
        // horizontal footprint (collider half-width plus the ball's radius). This covers all tiers
        // automatically since BoxCollider2D.size is set per-tier in CourtBuilder from the tier's widthScale.
        float halfWidth = bodyCollider != null ? bodyCollider.size.x * 0.5f : 0.43f;
        bool onHead = dy > 0.6f && dx < halfWidth + 0.25f;

        if (!onHead)
        {
            ballOnHeadStartTime = -1f;
            return;
        }

        if (ballOnHeadStartTime < 0f)
        {
            ballOnHeadStartTime = Time.time;
            return;
        }

        if (Time.time - ballOnHeadStartTime >= ballOnHeadHoldDuration)
        {
            // Step 1: jump up. Step 2 (after ballOnHeadDartDelay): dart horizontally in the opposite
            // direction of current motion so the ball — which has friction-acquired velocity from the
            // AI's prior heading — slides off the front of the head while the AI moves out from under it.
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, ballOnHeadJumpImpulse);

            float currentDir = Mathf.Abs(rb.linearVelocity.x) > 0.5f ? Mathf.Sign(rb.linearVelocity.x) : 0f;
            if (currentDir != 0f)
            {
                pendingDartDir = -currentDir;
            }
            else
            {
                // Stationary — dart away from whichever side the ball is leaning, or random if perfectly centered
                pendingDartDir = Mathf.Abs(dx) > 0.05f ? -Mathf.Sign(ballPos.x - myPos.x) : (Random.value < 0.5f ? -1f : 1f);
            }
            pendingDartTime = Time.time + ballOnHeadDartDelay;

            lastBallOnHeadShakeTime = Time.time;
            ballOnHeadStartTime = -1f;
            Debug.Log($"[BTierHoops] {aiName} bumped ball off head!");
        }
    }

    void TryStealFromOpponent()
    {
        if (ball == null || !ball.IsHeld || ball.holder == transform) return;
        if (opponent == null || ball.holder != opponent) return;
        if (Time.time - lastStealAttemptTime < stealAttemptCooldown) return;
        if (Vector2.Distance(GetArmPosition(), ball.transform.position) > armReachRadius) return;

        ball.Steal(transform);
        lastStealAttemptTime = Time.time;
        Debug.Log("[BTierHoops] AI stole the ball.");
    }

    Vector2 GetArmPosition()
    {
        var visuals = transform.Find("Visuals");
        float facing = (visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        return (Vector2)transform.position + new Vector2(armReachOffset.x * facing, armReachOffset.y);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var visuals = transform.Find("Visuals");
        float facing = (Application.isPlaying && visuals != null && visuals.localScale.x < 0f) ? -1f : 1f;
        Vector3 arm = transform.position + new Vector3(armReachOffset.x * facing, armReachOffset.y, 0f);
        Gizmos.DrawWireSphere(arm, armReachRadius);
    }

    void DecideState()
    {
        if (ball.holder == transform)
            currentState = State.Attack;
        else if (ball.IsHeld)
        {
            currentState = State.Defend;
            shotWindupStart = -1f;
        }
        else
        {
            currentState = State.ChaseBall;
            shotWindupStart = -1f;
        }
    }

    void DoChaseBall()
    {
        float dx = ball.transform.position.x - transform.position.x;
        float dir = Mathf.Abs(dx) < 0.1f ? 0f : Mathf.Sign(dx);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void DoAttack()
    {
        if (dunkAttemptThisPossession)
        {
            DoDunkDrive();
            return;
        }

        float distFromHoop = Mathf.Abs(targetHoop.position.x - transform.position.x);
        float dirToHoop = Mathf.Sign(targetHoop.position.x - transform.position.x);

        if (distFromHoop > optimalShotDistance + optimalDistanceTolerance)
        {
            // Drive toward hoop
            rb.linearVelocity = new Vector2(dirToHoop * moveSpeed, rb.linearVelocity.y);
            shotWindupStart = -1f;
        }
        else if (distFromHoop < optimalShotDistance - optimalDistanceTolerance)
        {
            // Too close — back off
            rb.linearVelocity = new Vector2(-dirToHoop * moveSpeed * 0.7f, rb.linearVelocity.y);
            shotWindupStart = -1f;
        }
        else
        {
            // Sweet spot — wind up and shoot
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (shotWindupStart < 0f) shotWindupStart = Time.time;
            if (Time.time - shotWindupStart >= shotWindupTime)
            {
                Shoot();
                shotWindupStart = -1f;
            }
        }
    }

    void DoDunkDrive()
    {
        float distFromHoop = Mathf.Abs(targetHoop.position.x - transform.position.x);
        float dirToHoop = Mathf.Sign(targetHoop.position.x - transform.position.x);
        bool airborne = Mathf.Abs(rb.linearVelocity.y) > 0.5f;
        bool atDunkPosition = distFromHoop <= dunkDriveDistance;

        if (!airborne && !atDunkPosition)
        {
            // Sprint to the rim
            rb.linearVelocity = new Vector2(dirToHoop * moveSpeed, rb.linearVelocity.y);
            return;
        }

        if (!airborne && atDunkPosition && dunkJumpTime < 0f)
        {
            // Jump straight up — slight forward carry from existing horizontal velocity
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, dunkJumpForce);
            dunkJumpTime = Time.time;
            return;
        }

        if (airborne && dunkJumpTime > 0f && Time.time - dunkJumpTime >= dunkShotDelay)
        {
            // Trigger Shoot — TryDunk fires inside if conditions met. Falls through to normal shot
            // (which the under-rim barrier will block) if reach is somehow too low.
            Shoot();
            dunkAttemptThisPossession = false;
            dunkJumpTime = -1f;
        }
    }

    void DoDefend()
    {
        if (opponent == null || ownHoop == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }
        // Stand `defenseStandoff` units ahead of the opponent, toward AI's own basket
        // (i.e. between opponent and the basket they're defending).
        float toOwnHoop = Mathf.Sign(ownHoop.position.x - opponent.position.x);
        if (toOwnHoop == 0f) toOwnHoop = (targetSide < 0 ? 1f : -1f);
        float defendX = opponent.position.x + toOwnHoop * defenseStandoff;

        float dx = defendX - transform.position.x;
        if (Mathf.Abs(dx) > defenseDeadzone)
            rb.linearVelocity = new Vector2(Mathf.Sign(dx) * moveSpeed * defenseSpeedMultiplier, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    void TryAutoPickup()
    {
        if (ball.IsHeld || !ball.CanBePickedUp()) return;
        Vector2 checkPos = (Vector2)transform.position + Vector2.up * pickupCheckYOffset;
        if (Vector2.Distance(checkPos, ball.transform.position) <= pickupRange)
            ball.Pickup(transform);
    }

    void Shoot()
    {
        if (!ball.IsHeld || ball.holder != transform) return;
        if (TryDunk()) return;

        // No shot cancellation — physics handles bad-position shots via the under-rim barrier.

        Vector2 from = ball.transform.position;
        Vector2 to = targetHoop.position;
        float ballGravityScale = ball.GetComponent<Rigidbody2D>().gravityScale;
        Vector2 baseVel = CalculateLaunchVelocity(from, to, shotFlightTime, ballGravityScale);

        bool fromOwnHalf = (transform.position.x * targetSide) < 0f;
        ball.lastShotPoints = fromOwnHalf ? 3 : 2;

        float accuracy = fromOwnHalf ? threePointAccuracy : twoPointAccuracy;
        float errorMag = (1f - Mathf.Clamp01(accuracy)) * 3.5f;
        Vector2 error = (Vector2)Random.insideUnitCircle * errorMag;

        ball.Release(baseVel + error);
    }

    bool TryDunk()
    {
        // Airborne proxy: vertical velocity meaningfully nonzero. F/D AIs never jump so this is dormant for them.
        bool airborne = Mathf.Abs(rb.linearVelocity.y) > 0.5f;
        if (!airborne) return false;
        if (Mathf.Abs(transform.position.x - targetHoop.position.x) > dunkRange) return false;
        if (transform.position.y + dunkReachHeight < targetHoop.position.y) return false;

        ball.lastShotPoints = 2;
        ball.Release(new Vector2(0f, -10f));
        ball.transform.position = (Vector3)targetHoop.position + new Vector3(0f, 0.30f, 0f);
        Debug.Log($"[BTierHoops] DUNK by {aiName}!");
        return true;
    }

    void ApplyFacing()
    {
        float vx = rb.linearVelocity.x;
        if (Mathf.Abs(vx) > 0.1f)
        {
            var v = transform.Find("Visuals");
            if (v != null)
            {
                var s = v.localScale;
                s.x = vx > 0 ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                v.localScale = s;
            }
        }
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
