using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static cross-scene state: which tier the player chose to fight, the player's display name,
/// and which tiers they've unlocked so far. Persists to PlayerPrefs so progress survives restarts.
///
/// Bridges MainMenu and Game scenes:
///   MainMenu → set SelectedTier, LoadScene("Game")
///   Game     → AISpawner reads SelectedTier; on win, calls UnlockNextTier
/// </summary>
public static class MatchSettings
{
    /// <summary>Scene names used by SceneManager.LoadScene. Match the .unity filenames in Assets/Scenes/.</summary>
    public const string MainMenuSceneName = "MainMenu";
    public const string GameSceneName = "Game";

    const string PrefPlayerName = "btierhoops.playerName";
    const string PrefUnlockedMask = "btierhoops.unlockedMask";
    const string PrefMatchResult = "btierhoops.lastMatchResult";  // "" / "win" / "loss"
    const string PrefMatchResultTier = "btierhoops.lastMatchResultTier"; // tier int

    /// <summary>Player's display name. Empty until first signup.</summary>
    public static string PlayerName { get; private set; } = "";

    /// <summary>Tier the user picked from the menu — read by AISpawner in the Game scene.</summary>
    public static AIController.Tier SelectedTier { get; private set; } = AIController.Tier.F;

    /// <summary>True after the menu has explicitly set a tier this session. Lets AISpawner fall back to its editor-default when the Game scene is launched directly without going through MainMenu.</summary>
    public static bool HasMenuSelectedTier { get; private set; } = false;

    /// <summary>Set the tier from the menu and mark it as user-selected.</summary>
    public static void SetSelectedTier(AIController.Tier t)
    {
        SelectedTier = t;
        HasMenuSelectedTier = true;
    }

    static int unlockedMask = 1 << (int)AIController.Tier.F;  // F unlocked by default

    /// <summary>True if the user has set a name (i.e. completed first-launch signup).</summary>
    public static bool HasAccount => !string.IsNullOrEmpty(PlayerName);

    /// <summary>Which tiers the user can currently select from the menu.</summary>
    public static bool IsTierUnlocked(AIController.Tier t) => (unlockedMask & (1 << (int)t)) != 0;

    /// <summary>Set the player's name and persist.</summary>
    public static void SetPlayerName(string name)
    {
        PlayerName = (name ?? "").Trim();
        PlayerPrefs.SetString(PrefPlayerName, PlayerName);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Mark a tier as unlocked. Called after a match win for the *next* tier in the F→D→C→B→A→S chain.
    /// Returns true if this was a new unlock (so the menu can highlight it).
    /// </summary>
    public static bool UnlockTier(AIController.Tier t)
    {
        int bit = 1 << (int)t;
        if ((unlockedMask & bit) != 0) return false;
        unlockedMask |= bit;
        PlayerPrefs.SetInt(PrefUnlockedMask, unlockedMask);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Return all tiers the user has unlocked, in F→S order.</summary>
    public static IEnumerable<AIController.Tier> UnlockedTiers()
    {
        foreach (AIController.Tier t in System.Enum.GetValues(typeof(AIController.Tier)))
            if (IsTierUnlocked(t)) yield return t;
    }

    /// <summary>Tier ordering: F=0 (worst) → S=5 (best). Defeating tier N unlocks tier N-1 in the enum (the next harder one).</summary>
    public static AIController.Tier? NextTierAfter(AIController.Tier defeated)
    {
        // Enum order is F, D, C, B, A, S. After F (idx 0) → D (idx 1), etc.
        int next = (int)defeated + 1;
        if (next > (int)AIController.Tier.S) return null;
        return (AIController.Tier)next;
    }

    /// <summary>
    /// Communicate match outcome from Game scene back to MainMenu so the menu can show
    /// a "You unlocked X!" or "Try again" banner on next load.
    /// </summary>
    public static void RecordMatchResult(AIController.Tier tier, bool won)
    {
        PlayerPrefs.SetString(PrefMatchResult, won ? "win" : "loss");
        PlayerPrefs.SetInt(PrefMatchResultTier, (int)tier);
        PlayerPrefs.Save();
    }

    public static bool TryConsumeMatchResult(out AIController.Tier tier, out bool won)
    {
        string r = PlayerPrefs.GetString(PrefMatchResult, "");
        tier = (AIController.Tier)PlayerPrefs.GetInt(PrefMatchResultTier, 0);
        won = r == "win";
        if (string.IsNullOrEmpty(r)) return false;
        // One-shot: clear after reading
        PlayerPrefs.DeleteKey(PrefMatchResult);
        PlayerPrefs.DeleteKey(PrefMatchResultTier);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>Load persisted state from PlayerPrefs. Call once at app start (e.g. in MainMenuController.Awake).</summary>
    public static void Load()
    {
        PlayerName = PlayerPrefs.GetString(PrefPlayerName, "");
        // Default unlocked mask = F only. Always keep F unlocked.
        unlockedMask = PlayerPrefs.GetInt(PrefUnlockedMask, 1 << (int)AIController.Tier.F);
        unlockedMask |= 1 << (int)AIController.Tier.F;
    }

    /// <summary>Wipe all saved state — for a "reset progress" / "sign out" button.</summary>
    public static void ResetAll()
    {
        PlayerPrefs.DeleteKey(PrefPlayerName);
        PlayerPrefs.DeleteKey(PrefUnlockedMask);
        PlayerPrefs.DeleteKey(PrefMatchResult);
        PlayerPrefs.DeleteKey(PrefMatchResultTier);
        PlayerPrefs.Save();
        PlayerName = "";
        unlockedMask = 1 << (int)AIController.Tier.F;
        SelectedTier = AIController.Tier.F;
        HasMenuSelectedTier = false;
    }
}
