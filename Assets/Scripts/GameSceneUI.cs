using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UI controller for GameScene.
/// Manages scoreboards, active turn indicators, judgment popups, live key indicators, and Game Over dialog.
/// </summary>
public class GameSceneUI : MonoBehaviour
{
    [Header("Top HUD")]
    [SerializeField] private Text _roundText;
    [SerializeField] private Text _phaseText;
    [SerializeField] private Text _scoreP1Text;
    [SerializeField] private Text _scoreP2Text;
    [SerializeField] private Text _rolesBannerText;

    [Header("Live Input Panels")]
    [SerializeField] private Image _indicatorP1;
    [SerializeField] private Text _indicatorP1Text;
    [SerializeField] private Image _indicatorP2;
    [SerializeField] private Text _indicatorP2Text;

    [Header("Judgment & Settlement Feedback")]
    [SerializeField] private Text _judgmentText;
    [SerializeField] private Text _settlementText;

    [Header("In-Game Buttons")]
    [SerializeField] private Button _titleButton;
    [SerializeField] private Button _restartButton;

    [Header("Game Over Modal")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private Text _winnerTitleText;
    [SerializeField] private Text _finalScoreP1Text;
    [SerializeField] private Text _finalScoreP2Text;
    [SerializeField] private Text _statsP1Text;
    [SerializeField] private Text _statsP2Text;
    [SerializeField] private Button _modalPlayAgainButton;
    [SerializeField] private Button _modalMainMenuButton;

    private Coroutine _judgmentCoroutine;
    private Coroutine _settlementCoroutine;

    private readonly Color _colorP1 = new Color(0.2f, 0.8f, 1.0f, 1f);
    private readonly Color _colorP2 = new Color(1.0f, 0.75f, 0.2f, 1f);
    private readonly Color _normalKeyColor = new Color(0.18f, 0.22f, 0.28f, 1f);

    private void Start()
    {
        if (_titleButton != null)
        {
            _titleButton.onClick.AddListener(OnTitleClicked);
        }
        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(OnRestartClicked);
        }
        if (_modalPlayAgainButton != null)
        {
            _modalPlayAgainButton.onClick.AddListener(OnRestartClicked);
        }
        if (_modalMainMenuButton != null)
        {
            _modalMainMenuButton.onClick.AddListener(OnTitleClicked);
        }

        if (_gameOverPanel != null)
        {
            _gameOverPanel.SetActive(false);
        }

        if (_judgmentText != null) _judgmentText.text = "";
        if (_settlementText != null) _settlementText.text = "";

        // Wire game manager events
        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.OnBeatTick += HandleBeatTick;
            RhythmGameManager.Instance.OnJudgment += HandleJudgment;
            RhythmGameManager.Instance.OnScoreSettled += HandleScoreSettled;
            RhythmGameManager.Instance.OnGameOver += HandleGameOver;
            RhythmGameManager.Instance.OnRoundStarted += HandleRoundStarted;
        }

