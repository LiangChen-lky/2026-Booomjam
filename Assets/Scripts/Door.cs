using Pathfinding;
using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door State")]
    [SerializeField] private bool startsOpen;
    [SerializeField] private bool requiresPlayerFirstOpen;

    [Header("Nav Graph")]
    [SerializeField] private bool updateNavGraph = true;

    [Header("Audio")]
    [SerializeField] private bool playSound = true;

    private bool isOpen;
    private bool playerFirstOpenCompleted;

    public bool IsOpen => isOpen;
    public bool RequiresPlayerFirstOpen => requiresPlayerFirstOpen && !playerFirstOpenCompleted;
    public bool CanMonsterOpen => !RequiresPlayerFirstOpen;

    private void Awake()
    {
        isOpen = startsOpen;
        playerFirstOpenCompleted = !requiresPlayerFirstOpen;
        ApplyOpenState(isOpen, 0.7f);
    }

    public void MarkPlayerFirstOpenCompleted()
    {
        playerFirstOpenCompleted = true;
    }

    public void ApplyOpenState(bool opened, float openedSpriteAlpha)
    {
        isOpen = opened;

        var sr = GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = opened ? openedSpriteAlpha : 1f;
            sr.color = c;
        }

        foreach (var col in GetComponentsInChildren<Collider2D>(true))
        {
            if (!col.isTrigger)
            {
                col.enabled = !opened;
            }
        }

        OnDoorStateChanged();
    }

    public void OnDoorStateChanged()
    {
        if (updateNavGraph && AstarPath.active != null)
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                AstarPath.active.UpdateGraphs(collider.bounds);
            }
        }
    }

    public void PlayOpenSound()
    {
        if (playSound)
        {
            AudioManager.Instance.Play(SFX.DoorOpen);
        }
    }

    public void PlayCloseSound()
    {
        if (playSound)
        {
            AudioManager.Instance.Play(SFX.DoorClose);
        }
    }
}
