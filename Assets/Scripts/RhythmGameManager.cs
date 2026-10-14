using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master rhythm game controller and state machine.
/// Coordinates the rhythm clock, turn alternation, quantization, judgment, scoring normalization, and round progression.
/// </summary>
public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance { get; private set; }

    public enum GameState
    {
        NotStarted,
        Preparing,
        Playing,
        RoundTransition,
        GameOver
    }

    [Header("Component References")]
    [SerializeField] private RhythmConveyor _conveyor;
    [SerializeField] private GameSceneUI _ui;

    // Public state accessors
    public GameState CurrentState { get; private set; } = GameState.NotStarted;
    public int CurrentRound { get; private set; } = 1;
    public int P1Score { get; private set; } = 0;
    public int P2Score { get; private set; } = 0;

    // Stats tracking
    public int P1PerfectCount { get; private set; } = 0;
    public int P1GoodCount { get; private set; } = 0;
    public int P1BadCount { get; private set; } = 0;
    public int P1MissCount { get; private set; } = 0;

    public int P2PerfectCount { get; private set; } = 0;
    public int P2GoodCount { get; private set; } = 0;
    public int P2BadCount { get; private set; } = 0;
    public int P2MissCount { get; private set; } = 0;

    // Role identification: P1 = 1, P2 = 2
    public int CurrentAttacker => (CurrentRound % 2 == 1) ? 1 : 2;
    public int CurrentDefender => (CurrentRound % 2 == 1) ? 2 : 1;

    // Timing tracking
    private float _roundStartTime = 0f;
    private int _lastBeatIndex = -1;
    private int _lastMeasureIndex = -1;
    private float _lastStrikeTime = -10f;

    public float RoundStartTime => _roundStartTime;
    public float RoundElapsedTime => Time.time - _roundStartTime;
    public int CurrentMeasureIndex => _lastMeasureIndex >= 0 ? _lastMeasureIndex : 0;
    public int CurrentGameplayMeasure => Mathf.Max(0, CurrentMeasureIndex - GameConfig.PrepMeasures);
    public bool IsAttackingMeasure => (CurrentGameplayMeasure % 2 == 0);
    public int CurrentBeatInMeasure => _lastBeatIndex >= 0 ? (_lastBeatIndex % GameConfig.BeatsPerMeasure) : 0;


    // Placed nails categorized by measure index
    private readonly Dictionary<int, List<NailInstance>> _measureNails = new Dictionary<int, List<NailInstance>>();
    private readonly HashSet<int> _settledMeasures = new HashSet<int>();

    // Events
    public event Action<int, int> OnBeatTick;                // measure, beat
    public event Action<int> OnMeasureStart;                 // currentMeasure
    public event Action<HitTier, float> OnJudgment;         // tier, delta
    public event Action<int, int, int> OnScoreSettled;       // defenderPlayer, measureScore, totalScore
    public event Action<int> OnRoundStarted;                 // round
    public event Action<int> OnGameOver;                    // winner (1, 2, or 0 for tie)

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnActionAPressed -= HandleActionAPressed;
            GameInput.Instance.OnActionBPressed -= HandleActionBPressed;
        }
    }

    private void Start()
    {
        if (_conveyor == null)
        {
            _conveyor = FindAnyObjectByType<RhythmConveyor>();
        }
        if (_ui == null)
        {
            _ui = FindAnyObjectByType<GameSceneUI>();
        }

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnActionAPressed += HandleActionAPressed;
            GameInput.Instance.OnActionBPressed += HandleActionBPressed;
        }

        StartGame();
    }

    /// <summary>
    /// Starts a fresh game from Round 1.
    /// </summary>
    public void StartGame()
    {
        CurrentRound = 1;
        P1Score = 0;
        P2Score = 0;

        P1PerfectCount = 0;
        P1GoodCount = 0;
        P1BadCount = 0;
        P1MissCount = 0;

        P2PerfectCount = 0;
        P2GoodCount = 0;
        P2BadCount = 0;
        P2MissCount = 0;

        StartRound(CurrentRound);
    }

    private void StartRound(int round)
    {
        CurrentRound = round;
        _measureNails.Clear();
        _settledMeasures.Clear();
        _lastBeatIndex = -1;
        _lastMeasureIndex = -1;

        int totalMeasures = GameConfig.PrepMeasures + GameConfig.MeasuresPerRound * 2;
        if (_conveyor != null)
        {
            _conveyor.SetupRound(totalMeasures);
            _conveyor.StartConveyor();
        }

        _roundStartTime = Time.time;
        CurrentState = GameState.Preparing;

        OnRoundStarted?.Invoke(CurrentRound);
    }

    private void Update()
    {
        if (CurrentState != GameState.Preparing && CurrentState != GameState.Playing)
        {
            return;
        }

        float elapsedTime = Time.time - _roundStartTime;
        if (_conveyor != null)
        {
            _conveyor.UpdateConveyor(elapsedTime);
        }

        UpdateRhythmClock(elapsedTime);
        CheckNailTimeouts();
        CheckMeasureSettlements(elapsedTime);
    }

    private void UpdateRhythmClock(float elapsedTime)
    {
        float beatDuration = GameConfig.BeatDuration;
        float measureDuration = GameConfig.MeasureDuration;

        int currentTotalBeat = Mathf.FloorToInt(elapsedTime / beatDuration);
        int currentMeasure = Mathf.FloorToInt(elapsedTime / measureDuration);
        int beatInMeasure = currentTotalBeat % GameConfig.BeatsPerMeasure;

        // Beat tick event
        if (currentTotalBeat > _lastBeatIndex)
        {
            _lastBeatIndex = currentTotalBeat;
            bool isDownbeat = (beatInMeasure == 0);

            // Audio & Strobe feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayTick(isDownbeat);
            }

            if (_conveyor != null)
            {
                _conveyor.StrobePlaceLine();
                _conveyor.StrobeStrikeLine();
            }

            OnBeatTick?.Invoke(currentMeasure, beatInMeasure);
        }

        // Measure transition
        if (currentMeasure != _lastMeasureIndex)
        {
            int previousMeasure = _lastMeasureIndex;
            _lastMeasureIndex = currentMeasure;

            HandleMeasureTransition(previousMeasure, currentMeasure);
        }

        // Check if entire round is completed
        int totalMeasuresInRound = GameConfig.PrepMeasures + GameConfig.MeasuresPerRound * 2;
        // Wait an extra buffer of 0.3s after last measure so the final nail judgment settles cleanly
        if (elapsedTime >= totalMeasuresInRound * measureDuration + 0.3f)
        {
            CompleteRound();
        }
    }

    private void HandleMeasureTransition(int previousMeasure, int currentMeasure)
    {
        // Update state between Preparing and Playing
        if (currentMeasure < GameConfig.PrepMeasures)
        {
            CurrentState = GameState.Preparing;
        }
        else
        {
            CurrentState = GameState.Playing;
        }

        OnMeasureStart?.Invoke(currentMeasure);
    }

    /// <summary>
    /// Checks for completed defense measures and settles normalized scores once their judgment window concludes.
    /// </summary>
    private void CheckMeasureSettlements(float elapsedTime)
    {
        int maxDefenseMeasure = GameConfig.MeasuresPerRound * 2 - 1;
        for (int defM = 1; defM <= maxDefenseMeasure; defM += 2)
        {
            if (_settledMeasures.Contains(defM)) continue;

            float defenseMeasureEndTime = (defM + 1 + GameConfig.PrepMeasures) * GameConfig.MeasureDuration;

            // Settle when 200ms have passed after the defense measure ended (allowing all late windows to resolve)
            // Or when all placed nails in this measure have already been consumed and measure has reached end
            if (elapsedTime >= defenseMeasureEndTime + HitTierExtensions.WindowBadMs)
            {
                SettleMeasureScore(defM);
            }
            else if (elapsedTime >= defenseMeasureEndTime)
            {
                int attackM = defM - 1;
                if (_measureNails.TryGetValue(attackM, out var nails) && nails.Count > 0)
                {
                    bool allConsumed = true;
                    for (int i = 0; i < nails.Count; i++)
                    {
                        if (!nails[i].IsConsumed) { allConsumed = false; break; }
                    }
                    if (allConsumed)
                    {
                        SettleMeasureScore(defM);
                    }
                }
            }
        }
    }
    private void CheckNailTimeouts()
    {
        if (CurrentState != GameState.Playing) return;

        float currentTime = Time.time;
        foreach (var kvp in _measureNails)
        {
            var list = kvp.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var nail = list[i];
                if (!nail.IsConsumed && (currentTime - nail.TargetArrivalTime > HitTierExtensions.WindowBadMs))
                {
                    nail.ApplyTimeoutMiss();

                    // Track miss for current defender
                    RecordHitResult(CurrentDefender, HitTier.Miss);

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlayMiss();
                    }

                    OnJudgment?.Invoke(HitTier.Miss, 0.201f);
                }
            }
        }
    }

    /// <summary>
    /// Settles the normalized score for the completed defense measure.
    /// </summary>
    private void SettleMeasureScore(int defenseGameplayMeasure)
    {
        if (_settledMeasures.Contains(defenseGameplayMeasure)) return;
        _settledMeasures.Add(defenseGameplayMeasure);

        int attackGameplayMeasure = defenseGameplayMeasure - 1;
        int defender = CurrentDefender;

        _measureNails.TryGetValue(attackGameplayMeasure, out var nails);
        int n = nails != null ? nails.Count : 0;

        int rawScore = 0;
        if (nails != null)
        {
            foreach (var nail in nails)
            {
                rawScore += nail.Tier.GetBasePoints();
            }
        }

        int measureScore = 0;
        if (n == 0)
        {
            // Rule 6: If attacker placed 0 nails, defender automatically gets 600 full points
            measureScore = 600;
        }
        else if (n <= 6)
        {
            // Rule 7: Normalize to 600 points
            measureScore = Mathf.RoundToInt(rawScore * (600f / (n * 100f)));
        }
        else
        {
            // Rule 7: Retain raw score with max n * 100
            measureScore = rawScore;
        }

        if (defender == 1)
        {
            P1Score += measureScore;
            OnScoreSettled?.Invoke(1, measureScore, P1Score);
        }
        else
        {
            P2Score += measureScore;
            OnScoreSettled?.Invoke(2, measureScore, P2Score);
        }
    }

    private void CompleteRound()
    {
        CurrentState = GameState.RoundTransition;
        if (_conveyor != null) _conveyor.PauseConveyor();

        // Ensure last defense measure of this round is settled
        int lastDefenseMeasure = GameConfig.MeasuresPerRound * 2 - 1;
        SettleMeasureScore(lastDefenseMeasure);

        if (CurrentRound < GameConfig.TotalRounds)
        {
            StartCoroutine(RoundTransitionRoutine());
        }
        else
        {
            TriggerGameOver();
        }
    }

    private IEnumerator RoundTransitionRoutine()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFanfare();
        }

        yield return new WaitForSeconds(1.5f);

        StartRound(CurrentRound + 1);
    }

    private void TriggerGameOver()
    {
        CurrentState = GameState.GameOver;
        if (_conveyor != null) _conveyor.PauseConveyor();

        int winner = 0; // 0 = tie
        if (P1Score > P2Score) winner = 1;
        else if (P2Score > P1Score) winner = 2;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayFanfare();
        }

        OnGameOver?.Invoke(winner);
    }

    #region Input Handlers

    private void HandleActionAPressed()
    {
        // Player 1 input
        HandlePlayerInput(1);
    }

    private void HandleActionBPressed()
    {
        // Player 2 input
        HandlePlayerInput(2);
    }

    private void HandlePlayerInput(int player)
    {
        if (CurrentState != GameState.Playing && CurrentState != GameState.Preparing) return;

        if (player == CurrentDefender)
        {
            TryStrikeHammer();
        }
        else if (player == CurrentAttacker)
        {
            TryPlaceNail();
        }
    }

    private void TryPlaceNail()
    {
        float elapsedTime = Time.time - _roundStartTime;
        float gameplayTime = elapsedTime - GameConfig.PrepMeasures * GameConfig.MeasureDuration;

        // Allow placing if within gameplay phase, plus up to 150ms anticipation before measure 0
        if (gameplayTime < -0.15f) return;

        // Determine which attack measure this press targets
        int totalGameplayMeasures = GameConfig.MeasuresPerRound * 2;
        int targetAttackMeasure = -1;

        for (int m = 0; m < totalGameplayMeasures; m += 2)
        {
            float mStart = m * GameConfig.MeasureDuration;
            float mEnd = (m + 1) * GameConfig.MeasureDuration;

            // Target window for attack measure m: [mStart - 0.15s, mEnd)
            if (gameplayTime >= mStart - 0.15f && gameplayTime < mEnd)
            {
                targetAttackMeasure = m;
                break;
            }
        }

        if (targetAttackMeasure < 0) return; // Outside valid attack measure windows

        float measureStartTime = (targetAttackMeasure + GameConfig.PrepMeasures) * GameConfig.MeasureDuration;
        float timeInMeasure = elapsedTime - measureStartTime;

        // Quantize to nearest subdivision tick
        int tickIndex = Mathf.RoundToInt(timeInMeasure / GameConfig.SubdivisionDuration);
        tickIndex = Mathf.Clamp(tickIndex, 0, GameConfig.TicksPerMeasure - 1);

        if (!_measureNails.TryGetValue(targetAttackMeasure, out var list))
        {
            list = new List<NailInstance>();
            _measureNails[targetAttackMeasure] = list;
        }

        // Rule 6: Ignore duplicate placement at the same quantized tick
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].TickIndex == tickIndex)
            {
                return;
            }
        }

        // Calculate exact target arrival time at the striking line
        float quantizedTimeInMeasure = tickIndex * GameConfig.SubdivisionDuration;
        float actualMeasureStartAbs = _roundStartTime + measureStartTime;
        float targetArrivalTime = actualMeasureStartAbs + GameConfig.MeasureDuration + quantizedTimeInMeasure;

        // Calculate beat count in round for board positioning
        float quantizedBeatInRound = (targetAttackMeasure + GameConfig.PrepMeasures) * GameConfig.BeatsPerMeasure
                                     + (float)tickIndex / GameConfig.SubdivisionsPerBeat;

        if (_conveyor != null)
        {
            var nail = _conveyor.SpawnNailOnBoard(targetAttackMeasure, tickIndex, targetArrivalTime, quantizedBeatInRound);
            if (nail != null)
            {
                list.Add(nail);
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNailPlace();
        }
    }

    private void TryStrikeHammer()
    {
        float currentTime = Time.time;

        // Defense strike cooldown: 0.75 * current minimum subdivision duration (Rule 5)
        if (currentTime < _lastStrikeTime + GameConfig.DefenseCooldownDuration)
        {
            return;
        }
        _lastStrikeTime = currentTime;

        // Trigger hammer animation
        if (_conveyor != null && _conveyor.Hammer != null)
        {
            _conveyor.Hammer.TriggerStrike();
        }

        // Find the closest unconsumed nail across all active nails in this round
        NailInstance bestNail = null;
        float minAbsDelta = float.MaxValue;
        float bestDelta = 0f;

        foreach (var kvp in _measureNails)
        {
            var list = kvp.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var nail = list[i];
                if (!nail.IsConsumed)
                {
                    float delta = currentTime - nail.TargetArrivalTime;
                    float absDelta = Mathf.Abs(delta);
                    if (absDelta < minAbsDelta)
                    {
                        minAbsDelta = absDelta;
                        bestNail = nail;
                        bestDelta = delta;
                    }
                }
            }
        }

        // Check if within judgment window (200ms)
        if (bestNail != null && minAbsDelta <= HitTierExtensions.WindowBadMs)
        {
            HitTier tier = HitTier.Bad;
            if (minAbsDelta <= HitTierExtensions.WindowPerfectMs)
            {
                tier = HitTier.Perfect;
            }
            else if (minAbsDelta <= HitTierExtensions.WindowGoodMs)
            {
                tier = HitTier.Good;
            }

            bestNail.ApplyJudgment(tier, bestDelta);
            RecordHitResult(CurrentDefender, tier);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayHammerHit(tier);
            }

            OnJudgment?.Invoke(tier, bestDelta);
        }
        else
        {
            // Ghost tap: Defender swung hammer in the air outside judgment window.
            // Per Rule 5: Not penalized as Miss!
        }
    }

    private void RecordHitResult(int defender, HitTier tier)
    {
        if (defender == 1)
        {
            switch (tier)
            {
                case HitTier.Perfect: P1PerfectCount++; break;
                case HitTier.Good: P1GoodCount++; break;
                case HitTier.Bad: P1BadCount++; break;
                case HitTier.Miss: P1MissCount++; break;
            }
        }
        else
        {
            switch (tier)
            {
                case HitTier.Perfect: P2PerfectCount++; break;
                case HitTier.Good: P2GoodCount++; break;
                case HitTier.Bad: P2BadCount++; break;
                case HitTier.Miss: P2MissCount++; break;
            }
        }
    }

    #endregion

    /// <summary>
    /// Helper to get current measure progress from 0.0 to 1.0.
    /// </summary>
    public float GetCurrentMeasureProgress()
    {
        if (CurrentState == GameState.NotStarted || CurrentState == GameState.GameOver) return 0f;
        float elapsedTime = Time.time - _roundStartTime;
        float timeInMeasure = elapsedTime % GameConfig.MeasureDuration;
        return Mathf.Clamp01(timeInMeasure / GameConfig.MeasureDuration);
    }
}
