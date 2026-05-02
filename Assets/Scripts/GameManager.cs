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
    [Tooltip("Display name for the human player in score and win text. AISpawner overwrites this from MatchSettings at runtime.")]
    public string playerName = "";

    [Header("AI")]
    public Transform ai;
    public Vector3 aiSpawnPosition = new Vector3(3f, 0f, 0f);
    [Tooltip("Display name for the AI in score and win text. Set by AISpawner from the AI's tier.")]
    public string aiName = "";
    [Tooltip("Tier the player is currently fighting. Set by AISpawner so we know which tier to mark defeated on a win.")]
    public AIController.Tier currentMatchTier = AIController.Tier.F;

    [Header("Out-of-Bounds")]
    public float oobBelowY = -6f;
    public float oobAboveY = 9f;
    public float oobSideX = 12.5f;

    public GameState State { get; private set; } = GameState.Playing;
    public bool SuddenDeath { get; private set; }
    public bool BallInPlay { get; private set; } = true;

    private float matchTimer;
    private float resetAtTime = -1f;

    static readonly Color TimerNormalColor = Color.white;
    static readonly Color TimerOvertimeColor = new Color(1f, 0.45f, 0.20f);

    void Awake()
    {
        Instance = this;
        matchTimer = matchDuration;
        State = GameState.Playing;
        SuddenDeath = false;
        if (winPanel != null) winPanel.SetActive(false);
        UpdateScoreUI();
        UpdateTimerUI();
    }

    /// <summary>Public hook for AISpawner to call after it sets aiName/playerName so the score banner reflects them.</summary>
    public void RefreshScoreUI() => UpdateScoreUI();

    void Update()
    {
        var kb = Keyboard.current;

        if (State == GameState.Playing)
        {
            // Tick down match timer only while play is live
            if (BallInPlay && !SuddenDeath)
            {
                matchTimer = Mathf.Max(0f, matchTimer - Time.deltaTime);
                UpdateTimerUI();
            }

            // R = manual emergency reset (also resumes play if paused)
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                ResetEverything();
                BallInPlay = true;
                resetAtTime = -1f;
                Debug.Log("[BTierHoops] Manual reset (R key).");
            }

            // Free-ball OOB
            if (ball != null && !ball.IsHeld && IsOOB(ball.transform.position))
            {
                Debug.Log($"[BTierHoops] Ball OOB at {ball.transform.position}, resetting.");
                ResetBall();
            }

            // Player OOB
            if (player != null && IsOOB(player.position))
            {
                Debug.Log($"[BTierHoops] Player OOB at {player.position}, resetting.");
                ResetPlayer();
            }

            // AI OOB
            if (ai != null && IsOOB(ai.position))
            {
                Debug.Log($"[BTierHoops] AI OOB at {ai.position}, resetting.");
                ResetAI();
            }

            // Delayed full reset after a make / overtime entry
            if (resetAtTime > 0f && Time.time >= resetAtTime)
            {
                ResetEverything();
                BallInPlay = true;
                resetAtTime = -1f;
            }

            // Win conditions
            if (playerScore >= winScore || aiScore >= winScore)
            {
                EndMatch();
                return;
            }

            // Timer expiry — only when play is live (don't expire during the post-score pause)
            if (matchTimer <= 0f && !SuddenDeath && BallInPlay)
            {
                if (playerScore == aiScore)
                {
                    SuddenDeath = true;
                    BallInPlay = false;
                    resetAtTime = Time.time + resetDelay;
                    UpdateTimerUI();
                    Debug.Log("[BTierHoops] Time! Tied — entering sudden death (court resetting).");
                }
                else
                {
                    EndMatch();
                }
            }
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
        Debug.Log($"[BTierHoops] Score! +{points} → {playerName} {playerScore} - {aiName} {aiScore}");

        // Pause the clock until the court resets and the ball drops again
        BallInPlay = false;

        // Sudden death: any non-tied score ends the match immediately
        if (SuddenDeath && playerScore != aiScore)
        {
            EndMatch();
            return;
        }

        resetAtTime = Time.time + resetDelay;
    }

    public void ResetBall()
    {
        if (ball == null) return;
        if (ball.IsHeld) ball.Release(Vector2.zero);
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

    public void ResetAI()
    {
        if (ai == null) return;
        var rb = ai.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f; }
        ai.position = aiSpawnPosition;
    }

    public void ResetEverything()
    {
        ResetBall();
        ResetPlayer();
        ResetAI();
    }

    void EndMatch()
    {
        State = GameState.Ended;
        bool playerWon = playerScore > aiScore;
        bool aiWon = aiScore > playerScore;

        string winner;
        if (playerWon) winner = string.IsNullOrEmpty(playerName) ? "PLAYER WINS!" : $"{playerName} WINS!";
        else if (aiWon) winner = $"{aiName} WINS!";
        else winner = "TIE GAME";

        // Persist the result so the MainMenu can announce it on its next load
        MatchSettings.RecordMatchResult(currentMatchTier, playerWon);

        // Progress: defeating tier N unlocks the next harder tier (F → D → C → B → A → S)
        string unlockBanner = "";
        if (playerWon)
        {
            var next = MatchSettings.NextTierAfter(currentMatchTier);
            if (next.HasValue && MatchSettings.UnlockTier(next.Value))
            {
                var nextName = AIController.GetTierData(next.Value).displayName;
                unlockBanner = $"\n<size=60%>UNLOCKED {next.Value} — {nextName}</size>";
            }
        }

        if (winText != null) winText.text = winner + unlockBanner;
        if (winPanel != null) winPanel.SetActive(true);
        Debug.Log($"[BTierHoops] Match ended: {winner} ({playerScore}-{aiScore}){unlockBanner.Replace("\n", " ")}");
    }

    void RestartMatch()
    {
        playerScore = 0;
        aiScore = 0;
        matchTimer = matchDuration;
        State = GameState.Playing;
        SuddenDeath = false;
        BallInPlay = true;
        resetAtTime = -1f;
        if (winPanel != null) winPanel.SetActive(false);

        ResetEverything();
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
        if (scoreText == null) return;
        string left = string.IsNullOrEmpty(playerName) ? $"{playerScore}" : $"{playerName}  {playerScore}";
        string right = string.IsNullOrEmpty(aiName) ? $"{aiScore}" : $"{aiScore}  {aiName}";
        scoreText.text = $"{left}   -   {right}";
    }

    void UpdateTimerUI()
    {
        if (timerText == null) return;
        if (SuddenDeath)
        {
            timerText.text = "NEXT POINT WINS";
            timerText.color = TimerOvertimeColor;
            return;
        }
        int totalSec = Mathf.CeilToInt(matchTimer);
        int min = totalSec / 60;
        int sec = totalSec % 60;
        timerText.text = $"{min}:{sec:00}";
        timerText.color = TimerNormalColor;
    }
}
