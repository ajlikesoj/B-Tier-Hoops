using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallIgnoreBall : MonoBehaviour
{
    void Start()
    {
        var ball = Object.FindFirstObjectByType<BasketballBall>();
        if (ball == null) return;
        var ballCol = ball.GetComponent<Collider2D>();
        var myCol = GetComponent<Collider2D>();
        if (ballCol != null && myCol != null)
            Physics2D.IgnoreCollision(myCol, ballCol, true);
    }
}
