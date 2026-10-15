using System.Collections;
using UnityEngine;

/// <summary>
/// Controller for the defender's hammer sprite at the striking line.
/// Switches to Frame 1 (Hammer_1, strike) on key press, then returns to Frame 0 (Hammer_0, idle).
/// Preserves and updates its base position dynamically to avoid displacement after strikes.
/// </summary>
public class HammerController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private float _strikeVisualDuration = 0.09f;
    [Tooltip("Downward vertical punch displacement (pixels) applied during hammer strike.")]
    [SerializeField] private float _strikePunchY = -3f;

    private Coroutine _strikeCoroutine;
    private Vector3 _basePosition;

    public Vector3 BasePosition => _basePosition;

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
        _basePosition = transform.localPosition;
    }

    private void Start()
    {
        SetIdleFrame();
    }

    /// <summary>
    /// Explicitly sets the base idle position of the hammer (e.g. at the striking line + 8px visual offset).
    /// </summary>
    public void SetPosition(Vector3 localPos)
    {
        _basePosition = localPos;
        transform.localPosition = localPos;
    }

    /// <summary>
    /// Triggers the hammer strike visual animation.
    /// </summary>
    public void TriggerStrike()
    {
        if (_strikeCoroutine != null)
        {
            StopCoroutine(_strikeCoroutine);
        }
        _strikeCoroutine = StartCoroutine(StrikeRoutine());
    }

    private IEnumerator StrikeRoutine()
    {
        SetStrikeFrame();
        // Slight downward punch for juicy game feel
        transform.localPosition = _basePosition + new Vector3(0, _strikePunchY, 0);

        yield return new WaitForSeconds(_strikeVisualDuration);

        transform.localPosition = _basePosition;
        SetIdleFrame();
        _strikeCoroutine = null;
    }

    public void SetIdleFrame()
    {
        if (_spriteRenderer != null && GameSpriteManager.Instance != null)
        {
            _spriteRenderer.sprite = GameSpriteManager.Instance.GetHammerSprite(0);
        }
    }

    public void SetStrikeFrame()
    {
        if (_spriteRenderer != null && GameSpriteManager.Instance != null)
        {
            _spriteRenderer.sprite = GameSpriteManager.Instance.GetHammerSprite(1);
        }
    }
}
