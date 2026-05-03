using UnityEngine;

/// <summary>
/// Spawns a floating block-letter name tag above the human player's head, mirroring how
/// AISpawner creates a name tag for the AI. The player's name comes from MatchSettings,
/// not a TierData preset, so this lives on the player rather than going through CharacterFactory.BuildAI.
/// </summary>
public class PlayerNameTag : MonoBehaviour
{
    void Start()
    {
        // Defensive — covers the case where Game scene was launched directly (Play in editor)
        // without going through MainMenu, so MatchSettings hasn't been hydrated from PlayerPrefs.
        MatchSettings.Load();

        string name = MatchSettings.PlayerName;
        if (string.IsNullOrWhiteSpace(name)) return; // no name set yet → no tag

        // Player isn't built with TierData, so visuals.localScale.y is the source of truth for height.
        var visuals = transform.Find("Visuals");
        float heightScale = visuals != null ? Mathf.Abs(visuals.localScale.y) : 1f;

        CharacterFactory.BuildNameTag(transform, name, heightScale);
    }
}
