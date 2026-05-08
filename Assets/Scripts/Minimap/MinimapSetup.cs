#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

public class MinimapSetup : MonoBehaviour
{
    [Header("小地图设置")]
    [SerializeField] private int textureSize = 512;
    [SerializeField] private float orthographicSize = 20f;
    [SerializeField] private float uiWidth = 200f;
    [SerializeField] private float uiHeight = 200f;
    [SerializeField] private Vector2 uiOffset = new Vector2(-10f, -10f);
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    [SerializeField] private int canvasSortingOrder = 100;

    private Camera minimapCamera;
    private RenderTexture renderTexture;
    private Canvas canvas;
    private RawImage rawImage;
    private Transform playerTransform;
    private bool hasLoggedPlayerWarning = false;

    private void Awake()
    {
        CreateMinimapCamera();
        CreateMinimapUI();
        FindPlayer();
    }

    private void OnValidate()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = orthographicSize;
            minimapCamera.backgroundColor = backgroundColor;
        }
        if (renderTexture != null && (renderTexture.width != textureSize || renderTexture.height != textureSize))
        {
            renderTexture.Release();
            renderTexture.width = textureSize;
            renderTexture.height = textureSize;
            renderTexture.Create();
        }
        if (rawImage != null)
        {
            RectTransform rt = rawImage.rectTransform;
            rt.sizeDelta = new Vector2(uiWidth, uiHeight);
            rt.anchoredPosition = uiOffset;
        }
    }

    private void Update()
    {
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = orthographicSize;
        }
    }

    private void LateUpdate()
    {
        if (playerTransform == null)
        {
            FindPlayer();
        }

        if (playerTransform != null && minimapCamera != null)
        {
            Vector3 pos = minimapCamera.transform.position;
            pos.x = playerTransform.position.x;
            pos.y = playerTransform.position.y;
            minimapCamera.transform.position = pos;
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }

    private void CreateMinimapCamera()
    {
        GameObject cameraObj = new GameObject("MinimapCamera");
        cameraObj.transform.SetParent(transform);

        minimapCamera = cameraObj.AddComponent<Camera>();
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = orthographicSize;
        minimapCamera.depth = -1;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = backgroundColor;
        minimapCamera.cullingMask = ~0;
        minimapCamera.transform.position = new Vector3(0f, 0f, -10f);

        renderTexture = new RenderTexture(textureSize, textureSize, 16);
        renderTexture.Create();
        minimapCamera.targetTexture = renderTexture;
    }

    private void CreateMinimapUI()
    {
        GameObject canvasObj = new GameObject("MinimapCanvas");
        canvasObj.transform.SetParent(transform);

        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject rawImageObj = new GameObject("MinimapRawImage");
        rawImageObj.transform.SetParent(canvasObj.transform, false);

        rawImage = rawImageObj.AddComponent<RawImage>();
        rawImage.texture = renderTexture;
        rawImage.color = Color.white;

        GameObject borderObj = new GameObject("MinimapBorder");
        borderObj.transform.SetParent(canvasObj.transform, false);
        RawImage border = borderObj.AddComponent<RawImage>();
        border.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);
        RectTransform brt = border.rectTransform;
        brt.anchorMin = new Vector2(1f, 1f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.pivot = new Vector2(1f, 1f);
        brt.sizeDelta = new Vector2(uiWidth + 4f, uiHeight + 4f);
        brt.anchoredPosition = uiOffset;
        borderObj.transform.SetSiblingIndex(0);

        RectTransform rt = rawImage.rectTransform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(uiWidth, uiHeight);
        rt.anchoredPosition = uiOffset;
    }

    private void FindPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            hasLoggedPlayerWarning = false;
        }
        else if (!hasLoggedPlayerWarning)
        {
            Debug.LogWarning("[Minimap] Player not found with tag 'Player'");
            hasLoggedPlayerWarning = true;
        }
    }
}
#endif
