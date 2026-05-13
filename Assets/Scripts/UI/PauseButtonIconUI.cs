using UnityEngine;
using UnityEngine.UI;

public class PauseButtonIconUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image iconImage;

    [Header("Sprites")]
    [SerializeField] private Sprite runningSprite;
    [SerializeField] private Sprite pausedSprite;

    private bool hasLastPausedState;
    private bool lastPausedState;

    private void Awake()
    {
        SetupButton();
        lastPausedState = !PauseMenu.IsPaused;
        RefreshIcon();
    }

    private void OnDestroy()
    {
        if (pauseButton != null)
            pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
    }

    private void Update()
    {
        RefreshIcon();
    }

    private void SetupButton()
    {
        if (pauseButton == null)
            pauseButton = GetComponent<Button>();

        if (pauseButton == null)
            pauseButton = gameObject.AddComponent<Button>();

        ResolveIconImage();

        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
            pauseButton.targetGraphic = iconImage;
        }

        pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
        pauseButton.onClick.AddListener(OnPauseButtonClicked);
    }

    private void RefreshIcon()
    {
        bool isPaused = PauseMenu.IsPaused;
        if (iconImage == null)
            ResolveIconImage();

        if (iconImage == null || (hasLastPausedState && isPaused == lastPausedState))
            return;

        hasLastPausedState = true;
        lastPausedState = isPaused;
        Sprite sprite = isPaused ? pausedSprite : runningSprite;
        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    private void ResolveIconImage()
    {
        if (iconImage != null)
            return;

        iconImage = GetComponent<Image>();

        if (iconImage == null && pauseButton != null)
            iconImage = pauseButton.targetGraphic as Image;

        // Do not auto-pick child images here. This script is often placed in a
        // shared Canvas/toolbar, and grabbing the first child image can replace
        // an unrelated icon such as the monitor button.
    }

    private void OnPauseButtonClicked()
    {
        if (PauseMenu.IsPaused)
            PauseMenu.Resume();
        else
            PauseMenu.Pause();

        RefreshIcon();
    }
}
