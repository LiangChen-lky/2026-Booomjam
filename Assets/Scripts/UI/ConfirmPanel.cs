using System;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPanel : MonoBehaviour
{
    private static ConfirmPanel instance;

    [SerializeField] private float panelWidth = 520f;
    [SerializeField] private float panelHeight = 220f;
    [SerializeField] private int messageFontSize = 30;
    [SerializeField] private int buttonFontSize = 24;
    [SerializeField] private Color bgColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color buttonColor = new Color(0.18f, 0.18f, 0.18f, 1f);
    [SerializeField] private Color highlightedButtonColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color textColor = Color.white;

    private GameObject panelRoot;
    private Text messageText;
    private Action onConfirm;
    private Action onCancel;
    private PlayerController currentPlayer;
    private bool isShowing;
    private bool readyForKeyboardInput;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLockState;

    public static bool IsShowing => instance != null && instance.isShowing;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        CreateUI();
        panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Update()
    {
        if (!isShowing) return;
        if (!readyForKeyboardInput)
        {
            readyForKeyboardInput = true;
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
        {
            Confirm();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
    }

    public static void Show(string message, PlayerController player, Action confirmAction, Action cancelAction = null)
    {
        EnsureInstance();
        instance.ShowInternal(message, player, confirmAction, cancelAction);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;

        var panelObject = new GameObject("ConfirmPanel");
        panelObject.AddComponent<ConfirmPanel>();
    }

    private void ShowInternal(string message, PlayerController player, Action confirmAction, Action cancelAction)
    {
        messageText.text = message;
        onConfirm = confirmAction;
        onCancel = cancelAction;
        currentPlayer = player;
        isShowing = true;
        readyForKeyboardInput = false;
        previousCursorVisible = Cursor.visible;
        previousCursorLockState = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        panelRoot.SetActive(true);
        currentPlayer?.Input.DisablePlayerMoveInput();
        AudioManager.Instance.Play(SFX.UIPopupOpen);
    }

    private void Confirm()
    {
        var callback = onConfirm;
        Hide();
        callback?.Invoke();
    }

    private void Cancel()
    {
        var callback = onCancel;
        Hide();
        callback?.Invoke();
    }

    private void Hide()
    {
        AudioManager.Instance.Play(SFX.UIPopupClose);
        panelRoot.SetActive(false);
        isShowing = false;
        Cursor.visible = previousCursorVisible;
        Cursor.lockState = previousCursorLockState;
        currentPlayer?.Input.EnablePlayerMoveInput();
        currentPlayer = null;
        onConfirm = null;
        onCancel = null;
    }

    private void CreateUI()
    {
        EnsureEventSystem();

        var canvasObj = new GameObject("ConfirmCanvas");
        canvasObj.transform.SetParent(transform, false);

        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("ConfirmPanelRoot");
        panelRoot.transform.SetParent(canvasObj.transform, false);

        var bg = panelRoot.AddComponent<Image>();
        bg.color = bgColor;

        var panelRt = panelRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);

        var messageObj = new GameObject("Message");
        messageObj.transform.SetParent(panelRoot.transform, false);

        messageText = messageObj.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = messageFontSize;
        messageText.color = textColor;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;

        var messageRt = messageObj.GetComponent<RectTransform>();
        messageRt.anchorMin = new Vector2(0f, 0.42f);
        messageRt.anchorMax = new Vector2(1f, 1f);
        messageRt.offsetMin = new Vector2(32f, 0f);
        messageRt.offsetMax = new Vector2(-32f, -20f);

        CreateButton("YesButton", "是", new Vector2(0.28f, 0.22f), Confirm);
        CreateButton("NoButton", "否", new Vector2(0.72f, 0.22f), Cancel);
    }

    private void CreateButton(string name, string label, Vector2 anchor, UnityEngine.Events.UnityAction callback)
    {
        var buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(panelRoot.transform, false);

        var image = buttonObj.AddComponent<Image>();
        image.color = buttonColor;

        var button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = buttonColor,
            highlightedColor = highlightedButtonColor,
            pressedColor = highlightedButtonColor,
            selectedColor = highlightedButtonColor,
            disabledColor = buttonColor,
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        button.onClick.AddListener(callback);

        var buttonRt = buttonObj.GetComponent<RectTransform>();
        buttonRt.anchorMin = anchor;
        buttonRt.anchorMax = anchor;
        buttonRt.pivot = new Vector2(0.5f, 0.5f);
        buttonRt.sizeDelta = new Vector2(150f, 54f);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        var text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = buttonFontSize;
        text.color = textColor;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = label;

        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }
}
