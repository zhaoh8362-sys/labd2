using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Keybind settings UI controller.
/// Allows viewing and modifying keybindings for Action A and Action B,
/// restoring default 6ygb split configuration, and live testing inputs.
/// </summary>
public class KeybindSettingUI : MonoBehaviour
{
    [Header("Display Text")]
    [SerializeField] private Text _actionADisplayText;
    [SerializeField] private Text _actionBDisplayText;
    [SerializeField] private Text _promptText;

    [Header("Buttons")]
    [SerializeField] private Button _rebindAButton;
    [SerializeField] private Button _rebindBButton;
    [SerializeField] private Button _resetDefaultButton;
    [SerializeField] private Button _backButton;

    [Header("Test Indicators")]
    [SerializeField] private Image _testIndicatorA;
    [SerializeField] private Text _testIndicatorTextA;
    [SerializeField] private Image _testIndicatorB;
    [SerializeField] private Text _testIndicatorTextB;

    private readonly Color _normalColor = new Color(0.18f, 0.22f, 0.28f, 1f);
    private readonly Color _pressedColorA = new Color(0.15f, 0.75f, 0.95f, 1f);
    private readonly Color _pressedColorB = new Color(0.95f, 0.65f, 0.15f, 1f);

    private void Start()
    {
        if (_rebindAButton != null)
        {
            _rebindAButton.onClick.AddListener(OnRebindAClicked);
        }

        if (_rebindBButton != null)
        {
            _rebindBButton.onClick.AddListener(OnRebindBClicked);
        }

        if (_resetDefaultButton != null)
        {
            _resetDefaultButton.onClick.AddListener(OnResetDefaultClicked);
        }

        if (_backButton != null)
        {
            _backButton.onClick.AddListener(OnBackClicked);
        }

        RefreshUI();

        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnBindingsChanged += RefreshUI;
        }
    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnBindingsChanged -= RefreshUI;
            GameInput.Instance.CancelRebinding();
        }
    }

    private void Update()
    {
        if (GameInput.Instance == null) return;

        bool isAPressed = GameInput.Instance.IsActionAPressed;
        bool isBPressed = GameInput.Instance.IsActionBPressed;

        if (_testIndicatorA != null)
        {
            _testIndicatorA.color = isAPressed ? _pressedColorA : _normalColor;
        }

        if (_testIndicatorTextA != null)
        {
            _testIndicatorTextA.text = isAPressed ? "Action A [Pressed]" : "Action A";
        }

        if (_testIndicatorB != null)
        {
            _testIndicatorB.color = isBPressed ? _pressedColorB : _normalColor;
        }

        if (_testIndicatorTextB != null)
        {
            _testIndicatorTextB.text = isBPressed ? "Action B [Pressed]" : "Action B";
        }
    }

    private void RefreshUI()
    {
        if (GameInput.Instance == null) return;

        if (_actionADisplayText != null)
        {
            _actionADisplayText.text = GameInput.Instance.GetActionADisplayName();
        }

        if (_actionBDisplayText != null)
        {
            _actionBDisplayText.text = GameInput.Instance.GetActionBDisplayName();
        }

        if (_promptText != null)
        {
            _promptText.text = "Tip: The game only requires two inputs. Click a rebind button and press any keyboard key.";
        }

        SetButtonsInteractable(true);
    }

    private void OnRebindAClicked()
    {
        if (GameInput.Instance == null) return;

        SetButtonsInteractable(false);
        if (_promptText != null)
        {
            _promptText.text = "[Rebinding Action A] Press any keyboard key... (Press Esc to cancel)";
        }

        GameInput.Instance.StartRebindingActionA(
            onComplete: keyName =>
            {
                RefreshUI();
                if (_promptText != null)
                {
                    _promptText.text = $"Successfully bound [Action A] to: {keyName}";
                }
            },
            onCancel: () =>
            {
                RefreshUI();
                if (_promptText != null)
                {
                    _promptText.text = "Cancelled rebinding Action A.";
                }
            });
    }

    private void OnRebindBClicked()
    {
        if (GameInput.Instance == null) return;

        SetButtonsInteractable(false);
        if (_promptText != null)
        {
            _promptText.text = "[Rebinding Action B] Press any keyboard key... (Press Esc to cancel)";
        }

        GameInput.Instance.StartRebindingActionB(
            onComplete: keyName =>
            {
                RefreshUI();
                if (_promptText != null)
                {
                    _promptText.text = $"Successfully bound [Action B] to: {keyName}";
                }
            },
            onCancel: () =>
            {
                RefreshUI();
                if (_promptText != null)
                {
                    _promptText.text = "Cancelled rebinding Action B.";
                }
            });
    }

    private void OnResetDefaultClicked()
    {
        if (GameInput.Instance == null) return;

        GameInput.Instance.ResetToDefault();
        RefreshUI();
        if (_promptText != null)
        {
            _promptText.text = "Restored default split bindings (6ygb & left to Action A, right to Action B).";
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_rebindAButton != null) _rebindAButton.interactable = interactable;
        if (_rebindBButton != null) _rebindBButton.interactable = interactable;
        if (_resetDefaultButton != null) _resetDefaultButton.interactable = interactable;
        if (_backButton != null) _backButton.interactable = interactable;
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("TitleScene");
    }
}
