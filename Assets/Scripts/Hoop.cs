using UnityEngine;

public class Hoop : MonoBehaviour
{
    [Tooltip("+1 = right hoop (player scores here). -1 = left hoop (AI scores here).")]
    public int side = 1;

    [Tooltip("Min downward speed (negative y velocity) required to count as a make.")]
    public float minDownwardSpeed = 0.5f;

    private float lastScoreTime = -999f;
    private const float ScoreCooldown = 0.3f;

    void OnTriggerEnter2D(Collider2D other)
    {
        var ball = other.GetComponent<BasketballBall>();
        if (ball == null) return;
        if (ball.IsHeld) return;
        if (Time.time - lastScoreTime < ScoreCooldown) return;

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        if (rb.linearVelocity.y > -minDownwardSpeed) return;

        lastScoreTime = Time.time;
        if (GameManager.Instance != null)
            GameManager.Instance.OnHoopScored(side, ball.lastShotPoints);
    }
}
