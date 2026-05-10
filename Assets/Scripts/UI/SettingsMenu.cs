using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string MOUSE_SENSITIVITY_KEY = "MouseSensitivity";
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string BGM_VOLUME_KEY = "BGMVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string FULLSCREEN_KEY = "Fullscreen";
    private const string RESOLUTION_INDEX_KEY = "ResolutionIndex";

    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Slider sensitivitySlider;
    private Toggle fullscreenToggle;
    private Dropdown resolutionDropdown;
    private Text sensitivityValueText;
    private UnityAction closeAction;

    private Resolution[] availableResolutions;
    private int selectedResolutionIndex;
    private bool isFullscreen;

    private void Awake()
    {
        LoadSettings();
        CreateUI();
    }

    public void Initialize(UnityAction onClose)
    {
        closeAction = onClose;
    }

    private void OnEnable()
    {
        LoadSettings();
        UpdateUIValues();
    }

    private void CreateUI()
    {
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);

        var bgRect = gameObject.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var titleObj = CreateText(transform, "设置", 40, Color.white, new Vector2(0, 0));
        var titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(400, 60);
        titleRect.anchoredPosition = new Vector2(0, -30);

        float startY = -100f;
        float groupSpacing = 40f;
        float currentY = startY;

        var volumeTitle = CreateSectionTitle(transform, "音量", currentY);
        currentY -= 35f;

        masterSlider = CreateSliderWithLabel(transform, "主音量", currentY, 0f, 1f, AudioManager.Instance.GetMasterVolumeValue());
        masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        currentY -= 50f;

        bgmSlider = CreateSliderWithLabel(transform, "BGM", currentY, 0f, 1f, AudioManager.Instance.GetBGMVolumeValue());
        bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        currentY -= 50f;

        sfxSlider = CreateSliderWithLabel(transform, "SFX", currentY, 0f, 1f, AudioManager.Instance.GetSFXVolumeValue());
        sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        currentY -= 50f + groupSpacing;

        var displayTitle = CreateSectionTitle(transform, "显示", currentY);
        currentY -= 35f;

        fullscreenToggle = CreateToggleWithLabel(transform, "全屏", currentY, Screen.fullScreen);
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        currentY -= 50f;

        availableResolutions = Screen.resolutions;
        var resolutionOptions = new List<string>();
        selectedResolutionIndex = GetSavedOrCurrentResolutionIndex();

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var res = availableResolutions[i];
            resolutionOptions.Add($"{res.width} x {res.height} @ {res.refreshRateRatio}Hz");
        }

        resolutionDropdown = CreateDropdownWithLabel(transform, "分辨率", currentY, resolutionOptions, selectedResolutionIndex);
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        currentY -= 60f + groupSpacing;

        var controlTitle = CreateSectionTitle(transform, "控制", currentY);
        currentY -= 35f;

        sensitivitySlider = CreateSliderWithLabel(transform, "鼠标灵敏度", currentY, 0.1f, 2f, PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1f));
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        var sliderRect = sensitivitySlider.GetComponent<RectTransform>();

        var valueObj = new GameObject("SensitivityValue");
        valueObj.transform.SetParent(transform, false);
        sensitivityValueText = valueObj.AddComponent<Text>();
        sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
        sensitivityValueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sensitivityValueText.fontSize = 20;
        sensitivityValueText.color = Color.white;
        sensitivityValueText.alignment = TextAnchor.MiddleCenter;
        var valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.5f, 0.5f);
        valueRect.anchorMax = new Vector2(0.5f, 0.5f);
        valueRect.anchoredPosition = sliderRect.anchoredPosition + new Vector2(220, 0);
        valueRect.sizeDelta = new Vector2(60, 30);
        currentY -= 80f;

        CreateButton(transform, "应用", new Vector2(-100, -200), OnApply);
        CreateButton(transform, "返回", new Vector2(100, -200), OnBack);
    }

    private GameObject CreateText(Transform parent, string text, int fontSize, Color color, Vector2 position)
    {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        var txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 40);
        rect.anchoredPosition = position;
        return obj;
    }

    private GameObject CreateSectionTitle(Transform parent, string text, float y)
    {
        var obj = CreateText(parent, text, 28, new Color(0.8f, 0.1f, 0.1f, 1f), Vector2.zero);
        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, y);
        rect.sizeDelta = new Vector2(400, 35);
        return obj;
    }

    private Slider CreateSliderWithLabel(Transform parent, string label, float y, float min, float max, float value)
    {
        var sliderObj = new GameObject(label + "Slider");
        sliderObj.transform.SetParent(parent, false);

        var sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 1f);
        sliderRect.anchorMax = new Vector2(0.5f, 1f);
        sliderRect.pivot = new Vector2(0.5f, 1f);
        sliderRect.anchoredPosition = new Vector2(0, y);
        sliderRect.sizeDelta = new Vector2(500, 30);

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        var fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
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
        handleAreaObj.transform.SetParent(sliderObj.transform, false);
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

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        var labelObj = CreateText(parent, label, 22, Color.white, Vector2.zero);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(-280, y);
        labelRect.sizeDelta = new Vector2(120, 30);

        return slider;
    }

    private Toggle CreateToggleWithLabel(Transform parent, string label, float y, bool value)
    {
        var toggleObj = new GameObject(label + "Toggle");
        toggleObj.transform.SetParent(parent, false);

        var toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0.5f, 1f);
        toggleRect.anchorMax = new Vector2(0.5f, 1f);
        toggleRect.pivot = new Vector2(0.5f, 1f);
        toggleRect.anchoredPosition = new Vector2(50, y);
        toggleRect.sizeDelta = new Vector2(30, 30);

        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(toggleObj.transform, false);
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

        var toggle = toggleObj.AddComponent<Toggle>();
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        toggle.isOn = value;

        var labelObj = CreateText(parent, label, 22, Color.white, Vector2.zero);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(-50, y);
        labelRect.sizeDelta = new Vector2(120, 30);

        return toggle;
    }

    private Dropdown CreateDropdownWithLabel(Transform parent, string label, float y, List<string> options, int defaultIndex)
    {
        var dropdownObj = new GameObject(label + "Dropdown");
        dropdownObj.transform.SetParent(parent, false);

        var dropdownRect = dropdownObj.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0.5f, 1f);
        dropdownRect.anchorMax = new Vector2(0.5f, 1f);
        dropdownRect.pivot = new Vector2(0.5f, 1f);
        dropdownRect.anchoredPosition = new Vector2(50, y);
        dropdownRect.sizeDelta = new Vector2(250, 35);

        var bgImage = dropdownObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var captionObj = new GameObject("Caption");
        captionObj.transform.SetParent(dropdownObj.transform, false);
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
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        var arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = Color.white;
        var arrowRect = arrowObj.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);

        var templateRect = CreateDropdownTemplate(dropdownObj.transform);

        var dropdown = dropdownObj.AddComponent<Dropdown>();
        dropdown.targetGraphic = bgImage;
        dropdown.captionText = captionText;
        dropdown.itemText = templateRect.GetComponentInChildren<Text>(true);
        dropdown.template = templateRect;

        foreach (var option in options)
        {
            dropdown.options.Add(new Dropdown.OptionData(option));
        }

        dropdown.value = defaultIndex;
        dropdown.RefreshShownValue();

        var labelObj = CreateText(parent, label, 22, Color.white, Vector2.zero);
        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(-180, y);
        labelRect.sizeDelta = new Vector2(120, 30);

        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent)
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

    private void CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        var btnObj = new GameObject(text + "Button");
        btnObj.transform.SetParent(parent, false);

        var btnImage = btnObj.AddComponent<Image>();
        btnImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        var button = btnObj.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 0.1f, 0.1f, 1f);
        colors.pressedColor = new Color(0.6f, 0.05f, 0.05f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(160, 45);
        rect.anchoredPosition = position;

        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        var txt = txtObj.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 24;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        var txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
    }

    private void UpdateUIValues()
    {
        if (masterSlider != null)
            masterSlider.value = AudioManager.Instance.GetMasterVolumeValue();
        if (bgmSlider != null)
            bgmSlider.value = AudioManager.Instance.GetBGMVolumeValue();
        if (sfxSlider != null)
            sfxSlider.value = AudioManager.Instance.GetSFXVolumeValue();
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1f);
            if (sensitivityValueText != null)
                sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
        }
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;
        if (resolutionDropdown != null)
            resolutionDropdown.value = selectedResolutionIndex;
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance.SetMasterVolume(value);
    }

    private void OnBGMVolumeChanged(float value)
    {
        AudioManager.Instance.SetBGMVolume(value);
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance.SetSFXVolume(value);
    }

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(MOUSE_SENSITIVITY_KEY, value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");
    }

    private void OnFullscreenChanged(bool value)
    {
        isFullscreen = value;
    }

    private void OnResolutionChanged(int index)
    {
        selectedResolutionIndex = index;
    }

    private void OnApply()
    {
        ApplyDisplaySettings(
            resolutionDropdown != null ? resolutionDropdown.value : selectedResolutionIndex,
            fullscreenToggle != null && fullscreenToggle.isOn,
            true);

        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterSlider != null ? masterSlider.value : 1f);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, bgmSlider != null ? bgmSlider.value : 0.7f);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxSlider != null ? sfxSlider.value : 1f);

        PlayerPrefs.Save();
        AudioManager.Instance.Play(SFX.UIClick);
    }

    private void OnBack()
    {
        LoadSettings();
        UpdateUIValues();

        if (closeAction != null)
        {
            closeAction.Invoke();
        }
        else
        {
            var pauseMenu = GetComponentInParent<PauseMenu>();
            if (pauseMenu != null)
            {
                pauseMenu.ReturnFromSettings();
            }
            else
            {
                var mainMenuUI = FindObjectOfType<MainMenuUI>();
                if (mainMenuUI != null)
                {
                    mainMenuUI.ReturnFromSettings();
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        AudioManager.Instance.Play(SFX.UIPopupClose);
    }

    private void LoadSettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
        float bgm = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.7f);
        float sfx = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
        float sensitivity = PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1f);
        bool fullscreen = PlayerPrefs.GetInt(FULLSCREEN_KEY, 1) == 1;
        int resIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, -1);

        AudioManager.Instance.SetMasterVolume(master);
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSFXVolume(sfx);

        if (resIndex >= 0 && resIndex < Screen.resolutions.Length)
        {
            var res = Screen.resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }

    private int GetSavedOrCurrentResolutionIndex()
    {
        int savedIndex = PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY, -1);
        if (savedIndex >= 0 && savedIndex < availableResolutions.Length)
        {
            return savedIndex;
        }

        int bestIndex = 0;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var res = availableResolutions[i];
            if (res.width == Screen.width && res.height == Screen.height)
            {
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void ApplyDisplaySettings(int resolutionIndex, bool fullscreen, bool save)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
        {
            availableResolutions = Screen.resolutions;
        }

        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
        {
            return;
        }

        selectedResolutionIndex = resolutionIndex;
        var res = availableResolutions[resolutionIndex];
        var fullscreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(res.width, res.height, fullscreenMode);

        if (!save)
        {
            return;
        }

        PlayerPrefs.SetInt(FULLSCREEN_KEY, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
        PlayerPrefs.Save();
    }

    public static float GetMouseSensitivity()
    {
        return PlayerPrefs.GetFloat(MOUSE_SENSITIVITY_KEY, 1f);
    }
}