        UpdateHUD();
    }

    private void OnDestroy()
    {
        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.OnBeatTick -= HandleBeatTick;
            RhythmGameManager.Instance.OnJudgment -= HandleJudgment;
            RhythmGameManager.Instance.OnScoreSettled -= HandleScoreSettled;
            RhythmGameManager.Instance.OnGameOver -= HandleGameOver;
            RhythmGameManager.Instance.OnRoundStarted -= HandleRoundStarted;
        }
    }

    private void Update()
    {
        UpdateLiveInputIndicators();
    }

    private void UpdateLiveInputIndicators()
    {
        if (GameInput.Instance == null) return;

        bool isP1Pressed = GameInput.Instance.IsActionAPressed;
        bool isP2Pressed = GameInput.Instance.IsActionBPressed;

        if (_indicatorP1 != null)
        {
            _indicatorP1.color = isP1Pressed ? _colorP1 : _normalKeyColor;
        }
        if (_indicatorP2 != null)
        {
            _indicatorP2.color = isP2Pressed ? _colorP2 : _normalKeyColor;
        }
    }

    private void HandleBeatTick(int measure, int beat)
    {
        UpdateHUD();
    }

    private void HandleRoundStarted(int round)
    {
        UpdateHUD();
        if (_settlementText != null) _settlementText.text = "";
        if (_judgmentText != null) _judgmentText.text = "";
    }

    private void UpdateHUD()
    {
        var gm = RhythmGameManager.Instance;
        if (gm == null) return;

        if (_roundText != null)
        {
            _roundText.text = $"ROUND {gm.CurrentRound} / {GameConfig.TotalRounds}";
        }

        if (_scoreP1Text != null)
        {
            _scoreP1Text.text = $"P1 (Left): {gm.P1Score} pts";
        }
        if (_scoreP2Text != null)
        {
            _scoreP2Text.text = $"P2 (Right): {gm.P2Score} pts";
        }

        string p1Role = (gm.CurrentAttacker == 1) ? "ATTACKER" : "DEFENDER";
        string p2Role = (gm.CurrentAttacker == 2) ? "ATTACKER" : "DEFENDER";

        if (_rolesBannerText != null)
        {
            _rolesBannerText.text = $"P1 [Action A]: {p1Role}   |   P2 [Action B]: {p2Role}";
        }

        if (_indicatorP1Text != null)
        {
            _indicatorP1Text.text = $"P1: {p1Role}\n({GameInput.Instance?.GetActionADisplayName() ?? "Action A"})";
        }
        if (_indicatorP2Text != null)
        {
            _indicatorP2Text.text = $"P2: {p2Role}\n({GameInput.Instance?.GetActionBDisplayName() ?? "Action B"})";
        }

        if (_phaseText != null)
        {
            if (gm.CurrentState == RhythmGameManager.GameState.Preparing)
            {
                int prepMeasure = gm.CurrentMeasureIndex + 1;
                int prepBeat = gm.CurrentBeatInMeasure + 1;
                _phaseText.text = $"PREPARING: MEASURE {prepMeasure} / {GameConfig.PrepMeasures}  (Beat {prepBeat}/{GameConfig.BeatsPerMeasure})";
                _phaseText.color = new Color(1.0f, 0.9f, 0.25f, 1f);
            }
            else if (gm.CurrentState == RhythmGameManager.GameState.Playing)
            {
                int totalGameplayMeasures = GameConfig.MeasuresPerRound * 2;
                int currentM = gm.CurrentGameplayMeasure + 1;
                int currentB = gm.CurrentBeatInMeasure + 1;

                if (gm.IsAttackingMeasure)
                {
                    string attPlayer = (gm.CurrentAttacker == 1) ? "P1" : "P2";
                    _phaseText.text = $"[ATTACK MEASURE {currentM}/{totalGameplayMeasures}] - {attPlayer} Place Rhythm! (Beat {currentB})";
                    _phaseText.color = (gm.CurrentAttacker == 1) ? _colorP1 : _colorP2;
                }
                else
                {
                    string defPlayer = (gm.CurrentDefender == 1) ? "P1" : "P2";
                    _phaseText.text = $"[DEFENSE MEASURE {currentM}/{totalGameplayMeasures}] - {defPlayer} Strike Nails! (Beat {currentB})";
                    _phaseText.color = (gm.CurrentDefender == 1) ? _colorP1 : _colorP2;
                }
            }
            else if (gm.CurrentState == RhythmGameManager.GameState.RoundTransition)
            {
                _phaseText.text = "ROLES SWAP! Prepare for Next Round...";
                _phaseText.color = Color.white;
            }
        }
    }

    private void HandleJudgment(HitTier tier, float deltaSeconds)
    {
        if (_judgmentText == null) return;

        if (_judgmentCoroutine != null)
        {
            StopCoroutine(_judgmentCoroutine);
        }

        string text = tier.GetDisplayText();
        if (tier != HitTier.Miss)
        {
            string earlyLate = deltaSeconds < 0 ? " (EARLY)" : " (LATE)";
            text += earlyLate + $" +{tier.GetBasePoints()}";
        }

        _judgmentCoroutine = StartCoroutine(ShowJudgmentRoutine(text, tier.GetDisplayColor()));
    }

    private IEnumerator ShowJudgmentRoutine(string message, Color color)
    {
        _judgmentText.text = message;
        _judgmentText.color = color;

        yield return new WaitForSeconds(0.45f);

        // Fade out
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            _judgmentText.color = new Color(color.r, color.g, color.b, Mathf.Lerp(1f, 0f, t / 0.2f));
            yield return null;
        }

        _judgmentText.text = "";
        _judgmentCoroutine = null;
    }

    private void HandleScoreSettled(int defenderPlayer, int measureScore, int totalScore)
    {
        UpdateHUD();

        if (_settlementText != null)
        {
            if (_settlementCoroutine != null)
            {
                StopCoroutine(_settlementCoroutine);
            }
            _settlementCoroutine = StartCoroutine(ShowSettlementRoutine($"P{defenderPlayer} +{measureScore} PTS (Total: {totalScore})"));
        }
    }

    private IEnumerator ShowSettlementRoutine(string text)
    {
        _settlementText.text = text;
        _settlementText.color = new Color(0.3f, 1f, 0.5f, 1f);

        yield return new WaitForSeconds(1.2f);

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            _settlementText.color = new Color(0.3f, 1f, 0.5f, Mathf.Lerp(1f, 0f, t / 0.3f));
            yield return null;
        }

        _settlementText.text = "";
        _settlementCoroutine = null;
    }

    private void HandleGameOver(int winner)
    {
        var gm = RhythmGameManager.Instance;
        if (gm == null || _gameOverPanel == null) return;

        _gameOverPanel.SetActive(true);

        if (_winnerTitleText != null)
        {
            if (winner == 1)
            {
                _winnerTitleText.text = "PLAYER 1 WINS!";
                _winnerTitleText.color = _colorP1;
            }
            else if (winner == 2)
            {
                _winnerTitleText.text = "PLAYER 2 WINS!";
                _winnerTitleText.color = _colorP2;
            }
            else
            {
                _winnerTitleText.text = "IT'S A TIE!";
                _winnerTitleText.color = Color.white;
            }
        }

        if (_finalScoreP1Text != null) _finalScoreP1Text.text = $"Player 1 Final Score: {gm.P1Score} pts";
        if (_finalScoreP2Text != null) _finalScoreP2Text.text = $"Player 2 Final Score: {gm.P2Score} pts";

        if (_statsP1Text != null)
        {
            _statsP1Text.text = $"P1 Defense Stats:\nPerfect: {gm.P1PerfectCount}  |  Good: {gm.P1GoodCount}  |  Bad: {gm.P1BadCount}  |  Miss: {gm.P1MissCount}";
        }
        if (_statsP2Text != null)
        {
            _statsP2Text.text = $"P2 Defense Stats:\nPerfect: {gm.P2PerfectCount}  |  Good: {gm.P2GoodCount}  |  Bad: {gm.P2BadCount}  |  Miss: {gm.P2MissCount}";
        }
    }

    private void OnTitleClicked()
    {
        SceneManager.LoadScene("TitleScene");
    }

    private void OnRestartClicked()
    {
        if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.StartGame();
        }
    }
}
