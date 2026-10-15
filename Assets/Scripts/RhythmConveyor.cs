using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the scrolling conveyor board, board block tiles, aim lines, and beat strobe pulses.
/// Positions the placing line and striking line symmetrically around x = 0 based on beats per measure.
/// </summary>
public class RhythmConveyor : MonoBehaviour
{
    [Header("Conveyor Settings (Pixels)")]
    [SerializeField] private float _boardBaseY = 140f;
    [SerializeField] private float _nailY = 172f;
    [SerializeField] private float _hammerY = 212f;

    [Header("Fine-Tuning Offsets (Pixels)")]
    [Tooltip("Horizontal pixel offset applied to the hammer relative to the striking line.")]
    [SerializeField] private float _hammerOffsetX = 8f;

    [Tooltip("Horizontal pixel offset applied to each board block center to align holes with beats.")]
    [SerializeField] private float _boardHoleOffsetX = -24f;

    [Tooltip("Vertical half-height of the aim lines (spans from baseY - halfHeight to baseY + halfHeight).")]
    [SerializeField] private float _aimLineHalfHeight = 90f;

    [Header("Aim Line Visuals")]
    [SerializeField] private LineRenderer _placeLine;
    [SerializeField] private LineRenderer _strikeLine;
    [SerializeField] private SpriteRenderer _placeStrobe;
    [SerializeField] private SpriteRenderer _strikeStrobe;

    [Header("Hammer Controller")]
    [SerializeField] private HammerController _hammer;

    // Moving root for all board blocks and placed nails
    private GameObject _boardContainer;
    private readonly List<GameObject> _spawnedBlocks = new List<GameObject>();
    private readonly List<NailInstance> _spawnedNails = new List<NailInstance>();

    // Current conveyor position state
    private float _conveyorSpeed = 0f;
    private float _boardStartX = 0f;
    private bool _isRunning = false;
    private float _elapsedTime = 0f;

