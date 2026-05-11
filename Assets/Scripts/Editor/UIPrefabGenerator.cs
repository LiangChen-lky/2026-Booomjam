#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIPrefabGenerator
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string SettingsPanelPath = PrefabFolder + "/SettingsPanel.prefab";
    private const string MainMenuPath = PrefabFolder + "/MainMenu.prefab";
    private const string PauseMenuPath = PrefabFolder + "/PauseMenu.prefab";
    private const string VictoryMenuPath = PrefabFolder + "/VictoryMenu.prefab";
    private const string FailureMenuPath = PrefabFolder + "/FailureMenu.prefab";

    [MenuItem("Tools/Generate UI Prefabs")]
    public static void GenerateAllPrefabs()
    {
        EnsureFolder();

        var settingsPanelPrefab = CreateSettingsPanelPrefab();
        CreateMainMenuPrefab(settingsPanelPrefab);
        CreatePauseMenuPrefab(settingsPanelPrefab);
        CreateVictoryMenuPrefab();
        CreateFailureMenuPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("UI prefabs generated.");
        EditorUtility.DisplayDialog("完成", "UI prefabs have been generated.", "确定");
    }

    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(PrefabFolder))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    private static GameObject CreateSettingsPanelPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPanelPath);
        if (existing != null)
            return existing;

        var root = new GameObject("SettingsPanel");
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.sprite = null;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = false;

        var settingsMenu = root.AddComponent<SettingsMenu>();

        var panel = new GameObject("Panel");
        panel.transform.SetParent(root.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(960f, 760f);

        var panelBg = panel.AddComponent<Image>();
        panelBg.sprite = LoadSprite("Assets/Sprites/ui/ui_setup_bg.jpg");
        panelBg.type = Image.Type.Simple;
        panelBg.preserveAspect = false;
        panelBg.color = Color.white;

        var title = CreateText(panel.transform, "Title", "设置", 40, Color.white, new Vector2(0, 320), new Vector2(400, 60));
        title.alignment = TextAnchor.MiddleCenter;
        title.fontStyle = FontStyle.Bold;

        var volumeTitle = CreateText(panel.transform, "VolumeTitle", "音量", 28, new Color(0.8f, 0.1f, 0.1f, 1f), new Vector2(0, 250), new Vector2(400, 35));
        volumeTitle.alignment = TextAnchor.MiddleCenter;

        var master = CreateSliderRow(panel.transform, "MasterVolumeSlider", "主音量", 190);
        var bgm = CreateSliderRow(panel.transform, "BGMVolumeSlider", "BGM", 130);
        var sfx = CreateSliderRow(panel.transform, "SFXVolumeSlider", "SFX", 70);

        var displayTitle = CreateText(panel.transform, "DisplayTitle", "显示", 28, new Color(0.8f, 0.1f, 0.1f, 1f), new Vector2(0, -10), new Vector2(400, 35));
        displayTitle.alignment = TextAnchor.MiddleCenter;

        var fullscreen = CreateToggleRow(panel.transform, "FullscreenToggle", "全屏", -70);
        var resolution = CreateDropdownRow(panel.transform, "ResolutionDropdown", "分辨率", -130);

        var controlTitle = CreateText(panel.transform, "ControlTitle", "控制", 28, new Color(0.8f, 0.1f, 0.1f, 1f), new Vector2(0, -210), new Vector2(400, 35));
        controlTitle.alignment = TextAnchor.MiddleCenter;

        var sensitivity = CreateSliderRow(panel.transform, "MouseSensitivitySlider", "鼠标灵敏度", -270, 0.1f, 2f, 1f);
        var sensitivityValue = CreateText(panel.transform, "SensitivityValue", "1.0", 20, Color.white, new Vector2(220, -270), new Vector2(60, 30));
        sensitivityValue.alignment = TextAnchor.MiddleCenter;

        var apply = CreateButton(panel.transform, "ApplyButton", "应用", new Vector2(-100, -350), new Vector2(160, 45));
        var back = CreateButton(panel.transform, "BackButton", "返回", new Vector2(100, -350), new Vector2(160, 45));

        var so = new SerializedObject(settingsMenu);
        so.FindProperty("masterSlider").objectReferenceValue = master;
        so.FindProperty("bgmSlider").objectReferenceValue = bgm;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("sensitivitySlider").objectReferenceValue = sensitivity;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        so.FindProperty("resolutionDropdown").objectReferenceValue = resolution;
        so.FindProperty("sensitivityValueText").objectReferenceValue = sensitivityValue;
        so.FindProperty("applyButton").objectReferenceValue = apply;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, SettingsPanelPath);
    }

    private static void CreateMainMenuPrefab(GameObject settingsPanelPrefab)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPath);
        if (existing != null)
            return;

        var root = new GameObject("MainMenu");
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        root.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var bg = root.AddComponent<Image>();
        bg.sprite = LoadSprite("Assets/Sprites/ui/ui_bg1.png");
        bg.color = Color.white;
        bg.type = Image.Type.Simple;
        bg.preserveAspect = false;

        var title = CreateImage(root.transform, "Title", LoadSprite("Assets/Sprites/ui/ui_start_说明.png"), new Vector2(650, 180), new Vector2(0, -180));

        var buttons = new GameObject("Buttons");
        buttons.transform.SetParent(root.transform, false);
        var layout = buttons.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var btnRect = buttons.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, -90);
        btnRect.sizeDelta = new Vector2(280, 250);

        CreateImageButton(buttons.transform, "StartButton", LoadSprite("Assets/Sprites/ui/ui_start_start.png"));
        CreateImageButton(buttons.transform, "SettingsButton", LoadSprite("Assets/Sprites/ui/ui_start_setup.png"));
        CreateImageButton(buttons.transform, "ExitButton", LoadSprite("Assets/Sprites/ui/ui_start_quit.png"));

        var menuUI = root.AddComponent<MainMenuUI>();
        var so = new SerializedObject(menuUI);
        so.FindProperty("startButton").objectReferenceValue = buttons.transform.Find("StartButton").GetComponent<Button>();
        so.FindProperty("settingsButton").objectReferenceValue = buttons.transform.Find("SettingsButton").GetComponent<Button>();
        so.FindProperty("exitButton").objectReferenceValue = buttons.transform.Find("ExitButton").GetComponent<Button>();
        so.FindProperty("settingsPanelPrefab").objectReferenceValue = settingsPanelPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, MainMenuPath);
    }

    private static void CreatePauseMenuPrefab(GameObject settingsPanelPrefab)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PauseMenuPath);
        if (existing != null)
            return;

        var root = new GameObject("PauseMenu");
        root.transform.localScale = Vector3.one;
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        var pauseCanvas = new GameObject("PauseCanvas");
        pauseCanvas.transform.SetParent(root.transform, false);
        var pauseRect = pauseCanvas.AddComponent<RectTransform>();
        pauseRect.anchorMin = Vector2.zero;
        pauseRect.anchorMax = Vector2.one;
        pauseRect.offsetMin = Vector2.zero;
        pauseRect.offsetMax = Vector2.zero;

        var menuRoot = new GameObject("PauseMenuRoot");
        menuRoot.transform.SetParent(pauseCanvas.transform, false);
        var menuRect = menuRoot.AddComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        var bg = menuRoot.AddComponent<Image>();
        bg.sprite = LoadSprite("Assets/Sprites/ui/ui_pause_bg.png");
        bg.type = Image.Type.Simple;
        bg.preserveAspect = false;
        bg.color = new Color(1f, 1f, 1f, 0.85f);

        var title = CreateText(menuRoot.transform, "Title", "游戏已暂停", 48, Color.white, new Vector2(0, 120), new Vector2(400, 60));
        title.alignment = TextAnchor.MiddleCenter;

        var buttons = new GameObject("Buttons");
        buttons.transform.SetParent(menuRoot.transform, false);
        var layout = buttons.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var btnRect = buttons.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0.5f);
        btnRect.anchorMax = new Vector2(0.5f, 0.5f);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.anchoredPosition = new Vector2(0, 10);
        btnRect.sizeDelta = new Vector2(320, 240);

        CreatePauseButton(buttons.transform, "ResumeButton", "继续游戏", LoadSprite("Assets/Sprites/ui/ui_pause_start.png"));
        CreatePauseButton(buttons.transform, "SettingsButton", "设置", LoadSprite("Assets/Sprites/ui/ui_pause_setup.png"));
        CreatePauseButton(buttons.transform, "QuitButton", "返回主菜单", LoadSprite("Assets/Sprites/ui/ui_pause_home.png"));

        var pauseMenu = root.AddComponent<PauseMenu>();
        var so = new SerializedObject(pauseMenu);
        so.FindProperty("settingsPanelPrefab").objectReferenceValue = settingsPanelPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, PauseMenuPath);
    }

    private static void CreateVictoryMenuPrefab()
    {
        CreateGameEndMenuPrefab(
            VictoryMenuPath,
            "VictoryMenu",
            "闯关成功",
            "重新挑战",
            new Color(0.1f, 0.55f, 0.45f, 0.9f));
    }

    private static void CreateFailureMenuPrefab()
    {
        CreateGameEndMenuPrefab(
            FailureMenuPath,
            "FailureMenu",
            "挑战失败",
            "重新开始",
            new Color(0.55f, 0.1f, 0.1f, 0.9f));
    }

    private static void CreateGameEndMenuPrefab(string path, string rootName, string title, string retryLabel, Color titleColor)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return;

        var root = new GameObject(rootName);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        root.AddComponent<GraphicRaycaster>();

        var menuRoot = new GameObject("GameEndMenuRoot");
        menuRoot.transform.SetParent(root.transform, false);
        var menuRect = menuRoot.AddComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;

        var bg = menuRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.78f);

        var titleText = CreateText(menuRoot.transform, "Title", title, 52, titleColor, new Vector2(0f, 120f), new Vector2(600f, 80f));
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;

        CreateButton(menuRoot.transform, "RetryButton", retryLabel, new Vector2(0f, -20f), new Vector2(260f, 54f));
        CreateButton(menuRoot.transform, "MainMenuButton", "返回主菜单", new Vector2(0f, -90f), new Vector2(260f, 54f));

        root.AddComponent<GameEndMenuUI>();
        SavePrefab(root, path);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Text CreateText(Transform parent, string name, string text, int fontSize, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var txt = go.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return txt;
    }

    private static Image CreateImage(Transform parent, string name, Sprite sprite, Vector2 sizeDelta, Vector2 anchoredPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return image;
    }

    private static Button CreateImageButton(Transform parent, string name, Sprite sprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        button.colors = colors;

        var rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240, 60);

        return button;
    }

    private static Button CreatePauseButton(Transform parent, string name, string label, Sprite sprite)
    {
        var button = CreateImageButton(parent, name, sprite);
        var buttonTransform = button.transform;

        var bgObj = new GameObject("VisibleBackground");
        bgObj.transform.SetParent(buttonTransform, false);
        bgObj.transform.SetAsFirstSibling();
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.85f);
        bgImage.raycastTarget = false;
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(buttonTransform, false);
        var labelText = labelObj.AddComponent<Text>();
        labelText.text = label;
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 24;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private static Button CreateButton(Transform parent, string name, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.05f, 0.05f, 1f);
        button.colors = colors;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(go.transform, false);
        var txt = txtObj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.raycastTarget = false;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        return button;
    }

    private static Slider CreateSliderRow(Transform parent, string name, string label, float y)
    {
        return CreateSliderRow(parent, name, label, y, 0f, 1f, 1f);
    }

    private static Slider CreateSliderRow(Transform parent, string name, string label, float y, float min, float max, float value)
    {
        var row = new GameObject(name + "_Row");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0, y);
        rowRect.sizeDelta = new Vector2(520, 40);

        var labelText = CreateText(row.transform, name + "_Label", label, 22, Color.white, new Vector2(-220, 0), new Vector2(160, 30));
        labelText.alignment = TextAnchor.MiddleLeft;

        var sliderGo = new GameObject(name);
        sliderGo.transform.SetParent(row.transform, false);
        var sliderRect = sliderGo.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(60, 0);
        sliderRect.sizeDelta = new Vector2(360, 30);

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderGo.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderGo.transform, false);
        var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0);
        fillAreaRect.anchorMax = new Vector2(1, 1);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-15, 0);

        var fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        var fillImage = fillObj.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        var fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        var handleAreaObj = new GameObject("Handle Slide Area");
        handleAreaObj.transform.SetParent(sliderGo.transform, false);
        var handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        var handleObj = new GameObject("Handle");
        handleObj.transform.SetParent(handleAreaObj.transform, false);
        var handleImage = handleObj.AddComponent<Image>();
        handleImage.color = Color.white;
        var handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);

        var slider = sliderGo.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static Toggle CreateToggleRow(Transform parent, string name, string label, float y)
    {
        var row = new GameObject(name + "_Row");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0, y);
        rowRect.sizeDelta = new Vector2(520, 40);

        var labelText = CreateText(row.transform, name + "_Label", label, 22, Color.white, new Vector2(-220, 0), new Vector2(160, 30));
        labelText.alignment = TextAnchor.MiddleLeft;

        var toggleGo = new GameObject(name);
        toggleGo.transform.SetParent(row.transform, false);
        var toggleRect = toggleGo.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleRect.pivot = new Vector2(0.5f, 0.5f);
        toggleRect.anchoredPosition = new Vector2(60, 0);
        toggleRect.sizeDelta = new Vector2(30, 30);

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleGo.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkImage = checkObj.AddComponent<Image>();
        checkImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        var checkRect = checkObj.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.1f, 0.1f);
        checkRect.anchorMax = new Vector2(0.9f, 0.9f);
        checkRect.sizeDelta = Vector2.zero;

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = true;
        return toggle;
    }

    private static Dropdown CreateDropdownRow(Transform parent, string name, string label, float y)
    {
        var row = new GameObject(name + "_Row");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0.5f);
        rowRect.anchorMax = new Vector2(0.5f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.anchoredPosition = new Vector2(0, y);
        rowRect.sizeDelta = new Vector2(520, 40);

        var labelText = CreateText(row.transform, name + "_Label", label, 22, Color.white, new Vector2(-220, 0), new Vector2(160, 30));
        labelText.alignment = TextAnchor.MiddleLeft;

        var dropdownGo = new GameObject(name);
        dropdownGo.transform.SetParent(row.transform, false);
        var dropdownRect = dropdownGo.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 0.5f);
        dropdownRect.anchorMax = new Vector2(0.5f, 0.5f);
        dropdownRect.pivot = new Vector2(0.5f, 0.5f);
        dropdownRect.anchoredPosition = new Vector2(60, 0);
        dropdownRect.sizeDelta = new Vector2(250, 35);

        var bgImage = dropdownGo.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var captionObj = new GameObject("Caption");
        captionObj.transform.SetParent(dropdownGo.transform, false);
        var captionText = captionObj.AddComponent<Text>();
        captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        captionText.fontSize = 18;
        captionText.color = Color.white;
        captionText.alignment = TextAnchor.MiddleLeft;
        var captionRect = captionObj.GetComponent<RectTransform>();
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = new Vector2(10, 0);
        captionRect.offsetMax = new Vector2(-30, 0);

        var arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownGo.transform, false);
        var arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = Color.white;
        var arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);

        var templateRect = CreateDropdownTemplate(dropdownGo.transform);

        var dropdown = dropdownGo.AddComponent<Dropdown>();
        dropdown.targetGraphic = bgImage;
        dropdown.captionText = captionText;
        dropdown.itemText = templateRect.GetComponentInChildren<Text>(true);
        dropdown.template = templateRect;
        dropdown.value = 0;
        dropdown.RefreshShownValue();

        return dropdown;
    }

    private static RectTransform CreateDropdownTemplate(Transform parent)
    {
        var templateObj = new GameObject("Template");
        templateObj.transform.SetParent(parent, false);
        var templateImage = templateObj.AddComponent<Image>();
        templateImage.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        var scrollRect = templateObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        var templateRect = templateObj.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -35f);
        templateRect.sizeDelta = new Vector2(0f, 160f);

        var viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        var viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.05f);
        var viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        var viewportRect = viewportObj.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;

        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 35f);

        var itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        var itemImage = itemObj.AddComponent<Image>();
        itemImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var itemToggle = itemObj.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemImage;
        var itemRect = itemObj.GetComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(1f, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0f, 35f);

        var checkmarkObj = new GameObject("Item Checkmark");
        checkmarkObj.transform.SetParent(itemObj.transform, false);
        var checkmarkImage = checkmarkObj.AddComponent<Image>();
        checkmarkImage.color = new Color(0.8f, 0.1f, 0.1f, 1f);
        itemToggle.graphic = checkmarkImage;
        var checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
        checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
        checkmarkRect.sizeDelta = new Vector2(20f, 20f);
        checkmarkRect.anchoredPosition = new Vector2(15f, 0f);

        var itemLabelObj = new GameObject("Item Label");
        itemLabelObj.transform.SetParent(itemObj.transform, false);
        var itemText = itemLabelObj.AddComponent<Text>();
        itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemText.fontSize = 18;
        itemText.color = Color.white;
        itemText.alignment = TextAnchor.MiddleLeft;
        var itemLabelRect = itemLabelObj.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(35f, 0f);
        itemLabelRect.offsetMax = new Vector2(-10f, 0f);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        templateObj.SetActive(false);

        return templateRect;
    }
}
#endif
