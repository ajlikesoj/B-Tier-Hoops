using UnityEngine;

public class BallPickup : MonoBehaviour
{
    public Transform holdPoint;
    public float throwForce = 16f;

    private GameObject heldBall;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (heldBall == null)
            {
                TryPickupBall();
            }
            else
            {
                ThrowBall();
            }
        }
    }

    void TryPickupBall()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.5f);

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Ball"))
            {
                heldBall = hit.gameObject;

                Rigidbody2D ballRb = heldBall.GetComponent<Rigidbody2D>();
                ballRb.simulated = false;

                heldBall.transform.position = holdPoint.position;
                heldBall.transform.SetParent(holdPoint);

                break;
            }
        }
    }

    void ThrowBall()
    {
        Rigidbody2D ballRb = heldBall.GetComponent<Rigidbody2D>();

        heldBall.transform.SetParent(null);
        ballRb.simulated = true;

        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(new Vector2(1.5f, 3f).normalized * throwForce, ForceMode2D.Impulse);

        heldBall = null;
    }
}