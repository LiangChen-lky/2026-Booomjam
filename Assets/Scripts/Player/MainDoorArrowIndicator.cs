using UnityEngine;

/// <summary>
/// Shows a small world-space arrow near the player after all keys are collected.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public class MainDoorArrowIndicator : MonoBehaviour
{
    [SerializeField] private MainDoor targetDoor;
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private Color arrowColor = new Color(1f, 0.92f, 0.25f, 0.95f);
    [SerializeField, Min(0.1f)] private float distanceFromPlayer = 1.2f;
    [SerializeField, Min(0.1f)] private float arrowScale = 0.65f;
    [SerializeField] private int sortingOrder = 200;
    [SerializeField] private bool hideWhenDoorUnlocked = true;

    private PlayerController player;
    private KeyManager keyManager;
    private SpriteRenderer arrowRenderer;
    private Transform arrowTransform;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        ResolveTargetDoor();
        CreateArrow();
        SetArrowVisible(false);
    }

    private void LateUpdate()
    {
        if (targetDoor == null)
        {
            ResolveTargetDoor();
        }

        bool shouldShow = targetDoor != null
            && HasAllKeys()
            && (!hideWhenDoorUnlocked || !targetDoor.IsUnlocked);

        SetArrowVisible(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        Vector3 playerPosition = transform.position;
        Vector3 doorPosition = targetDoor.transform.position;
        Vector2 direction = doorPosition - playerPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 normalizedDirection = direction.normalized;
        arrowTransform.position = playerPosition + (Vector3)(normalizedDirection * distanceFromPlayer);
        arrowTransform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(normalizedDirection.y, normalizedDirection.x) * Mathf.Rad2Deg);
    }

    private bool HasAllKeys()
    {
        if (keyManager == null)
        {
            keyManager = FindObjectOfType<KeyManager>();
        }

        int requiredKeys = keyManager != null ? keyManager.TotalKeys : KeyGameConfig.DefaultKeyCount;
        return (player != null && player.CurrentKey >= requiredKeys)
            || (keyManager != null && keyManager.HasAllKeys());
    }

    private void ResolveTargetDoor()
    {
        if (targetDoor == null)
        {
            targetDoor = FindObjectOfType<MainDoor>(true);
        }
    }

    private void CreateArrow()
    {
        GameObject arrowObject = new GameObject("MainDoorArrowIndicator");
        arrowObject.transform.SetParent(transform, false);
        arrowTransform = arrowObject.transform;
        arrowTransform.localScale = Vector3.one * arrowScale;

        arrowRenderer = arrowObject.AddComponent<SpriteRenderer>();
        arrowRenderer.sprite = arrowSprite != null ? arrowSprite : CreateDefaultArrowSprite();
        arrowRenderer.color = arrowColor;
        arrowRenderer.sortingOrder = sortingOrder;
    }

    private void SetArrowVisible(bool visible)
    {
        if (arrowRenderer != null && arrowRenderer.enabled != visible)
        {
            arrowRenderer.enabled = visible;
        }
    }

    private static Sprite CreateDefaultArrowSprite()
    {
        const int width = 64;
        const int height = 32;
        const float pixelsPerUnit = 32f;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 fill = new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (IsArrowPixel(x, y, width, height))
                {
                    pixels[y * width + x] = fill;
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private static bool IsArrowPixel(int x, int y, int width, int height)
    {
        float centerY = (height - 1) * 0.5f;
        float halfShaftHeight = height * 0.16f;
        int headStart = Mathf.RoundToInt(width * 0.58f);

        if (x < headStart)
        {
            return Mathf.Abs(y - centerY) <= halfShaftHeight;
        }

        float headProgress = Mathf.InverseLerp(headStart, width - 1, x);
        float halfHeadHeight = Mathf.Lerp(height * 0.45f, 0f, headProgress);
        return Mathf.Abs(y - centerY) <= halfHeadHeight;
    }
}
