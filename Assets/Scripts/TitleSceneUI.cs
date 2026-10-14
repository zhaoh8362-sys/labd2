using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Title scene UI controller.
/// Provides configuration for BPM, Beats per measure, Measures per round, Total rounds, and Snap Quantization,
/// plus navigation to GameScene / KeybindScene and live two-button feedback.
/// </summary>
public class TitleSceneUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _keybindButton;
    [SerializeField] private Button _quitButton;

    [Header("BPM Controls (60 - 300)")]
    [SerializeField] private Button _bpmMinusButton;
    [SerializeField] private Button _bpmPlusButton;
    [SerializeField] private Text _bpmValueText;

    [Header("Beats Per Measure Controls (2 - 8)")]
    [SerializeField] private Button _beatsMinusButton;
    [SerializeField] private Button _beatsPlusButton;
    [SerializeField] private Text _beatsValueText;

    [Header("Measures Per Round Controls (4 - 16)")]
    [SerializeField] private Button _measuresMinusButton;
    [SerializeField] private Button _measuresPlusButton;
    [SerializeField] private Text _measuresValueText;

    [Header("Total Rounds Controls (1 - 8)")]
    [SerializeField] private Button _roundsMinusButton;
    [SerializeField] private Button _roundsPlusButton;
    [SerializeField] private Text _roundsValueText;

    [Header("Quantization Controls (8, 12, 16, 24)")]
    [SerializeField] private Button _snapCycleButton;
    [SerializeField] private Text _snapValueText;

    [Header("Input Live Indicators")]
    [SerializeField] private Image _indicatorA;
    [SerializeField] private Text _indicatorTextA;
    [SerializeField] private Image _indicatorB;
    [SerializeField] private Text _indicatorTextB;
    [SerializeField] private Text _bindingInfoText;

    private readonly Color _normalColor = new Color(0.18f, 0.22f, 0.28f, 1f);
    private readonly Color _pressedColorA = new Color(0.15f, 0.75f, 0.95f, 1f);
    private readonly Color _pressedColorB = new Color(0.95f, 0.65f, 0.15f, 1f);

    private static readonly int[] QuantizationOptions = new int[] { 8, 12, 16, 24 };

    private void Start()
    {
        // Parameter control buttons
        if (_bpmMinusButton != null) _bpmMinusButton.onClick.AddListener(() => ChangeBpm(-10));
        if (_bpmPlusButton != null) _bpmPlusButton.onClick.AddListener(() => ChangeBpm(10));

        if (_beatsMinusButton != null) _beatsMinusButton.onClick.AddListener(() => ChangeBeats(-1));
        if (_beatsPlusButton != null) _beatsPlusButton.onClick.AddListener(() => ChangeBeats(1));

        if (_measuresMinusButton != null) _measuresMinusButton.onClick.AddListener(() => ChangeMeasures(-1));
        if (_measuresPlusButton != null) _measuresPlusButton.onClick.AddListener(() => ChangeMeasures(1));

        if (_roundsMinusButton != null) _roundsMinusButton.onClick.AddListener(() => ChangeRounds(-1));
        if (_roundsPlusButton != null) _roundsPlusButton.onClick.AddListener(() => ChangeRounds(1));

        if (_snapCycleButton != null) _snapCycleButton.onClick.AddListener(CycleQuantization);

        // Navigation buttons
        if (_startButton != null) _startButton.onClick.AddListener(OnStartClicked);
        if (_keybindButton != null) _keybindButton.onClick.AddListener(OnKeybindClicked);
        if (_quitButton != null) _quitButton.onClick.AddListener(OnQuitClicked);

        UpdateAllConfigLabels();
        UpdateBindingInfo();

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnBindingsChanged += UpdateBindingInfo;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnBindingsChanged -= UpdateBindingInfo;
        }
    }

    private void Update()
    {
        if (GameInput.Instance == null) return;

        bool isAPressed = GameInput.Instance.IsActionAPressed;
        bool isBPressed = GameInput.Instance.IsActionBPressed;

        if (_indicatorA != null)
        {
            _indicatorA.color = isAPressed ? _pressedColorA : _normalColor;
        }

        if (_indicatorTextA != null)
        {
            _indicatorTextA.text = isAPressed ? "P1 [Pressed]" : "P1 (Action A)";
        }

        if (_indicatorB != null)
        {
            _indicatorB.color = isBPressed ? _pressedColorB : _normalColor;
        }

        if (_indicatorTextB != null)
        {
            _indicatorTextB.text = isBPressed ? "P2 [Pressed]" : "P2 (Action B)";
        }
    }

    private void ChangeBpm(int delta)
    {
        GameConfig.Bpm = Mathf.Clamp(GameConfig.Bpm + delta, 60, 300);
        UpdateAllConfigLabels();
    }

    private void ChangeBeats(int delta)
    {
        GameConfig.BeatsPerMeasure = Mathf.Clamp(GameConfig.BeatsPerMeasure + delta, 2, 8);
        UpdateAllConfigLabels();
    }

    private void ChangeMeasures(int delta)
    {
        GameConfig.MeasuresPerRound = Mathf.Clamp(GameConfig.MeasuresPerRound + delta, 4, 16);
        UpdateAllConfigLabels();
    }

    private void ChangeRounds(int delta)
    {
        GameConfig.TotalRounds = Mathf.Clamp(GameConfig.TotalRounds + delta, 1, 8);
        UpdateAllConfigLabels();
    }

    private void CycleQuantization()
    {
        int currentIndex = Array.IndexOf(QuantizationOptions, GameConfig.Quantization);
        if (currentIndex < 0) currentIndex = 2; // default 16
        int nextIndex = (currentIndex + 1) % QuantizationOptions.Length;
        GameConfig.Quantization = QuantizationOptions[nextIndex];
        UpdateAllConfigLabels();
    }

    private void UpdateAllConfigLabels()
    {
        if (_bpmValueText != null)
        {
            _bpmValueText.text = $"BPM: {GameConfig.Bpm}";
        }

        if (_beatsValueText != null)
        {
            _beatsValueText.text = $"Beats: {GameConfig.BeatsPerMeasure} / measure";
        }

        if (_measuresValueText != null)
        {
            _measuresValueText.text = $"Measures: {GameConfig.MeasuresPerRound} pairs ({GameConfig.MeasuresPerRound * 2} measures)";
        }

        if (_roundsValueText != null)
        {
            _roundsValueText.text = $"Rounds: {GameConfig.TotalRounds}";
        }

        if (_snapValueText != null)
        {
            string snapName;
            switch (GameConfig.Quantization)
            {
                case 8: snapName = "8th Note (1/2 beat)"; break;
                case 12: snapName = "12th Note (Triplet)"; break;
                case 16: snapName = "16th Note (1/4 beat)"; break;
                case 24: snapName = "24th Note (Sextuplet)"; break;
                default: snapName = $"{GameConfig.Quantization}th Note"; break;
            }
            _snapValueText.text = $"Snap: {snapName}";
        }
    }

    private void UpdateBindingInfo()
    {
        if (_bindingInfoText != null && GameInput.Instance != null)
        {
            _bindingInfoText.text = $"Player 1: [Action A: {GameInput.Instance.GetActionADisplayName()}]   |   Player 2: [Action B: {GameInput.Instance.GetActionBDisplayName()}]";
        }
    }

    private void OnStartClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void OnKeybindClicked()
    {
        SceneManager.LoadScene("KeybindScene");
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
