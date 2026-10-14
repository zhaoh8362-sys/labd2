using UnityEngine;

/// <summary>
/// Timing judgment tiers for defender's hammer strikes.
/// </summary>
public enum HitTier
{
    None = 0,
    Perfect = 1,
    Good = 2,
    Bad = 3,
    Miss = 4
}

public static class HitTierExtensions
{
    public const float WindowPerfectMs = 0.030f; // +-30ms
    public const float WindowGoodMs = 0.080f;    // +-80ms
    public const float WindowBadMs = 0.200f;     // +-200ms

    /// <summary>
    /// Base points awarded per note for this tier.
    /// Perfect: 100, Good: 60, Bad: 40, Miss: 0.
    /// </summary>
    public static int GetBasePoints(this HitTier tier)
    {
        switch (tier)
        {
            case HitTier.Perfect: return 100;
            case HitTier.Good: return 60;
            case HitTier.Bad: return 40;
            case HitTier.Miss: return 0;
            default: return 0;
        }
    }

    /// <summary>
    /// Returns the nail sprite frame index (0..8) based on timing error delta (seconds).
    /// delta = strikeTime - targetArrivalTime.
    /// Negative delta means Early, positive delta means Late.
    /// Frame 0: Initial upright.
    /// Early: 1 (Miss), 2 (Bad), 3 (Good), 4 (Perfect).
    /// Late:  5 (Perfect), 6 (Good), 7 (Bad), 8 (Miss).
    /// </summary>
    public static int GetNailFrameIndex(HitTier tier, float deltaSeconds)
    {
        if (tier == HitTier.None) return 0;

        bool isEarly = deltaSeconds < 0f;

        switch (tier)
        {
            case HitTier.Perfect:
                return isEarly ? 4 : 5;
            case HitTier.Good:
                return isEarly ? 3 : 6;
            case HitTier.Bad:
                return isEarly ? 2 : 7;
            case HitTier.Miss:
                return isEarly ? 1 : 8;
            default:
                return 0;
        }
    }

    /// <summary>
    /// UI display text for judgment.
    /// </summary>
    public static string GetDisplayText(this HitTier tier)
    {
        switch (tier)
        {
            case HitTier.Perfect: return "PERFECT!";
            case HitTier.Good: return "GOOD!";
            case HitTier.Bad: return "BAD!";
            case HitTier.Miss: return "MISS!";
            default: return "";
        }
    }

    /// <summary>
    /// Color associated with judgment tier for UI feedback.
    /// </summary>
    public static Color GetDisplayColor(this HitTier tier)
    {
        switch (tier)
        {
            case HitTier.Perfect: return new Color(1.0f, 0.85f, 0.2f, 1.0f); // Gold
            case HitTier.Good: return new Color(0.2f, 0.9f, 0.4f, 1.0f);    // Green
            case HitTier.Bad: return new Color(0.3f, 0.7f, 1.0f, 1.0f);     // Blue
            case HitTier.Miss: return new Color(0.9f, 0.25f, 0.25f, 1.0f);  // Red
            default: return Color.white;
        }
    }
}
