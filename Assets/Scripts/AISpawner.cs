using UnityEngine;

/// <summary>
/// Runtime AI spawner. The Game scene is built without an AI baked in — instead this MonoBehaviour
/// sits in the scene with all the references it needs (ball, hoops, player), and on Start it reads
/// MatchSettings.SelectedTier (set by the MainMenu) to construct the chosen opponent.
///
/// Falls back to defaultTierForEditor if the Game scene was launched directly (Play in editor without
/// going through MainMenu) and MatchSettings still holds its enum default.
/// </summary>
public class AISpawner : MonoBehaviour
{
    [Header("Wiring (set by CourtBuilder at scene-build time)")]
    public BasketballBall ball;
    public Transform targetHoop;     // hoop the AI shoots at (left)
    public Transform ownHoop;        // hoop the AI defends (right)
    public Transform opponent;       // human player
    public Vector3 spawnPosition = new Vector3(3f, 0f, 0f);
    public Color jerseyColor = new Color(0.18f, 0.40f, 0.85f);
    public GameManager gameManager;
    public PlayerController playerController;

    [Header("Editor Default")]
    [Tooltip("Tier to spawn when MatchSettings.SelectedTier hasn't been set (i.e. Play pressed directly on the Game scene).")]
    public AIController.Tier defaultTierForEditor = AIController.Tier.F;

    void Start()
    {
        // Defensive — covers the case where the Game scene was launched directly (Play in editor)
        // without going through MainMenu, so MatchSettings hasn't been hydrated from PlayerPrefs yet.
        MatchSettings.Load();

        // If the user came from the MainMenu, MatchSettings.HasMenuSelectedTier is true and we use
        // their pick. If they pressed Play directly on the Game scene from the editor (dev iteration),
        // fall back to the Inspector-set defaultTierForEditor so we can quickly test specific opponents.
        var tier = MatchSettings.HasMenuSelectedTier ? MatchSettings.SelectedTier : defaultTierForEditor;

        // Spawn the AI character + jersey letter + name tag, attach AIController, wire ball/hoops/opponent
        var ai = CharacterFactory.BuildAI(spawnPosition, jerseyColor, tier, ball, targetHoop, ownHoop, opponent);

        // Finish wiring the GameManager and player so steal/score/reset all know about the AI
        if (gameManager != null)
        {
            gameManager.ai = ai.transform;
            gameManager.aiSpawnPosition = spawnPosition;
            gameManager.aiName = AIController.GetTierData(tier).displayName;
            gameManager.playerName = MatchSettings.PlayerName; // empty string is fine — score UI collapses the prefix
            gameManager.currentMatchTier = tier;               // so GameManager can call MatchSettings.UnlockTier on win
            gameManager.RefreshScoreUI();                      // refresh now that names are set (Awake fired with blanks)
        }
        if (playerController != null)
        {
            playerController.opponent = ai.transform;
        }

        Debug.Log($"[BTierHoops] AI spawned: tier={tier} name={AIController.GetTierData(tier).displayName}");
    }
}
