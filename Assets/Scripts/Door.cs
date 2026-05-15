using Pathfinding;
using UnityEngine;

public class Door : MonoBehaviour
{
    private const float NavGraphUpdatePadding = 0.1f;

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
        bool hadBoundsBefore = TryGetNavGraphBounds(out Bounds graphBounds);

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

        if (TryGetNavGraphBounds(out Bounds boundsAfter))
        {
            if (hadBoundsBefore)
            {
                graphBounds.Encapsulate(boundsAfter);
            }
            else
            {
                graphBounds = boundsAfter;
                hadBoundsBefore = true;
            }
        }

        OnDoorStateChanged(graphBounds, hadBoundsBefore);
    }

    public void OnDoorStateChanged()
    {
        OnDoorStateChanged(new Bounds(), false);
    }

    private void OnDoorStateChanged(Bounds changedBounds, bool hasChangedBounds)
    {
        if (updateNavGraph && AstarPath.active != null)
        {
            if (!hasChangedBounds && !TryGetNavGraphBounds(out changedBounds))
            {
                return;
            }

            changedBounds.Expand(NavGraphUpdatePadding);
            UpdateNavGraph(changedBounds);
        }
    }

    private void UpdateNavGraph(Bounds changedBounds)
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        if (!isOpen)
        {
            AstarPath.active.UpdateGraphs(changedBounds);
            AstarPath.active.FlushGraphUpdates();
            return;
        }

        bool[] triggerWasEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D col = colliders[i];
            if (col != null && col.isTrigger)
            {
                triggerWasEnabled[i] = col.enabled;
                col.enabled = false;
            }
        }

        try
        {
            AstarPath.active.UpdateGraphs(changedBounds);
            AstarPath.active.FlushGraphUpdates();
        }
        finally
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D col = colliders[i];
                if (col != null && col.isTrigger)
                {
                    col.enabled = triggerWasEnabled[i];
                }
            }
        }
    }

    private bool TryGetNavGraphBounds(out Bounds bounds)
    {
        bounds = new Bounds();
        bool hasBounds = false;
        foreach (var col in GetComponentsInChildren<Collider2D>(true))
        {
            if (col == null || !col.enabled) continue;

            if (!hasBounds)
            {
                bounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds;
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
