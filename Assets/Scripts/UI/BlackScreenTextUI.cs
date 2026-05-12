using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

/// <summary>
/// Full-screen black text panel used for opening/end interludes.
/// Builds its own fallback UI so scenes do not need an authored prefab.
/// </summary>
public class BlackScreenTextUI : MonoBehaviour
{
    private const float InputGuardSeconds = 0.15f;

    private GameObject root;
    private Text messageText;
    private Text continueHintText;
    private Action continueAction;
    private float shownAt;

    public static BlackScreenTextUI Show(string message, string continueHint, Action onContinue)
    {
        var ui = FindObjectOfType<BlackScreenTextUI>();
        if (ui == null)
        {
            ui = new GameObject("[BlackScreenTextUI]").AddComponent<BlackScreenTextUI>();
        }

        ui.ShowInternal(message, continueHint, onContinue);
        return ui;
    }

    private void Awake()
    {
        EnsureUI();
        Hide();
    }

    private void Update()
    {
        if (root == null || !root.activeSelf)
        {
            return;
        }

        if (Time.unscaledTime - shownAt < InputGuardSeconds)
        {
            return;
        }

        if (!WasAnyInputPressedThisFrame())
        {
            return;
        }

        var callback = continueAction;
        continueAction = null;
        Hide();
        callback?.Invoke();
    }

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void ShowInternal(string message, string continueHint, Action onContinue)
    {
        EnsureUI();
        continueAction = onContinue;
        shownAt = Time.unscaledTime;

        if (messageText != null)
        {
            messageText.text = message ?? string.Empty;
        }

        if (continueHintText != null)
        {
            continueHintText.text = continueHint ?? string.Empty;
            continueHintText.gameObject.SetActive(!string.IsNullOrEmpty(continueHint));
        }

        root.SetActive(true);
    }

    private void EnsureUI()
    {
        if (root != null)
        {
            return;
        }

        root = new GameObject("BlackScreenTextCanvas");
        root.transform.SetParent(transform, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        var background = root.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        messageText = CreateText("Message", 36, TextAnchor.MiddleCenter, new Vector2(1280f, 520f), new Vector2(0f, 40f));
        continueHintText = CreateText("ContinueHint", 24, TextAnchor.MiddleCenter, new Vector2(640f, 60f), new Vector2(0f, -360f));
    }

    private Text CreateText(string objectName, int fontSize, TextAnchor alignment, Vector2 size, Vector2 position)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(root.transform, false);

        var text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        return text;
    }

    private static bool WasAnyInputPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        if (Mouse.current != null
            && (Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        if (Gamepad.current != null)
        {
            foreach (var control in Gamepad.current.allControls)
            {
                if (control is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }
        }

        try
        {
            return UnityEngine.Input.anyKeyDown;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
