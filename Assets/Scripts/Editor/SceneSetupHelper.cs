using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Scene setup helper and builder.
/// Generates TitleScene (with rhythm game configurations), GameScene (with 640x360 pixel conveyor board & duel HUD),
/// and KeybindScene in English.
/// </summary>
public static class SceneSetupHelper
{
    private static Font _font;
    private static Sprite _uiSprite;
    private static Sprite _panelSprite;

    private static void InitResources()
    {
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        _panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    }

    [MenuItem("Tools/Build All Scenes")]
    public static void BuildAllScenes()
    {
        InitResources();

        // 1. Setup sprite slicing and import settings first
        SpriteImportSetup.SetupAllSprites();

        if (!Directory.Exists("Assets/Scenes"))
        {
            Directory.CreateDirectory("Assets/Scenes");
        }

        if (!Directory.Exists("Assets/Resources"))
        {
            Directory.CreateDirectory("Assets/Resources");
        }

        CreateGameSpriteManagerPrefab();
        CreateAudioManagerPrefab();

        BuildTitleScene("Assets/Scenes/TitleScene.unity");
        BuildGameScene("Assets/Scenes/GameScene.unity");
        BuildKeybindScene("Assets/Scenes/KeybindScene.unity");

        // Set Build Settings
        var buildScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/KeybindScene.unity", true)
        };
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
        Debug.Log("All 3 scenes successfully created in English and added to Build Settings!");
    }

    private static void CreateGameSpriteManagerPrefab()
    {
        var tempGo = new GameObject("GameSpriteManager");
        var mgr = tempGo.AddComponent<GameSpriteManager>();
        mgr.LoadEditorAssets();

        string prefabPath = "Assets/Resources/GameSpriteManager.prefab";
        PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
        Object.DestroyImmediate(tempGo);
    }

    private static void CreateAudioManagerPrefab()
    {
        var tempGo = new GameObject("AudioManager");
        var mgr = tempGo.AddComponent<AudioManager>();
        mgr.EnsureMixerLoaded();

        string prefabPath = "Assets/Resources/AudioManager.prefab";
        PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
        Object.DestroyImmediate(tempGo);
    }

    private static void BuildTitleScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupCameraAndLight(false);
        SetupEventSystem();

        var (canvasGo, canvasScaler) = SetupCanvas("TitleCanvas", new Vector2(1920, 1080));

        // Root background panel
        var rootPanel = CreatePanel(canvasGo.transform, "BackgroundPanel", new Color(0.10f, 0.12f, 0.15f, 1f));
        SetStretch(rootPanel.GetComponent<RectTransform>());

        // Center configuration card (header removed per user request)
        var configCardObj = CreatePanel(rootPanel.transform, "ConfigCard", new Color(0.14f, 0.17f, 0.22f, 1f));
        var configCardRect = configCardObj.GetComponent<RectTransform>();
        configCardRect.anchorMin = new Vector2(0.5f, 0.65f);
        configCardRect.anchorMax = new Vector2(0.5f, 0.65f);
        configCardRect.sizeDelta = new Vector2(900, 330);

        var configCardTitle = CreateText(configCardObj.transform, "ConfigTitle", "MATCH SETTINGS", 22, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.4f, 0.85f, 1f));
        configCardTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 135);

        // Row 1: BPM
        var (bpmMinusBtn, bpmPlusBtn, bpmValText) = CreateStepperRow(configCardObj.transform, "BpmRow", "BPM (60 - 300):", "BPM: 120", 85);

        // Row 2: Beats Per Measure
        var (beatsMinusBtn, beatsPlusBtn, beatsValText) = CreateStepperRow(configCardObj.transform, "BeatsRow", "Beats / Measure (2 - 8):", "Beats: 4 / measure", 35);

        // Row 3: Measures Per Round
        var (measuresMinusBtn, measuresPlusBtn, measuresValText) = CreateStepperRow(configCardObj.transform, "MeasuresRow", "Measures / Round (4 - 16):", "Measures: 8 pairs (16 measures)", -15);

        // Row 4: Total Rounds
        var (roundsMinusBtn, roundsPlusBtn, roundsValText) = CreateStepperRow(configCardObj.transform, "RoundsRow", "Total Rounds (1 - 8):", "Rounds: 2", -65);

        // Row 5: Quantization Snap
        var (snapCycleBtn, snapValText) = CreateCycleRow(configCardObj.transform, "SnapRow", "Grid Snap (Subdivision):", "Snap: 16th Note (1/4 beat)", -115);

        // Menu buttons container below config card
        var menuObj = CreateUIObject("MenuContainer", rootPanel.transform);
        var menuRect = menuObj.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.36f);
        menuRect.anchorMax = new Vector2(0.5f, 0.36f);
        menuRect.sizeDelta = new Vector2(800, 60);

        var startBtn = CreateStyledButton(menuObj.transform, "StartButton", "START GAME", new Vector2(-260, 0), new Vector2(240, 56), new Color(0.18f, 0.55f, 0.85f), 18);
        var keybindBtn = CreateStyledButton(menuObj.transform, "KeybindButton", "KEYBIND SETTINGS", new Vector2(0, 0), new Vector2(240, 56), new Color(0.22f, 0.28f, 0.36f), 18);
        var quitBtn = CreateStyledButton(menuObj.transform, "QuitButton", "QUIT GAME", new Vector2(260, 0), new Vector2(240, 56), new Color(0.22f, 0.28f, 0.36f), 18);

        // Bottom test bar
        var testBarObj = CreateUIObject("BottomTestBar", rootPanel.transform);
        var testBarRect = testBarObj.GetComponent<RectTransform>();
        testBarRect.anchorMin = new Vector2(0.5f, 0.16f);
        testBarRect.anchorMax = new Vector2(0.5f, 0.16f);
        testBarRect.sizeDelta = new Vector2(960, 110);

        var infoText = CreateText(testBarObj.transform, "BindingInfoText", "Player 1: [Action A]   |   Player 2: [Action B]", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.9f));
        infoText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 32);
        infoText.GetComponent<RectTransform>().sizeDelta = new Vector2(960, 30);

        // Live input indicators
        var indicatorAObj = CreatePanel(testBarObj.transform, "IndicatorA", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indARect = indicatorAObj.GetComponent<RectTransform>();
        indARect.anchoredPosition = new Vector2(-160, -18);
        indARect.sizeDelta = new Vector2(240, 46);
        var indAText = CreateText(indicatorAObj.transform, "TextA", "P1 (Action A)", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(indAText.GetComponent<RectTransform>());

        var indicatorBObj = CreatePanel(testBarObj.transform, "IndicatorB", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indBRect = indicatorBObj.GetComponent<RectTransform>();
        indBRect.anchoredPosition = new Vector2(160, -18);
        indBRect.sizeDelta = new Vector2(240, 46);
        var indBText = CreateText(indicatorBObj.transform, "TextB", "P2 (Action B)", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(indBText.GetComponent<RectTransform>());

        // Attach TitleSceneUI controller
        var controller = canvasGo.AddComponent<TitleSceneUI>();
        var so = new SerializedObject(controller);
        so.FindProperty("_startButton").objectReferenceValue = startBtn.GetComponent<Button>();
        so.FindProperty("_keybindButton").objectReferenceValue = keybindBtn.GetComponent<Button>();
        so.FindProperty("_quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();

        so.FindProperty("_bpmMinusButton").objectReferenceValue = bpmMinusBtn.GetComponent<Button>();
        so.FindProperty("_bpmPlusButton").objectReferenceValue = bpmPlusBtn.GetComponent<Button>();
        so.FindProperty("_bpmValueText").objectReferenceValue = bpmValText;

        so.FindProperty("_beatsMinusButton").objectReferenceValue = beatsMinusBtn.GetComponent<Button>();
        so.FindProperty("_beatsPlusButton").objectReferenceValue = beatsPlusBtn.GetComponent<Button>();
        so.FindProperty("_beatsValueText").objectReferenceValue = beatsValText;

        so.FindProperty("_measuresMinusButton").objectReferenceValue = measuresMinusBtn.GetComponent<Button>();
        so.FindProperty("_measuresPlusButton").objectReferenceValue = measuresPlusBtn.GetComponent<Button>();
        so.FindProperty("_measuresValueText").objectReferenceValue = measuresValText;

        so.FindProperty("_roundsMinusButton").objectReferenceValue = roundsMinusBtn.GetComponent<Button>();
        so.FindProperty("_roundsPlusButton").objectReferenceValue = roundsPlusBtn.GetComponent<Button>();
        so.FindProperty("_roundsValueText").objectReferenceValue = roundsValText;

        so.FindProperty("_snapCycleButton").objectReferenceValue = snapCycleBtn.GetComponent<Button>();
        so.FindProperty("_snapValueText").objectReferenceValue = snapValText;

        so.FindProperty("_indicatorA").objectReferenceValue = indicatorAObj.GetComponent<Image>();
        so.FindProperty("_indicatorTextA").objectReferenceValue = indAText;
        so.FindProperty("_indicatorB").objectReferenceValue = indicatorBObj.GetComponent<Image>();
        so.FindProperty("_indicatorTextB").objectReferenceValue = indBText;
        so.FindProperty("_bindingInfoText").objectReferenceValue = infoText;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static (GameObject minusBtn, GameObject plusBtn, Text valueText) CreateStepperRow(Transform parent, string name, string label, string defaultValue, float yPos)
    {
        var rowObj = CreateUIObject(name, parent);
        var rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(820, 40);

        var labelText = CreateText(rowObj.transform, "Label", label, 18, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        var labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);
        labelRect.sizeDelta = new Vector2(300, 36);

        var minusBtn = CreateStyledButton(rowObj.transform, "MinusButton", "-", new Vector2(160, 0), new Vector2(40, 34), new Color(0.25f, 0.32f, 0.42f), 20);
        var valText = CreateText(rowObj.transform, "ValueText", defaultValue, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.4f, 0.85f, 1f));
        var valRect = valText.GetComponent<RectTransform>();
        valRect.anchoredPosition = new Vector2(270, 0);
        valRect.sizeDelta = new Vector2(170, 36);

        var plusBtn = CreateStyledButton(rowObj.transform, "PlusButton", "+", new Vector2(380, 0), new Vector2(40, 34), new Color(0.25f, 0.32f, 0.42f), 20);

        return (minusBtn, plusBtn, valText);
    }

    private static (GameObject cycleBtn, Text valueText) CreateCycleRow(Transform parent, string name, string label, string defaultValue, float yPos)
    {
        var rowObj = CreateUIObject(name, parent);
        var rowRect = rowObj.GetComponent<RectTransform>();
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(820, 40);

        var labelText = CreateText(rowObj.transform, "Label", label, 18, FontStyle.Normal, TextAnchor.MiddleLeft, Color.white);
        var labelRect = labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0.5f);
        labelRect.anchorMax = new Vector2(0, 0.5f);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(20, 0);
        labelRect.sizeDelta = new Vector2(300, 36);

        var valText = CreateText(rowObj.transform, "ValueText", defaultValue, 18, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1.0f, 0.85f, 0.3f));
        var valRect = valText.GetComponent<RectTransform>();
        valRect.anchoredPosition = new Vector2(220, 0);
        valRect.sizeDelta = new Vector2(230, 36);

        var cycleBtn = CreateStyledButton(rowObj.transform, "CycleButton", "CHANGE", new Vector2(365, 0), new Vector2(90, 34), new Color(0.25f, 0.45f, 0.65f), 14);

        return (cycleBtn, valText);
    }

    private static void BuildGameScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 640x360 Pixel Perfect Camera for pixel art
        SetupCameraAndLight(true);
        SetupEventSystem();

        // 1. Singletons
        var mgrGo = new GameObject("[GameSpriteManager]");
        var spriteMgr = mgrGo.AddComponent<GameSpriteManager>();
        spriteMgr.LoadEditorAssets();

        var audioGo = new GameObject("[AudioManager]");
        var audioMgr = audioGo.AddComponent<AudioManager>();
        audioMgr.EnsureMixerLoaded();

        // 2. Rhythm Conveyor
        var conveyorGo = new GameObject("RhythmConveyor");
        conveyorGo.transform.position = Vector3.zero;
        var conveyor = conveyorGo.AddComponent<RhythmConveyor>();

        // Hammer Child
        var hammerGo = new GameObject("Hammer");
        hammerGo.transform.SetParent(conveyorGo.transform, false);
        hammerGo.transform.localPosition = new Vector3(GameConfig.StrikingLineX + 8f, 212f, 0);
        var hammerSr = hammerGo.AddComponent<SpriteRenderer>();
        hammerSr.sortingOrder = 4;
        var hammerCtrl = hammerGo.AddComponent<HammerController>();

        // Wire Hammer on Conveyor
        var conveyorSo = new SerializedObject(conveyor);
        conveyorSo.FindProperty("_hammer").objectReferenceValue = hammerCtrl;
        conveyorSo.ApplyModifiedPropertiesWithoutUndo();

        // 3. RhythmGameManager
        var gmGo = new GameObject("RhythmGameManager");
        var gm = gmGo.AddComponent<RhythmGameManager>();

        // 4. UI Canvas (640x360 reference resolution matching the Pixel Perfect Camera grid)
        var (canvasGo, canvasScaler) = SetupCanvas("GameCanvas", new Vector2(640, 360));

        // Top HUD Panel
        var topHudObj = CreatePanel(canvasGo.transform, "TopHUD", new Color(0.12f, 0.15f, 0.20f, 0.92f));
        var topHudRect = topHudObj.GetComponent<RectTransform>();
        topHudRect.anchorMin = new Vector2(0.5f, 1f);
        topHudRect.anchorMax = new Vector2(0.5f, 1f);
        topHudRect.pivot = new Vector2(0.5f, 1f);
        topHudRect.anchoredPosition = new Vector2(0, -6);
        topHudRect.sizeDelta = new Vector2(620, 64);

        var roundText = CreateText(topHudObj.transform, "RoundText", "ROUND 1 / 2", 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        roundText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var roundRect = roundText.GetComponent<RectTransform>();
        roundRect.anchoredPosition = new Vector2(0, 18);
        roundRect.sizeDelta = new Vector2(300, 20);

        var rolesBannerText = CreateText(topHudObj.transform, "RolesBanner", "P1 [Action A]: ATTACKER   |   P2 [Action B]: DEFENDER", 11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.4f, 0.85f, 1f));
        rolesBannerText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rolesRect = rolesBannerText.GetComponent<RectTransform>();
        rolesRect.anchoredPosition = new Vector2(0, 2);
        rolesRect.sizeDelta = new Vector2(420, 18);

        var phaseText = CreateText(topHudObj.transform, "PhaseText", "PREPARING... (Listen to the rhythm!)", 11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.2f));
        phaseText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var phaseRect = phaseText.GetComponent<RectTransform>();
        phaseRect.anchoredPosition = new Vector2(0, -14);
        phaseRect.sizeDelta = new Vector2(440, 18);

        var scoreP1Text = CreateText(topHudObj.transform, "ScoreP1Text", "P1 (Left): 0 pts", 12, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.2f, 0.8f, 1f));
        scoreP1Text.horizontalOverflow = HorizontalWrapMode.Overflow;
        var p1Rect = scoreP1Text.GetComponent<RectTransform>();
        p1Rect.anchorMin = new Vector2(0f, 0.5f);
        p1Rect.anchorMax = new Vector2(0f, 0.5f);
        p1Rect.pivot = new Vector2(0f, 0.5f);
        p1Rect.anchoredPosition = new Vector2(12, 0);
        p1Rect.sizeDelta = new Vector2(180, 24);

        var scoreP2Text = CreateText(topHudObj.transform, "ScoreP2Text", "P2 (Right): 0 pts", 12, FontStyle.Bold, TextAnchor.MiddleRight, new Color(1f, 0.75f, 0.2f));
        scoreP2Text.horizontalOverflow = HorizontalWrapMode.Overflow;
        var p2Rect = scoreP2Text.GetComponent<RectTransform>();
        p2Rect.anchorMin = new Vector2(1f, 0.5f);
        p2Rect.anchorMax = new Vector2(1f, 0.5f);
        p2Rect.pivot = new Vector2(1f, 0.5f);
        p2Rect.anchoredPosition = new Vector2(-12, 0);
        p2Rect.sizeDelta = new Vector2(180, 24);

        // Center Judgment & Settlement Popups
        var centerFeedbackObj = CreateUIObject("CenterFeedback", canvasGo.transform);
        var cfRect = centerFeedbackObj.GetComponent<RectTransform>();
        cfRect.anchoredPosition = new Vector2(0, 35);
        cfRect.sizeDelta = new Vector2(500, 60);

        var judgmentText = CreateText(centerFeedbackObj.transform, "JudgmentText", "", 22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        judgmentText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 12);
        judgmentText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 30);

        var settlementText = CreateText(centerFeedbackObj.transform, "SettlementText", "", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.3f, 1f, 0.5f));
        settlementText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -14);
        settlementText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 24);

        // Bottom Controls & Input Indicators
        var bottomContainer = CreateUIObject("BottomControls", canvasGo.transform);
        var bottomRect = bottomContainer.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0.5f, 0f);
        bottomRect.anchorMax = new Vector2(0.5f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.anchoredPosition = new Vector2(0, 8);
        bottomRect.sizeDelta = new Vector2(620, 44);

        var indicatorP1Obj = CreatePanel(bottomContainer.transform, "IndicatorP1", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indP1Rect = indicatorP1Obj.GetComponent<RectTransform>();
        indP1Rect.anchoredPosition = new Vector2(-195, 0);
        indP1Rect.sizeDelta = new Vector2(145, 36);
        var indP1Text = CreateText(indicatorP1Obj.transform, "TextP1", "P1: Action A", 11, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(indP1Text.GetComponent<RectTransform>());

        var restartBtn = CreateStyledButton(bottomContainer.transform, "RestartButton", "RESTART", new Vector2(-55, 0), new Vector2(85, 30), new Color(0.25f, 0.45f, 0.65f), 11);
        var titleBtn = CreateStyledButton(bottomContainer.transform, "TitleButton", "TITLE MENU", new Vector2(55, 0), new Vector2(85, 30), new Color(0.25f, 0.32f, 0.42f), 11);

        var indicatorP2Obj = CreatePanel(bottomContainer.transform, "IndicatorP2", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indP2Rect = indicatorP2Obj.GetComponent<RectTransform>();
        indP2Rect.anchoredPosition = new Vector2(195, 0);
        indP2Rect.sizeDelta = new Vector2(145, 36);
        var indP2Text = CreateText(indicatorP2Obj.transform, "TextP2", "P2: Action B", 11, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(indP2Text.GetComponent<RectTransform>());

        // Game Over Modal Dialog
        var gameOverModal = CreatePanel(canvasGo.transform, "GameOverModal", new Color(0.08f, 0.10f, 0.14f, 0.96f));
        var modalRect = gameOverModal.GetComponent<RectTransform>();
        modalRect.anchorMin = new Vector2(0.5f, 0.5f);
        modalRect.anchorMax = new Vector2(0.5f, 0.5f);
        modalRect.sizeDelta = new Vector2(460, 280);

        var winnerTitle = CreateText(gameOverModal.transform, "WinnerTitle", "PLAYER 1 WINS!", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        winnerTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 105);
        winnerTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 34);

        var finalScoreP1 = CreateText(gameOverModal.transform, "FinalScoreP1", "Player 1 Final Score: 0 pts", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.2f, 0.8f, 1f));
        finalScoreP1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 68);
        finalScoreP1.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 24);

        var statsP1 = CreateText(gameOverModal.transform, "StatsP1", "P1 Stats:\nPerfect: 0  |  Good: 0  |  Bad: 0  |  Miss: 0", 11, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        statsP1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 42);
        statsP1.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 22);

        var finalScoreP2 = CreateText(gameOverModal.transform, "FinalScoreP2", "Player 2 Final Score: 0 pts", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.2f));
        finalScoreP2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 2);
        finalScoreP2.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 24);

        var statsP2 = CreateText(gameOverModal.transform, "StatsP2", "P2 Stats:\nPerfect: 0  |  Good: 0  |  Bad: 0  |  Miss: 0", 11, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
        statsP2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -24);
        statsP2.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 22);

        var modalPlayAgainBtn = CreateStyledButton(gameOverModal.transform, "PlayAgainButton", "PLAY AGAIN", new Vector2(-80, -95), new Vector2(130, 36), new Color(0.18f, 0.55f, 0.85f), 12);
        var modalMainMenuBtn = CreateStyledButton(gameOverModal.transform, "MainMenuButton", "MAIN MENU", new Vector2(80, -95), new Vector2(130, 36), new Color(0.25f, 0.32f, 0.42f), 12);

        gameOverModal.SetActive(false);

        // Wire GameSceneUI controller
        var uiController = canvasGo.AddComponent<GameSceneUI>();
        var uiSo = new SerializedObject(uiController);
        uiSo.FindProperty("_roundText").objectReferenceValue = roundText;
        uiSo.FindProperty("_phaseText").objectReferenceValue = phaseText;
        uiSo.FindProperty("_scoreP1Text").objectReferenceValue = scoreP1Text;
        uiSo.FindProperty("_scoreP2Text").objectReferenceValue = scoreP2Text;
        uiSo.FindProperty("_rolesBannerText").objectReferenceValue = rolesBannerText;

        uiSo.FindProperty("_indicatorP1").objectReferenceValue = indicatorP1Obj.GetComponent<Image>();
        uiSo.FindProperty("_indicatorP1Text").objectReferenceValue = indP1Text;
        uiSo.FindProperty("_indicatorP2").objectReferenceValue = indicatorP2Obj.GetComponent<Image>();
        uiSo.FindProperty("_indicatorP2Text").objectReferenceValue = indP2Text;

        uiSo.FindProperty("_judgmentText").objectReferenceValue = judgmentText;
        uiSo.FindProperty("_settlementText").objectReferenceValue = settlementText;

        uiSo.FindProperty("_titleButton").objectReferenceValue = titleBtn.GetComponent<Button>();
        uiSo.FindProperty("_restartButton").objectReferenceValue = restartBtn.GetComponent<Button>();

        uiSo.FindProperty("_gameOverPanel").objectReferenceValue = gameOverModal;
        uiSo.FindProperty("_winnerTitleText").objectReferenceValue = winnerTitle;
        uiSo.FindProperty("_finalScoreP1Text").objectReferenceValue = finalScoreP1;
        uiSo.FindProperty("_finalScoreP2Text").objectReferenceValue = finalScoreP2;
        uiSo.FindProperty("_statsP1Text").objectReferenceValue = statsP1;
        uiSo.FindProperty("_statsP2Text").objectReferenceValue = statsP2;
        uiSo.FindProperty("_modalPlayAgainButton").objectReferenceValue = modalPlayAgainBtn.GetComponent<Button>();
        uiSo.FindProperty("_modalMainMenuButton").objectReferenceValue = modalMainMenuBtn.GetComponent<Button>();
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        // Wire RhythmGameManager
        var gmSo = new SerializedObject(gm);
        gmSo.FindProperty("_conveyor").objectReferenceValue = conveyor;
        gmSo.FindProperty("_ui").objectReferenceValue = uiController;
        gmSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static void BuildKeybindScene(string scenePath)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupCameraAndLight(false);
        SetupEventSystem();

        var (canvasGo, canvasScaler) = SetupCanvas("KeybindCanvas", new Vector2(1920, 1080));

        var rootPanel = CreatePanel(canvasGo.transform, "BackgroundPanel", new Color(0.10f, 0.12f, 0.15f, 1f));
        SetStretch(rootPanel.GetComponent<RectTransform>());

        // Header
        var headerObj = CreateUIObject("Header", rootPanel.transform);
        var headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0.5f, 0.85f);
        headerRect.anchorMax = new Vector2(0.5f, 0.85f);
        headerRect.sizeDelta = new Vector2(960, 120);

        var titleText = CreateText(headerObj.transform, "TitleText", "KEYBIND SETTINGS", 44, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;
        var titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 20);
        titleRect.sizeDelta = new Vector2(960, 60);

        var descText = CreateText(headerObj.transform, "DescText", "Default layout: 6, Y, G, B and all keys to their left allocated to Player 1 (Action A), right to Player 2 (Action B).\nClick rebind to customize any key.", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.85f));
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var descRect = descText.GetComponent<RectTransform>();
        descRect.anchoredPosition = new Vector2(0, -25);
        descRect.sizeDelta = new Vector2(960, 50);

        // Center settings card
        var cardObj = CreatePanel(rootPanel.transform, "SettingsCard", new Color(0.14f, 0.17f, 0.22f, 1f));
        var cardRect = cardObj.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.48f);
        cardRect.anchorMax = new Vector2(0.5f, 0.48f);
        cardRect.sizeDelta = new Vector2(880, 380);

        // Row A
        var rowAObj = CreateUIObject("RowA", cardObj.transform);
        var rowARect = rowAObj.GetComponent<RectTransform>();
        rowARect.anchoredPosition = new Vector2(0, 120);
        rowARect.sizeDelta = new Vector2(820, 60);

        var labelA = CreateText(rowAObj.transform, "LabelA", "Player 1 (Action A):", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        labelA.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 0);
        labelA.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);

        var displayA = CreateText(rowAObj.transform, "DisplayA", "Default (Left Keys)", 20, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.2f, 0.8f, 1.0f));
        displayA.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 0);
        displayA.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 50);

        var rebindABtn = CreateStyledButton(rowAObj.transform, "RebindAButton", "REBIND", new Vector2(330, 0), new Vector2(120, 44), new Color(0.2f, 0.6f, 0.9f), 16);

        // Row B
        var rowBObj = CreateUIObject("RowB", cardObj.transform);
        var rowBRect = rowBObj.GetComponent<RectTransform>();
        rowBRect.anchoredPosition = new Vector2(0, 40);
        rowBRect.sizeDelta = new Vector2(820, 60);

        var labelB = CreateText(rowBObj.transform, "LabelB", "Player 2 (Action B):", 22, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        labelB.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 0);
        labelB.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 50);

        var displayB = CreateText(rowBObj.transform, "DisplayB", "Default (Right Keys)", 20, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1.0f, 0.75f, 0.2f));
        displayB.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, 0);
        displayB.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 50);

        var rebindBBtn = CreateStyledButton(rowBObj.transform, "RebindBButton", "REBIND", new Vector2(330, 0), new Vector2(120, 44), new Color(0.2f, 0.6f, 0.9f), 16);

        // Prompt text
        var promptText = CreateText(cardObj.transform, "PromptText", "", 20, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        promptText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
        promptText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 40);

        // Card bottom buttons
        var resetBtn = CreateStyledButton(cardObj.transform, "ResetButton", "RESET DEFAULTS", new Vector2(-120, -120), new Vector2(200, 48), new Color(0.35f, 0.25f, 0.25f), 16);
        var backBtn = CreateStyledButton(cardObj.transform, "BackButton", "BACK TO TITLE", new Vector2(120, -120), new Vector2(200, 48), new Color(0.22f, 0.45f, 0.65f), 16);

        // Bottom test section
        var testObj = CreateUIObject("TestSection", rootPanel.transform);
        var testRect = testObj.GetComponent<RectTransform>();
        testRect.anchorMin = new Vector2(0.5f, 0.12f);
        testRect.anchorMax = new Vector2(0.5f, 0.12f);
        testRect.sizeDelta = new Vector2(960, 100);

        var testTitle = CreateText(testObj.transform, "TestTitle", "Test Your Controls Live:", 18, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.7f, 0.75f, 0.85f));
        testTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 32);

        var testIndA = CreatePanel(testObj.transform, "TestIndicatorA", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indARect = testIndA.GetComponent<RectTransform>();
        indARect.anchoredPosition = new Vector2(-160, -18);
        indARect.sizeDelta = new Vector2(240, 46);
        var testTextA = CreateText(testIndA.transform, "TextA", "Player 1", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(testTextA.GetComponent<RectTransform>());

        var testIndB = CreatePanel(testObj.transform, "TestIndicatorB", new Color(0.18f, 0.22f, 0.28f, 1f));
        var indBRect = testIndB.GetComponent<RectTransform>();
        indBRect.anchoredPosition = new Vector2(160, -18);
        indBRect.sizeDelta = new Vector2(240, 46);
        var testTextB = CreateText(testIndB.transform, "TextB", "Player 2", 18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(testTextB.GetComponent<RectTransform>());

        // Attach controller
        var controller = canvasGo.AddComponent<KeybindSettingUI>();
        var so = new SerializedObject(controller);
        so.FindProperty("_actionADisplayText").objectReferenceValue = displayA;
        so.FindProperty("_actionBDisplayText").objectReferenceValue = displayB;
        so.FindProperty("_promptText").objectReferenceValue = promptText;
        so.FindProperty("_rebindAButton").objectReferenceValue = rebindABtn.GetComponent<Button>();
        so.FindProperty("_rebindBButton").objectReferenceValue = rebindBBtn.GetComponent<Button>();
        so.FindProperty("_resetDefaultButton").objectReferenceValue = resetBtn.GetComponent<Button>();
        so.FindProperty("_backButton").objectReferenceValue = backBtn.GetComponent<Button>();
        so.FindProperty("_testIndicatorA").objectReferenceValue = testIndA.GetComponent<Image>();
        so.FindProperty("_testIndicatorTextA").objectReferenceValue = testTextA;
        so.FindProperty("_testIndicatorB").objectReferenceValue = testIndB.GetComponent<Image>();
        so.FindProperty("_testIndicatorTextB").objectReferenceValue = testTextB;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, scenePath);
    }

    private static GameObject SetupCameraAndLight(bool is640x360PixelPerfect)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = is640x360PixelPerfect
            ? new Vector3(320f, 180f, -10f)
            : new Vector3(0f, 0f, -10f);

        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.10f, 0.14f, 1f);
        cam.orthographic = true;
        cam.orthographicSize = is640x360PixelPerfect ? 180f : 5f;
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        if (is640x360PixelPerfect)
        {
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.refResolutionX = 640;
            ppc.refResolutionY = 360;
            ppc.assetsPPU = 1;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
        }

        var lightGo = new GameObject("Global Light 2D");
        var light = lightGo.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1.0f;

        return camGo;
    }

    private static void SetupEventSystem()
    {
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();
    }

    private static (GameObject canvasGo, CanvasScaler scaler) SetupCanvas(string name, Vector2 referenceResolution)
    {
        var canvasGo = new GameObject(name);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        return (canvasGo, scaler);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.sprite = _panelSprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        return go;
    }

    private static Text CreateText(Transform parent, string name, string content, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        var go = CreateUIObject(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600, 40);

        var text = go.AddComponent<Text>();
        text.font = _font;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static GameObject CreateStyledButton(Transform parent, string name, string label, Vector2 position, Vector2 size, Color baseColor, int fontSize = 20)
    {
        var go = CreateUIObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Sliced;
        img.color = baseColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        var text = CreateText(go.transform, "Text", label, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetStretch(text.GetComponent<RectTransform>());

        return go;
    }

    private static void SetStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
