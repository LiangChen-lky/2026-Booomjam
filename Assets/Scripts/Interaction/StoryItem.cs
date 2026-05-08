using UnityEngine;

/// <summary>
/// 线索物品（纸条、日记等）的文字内容。
/// 配合 InteractableItem 组件使用，onInteracted 绑定 ShowStory。
/// </summary>
public class StoryItem : MonoBehaviour
{
    [SerializeField, TextArea(3, 10)] private string storyText;

    public void ShowStory(PlayerController player)
    {
        StoryPanel.Show(storyText, player);
    }
}
