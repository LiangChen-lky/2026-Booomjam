using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一次性触发式交互物品（旅行袋、纸条、日记等）。
/// 玩家在范围内按交互键时触发 UnityEvent。
/// </summary>
public class InteractableItem : MonoBehaviour
{
    [SerializeField] private UnityEvent<PlayerController> onInteracted;

    public void OnInteracted(PlayerController player)
    {
        onInteracted?.Invoke(player);
    }
}
