using System;

/// <summary>
/// Tunables returned by the Python LangGraph service; applied on top of tier baselines on the AI.
/// </summary>
[Serializable]
public class OpponentLearningProfile
{
    public float defenseStandoffMultiplier = 1f;
    public float defenseSpeedMultiplier = 1f;
    public float jumpBlockBonus = 0f;
    public float stealCooldownMultiplier = 1f;
    public float reactionDelayMultiplier = 1f;
    public string summary = "";
}
