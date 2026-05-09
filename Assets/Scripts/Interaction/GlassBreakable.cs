using UnityEngine;

public class GlassBreakable : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glassSprite;
    [SerializeField] private Collider2D physicsCollider;
    [SerializeField, Tooltip("破碎后碎片粒子预制体（可选）")]
    private GameObject breakEffectPrefab;

    private bool isBroken = false;

    public void Break()
    {
        if (isBroken) return;
        isBroken = true;

        AudioManager.Instance.PlayAtPosition(SFX.GlassBreak, transform.position);

        if (physicsCollider != null) physicsCollider.enabled = false;
        if (glassSprite != null) glassSprite.enabled = false;

        if (breakEffectPrefab != null)
        {
            Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isBroken) return;
        if (other.CompareTag("Monster"))
        {
            Break();
        }
    }
}
