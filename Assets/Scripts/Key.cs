using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    private bool isCollected = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 确保碰撞体是玩家且还没被收集
        if (other.CompareTag("Player") && !isCollected)
        {
            KeyManager km = other.GetComponent<KeyManager>();

            if (km != null)
            {
                km.CollectKey();
                isCollected = true;

                AudioManager.Instance.Play(SFX.KeyPickup);

                GetComponent<Collider2D>().enabled = false;
                GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
            }
        }
    }
}
