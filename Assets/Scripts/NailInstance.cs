using UnityEngine;

/// <summary>
/// Represents a single nail placed onto the conveyor board.
/// Moves with the board and updates its visual sprite frame (0..8) upon defender strike or timeout.
/// </summary>
public class NailInstance : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;

    public int MeasureIndex { get; private set; }
    public int TickIndex { get; private set; }
    public float TargetArrivalTime { get; private set; }
    public bool IsConsumed { get; private set; }
    public HitTier Tier { get; private set; } = HitTier.None;
    public float DeltaTime { get; private set; } = 0f;

    private void Awake()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
    }

    /// <summary>
    /// Initializes this nail instance with its timing and measure parameters.
    /// </summary>
    public void Initialize(int measureIndex, int tickIndex, float targetArrivalTime)
    {
        MeasureIndex = measureIndex;
        TickIndex = tickIndex;
        TargetArrivalTime = targetArrivalTime;
        IsConsumed = false;
        Tier = HitTier.None;
        DeltaTime = 0f;

        SetFrame(0); // Upright initial state
    }

    /// <summary>
    /// Applies the hit judgment, updates the sprite frame, and marks the nail as consumed.
    /// </summary>
    public void ApplyJudgment(HitTier tier, float deltaSeconds)
    {
        if (IsConsumed) return;

        IsConsumed = true;
        Tier = tier;
        DeltaTime = deltaSeconds;

        int frameIndex = HitTierExtensions.GetNailFrameIndex(tier, deltaSeconds);
        SetFrame(frameIndex);
    }

    /// <summary>
    /// Marks the nail as timed out (Late Miss).
    /// </summary>
    public void ApplyTimeoutMiss()
    {
        if (IsConsumed) return;

        IsConsumed = true;
        Tier = HitTier.Miss;
        DeltaTime = 0.201f; // past the 200ms window

        SetFrame(8); // Late Miss frame
    }

    /// <summary>
    /// Updates the sprite to the requested nail frame index (0..8).
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        if (_spriteRenderer != null && GameSpriteManager.Instance != null)
        {
            _spriteRenderer.sprite = GameSpriteManager.Instance.GetNailSprite(frameIndex);
        }
    }
}
