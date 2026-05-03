using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the MainMenu scene: loads persisted state, shows name-entry on first launch, paints
/// the 6 tier tiles with locked/unlocked state, announces last-match result, and on tile click
/// stamps MatchSettings.SelectedTier and loads the Game scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public class TierTile
    {
        public AIController.Tier tier;
        public Button button;
        public Image background;
        public TMP_Text tierLetterLabel;
        public TMP_Text nameLabel;
        [Tooltip("Tile color when unlocked (set per-tier by MainMenuBuilder so each tier reads as a distinct difficulty).")]
        public Color unlockedColor = new Color(0.18f, 0.40f, 0.85f);
        [Tooltip("Stripe accent at top of tile (also tinted to unlockedColor when unlocked).")]
        public Image stripe;
        [Tooltip("Difficulty dots — first N (where N = tier index + 1) light up in unlockedColor when unlocked.")]
        public Image[] difficultyDots;
    }

    [Header("Tier tiles (one per tier — set up by MainMenuBuilder)")]
    public TierTile[] tiles;

    [Header("Header / banner")]
    public TMP_Text greetingText;
    public TMP_Text resultBanner;

    [Header("Name entry overlay")]
    public GameObject nameEntryPanel;
    public TMP_InputField nameInput;
    public Button nameSubmitButton;

    [Header("Reset progress button (corner)")]
    public Button resetProgressButton;

    [Header("How-to-Play overlay")]
    public GameObject howToPlayPanel;
    public Button howToPlayOpenButton;   // sits in the menu corner — re-opens the overlay
    public Button howToPlayCloseButton;  // "PLAY" button inside the overlay — dismisses it

    [Header("Learning service")]
    [Tooltip("Must match OpponentLearningService.baseUrl on the Game scene (leaderboard + per-user AI).")]
    public string learningServiceBaseUrl = "http://127.0.0.1:8765";

    static readonly Color LockedColor       = new Color(0.22f, 0.22f, 0.26f);
    static readonly Color LockedStripeColor = new Color(0.30f, 0.30f, 0.34f);
    static readonly Color LockedDotColor    = new Color(0.30f, 0.30f, 0.34f);
    static readonly Color UnlockedTextColor = Color.white;
    static readonly Color LockedTextColor   = new Color(0.55f, 0.55f, 0.60f);

    void Awake()
    {
        MatchSettings.Load();
        // No matches happening on the menu — silence any crowd ambience left over from a previous game.
        if (SoundHandler.Instance != null) SoundHandler.Instance.PlayCrowd(false);
    }

    void Start()
    {
        // Wire tile click handlers (capture each tier in a local so the lambda binds correctly)
        if (tiles != null)
        {
            foreach (var t in tiles)
            {
                if (t.button == null) continue;
                var capturedTier = t.tier;
                t.button.onClick.AddListener(() => OnTileClicked(capturedTier));
            }
        }

        if (nameSubmitButton != null) nameSubmitButton.onClick.AddListener(OnSubmitName);
        if (nameInput != null) nameInput.onSubmit.AddListener(_ => OnSubmitName());
        if (resetProgressButton != null) resetProgressButton.onClick.AddListener(OnResetProgress);
        if (howToPlayOpenButton != null) howToPlayOpenButton.onClick.AddListener(ShowHowToPlay);
        if (howToPlayCloseButton != null) howToPlayCloseButton.onClick.AddListener(HideHowToPlay);

        // First launch: pop the How-to-Play overlay automatically; subsequent launches require the menu button.
        if (howToPlayPanel != null) howToPlayPanel.SetActive(!MatchSettings.HasSeenInstructions);

        ShowMatchResultBanner();
        RefreshUI();

        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
            LeaderboardPanel.Ensure(canvas, learningServiceBaseUrl);

        // First launch: prompt for a name. Tiles stay non-clickable until a name is set.
        if (!MatchSettings.HasAccount) ShowNameEntry();
        else HideNameEntry();
    }

    void RefreshUI()
    {
        if (greetingText != null)
            greetingText.text = MatchSettings.HasAccount ? $"WELCOME, {MatchSettings.PlayerName}" : "WELCOME";

        if (tiles == null) return;
        foreach (var t in tiles)
        {
            bool unlocked = MatchSettings.IsTierUnlocked(t.tier);
            if (t.background != null) t.background.color = unlocked ? t.unlockedColor : LockedColor;
            if (t.stripe != null)     t.stripe.color     = unlocked ? Brighten(t.unlockedColor, 1.25f) : LockedStripeColor;
            if (t.button != null)     t.button.interactable = unlocked && MatchSettings.HasAccount;
            if (t.tierLetterLabel != null)
            {
                t.tierLetterLabel.text = t.tier.ToString();
                t.tierLetterLabel.color = unlocked ? UnlockedTextColor : LockedTextColor;
            }
            if (t.nameLabel != null)
            {
                t.nameLabel.text = unlocked ? AIController.GetTierData(t.tier).displayName : "LOCKED";
                t.nameLabel.color = unlocked ? UnlockedTextColor : LockedTextColor;
            }
            if (t.difficultyDots != null)
            {
                int litCount = (int)t.tier + 1;  // F=1 dot, S=6 dots
                Color litColor = unlocked ? Brighten(t.unlockedColor, 1.4f) : LockedDotColor;
                for (int i = 0; i < t.difficultyDots.Length; i++)
                {
                    if (t.difficultyDots[i] == null) continue;
                    t.difficultyDots[i].color = i < litCount ? litColor : new Color(0.18f, 0.18f, 0.20f);
                }
            }
        }
    }

    static Color Brighten(Color c, float factor)
    {
        return new Color(Mathf.Min(1f, c.r * factor), Mathf.Min(1f, c.g * factor), Mathf.Min(1f, c.b * factor), c.a);
    }

    void ShowMatchResultBanner()
    {
        if (resultBanner == null) return;
        if (!MatchSettings.TryConsumeMatchResult(out var lastTier, out var won))
        {
            resultBanner.text = "";
            return;
        }
        string tierName = AIController.GetTierData(lastTier).displayName;
        if (won)
        {
            var next = MatchSettings.NextTierAfter(lastTier);
            if (next.HasValue)
            {
                string nextName = AIController.GetTierData(next.Value).displayName;
                resultBanner.text = $"BEAT {tierName} — UNLOCKED {next.Value} ({nextName})";
                resultBanner.color = new Color(0.30f, 0.95f, 0.30f);
            }
            else
            {
                resultBanner.text = $"YOU BEAT {tierName} — CHAMPION OF ALL TIERS";
                resultBanner.color = new Color(1.00f, 0.85f, 0.20f);
            }
        }
        else
        {
            resultBanner.text = $"LOST TO {tierName} — TRY AGAIN";
            resultBanner.color = new Color(0.95f, 0.30f, 0.30f);
        }
    }

    void OnTileClicked(AIController.Tier tier)
    {
        if (!MatchSettings.IsTierUnlocked(tier)) return;
        if (!MatchSettings.HasAccount) { ShowNameEntry(); return; }

        SoundHandler.Instance?.PlayButton();
        MatchSettings.SetSelectedTier(tier);
        SceneManager.LoadScene(MatchSettings.GameSceneName);
    }

    void OnSubmitName()
    {
        if (nameInput == null) return;
        string name = nameInput.text;
        if (string.IsNullOrWhiteSpace(name)) return;
        SoundHandler.Instance?.PlayButton();
        MatchSettings.SetPlayerName(name);
        HideNameEntry();
        RefreshUI();
    }

    void ShowNameEntry()
    {
        if (nameEntryPanel == null) return;
        nameEntryPanel.SetActive(true);
        if (nameInput != null)
        {
            nameInput.text = MatchSettings.PlayerName;
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }

    void HideNameEntry()
    {
        if (nameEntryPanel != null) nameEntryPanel.SetActive(false);
    }

    void OnResetProgress()
    {
        SoundHandler.Instance?.PlayButton();
        MatchSettings.ResetAll();
        RefreshUI();
        ShowNameEntry();
        if (resultBanner != null) resultBanner.text = "PROGRESS RESET";
    }

    void ShowHowToPlay()
    {
        SoundHandler.Instance?.PlayButton();
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    void HideHowToPlay()
    {
        SoundHandler.Instance?.PlayButton();
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        MatchSettings.MarkInstructionsSeen();
    }
}
