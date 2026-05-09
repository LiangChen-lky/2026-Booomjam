using UnityEngine;
using Pathfinding;

public class Door : MonoBehaviour
{
    [Header("寻路更新")]
    [SerializeField] private bool updateNavGraph = true;

    [Header("音效")]
    [SerializeField] private bool playSound = true;

    /// <summary>
    /// 当门状态改变时调用此方法更新寻路网格
    /// </summary>
    public void OnDoorStateChanged()
    {
        if (updateNavGraph && AstarPath.active != null)
        {
            AstarPath.active.UpdateGraphs(GetComponent<Collider2D>().bounds);
        }
    }

    public void PlayOpenSound()
    {
        if (playSound)
            AudioManager.Instance.Play(SFX.DoorOpen);
    }

    public void PlayCloseSound()
    {
        if (playSound)
            AudioManager.Instance.Play(SFX.DoorClose);
    }
}
