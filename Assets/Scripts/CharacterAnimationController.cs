using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterAnimationController : MonoBehaviour
{
    [Header("Rig")]
    public Transform visualsRoot;
    public Transform body;
    public Transform head;
    public Transform hair;
    public Transform frontArm;
    public Transform backArm;
    public Transform frontHand;
    public Transform backHand;
    public Transform frontLeg;
    public Transform backLeg;
    public Transform frontShoe;
    public Transform backShoe;

    [Header("Timing")]
    public float idleBobSpeed = 2.2f;
    public float idleBobAmount = 0.03f;
    public float runCycleSpeed = 9f;
    public float dribbleCycleSpeed = 4f;
    public float settleLerp = 14f;

    [Header("Shot Poses")]
    public float shotPoseDuration = 0.34f;
    public float dunkPoseDuration = 0.28f;
    public float stealPoseDuration = 0.18f;
    public float jumpLiftVisual = 0.04f;
    [Tooltip("How long the shooter holds the high follow-through pose after release.")]
    public float shotFollowThroughHold = 0.16f;

    [Header("Dribble Ball")]
    public bool enableDribbleBallMotion = true;
    public float dribbleBallDrop = 0.62f;
    public float dribbleBallForward = 0.08f;

    [Header("Jump Hop")]
    public float hopDuration = 0.14f;
    public float hopLift = 0.12f;

    public bool IsDribbling => currentMotion == Motion.Dribble;

    Rigidbody2D rb;
    PlayerController playerController;
    BasketballBall ball;

    enum Motion { Idle, Run, Jump, Dribble, Shoot, Dunk, Steal }

    Motion currentMotion = Motion.Idle;
    float shotTimer;
    float dunkTimer;
    float stealTimer;
    float charge01;
    float shotReleaseCharge = 1f;
    float hopTimer = 0f;

    struct PartPose
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 scale;
    }

    readonly System.Collections.Generic.Dictionary<Transform, PartPose> basePose = new System.Collections.Generic.Dictionary<Transform, PartPose>();
    // Hand-to-arm offsets (in arm local space) so hands follow arm rotation cleanly
    Vector3 frontHandOffsetLocal = Vector3.zero;
    Vector3 backHandOffsetLocal = Vector3.zero;
    Quaternion frontHandRotOffset = Quaternion.identity;
    Quaternion backHandRotOffset = Quaternion.identity;
    bool hasFrontHandOffsets = false;
    bool hasBackHandOffsets = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerController = GetComponent<PlayerController>();
        CacheRigReferencesIfMissing();
        CaptureBasePose();
    }

    void Update()
    {
        if (ball == null)
        {
            ball = FindFirstObjectByType<BasketballBall>();
        }

        if (shotTimer > 0f) shotTimer -= Time.deltaTime;
        if (dunkTimer > 0f) dunkTimer -= Time.deltaTime;
        if (stealTimer > 0f) stealTimer -= Time.deltaTime;
        if (hopTimer > 0f) hopTimer = Mathf.Max(0f, hopTimer - Time.deltaTime);

        EvaluateMotion();
        AnimatePose();
    }

    public void SetCharge(float normalizedCharge)
    {
        charge01 = Mathf.Clamp01(normalizedCharge);
    }

    public void TriggerShoot(float releaseCharge = 1f)
    {
        shotTimer = shotPoseDuration;
        shotReleaseCharge = Mathf.Clamp01(releaseCharge);
        charge01 = 0f;
    }

    public void TriggerDunk()
    {
        dunkTimer = dunkPoseDuration;
        charge01 = 0f;
    }

    public void TriggerSteal()
    {
        stealTimer = stealPoseDuration;
    }

    public void TriggerJump()
    {
        hopTimer = hopDuration;
    }

    public Vector2 GetBallHoldOffset(Vector2 baseOffset, float facing)
    {
        if (!enableDribbleBallMotion)
            return new Vector2(baseOffset.x * facing, baseOffset.y);

        // Don't dribble while airborne (no dribbling on imaginary ground)
        if (playerController != null && !playerController.IsGrounded)
            return new Vector2(baseOffset.x * facing, baseOffset.y);

        // Don't dribble during shot/dunk release
        if (currentMotion == Motion.Shoot || currentMotion == Motion.Dunk)
            return new Vector2(baseOffset.x * facing, baseOffset.y);

        // Adjust drop amount based on visual height so tall characters hit the ground
        float heightScale = 1f;
        if (visualsRoot != null) heightScale = Mathf.Abs(visualsRoot.localScale.y);
        float drop = dribbleBallDrop * Mathf.Max(0.9f, heightScale);
        float speed = Mathf.Max(0.05f, dribbleCycleSpeed * Mathf.Clamp01(1f / Mathf.Max(0.5f, heightScale)));

        // Determine holder collider bottom so the ball visibly bounces to the ground
        float groundLocalY = baseOffset.y; // fallback
        var bodyCol = GetComponent<BoxCollider2D>();
        if (bodyCol != null)
        {
            float halfHeight = bodyCol.size.y * 0.5f;
            // find ball radius if possible
            float ballRadius = 0.18f;
            if (ball != null)
            {
                var bcol = ball.GetComponent<CircleCollider2D>();
                if (bcol != null) ballRadius = bcol.radius * Mathf.Abs(ball.transform.lossyScale.y);
            }
            groundLocalY = -halfHeight + ballRadius + 0.02f;
        }

        float t = Mathf.Sin(Time.time * speed * Mathf.PI * 2f) * 0.5f + 0.5f;
        float y = Mathf.Lerp(baseOffset.y, groundLocalY, t);
        float x = baseOffset.x + dribbleBallForward;
        return new Vector2(x * facing, y);
    }

    void EvaluateMotion()
    {
        if (dunkTimer > 0f)
        {
            currentMotion = Motion.Dunk;
            return;
        }
        if (shotTimer > 0f)
        {
            currentMotion = Motion.Shoot;
            return;
        }
        if (stealTimer > 0f)
        {
            currentMotion = Motion.Steal;
            return;
        }

        bool airborne = Mathf.Abs(rb.linearVelocity.y) > 0.15f;
        if (playerController != null) airborne = !playerController.IsGrounded;
        if (airborne)
        {
            currentMotion = Motion.Jump;
            return;
        }

        bool hasBall = ball != null && ball.IsHeld && ball.holder == transform;
        float speedX = Mathf.Abs(rb.linearVelocity.x);

        if (hasBall)
        {
            currentMotion = Motion.Dribble;
            return;
        }

        if (speedX > 0.2f)
        {
            currentMotion = Motion.Run;
            return;
        }

        currentMotion = Motion.Idle;
    }

    void AnimatePose()
    {
        float t = Time.time;

        float bodyY = 0f;
        float frontArmDeg = 0f;
        float backArmDeg = 0f;
        float frontLegDeg = 0f;
        float backLegDeg = 0f;
        float headTiltDeg = 0f;

        switch (currentMotion)
        {
            case Motion.Idle:
                bodyY = Mathf.Sin(t * idleBobSpeed) * idleBobAmount;
                break;

            case Motion.Run:
            {
                float c = Mathf.Sin(t * runCycleSpeed);
                frontLegDeg = c * 26f;
                backLegDeg = -c * 26f;
                frontArmDeg = -c * 20f;
                backArmDeg = c * 20f;
                bodyY = Mathf.Abs(Mathf.Sin(t * runCycleSpeed)) * 0.02f;
                break;
            }

            case Motion.Dribble:
            {
                float c = Mathf.Sin(t * dribbleCycleSpeed);
                float dribble = Mathf.Sin(t * dribbleCycleSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
                float locomotionScale = Mathf.Clamp01(Mathf.Abs(rb.linearVelocity.x) / 4.5f);
                frontLegDeg = c * Mathf.Lerp(5f, 16f, locomotionScale);
                backLegDeg = -c * Mathf.Lerp(5f, 16f, locomotionScale);
                frontArmDeg = Mathf.Lerp(-15f, 62f, dribble);
                backArmDeg = Mathf.Lerp(-20f, 6f, locomotionScale);
                bodyY = -0.02f;
                headTiltDeg = -2f;
                break;
            }

            case Motion.Jump:
            {
                // Hop-step visual at jump start: apply an extra lift while hopTimer > 0
                float extra = 0f;
                if (hopTimer > 0f)
                {
                    extra = Mathf.Lerp(hopLift, 0f, 1f - (hopTimer / hopDuration));
                }

                frontArmDeg = -25f;
                backArmDeg = -18f;
                frontLegDeg = 18f;
                backLegDeg = 8f;
                bodyY = jumpLiftVisual + extra;
                headTiltDeg = -4f;
                break;
            }
                break;

            case Motion.Shoot:
            {
                // Klay-inspired compact dip -> high quick release -> locked follow-through.
                float p = 1f - Mathf.Clamp01(shotTimer / Mathf.Max(0.001f, shotPoseDuration));
                float dipEnd = 0.26f;
                float releaseEnd = 0.62f;
                float followStart = 1f - Mathf.Clamp01(shotFollowThroughHold / Mathf.Max(0.001f, shotPoseDuration));

                float dip = p < dipEnd ? p / dipEnd : 1f;
                float release = p <= dipEnd ? 0f : Mathf.Clamp01((p - dipEnd) / Mathf.Max(0.001f, releaseEnd - dipEnd));
                float follow = p <= followStart ? 0f : Mathf.Clamp01((p - followStart) / Mathf.Max(0.001f, 1f - followStart));

                // Stronger knee dip on deeper charge, then a quick vertical snap.
                frontLegDeg = Mathf.Lerp(14f + 10f * shotReleaseCharge, 3f, release);
                backLegDeg = Mathf.Lerp(10f + 8f * shotReleaseCharge, 2f, release);

                float dipFrontArm = Mathf.Lerp(22f, 32f, shotReleaseCharge) * dip;
                float releaseFrontArm = Mathf.Lerp(-120f, -150f, shotReleaseCharge) * release;
                frontArmDeg = dipFrontArm + releaseFrontArm;

                float dipBackArm = Mathf.Lerp(16f, 24f, shotReleaseCharge) * dip;
                float releaseBackArm = Mathf.Lerp(-85f, -112f, shotReleaseCharge) * release;
                backArmDeg = dipBackArm + releaseBackArm;

                // Keep follow-through wrist snapped and high.
                if (follow > 0f)
                {
                    frontArmDeg = Mathf.Lerp(frontArmDeg, -150f, follow);
                    backArmDeg = Mathf.Lerp(backArmDeg, -112f, follow);
                }

                bodyY = Mathf.Lerp(-0.05f, 0.04f, release);
                headTiltDeg = Mathf.Lerp(-4f, -10f, release);
                break;
            }

            case Motion.Dunk:
            {
                // Two-hand throwdown: gather high with both hands, then violent downward snap.
                float p = 1f - Mathf.Clamp01(dunkTimer / Mathf.Max(0.001f, dunkPoseDuration));
                float gather = Mathf.Clamp01(p / 0.45f);
                float throwdown = p <= 0.45f ? 0f : Mathf.Clamp01((p - 0.45f) / 0.55f);

                float gatherFront = Mathf.Lerp(-80f, -130f, gather);
                float gatherBack = Mathf.Lerp(-70f, -120f, gather);

                frontArmDeg = Mathf.Lerp(gatherFront, 75f, throwdown);
                backArmDeg = Mathf.Lerp(gatherBack, 65f, throwdown);
                frontLegDeg = Mathf.Lerp(22f, 8f, throwdown);
                backLegDeg = Mathf.Lerp(12f, 5f, throwdown);
                bodyY = Mathf.Lerp(0.10f, 0.02f, throwdown);
                headTiltDeg = Mathf.Lerp(-12f, 8f, throwdown);
                break;
            }

            case Motion.Steal:
                frontArmDeg = 65f;
                backArmDeg = 15f;
                frontLegDeg = 10f;
                backLegDeg = -8f;
                bodyY = -0.02f;
                headTiltDeg = 5f;
                break;
        }

        ApplyPartOffset(body, new Vector3(0f, bodyY, 0f));
        ApplyPartOffset(head, new Vector3(0f, bodyY * 0.4f, 0f));
        ApplyPartOffset(hair, new Vector3(0f, bodyY * 0.45f, 0f));
        ApplyPartRotation(head, headTiltDeg);
        ApplyPartRotation(hair, headTiltDeg * 0.5f);

        ApplyLimb(frontArm, frontArmDeg);
        ApplyLimb(backArm, backArmDeg);
        // Position hands at the end of the arms so they don't look disconnected
        if (hasFrontHandOffsets && frontArm != null && frontHand != null)
        {
            var targetPos = frontArm.localPosition + (frontArm.localRotation * frontHandOffsetLocal);
            var targetRot = frontArm.localRotation * frontHandRotOffset;
            frontHand.localPosition = Vector3.Lerp(frontHand.localPosition, targetPos, Time.deltaTime * settleLerp);
            frontHand.localRotation = Quaternion.Slerp(frontHand.localRotation, targetRot, Time.deltaTime * settleLerp);
        }
        else
        {
            ApplyLimb(frontHand, frontArmDeg * 0.6f);
        }

        if (hasBackHandOffsets && backArm != null && backHand != null)
        {
            var targetPos = backArm.localPosition + (backArm.localRotation * backHandOffsetLocal);
            var targetRot = backArm.localRotation * backHandRotOffset;
            backHand.localPosition = Vector3.Lerp(backHand.localPosition, targetPos, Time.deltaTime * settleLerp);
            backHand.localRotation = Quaternion.Slerp(backHand.localRotation, targetRot, Time.deltaTime * settleLerp);
        }
        else
        {
            ApplyLimb(backHand, backArmDeg * 0.6f);
        }
        ApplyLimb(frontLeg, frontLegDeg);
        ApplyLimb(backLeg, backLegDeg);
        ApplyLimb(frontShoe, frontLegDeg * 0.35f);
        ApplyLimb(backShoe, backLegDeg * 0.35f);
    }

    void CacheRigReferencesIfMissing()
    {
        if (visualsRoot == null) visualsRoot = transform.Find("Visuals");
        if (visualsRoot == null) return;

        if (body == null) body = FindDeepChild(visualsRoot, "Body");
        if (head == null) head = FindDeepChild(visualsRoot, "Head");
        if (hair == null) hair = FindDeepChild(visualsRoot, "Hair");
        if (frontArm == null) frontArm = FindDeepChild(visualsRoot, "FrontArm");
        if (backArm == null) backArm = FindDeepChild(visualsRoot, "BackArm");
        if (frontHand == null) frontHand = FindDeepChild(visualsRoot, "FrontHand");
        if (backHand == null) backHand = FindDeepChild(visualsRoot, "BackHand");
        if (frontLeg == null) frontLeg = FindDeepChild(visualsRoot, "FrontLeg");
        if (backLeg == null) backLeg = FindDeepChild(visualsRoot, "BackLeg");
        if (frontShoe == null) frontShoe = FindDeepChild(visualsRoot, "FrontShoe");
        if (backShoe == null) backShoe = FindDeepChild(visualsRoot, "BackShoe");
    }

    Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName) return child;
            Transform match = FindDeepChild(child, childName);
            if (match != null) return match;
        }
        return null;
    }

    void CaptureBasePose()
    {
        basePose.Clear();
        Capture(body);
        Capture(head);
        Capture(hair);
        Capture(frontArm);
        Capture(backArm);
        Capture(frontHand);
        Capture(backHand);
        Capture(frontLeg);
        Capture(backLeg);
        Capture(frontShoe);
        Capture(backShoe);
        
        // Compute hand offsets relative to their arms so hands follow rotations
        hasFrontHandOffsets = false;
        hasBackHandOffsets = false;
        if (frontArm != null && frontHand != null && basePose.ContainsKey(frontArm) && basePose.ContainsKey(frontHand))
        {
            var a = basePose[frontArm];
            var h = basePose[frontHand];
            frontHandOffsetLocal = Quaternion.Inverse(a.rot) * (h.pos - a.pos);
            frontHandRotOffset = Quaternion.Inverse(a.rot) * h.rot;
            hasFrontHandOffsets = true;
        }
        if (backArm != null && backHand != null && basePose.ContainsKey(backArm) && basePose.ContainsKey(backHand))
        {
            var a = basePose[backArm];
            var h = basePose[backHand];
            backHandOffsetLocal = Quaternion.Inverse(a.rot) * (h.pos - a.pos);
            backHandRotOffset = Quaternion.Inverse(a.rot) * h.rot;
            hasBackHandOffsets = true;
        }
    }

    void Capture(Transform t)
    {
        if (t == null) return;
        basePose[t] = new PartPose { pos = t.localPosition, rot = t.localRotation, scale = t.localScale };
    }

    void ApplyPartOffset(Transform t, Vector3 offset)
    {
        if (!HasBase(t)) return;
        var p = basePose[t];
        t.localPosition = Vector3.Lerp(t.localPosition, p.pos + offset, Time.deltaTime * settleLerp);
    }

    void ApplyPartRotation(Transform t, float zDeg)
    {
        if (!HasBase(t)) return;
        var p = basePose[t];
        Quaternion target = p.rot * Quaternion.Euler(0f, 0f, zDeg);
        t.localRotation = Quaternion.Slerp(t.localRotation, target, Time.deltaTime * settleLerp);
    }

    void ApplyLimb(Transform t, float zDeg)
    {
        ApplyPartRotation(t, zDeg);
    }

    bool HasBase(Transform t)
    {
        return t != null && basePose.ContainsKey(t);
    }
}