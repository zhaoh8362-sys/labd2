using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Two-button game input system encapsulation (GameInput).
/// Manages the two core gameplay inputs (Action A / Action B).
/// Default layout: Keyboard 6, Y, G, B and all keys to their left are allocated to Action A.
/// All keys to their right are allocated to Action B.
/// Supports dynamic interactive rebinding to any keyboard key, persisted via PlayerPrefs.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    // Default left keyboard keys (including 6, Y, G, B and all keys to their left)
    public static readonly string[] DefaultLeftKeys = new string[]
    {
        // Function keys F1 - F6
        "<Keyboard>/f1", "<Keyboard>/f2", "<Keyboard>/f3", "<Keyboard>/f4", "<Keyboard>/f5", "<Keyboard>/f6",
        // Number row (` - 6)
        "<Keyboard>/backquote",
        "<Keyboard>/1", "<Keyboard>/2", "<Keyboard>/3", "<Keyboard>/4", "<Keyboard>/5", "<Keyboard>/6",
        // Top row (Tab, Q, W, E, R, T, Y)
        "<Keyboard>/tab",
        "<Keyboard>/q", "<Keyboard>/w", "<Keyboard>/e", "<Keyboard>/r", "<Keyboard>/t", "<Keyboard>/y",
        // Home row (CapsLock, A, S, D, F, G)
        "<Keyboard>/capsLock",
        "<Keyboard>/a", "<Keyboard>/s", "<Keyboard>/d", "<Keyboard>/f", "<Keyboard>/g",
        // Bottom row (LeftShift, Z, X, C, V, B)
        "<Keyboard>/leftShift",
        "<Keyboard>/z", "<Keyboard>/x", "<Keyboard>/c", "<Keyboard>/v", "<Keyboard>/b",
        // Modifier keys
        "<Keyboard>/leftCtrl", "<Keyboard>/leftAlt", "<Keyboard>/leftMeta"
    };

    // Default right keyboard keys (all keys to the right of 6, Y, G, B)
    public static readonly string[] DefaultRightKeys = new string[]
    {
        // Function keys F7 - F12
        "<Keyboard>/f7", "<Keyboard>/f8", "<Keyboard>/f9", "<Keyboard>/f10", "<Keyboard>/f11", "<Keyboard>/f12",
        // Number row (7 - 0, -, =, Backspace)
        "<Keyboard>/7", "<Keyboard>/8", "<Keyboard>/9", "<Keyboard>/0",
        "<Keyboard>/minus", "<Keyboard>/equals", "<Keyboard>/backspace",
        // Top row (U, I, O, P, [, ], \)
        "<Keyboard>/u", "<Keyboard>/i", "<Keyboard>/o", "<Keyboard>/p",
        "<Keyboard>/leftBracket", "<Keyboard>/rightBracket", "<Keyboard>/backslash",
        // Home row (H, J, K, L, ;, ', Enter)
        "<Keyboard>/h", "<Keyboard>/j", "<Keyboard>/k", "<Keyboard>/l",
        "<Keyboard>/semicolon", "<Keyboard>/quote", "<Keyboard>/enter",
        // Bottom row (N, M, ,, ., /, RightShift)
        "<Keyboard>/n", "<Keyboard>/m", "<Keyboard>/comma", "<Keyboard>/period",
        "<Keyboard>/slash", "<Keyboard>/rightShift",
        // Space and modifier keys
        "<Keyboard>/space", "<Keyboard>/rightAlt", "<Keyboard>/rightMeta", "<Keyboard>/rightCtrl", "<Keyboard>/contextMenu",
        // Navigation and arrow keys
        "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow",
        "<Keyboard>/insert", "<Keyboard>/delete", "<Keyboard>/home", "<Keyboard>/end",
        "<Keyboard>/pageUp", "<Keyboard>/pageDown",
        // Numpad
        "<Keyboard>/numLock", "<Keyboard>/numpad0", "<Keyboard>/numpad1", "<Keyboard>/numpad2",
        "<Keyboard>/numpad3", "<Keyboard>/numpad4", "<Keyboard>/numpad5", "<Keyboard>/numpad6",
        "<Keyboard>/numpad7", "<Keyboard>/numpad8", "<Keyboard>/numpad9", "<Keyboard>/numpadEnter",
        "<Keyboard>/numpadPlus", "<Keyboard>/numpadMinus", "<Keyboard>/numpadMultiply",
        "<Keyboard>/numpadDivide", "<Keyboard>/numpadPeriod", "<Keyboard>/numpadEquals"
    };

    private const string PrefsKeyCustomA = "GameInput_IsCustomA";
    private const string PrefsKeyPathA = "GameInput_PathA";
    private const string PrefsKeyDisplayA = "GameInput_DisplayA";

    private const string PrefsKeyCustomB = "GameInput_IsCustomB";
    private const string PrefsKeyPathB = "GameInput_PathB";
    private const string PrefsKeyDisplayB = "GameInput_DisplayB";

    // Binding states
    private bool _isCustomA;
    private string _customKeyPathA = "";
    private string _customKeyDisplayNameA = "";

    private bool _isCustomB;
    private string _customKeyPathB = "";
    private string _customKeyDisplayNameB = "";

    // Internal Input Action Map
    private InputActionMap _gameplayMap;
    private InputAction _actionA;
    private InputAction _actionB;

    // Active rebinding operation
    private InputActionRebindingExtensions.RebindingOperation _rebindOp;
    private InputAction _tempRebindAction;

    // Public events
    public event Action OnActionAPressed;
    public event Action OnActionAReleased;
    public event Action OnActionBPressed;
    public event Action OnActionBReleased;
    public event Action OnBindingsChanged;

    // Property accessors
    public bool IsActionAPressed => _actionA != null && _actionA.IsPressed();
    public bool IsActionBPressed => _actionB != null && _actionB.IsPressed();

    public bool WasActionAPressedThisFrame => _actionA != null && _actionA.WasPressedThisFrame();
    public bool WasActionBPressedThisFrame => _actionB != null && _actionB.WasPressedThisFrame();

    public bool IsCustomA => _isCustomA;
    public bool IsCustomB => _isCustomB;
    public string CustomKeyPathA => _customKeyPathA;
    public string CustomKeyDisplayNameA => _customKeyDisplayNameA;
    public string CustomKeyPathB => _customKeyPathB;
    public string CustomKeyDisplayNameB => _customKeyDisplayNameB;

    public bool IsRebinding => _rebindOp != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            var go = new GameObject("[GameInput]");
            go.AddComponent<GameInput>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPreferences();
        RebuildBindings();
    }

    private void OnDestroy()
    {
        CleanupRebinding();
        TeardownBindings();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnEnable()
    {
        _gameplayMap?.Enable();
    }

    private void OnDisable()
    {
        _gameplayMap?.Disable();
    }

    /// <summary>
    /// Reconstructs the action map and attaches bindings according to current configuration.
    /// </summary>
    public void RebuildBindings()
    {
        TeardownBindings();

        _gameplayMap = new InputActionMap("Gameplay");

        _actionA = _gameplayMap.AddAction("ActionA", InputActionType.Button);
        if (!_isCustomA)
        {
            foreach (var key in DefaultLeftKeys)
            {
                _actionA.AddBinding(key);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_customKeyPathA))
            {
                _actionA.AddBinding(_customKeyPathA);
            }
        }

        _actionB = _gameplayMap.AddAction("ActionB", InputActionType.Button);
        if (!_isCustomB)
        {
            foreach (var key in DefaultRightKeys)
            {
                _actionB.AddBinding(key);
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(_customKeyPathB))
            {
                _actionB.AddBinding(_customKeyPathB);
            }
        }

        _actionA.performed += HandleActionAPerformed;
        _actionA.canceled += HandleActionACanceled;
        _actionB.performed += HandleActionBPerformed;
        _actionB.canceled += HandleActionBCanceled;

        _gameplayMap.Enable();

        OnBindingsChanged?.Invoke();
    }

    private void TeardownBindings()
    {
        if (_actionA != null)
        {
            _actionA.performed -= HandleActionAPerformed;
            _actionA.canceled -= HandleActionACanceled;
        }

        if (_actionB != null)
        {
            _actionB.performed -= HandleActionBPerformed;
            _actionB.canceled -= HandleActionBCanceled;
        }

        if (_gameplayMap != null)
        {
            _gameplayMap.Disable();
            _gameplayMap.Dispose();
            _gameplayMap = null;
            _actionA = null;
            _actionB = null;
        }
    }

    private void HandleActionAPerformed(InputAction.CallbackContext ctx) => OnActionAPressed?.Invoke();
    private void HandleActionACanceled(InputAction.CallbackContext ctx) => OnActionAReleased?.Invoke();
    private void HandleActionBPerformed(InputAction.CallbackContext ctx) => OnActionBPressed?.Invoke();
    private void HandleActionBCanceled(InputAction.CallbackContext ctx) => OnActionBReleased?.Invoke();

    /// <summary>
    /// Returns human-readable display string for Action A.
    /// </summary>
    public string GetActionADisplayName()
    {
        if (!_isCustomA)
        {
            return "Default Split (6ygb & Left)";
        }
        return string.IsNullOrEmpty(_customKeyDisplayNameA) ? "Unassigned" : _customKeyDisplayNameA;
    }

    /// <summary>
    /// Returns human-readable display string for Action B.
    /// </summary>
    public string GetActionBDisplayName()
    {
        if (!_isCustomB)
        {
            return "Default Split (Right of 6ygb)";
        }
        return string.IsNullOrEmpty(_customKeyDisplayNameB) ? "Unassigned" : _customKeyDisplayNameB;
    }

    /// <summary>
    /// Initiates interactive rebinding for Action A (Escape to cancel).
    /// </summary>
    public void StartRebindingActionA(Action<string> onComplete, Action onCancel = null)
    {
        StartRebindingInternal(true, onComplete, onCancel);
    }

    /// <summary>
    /// Initiates interactive rebinding for Action B (Escape to cancel).
    /// </summary>
    public void StartRebindingActionB(Action<string> onComplete, Action onCancel = null)
    {
        StartRebindingInternal(false, onComplete, onCancel);
    }

    private void StartRebindingInternal(bool isActionA, Action<string> onComplete, Action onCancel)
    {
        CancelRebinding();

        // Temporarily disable gameplay input during rebinding
        _gameplayMap?.Disable();

        _tempRebindAction = new InputAction("TempRebind", InputActionType.Button);
        _tempRebindAction.AddBinding("<Keyboard>/anyKey");

        _rebindOp = _tempRebindAction.PerformInteractiveRebinding()
            .WithControlsHavingToMatchPath("<Keyboard>/*")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.05f)
            .OnComplete(op =>
            {
                var control = op.selectedControl;
                string path = control.path;
                string displayName = FormatControlDisplayName(control);

                if (isActionA)
                {
                    _isCustomA = true;
                    _customKeyPathA = path;
                    _customKeyDisplayNameA = displayName;
                }
                else
                {
                    _isCustomB = true;
                    _customKeyPathB = path;
                    _customKeyDisplayNameB = displayName;
                }

                SavePreferences();
                CleanupRebinding();
                RebuildBindings();

                onComplete?.Invoke(displayName);
            })
            .OnCancel(op =>
            {
                CleanupRebinding();
                _gameplayMap?.Enable();
                onCancel?.Invoke();
            });

        _rebindOp.Start();
    }

    /// <summary>
    /// Cancels active rebinding operation if one is currently running.
    /// </summary>
    public void CancelRebinding()
    {
        if (_rebindOp != null)
        {
            _rebindOp.Cancel();
            CleanupRebinding();
            _gameplayMap?.Enable();
        }
    }

    private void CleanupRebinding()
    {
        if (_rebindOp != null)
        {
            _rebindOp.Dispose();
            _rebindOp = null;
        }

        if (_tempRebindAction != null)
        {
            _tempRebindAction.Dispose();
            _tempRebindAction = null;
        }
    }

    /// <summary>
    /// Resets keybindings to the default split (6ygb & left for A, right for B).
    /// </summary>
    public void ResetToDefault()
    {
        CancelRebinding();

        _isCustomA = false;
        _customKeyPathA = "";
        _customKeyDisplayNameA = "";

        _isCustomB = false;
        _customKeyPathB = "";
        _customKeyDisplayNameB = "";

        SavePreferences();
        RebuildBindings();
    }

    /// <summary>
    /// Formats an InputControl name into a user-friendly display name.
    /// </summary>
    public static string FormatControlDisplayName(InputControl control)
    {
        if (control == null) return "None";
        string keyName = control.name.ToLowerInvariant();
        switch (keyName)
        {
            case "space": return "Space";
            case "enter": return "Enter";
            case "tab": return "Tab";
            case "capslock": return "Caps Lock";
            case "backspace": return "Backspace";
            case "leftshift": return "Left Shift";
            case "rightshift": return "Right Shift";
            case "leftctrl": return "Left Ctrl";
            case "rightctrl": return "Right Ctrl";
            case "leftalt": return "Left Alt";
            case "rightalt": return "Right Alt";
            case "leftmeta": return "Left Win/Cmd";
            case "rightmeta": return "Right Win/Cmd";
            case "contextmenu": return "Menu";
            case "uparrow": return "Up Arrow";
            case "downarrow": return "Down Arrow";
            case "leftarrow": return "Left Arrow";
            case "rightarrow": return "Right Arrow";
            case "backquote": return "Backquote (`)";
            case "minus": return "Minus (-)";
            case "equals": return "Equals (=)";
            case "leftbracket": return "[";
            case "rightbracket": return "]";
            case "backslash": return "\\";
            case "semicolon": return ";";
            case "quote": return "'";
            case "comma": return ",";
            case "period": return ".";
            case "slash": return "/";
            default:
                if (!string.IsNullOrWhiteSpace(control.displayName) && control.displayName.Length == 1 && char.IsLetterOrDigit(control.displayName[0]))
                {
                    return control.displayName.ToUpperInvariant();
                }
                return control.name.ToUpperInvariant();
        }
    }

    private void LoadPreferences()
    {
        _isCustomA = PlayerPrefs.GetInt(PrefsKeyCustomA, 0) == 1;
        _customKeyPathA = PlayerPrefs.GetString(PrefsKeyPathA, "");
        _customKeyDisplayNameA = PlayerPrefs.GetString(PrefsKeyDisplayA, "");

        _isCustomB = PlayerPrefs.GetInt(PrefsKeyCustomB, 0) == 1;
        _customKeyPathB = PlayerPrefs.GetString(PrefsKeyPathB, "");
        _customKeyDisplayNameB = PlayerPrefs.GetString(PrefsKeyDisplayB, "");
    }

    private void SavePreferences()
    {
        PlayerPrefs.SetInt(PrefsKeyCustomA, _isCustomA ? 1 : 0);
        PlayerPrefs.SetString(PrefsKeyPathA, _customKeyPathA);
        PlayerPrefs.SetString(PrefsKeyDisplayA, _customKeyDisplayNameA);

        PlayerPrefs.SetInt(PrefsKeyCustomB, _isCustomB ? 1 : 0);
        PlayerPrefs.SetString(PrefsKeyPathB, _customKeyPathB);
        PlayerPrefs.SetString(PrefsKeyDisplayB, _customKeyDisplayNameB);

        PlayerPrefs.Save();
    }
}
