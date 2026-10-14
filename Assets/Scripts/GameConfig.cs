using System;
using UnityEngine;

/// <summary>
/// Global configuration model for the rhythm game.
/// Persists parameters selected on TitleScene and provides rhythm timing helpers.
/// </summary>
public static class GameConfig
{
    // Configurable parameters with default values
    public static int Bpm = 120;                       // 60 - 300
    public static int BeatsPerMeasure = 4;             // 2 - 8
    public static int MeasuresPerRound = 8;            // 4 - 16 (pairs of measures, *2 = total measures)
    public static int TotalRounds = 2;                 // 1 - 8
    public static int Quantization = 16;               // 8, 12, 16, 24
    public const int PrepMeasures = 4;                 // 4 prep measures per round

    // Pixel art scale and dimensions
    public const float PixelsPerBeat = 64f;            // 1 Board_block = 64px = 1 beat
    public const int ScreenReferenceWidth = 640;
    public const int ScreenReferenceHeight = 360;

    // Timing helper properties
    public static float BeatDuration => 60f / Mathf.Max(30, Bpm);
    public static float MeasureDuration => BeatDuration * BeatsPerMeasure;

    /// <summary>
    /// Number of subdivision ticks per beat (e.g. 16th notes = 4 ticks per beat in 4/4).
    /// 8 -> 2, 12 -> 3, 16 -> 4, 24 -> 6
    /// </summary>
    public static int SubdivisionsPerBeat
    {
        get
        {
            switch (Quantization)
            {
                case 8: return 2;
                case 12: return 3;
                case 16: return 4;
                case 24: return 6;
                default: return 4;
            }
        }
    }

    public static int TicksPerMeasure => BeatsPerMeasure * SubdivisionsPerBeat;
    public static float SubdivisionDuration => BeatDuration / SubdivisionsPerBeat;
    public static float DefenseCooldownDuration => 0.75f * SubdivisionDuration;

    /// <summary>
    /// Conveyor movement speed in pixels per second: 64 px / BeatDuration.
    /// </summary>
    public static float ConveyorSpeed => PixelsPerBeat / BeatDuration;

    /// <summary>
    /// Physical distance between placing line and striking line: BeatsPerMeasure * 64 px.
    /// </summary>
    public static float MeasureDistance => BeatsPerMeasure * PixelsPerBeat;

    // Screen dimensions and center coordinates
    public const float ScreenWidth = 640f;
    public const float ScreenHeight = 360f;
    public const float ScreenCenterX = 320f;
    public const float ScreenCenterY = 180f;

    /// <summary>
    /// Symmetrical coordinates centered on the 640x360 screen (ScreenCenterX = 320):
    /// Left placing line at ScreenCenterX - MeasureDistance/2,
    /// Right striking line at ScreenCenterX + MeasureDistance/2.
    /// </summary>
    public static float PlacingLineX => ScreenCenterX - MeasureDistance / 2f;
    public static float StrikingLineX => ScreenCenterX + MeasureDistance / 2f;

    /// <summary>
    /// Resets all configurable parameters to defaults.
    /// </summary>
    public static void ResetToDefaults()
    {
        Bpm = 120;
        BeatsPerMeasure = 4;
        MeasuresPerRound = 8;
        TotalRounds = 2;
        Quantization = 16;
    }
}
