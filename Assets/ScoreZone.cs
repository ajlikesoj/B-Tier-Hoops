using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    public GameManager gameManager;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ball"))
        {
            Rigidbody2D ballRb = other.GetComponent<Rigidbody2D>();

            if (ballRb.linearVelocity.y < 0)
            {
                gameManager.AddScore(1);
            }
        }
    }
}