using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hides/shows the pre-built crowd members based on the tier the player chose from the menu.
/// Crowd density scales with prestige: F = empty arena, S = packed house.
///
/// CourtBuilder pre-builds the maximum crowd (~100 people) in the Background hierarchy and
/// populates the crowdMembers list on this component. At runtime this reads
/// MatchSettings.SelectedTier and disables every member beyond the configured count.
/// Order of fill is sequential (front rows / center seats first).
/// </summary>
public class CrowdManager : MonoBehaviour
{
    [Tooltip("Every spawnable crowd member, in fill order (filled first → filled last).")]
    public List<GameObject> crowdMembers = new List<GameObject>();

    [Tooltip("How many people watch each tier's match. Indexed by AIController.Tier (F, D, C, B, A, S).")]
    public int[] tierAttendance = new int[] { 0, 3, 5, 20, 50, 100 };

    void Start()
    {
        MatchSettings.Load();

        var tier = MatchSettings.HasMenuSelectedTier
            ? MatchSettings.SelectedTier
            : AIController.Tier.F;

        int count = AttendanceForTier(tier);
        ApplyAttendance(count);
        ApplyCrowdAmbience(count);
        Debug.Log($"[BTierHoops] Crowd: {count} people watching tier {tier}");
    }

    void ApplyCrowdAmbience(int count)
    {
        if (SoundHandler.Instance == null) return;
        if (count <= 0)
        {
            // F-tier: empty arena, no crowd noise
            SoundHandler.Instance.PlayCrowd(false);
            return;
        }
        // Volume scales with attendance: 3 fans whisper, 100 fans roar.
        // Curve uses sqrt(count/maxCount) so small crowds are still audible but not absurdly loud.
        int maxCount = (tierAttendance != null && tierAttendance.Length > 0) ? tierAttendance[tierAttendance.Length - 1] : 100;
        float t = Mathf.Sqrt(Mathf.Clamp01((float)count / Mathf.Max(1, maxCount)));
        float volume = Mathf.Lerp(0.18f, 0.85f, t);
        SoundHandler.Instance.PlayCrowd(true, null, volume);
    }

    int AttendanceForTier(AIController.Tier t)
    {
        int idx = (int)t;
        if (tierAttendance == null || idx < 0 || idx >= tierAttendance.Length) return 0;
        return Mathf.Clamp(tierAttendance[idx], 0, crowdMembers.Count);
    }

    void ApplyAttendance(int count)
    {
        for (int i = 0; i < crowdMembers.Count; i++)
        {
            if (crowdMembers[i] != null) crowdMembers[i].SetActive(i < count);
        }
    }
}