    // Aim line colors
    private readonly Color _placeColor = new Color(0.2f, 0.8f, 1.0f, 0.85f);
    private readonly Color _strikeColor = new Color(1.0f, 0.75f, 0.2f, 0.85f);
    private readonly Color _strobeColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);

    public float BoardBaseY => _boardBaseY;
    public float NailY => _nailY;
    public HammerController Hammer => _hammer;
    public IReadOnlyList<NailInstance> SpawnedNails => _spawnedNails;
    public float HammerOffsetX => _hammerOffsetX;
    public float BoardHoleOffsetX => _boardHoleOffsetX;

    private void Awake()
    {
        CreateBoardContainer();
        CreateAimLinesIfNeeded();
    }

    private void Start()
    {
        UpdatePositions();
    }

    private void OnValidate()
    {
        UpdatePositions();
    }

    /// <summary>
    /// Updates aim line positions and hammer base position based on current offsets and GameConfig.
    /// Acts as the single source of truth for both Edit mode and Play mode.
    /// </summary>
    public void UpdatePositions()
    {
        float placeX = GameConfig.PlacingLineX;
        float strikeX = GameConfig.StrikingLineX;

        if (_placeLine != null)
        {
            _placeLine.transform.localPosition = new Vector3(placeX, _boardBaseY, 0);
            _placeLine.SetPosition(0, new Vector3(0, -_aimLineHalfHeight, 0));
            _placeLine.SetPosition(1, new Vector3(0, _aimLineHalfHeight, 0));
        }
        if (_strikeLine != null)
        {
            _strikeLine.transform.localPosition = new Vector3(strikeX, _boardBaseY, 0);
            _strikeLine.SetPosition(0, new Vector3(0, -_aimLineHalfHeight, 0));
            _strikeLine.SetPosition(1, new Vector3(0, _aimLineHalfHeight, 0));
        }
        if (_hammer != null)
        {
            _hammer.SetPosition(new Vector3(strikeX + _hammerOffsetX, _hammerY, 0));
        }
    }

    private void CreateBoardContainer()
    {
        if (_boardContainer == null)
        {
            _boardContainer = new GameObject("BoardContainer");
            _boardContainer.transform.SetParent(transform, false);
        }
    }

    private void CreateAimLinesIfNeeded()
    {
        if (_placeLine == null)
        {
            _placeLine = CreateVerticalLine("PlaceAimLine", _placeColor, transform);
        }
        if (_strikeLine == null)
        {
            _strikeLine = CreateVerticalLine("StrikeAimLine", _strikeColor, transform);
        }
    }

    private LineRenderer CreateVerticalLine(string name, Color color, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 2f;
        lr.endWidth = 2f;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(0, -_aimLineHalfHeight, 0));
        lr.SetPosition(1, new Vector3(0, _aimLineHalfHeight, 0));

        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 5;

        return lr;
    }

    /// <summary>
    /// Reconfigures conveyor geometry and spawns all board blocks for the upcoming round.
    /// </summary>
    public void SetupRound(int totalMeasuresInRound)
    {
        UpdatePositions();
        float placeX = GameConfig.PlacingLineX;
        _conveyorSpeed = GameConfig.ConveyorSpeed;

        // Clear existing objects
        ClearBoardAndNails();

        // Total blocks = total measures * beats per measure + extra buffer
        int totalBeats = totalMeasuresInRound * GameConfig.BeatsPerMeasure + 8;

        // Spawn board blocks under _boardContainer
        // UX Optimization: Only spawn board blocks for Attack measures (even gameplay measures: 0, 2, 4...)
        // Prep measures and Defense measures have no board passing the placing line, visually indicating no attacker action.
        var spriteMgr = GameSpriteManager.Instance;
        if (spriteMgr != null)
        {
            spriteMgr.EnsureSpritesLoaded();
            int beatsPerMeasure = GameConfig.BeatsPerMeasure;
            int prepMeasures = GameConfig.PrepMeasures;

            for (int m = 0; m < totalMeasuresInRound; m++)
            {
                // Prep measures have no board
                if (m < prepMeasures) continue;

                int gameplayMeasure = m - prepMeasures;
                // Only even gameplay measures (0, 2, 4...) are attack measures with a board
                if (gameplayMeasure % 2 != 0) continue;

                for (int b = 0; b < beatsPerMeasure; b++)
                {
                    int globalBeat = m * beatsPerMeasure + b;
                    float blockX = -globalBeat * GameConfig.PixelsPerBeat + _boardHoleOffsetX;
                    var blockObj = spriteMgr.SpawnBoardBlock(new Vector3(blockX, 0, 0), 1, 0, _boardContainer.transform);
                    var sr = blockObj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.flipX = true;
                    }
                    _spawnedBlocks.Add(blockObj);
                }
            }
        }

        // At t = 0, beat 0 hole should be at placeX.
        // Beat 0 hole local X is 0. So container local X = placeX.
        _boardStartX = placeX;
        _boardContainer.transform.localPosition = new Vector3(_boardStartX, _boardBaseY, 0);
        _elapsedTime = 0f;
        _isRunning = false;
    }

    public void StartConveyor()
    {
        _isRunning = true;
    }

    public void PauseConveyor()
    {
        _isRunning = false;
    }

    public void UpdateConveyor(float roundElapsedTime)
    {
        if (!_isRunning) return;

        _elapsedTime = roundElapsedTime;

        if (_boardContainer != null)
        {
            // Moves rightward at constant speed
            float currentX = _boardStartX + _conveyorSpeed * _elapsedTime;
            _boardContainer.transform.localPosition = new Vector3(currentX, _boardBaseY, 0);
        }
    }

    /// <summary>
    /// Spawns a new nail on the board at the quantized beat position.
    /// </summary>
    public NailInstance SpawnNailOnBoard(int measureIndex, int tickIndex, float targetArrivalTime, float quantizedBeatInRound)
    {
        if (_boardContainer == null) return null;

        var spriteMgr = GameSpriteManager.Instance;
        if (spriteMgr == null) return null;

        // Local X on board corresponding to this beat
        float nailLocalX = -quantizedBeatInRound * GameConfig.PixelsPerBeat;
        Vector3 localPos = new Vector3(nailLocalX, _nailY - _boardBaseY, 0);

        var nailGo = spriteMgr.SpawnNail(0, localPos, 1, 2, _boardContainer.transform);
        var nailComp = nailGo.AddComponent<NailInstance>();
        nailComp.Initialize(measureIndex, tickIndex, targetArrivalTime);

        _spawnedNails.Add(nailComp);
        return nailComp;
    }

    /// <summary>
    /// Triggers a brief flash on the placing aim line.
    /// </summary>
    public void StrobePlaceLine()
    {
        if (_placeLine != null)
        {
            StartCoroutine(FlashLine(_placeLine, _strobeColor, _placeColor, 0.05f));
        }
    }

    /// <summary>
    /// Triggers a brief flash on the striking aim line.
    /// </summary>
    public void StrobeStrikeLine()
    {
        if (_strikeLine != null)
        {
            StartCoroutine(FlashLine(_strikeLine, _strobeColor, _strikeColor, 0.05f));
        }
    }

    private System.Collections.IEnumerator FlashLine(LineRenderer line, Color flashColor, Color normalColor, float duration)
    {
        line.startColor = flashColor;
        line.endColor = flashColor;

        yield return new WaitForSeconds(duration);

        line.startColor = normalColor;
        line.endColor = normalColor;
    }

    public void ClearBoardAndNails()
    {
        foreach (var block in _spawnedBlocks)
        {
            if (block != null) Destroy(block);
        }
        _spawnedBlocks.Clear();

        foreach (var nail in _spawnedNails)
        {
            if (nail != null && nail.gameObject != null) Destroy(nail.gameObject);
        }
        _spawnedNails.Clear();
    }
}
