using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private const string MouseSensitivityKey = "MouseSensitivity";
    private const string MasterVolumeKey = "MasterVolume";
    private const string BgmVolumeKey = "BGMVolume";
    private const string SfxVolumeKey = "SFXVolume";
    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionIndexKey = "ResolutionIndex";

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private Text sensitivityValueText;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;

    private UnityAction closeAction;
    private Resolution[] availableResolutions;
    private int selectedResolutionIndex;
    private bool listenersBound;

    private void Awake()
    {
        CacheReferences();
        BindEvents();
        RefreshResolutionOptions();
        LoadSettings();
        UpdateUIValues();
    }

    private void OnEnable()
    {
        CacheReferences();
        RefreshResolutionOptions();
        LoadSettings();
        UpdateUIValues();
    }

    private void OnDestroy()
    {
        UnbindEvents();
    }

    public void Initialize(UnityAction onClose)
    {
        closeAction = onClose;
    }

    private void CacheReferences()
    {
        if (masterSlider == null) masterSlider = FindChildComponent<Slider>("MasterVolumeSlider");
        if (bgmSlider == null) bgmSlider = FindChildComponent<Slider>("BGMVolumeSlider");
        if (sfxSlider == null) sfxSlider = FindChildComponent<Slider>("SFXVolumeSlider");
        if (sensitivitySlider == null) sensitivitySlider = FindChildComponent<Slider>("MouseSensitivitySlider");
        if (fullscreenToggle == null) fullscreenToggle = FindChildComponent<Toggle>("FullscreenToggle");
        if (resolutionDropdown == null) resolutionDropdown = FindChildComponent<Dropdown>("ResolutionDropdown");
        if (sensitivityValueText == null) sensitivityValueText = FindChildComponent<Text>("SensitivityValue");
        if (applyButton == null) applyButton = FindChildComponent<Button>("ApplyButton");
        if (backButton == null) backButton = FindChildComponent<Button>("BackButton");
    }

    private void BindEvents()
    {
        if (listenersBound) return;

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (applyButton != null) applyButton.onClick.AddListener(OnApply);
        if (backButton != null) backButton.onClick.AddListener(OnBack);

        listenersBound = true;
    }

    private void UnbindEvents()
    {
        if (!listenersBound) return;

        if (masterSlider != null) masterSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (applyButton != null) applyButton.onClick.RemoveListener(OnApply);
        if (backButton != null) backButton.onClick.RemoveListener(OnBack);

        listenersBound = false;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var child = FindChildRecursive(transform, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            var found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void RefreshResolutionOptions()
    {
        availableResolutions = Screen.resolutions;
        if (resolutionDropdown == null || availableResolutions == null || availableResolutions.Length == 0)
            return;

        var options = new List<string>(availableResolutions.Length);
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var res = availableResolutions[i];
            options.Add($"{res.width} x {res.height} @ {res.refreshRateRatio}Hz");
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        selectedResolutionIndex = GetSavedOrCurrentResolutionIndex();
        resolutionDropdown.value = Mathf.Clamp(selectedResolutionIndex, 0, resolutionDropdown.options.Count - 1);
        resolutionDropdown.RefreshShownValue();
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
            sensitivitySlider.value = PlayerPrefs.GetFloat(MouseSensitivityKey, 1f);
            if (sensitivityValueText != null)
                sensitivityValueText.text = sensitivitySlider.value.ToString("F1");
        }
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = Screen.fullScreen;
        if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
            resolutionDropdown.value = Mathf.Clamp(selectedResolutionIndex, 0, resolutionDropdown.options.Count - 1);
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
        PlayerPrefs.SetFloat(MouseSensitivityKey, value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");
    }

    private void OnFullscreenChanged(bool value)
    {
        ApplyDisplaySettings(
            resolutionDropdown != null ? resolutionDropdown.value : selectedResolutionIndex,
            value,
            false);
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

        PlayerPrefs.SetFloat(MasterVolumeKey, masterSlider != null ? masterSlider.value : 1f);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmSlider != null ? bgmSlider.value : 0.7f);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxSlider != null ? sfxSlider.value : 1f);

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
                var mainMenuUI = GetComponentInParent<MainMenuUI>();
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
        var master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        var bgm = PlayerPrefs.GetFloat(BgmVolumeKey, 0.7f);
        var sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        var fullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        var resIndex = PlayerPrefs.GetInt(ResolutionIndexKey, -1);

        AudioManager.Instance.SetMasterVolume(master);
        AudioManager.Instance.SetBGMVolume(bgm);
        AudioManager.Instance.SetSFXVolume(sfx);

        if (resIndex >= 0 && availableResolutions != null && resIndex < availableResolutions.Length)
        {
            var res = availableResolutions[resIndex];
            Screen.SetResolution(res.width, res.height, fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
            selectedResolutionIndex = resIndex;
        }
        else
        {
            Screen.fullScreen = fullscreen;
            selectedResolutionIndex = GetSavedOrCurrentResolutionIndex();
        }
    }

    private int GetSavedOrCurrentResolutionIndex()
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
            return 0;

        var savedIndex = PlayerPrefs.GetInt(ResolutionIndexKey, -1);
        if (savedIndex >= 0 && savedIndex < availableResolutions.Length)
            return savedIndex;

        var bestIndex = 0;
        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var res = availableResolutions[i];
            if (res.width == Screen.width && res.height == Screen.height)
            {
                bestIndex = i;
                break;
            }
        }

        return bestIndex;
    }

    private void ApplyDisplaySettings(int resolutionIndex, bool fullscreen, bool save)
    {
        if (availableResolutions == null || availableResolutions.Length == 0)
            availableResolutions = Screen.resolutions;

        if (availableResolutions == null || availableResolutions.Length == 0)
            return;

        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
            resolutionIndex = Mathf.Clamp(GetSavedOrCurrentResolutionIndex(), 0, availableResolutions.Length - 1);

        selectedResolutionIndex = resolutionIndex;
        var res = availableResolutions[resolutionIndex];
        var fullscreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        Screen.SetResolution(res.width, res.height, fullscreenMode);

        if (!save)
            return;

        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
        PlayerPrefs.Save();
    }

    public static float GetMouseSensitivity()
    {
        return PlayerPrefs.GetFloat(MouseSensitivityKey, 1f);
    }
}
