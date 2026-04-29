using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum GameState { Playing, Ended }

    public static GameManager Instance { get; private set; }

    [Header("Match")]
    public int winScore = 11;
    [Tooltip("Match length in seconds.")]
    public float matchDuration = 60f;

    [Header("Score")]
    public int playerScore;
    public int aiScore;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public GameObject winPanel;
    public TMP_Text winText;

    [Header("Ball")]
    public BasketballBall ball;
    public Vector3 ballSpawnPosition = new Vector3(0f, 1f, 0f);
    public float resetDelay = 1.0f;

    [Header("Player")]
    public Transform player;
    public Vector3 playerSpawnPosition = new Vector3(-3f, 0f, 0f);

    [Header("Out-of-Bounds")]
    public float oobBelowY = -6f;
    public float oobAboveY = 9f;
    public float oobSideX = 12.5f;

    public GameState State { get; private set; } = GameState.Playing;
    private float matchTimer;
    private float resetAtTime = -1f;

    void Awake()
    {
        Instance = this;
        matchTimer = matchDuration;
        State = GameState.Playing;
        if (winPanel != null) winPanel.SetActive(false);
        UpdateScoreUI();
        UpdateTimerUI();
    }

    void Update()
    {
        var kb = Keyboard.current;

        if (State == GameState.Playing)
        {
            matchTimer = Mathf.Max(0f, matchTimer - Time.deltaTime);
            UpdateTimerUI();

            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                ResetPlayer();
                if (ball != null && !ball.IsHeld) ResetBall();
                Debug.Log("[BTierHoops] Manual reset (R key).");
            }

            if (ball != null && !ball.IsHeld && IsOOB(ball.transform.position))
            {
                Debug.Log($"[BTierHoops] Ball OOB at {ball.transform.position}, resetting.");
                ResetBall();
            }

            if (player != null && IsOOB(player.position))
            {
                Debug.Log($"[BTierHoops] Player OOB at {player.position}, resetting.");
                ResetPlayer();
            }

            if (resetAtTime > 0f && Time.time >= resetAtTime)
            {
                ResetBall();
                resetAtTime = -1f;
            }

            if (matchTimer <= 0f || playerScore >= winScore || aiScore >= winScore)
                EndMatch();
        }
        else if (State == GameState.Ended)
        {
            if (kb != null && kb.rKey.wasPressedThisFrame)
                RestartMatch();
        }
    }

    public void OnHoopScored(int hoopSide, int points = 2)
    {
        if (State != GameState.Playing) return;
        if (hoopSide > 0) playerScore += points;
        else aiScore += points;
        UpdateScoreUI();
        resetAtTime = Time.time + resetDelay;
        Debug.Log($"[BTierHoops] Score! +{points} → Player {playerScore} - CPU {aiScore}");
    }

    public void ResetBall()
    {
        if (ball == null) return;
        if (ball.IsHeld) return;
        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        ball.transform.position = ballSpawnPosition;
        ball.transform.rotation = Quaternion.identity;
    }

    public void ResetPlayer()
    {
        if (player == null) return;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        player.position = playerSpawnPosition;
    }

    void EndMatch()
    {
        State = GameState.Ended;
        string winner;
        if (playerScore > aiScore) winner = "PLAYER WINS!";
        else if (aiScore > playerScore) winner = "CPU WINS!";
        else winner = "TIE GAME";
        if (winText != null) winText.text = winner;
        if (winPanel != null) winPanel.SetActive(true);
        Debug.Log($"[BTierHoops] Match ended: {winner} ({playerScore}-{aiScore})");
    }

    void RestartMatch()
    {
        playerScore = 0;
        aiScore = 0;
        matchTimer = matchDuration;
        State = GameState.Playing;
        if (winPanel != null) winPanel.SetActive(false);

        ResetPlayer();
        if (ball != null)
        {
            if (ball.IsHeld) ball.Release(Vector2.zero);
            ResetBall();
        }
        UpdateScoreUI();
        UpdateTimerUI();
        Debug.Log("[BTierHoops] Match restarted.");
    }

    bool IsOOB(Vector3 p)
    {
        return p.y < oobBelowY || p.y > oobAboveY || Mathf.Abs(p.x) > oobSideX;
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"PLAYER  {playerScore}   -   {aiScore}  CPU";
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        int totalSec = Mathf.CeilToInt(matchTimer);
        int min = totalSec / 60;
        int sec = totalSec % 60;
        timerText.text = $"{min}:{sec:00}";
    }
}
