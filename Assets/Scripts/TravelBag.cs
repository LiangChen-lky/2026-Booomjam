using UnityEngine;

/// <summary>
/// 旅行袋交互逻辑。配合 InteractableItem 组件使用。
/// </summary>
public class TravelBag : MonoBehaviour
{
    [Header("Key Layout")]
    [SerializeField] private GameObject keyVisual;
    [Header("Bag Visual")]
    [SerializeField] private SpriteRenderer bagRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openedSprite;
    [Header("Hint")]
    [SerializeField] private string emptyBagHintText = "这个旅行袋是空的";

    private InteractableItem interactableItem;
    private Collider2D[] interactionColliders;
    private bool hasKey;
    private bool isOpened;

    public bool HasKey => hasKey;
    public bool IsOpened => isOpened;

    private void Awake()
    {
        CacheVisualComponents();
        CacheKeyVisual();
        UpdateKeyVisual();
        UpdateBagVisual();
        TravelBagAllocator.RequestDistribution();
    }

    public bool CanReceiveKey => !isOpened && !hasKey;

    public void SetHasKey(bool value)
    {
        hasKey = value && !isOpened;
        UpdateKeyVisual();
    }

    /// <summary>
    /// 拾取旅行袋中的钥匙。由 InteractableItem 的 onInteracted 事件调用。
    /// </summary>
    public void PickupKey(PlayerController player)
    {
        if (isOpened)
        {
            ShowEmptyBagHint();
            return;
        }

        isOpened = true;
        bool awardedKey = hasKey;
        hasKey = false;
        UpdateKeyVisual();
        UpdateBagVisual();

        if (awardedKey)
        {
            if (player != null)
            {
                player.AddKey();
            }

            var audioManager = AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.Play(SFX.BagSearch);
            }
        }
        else
        {
            ShowEmptyBagHint();
        }
    }

    private void ShowEmptyBagHint()
    {
        var audioManager = AudioManager.Instance;
        if (audioManager != null)
        {
            audioManager.Play(SFX.EmptyBag);
        }

        if (!string.IsNullOrWhiteSpace(emptyBagHintText))
        {
            ScreenHintPanel.Show(emptyBagHintText);
        }
    }

    private void CacheVisualComponents()
    {
        if (bagRenderer == null)
        {
            bagRenderer = GetComponent<SpriteRenderer>();
        }

        if (interactableItem == null)
        {
            interactableItem = GetComponent<InteractableItem>();
        }

        if (interactionColliders == null || interactionColliders.Length == 0)
        {
            interactionColliders = GetComponentsInChildren<Collider2D>(true);
        }
    }

    private void CacheKeyVisual()
    {
        if (keyVisual != null)
        {
            return;
        }

        var keyTransform = transform.Find("Key");
        if (keyTransform != null)
        {
            keyVisual = keyTransform.gameObject;
        }
    }

    private void UpdateKeyVisual()
    {
        if (keyVisual != null)
        {
            keyVisual.SetActive(hasKey && !isOpened);
        }
    }

    private void UpdateBagVisual()
    {
        CacheVisualComponents();

        if (bagRenderer != null)
        {
            if (isOpened && openedSprite != null)
            {
                bagRenderer.sprite = openedSprite;
            }
            else if (!isOpened && closedSprite != null)
            {
                bagRenderer.sprite = closedSprite;
            }
        }

        SetInteractionEnabled(!isOpened);
    }

    private void SetInteractionEnabled(bool enabled)
    {
        if (interactableItem != null)
        {
            interactableItem.enabled = enabled;
        }

        if (interactionColliders == null)
        {
            return;
        }

        foreach (var collider in interactionColliders)
        {
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }
}
