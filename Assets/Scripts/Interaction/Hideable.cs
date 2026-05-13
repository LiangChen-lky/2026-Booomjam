using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hideable : MonoBehaviour
{
    [SerializeField] private Transform hidePoint;
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0.7f;

    private Collider2D col;
    private SpriteRenderer sr;
    private float originalAlpha;
    private readonly List<Collider2D> ignoredPlayerColliders = new List<Collider2D>();

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalAlpha = sr.color.a;
    }

    private void OnDisable()
    {
        RestoreIgnoredPlayerCollisions();
    }

    public void OnEnter(PlayerController player)
    {
        SetPlayerCollisionIgnored(player, true);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = hiddenAlpha;
            sr.color = c;
        }
        if (hidePoint != null)
            player.SetPlayerPosition(hidePoint.position);
        AudioManager.Instance.Play(SFX.HideIn);
    }

    public void OnExit(PlayerController player)
    {
        SetPlayerCollisionIgnored(player, false);
        if (sr != null)
        {
            Color c = sr.color;
            c.a = originalAlpha;
            sr.color = c;
        }
        AudioManager.Instance.Play(SFX.HideOut);
    }

    private void SetPlayerCollisionIgnored(PlayerController player, bool ignored)
    {
        if (col == null || player == null) return;

        if (ignored)
        {
            RestoreIgnoredPlayerCollisions();
            Collider2D[] playerColliders = player.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D playerCollider in playerColliders)
            {
                if (playerCollider == null || !playerCollider.enabled) continue;

                Physics2D.IgnoreCollision(col, playerCollider, true);
                ignoredPlayerColliders.Add(playerCollider);
            }

            return;
        }

        RestoreIgnoredPlayerCollisions();
    }

    private void RestoreIgnoredPlayerCollisions()
    {
        if (col == null)
        {
            ignoredPlayerColliders.Clear();
            return;
        }

        foreach (Collider2D playerCollider in ignoredPlayerColliders)
        {
            if (playerCollider != null)
            {
                Physics2D.IgnoreCollision(col, playerCollider, false);
            }
        }

        ignoredPlayerColliders.Clear();
    }
}
