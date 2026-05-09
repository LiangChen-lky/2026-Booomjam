using UnityEngine;

/// <summary>
/// 旅行袋交互逻辑。配合 InteractableItem 组件使用。
/// </summary>
public class TravelBag : MonoBehaviour
{
    /// <summary>
    /// 拾取旅行袋中的钥匙。由 InteractableItem 的 onInteracted 事件调用。
    /// </summary>
    public void PickupKey(PlayerController player)
    {
        var keyT = transform.Find("Key");
        if (keyT == null || !keyT.gameObject.activeSelf)
        {
            // 空书包
            AudioManager.Instance.Play(SFX.EmptyBag);
            return;
        }
        keyT.gameObject.SetActive(false);
        player.AddKey();
        AudioManager.Instance.Play(SFX.BagSearch);
    }
}
