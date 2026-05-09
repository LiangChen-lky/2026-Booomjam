using UnityEngine;

/// <summary>
/// 教程触发区域。玩家进入时显示操作提示。
/// 挂载在带有 Trigger Collider2D 的 GameObject 上。
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [SerializeField, TextArea(2, 5)]
    private string hintText = "按 E 键与物体互动";
    [SerializeField] private bool showOnlyOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (showOnlyOnce && hasTriggered) return;

        hasTriggered = true;
        AudioManager.Instance.Play(SFX.TutorialHint);

        var player = other.GetComponent<PlayerController>();
        StoryPanel.Show(hintText, player);
    }
}
