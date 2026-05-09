using UnityEngine;

/// <summary>
/// 大门交互组件。玩家集齐钥匙后按交互键解锁并逃脱。
/// 挂载在大门 GameObject 上，需配合 InteractableItem 使用。
/// </summary>
public class MainDoor : MonoBehaviour
{
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField, Range(0f, 1f)] private float unlockedAlpha = 0.5f;

    private bool isUnlocked = false;

    /// <summary>
    /// 由 InteractableItem 的 onInteracted 事件调用。
    /// </summary>
    public void TryUnlock(PlayerController player)
    {
        if (isUnlocked) return;

        var keyManager = FindObjectOfType<KeyManager>();
        if (keyManager == null || !keyManager.HasAllKeys())
        {
            // 钥匙不足
            return;
        }

        isUnlocked = true;

        // 播放解锁音效
        AudioManager.Instance.Play(SFX.MainDoorUnlock);

        // 视觉反馈：门变半透明
        if (doorSprite != null)
        {
            Color c = doorSprite.color;
            c.a = unlockedAlpha;
            doorSprite.color = c;
        }

        // 触发逃脱
        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerEscape();
    }
}
