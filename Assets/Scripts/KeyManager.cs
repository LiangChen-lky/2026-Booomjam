using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro; // 必须添加这个，因为你的 UI 是 TextMeshPro

public class KeyManager : MonoBehaviour
{
    public int keysCollected = 0;
    public int totalKeys = KeyGameConfig.DefaultKeyCount;

    public TextMeshProUGUI keyText; // 类型改为 TextMeshProUGUI
    public Image keyImage;

    [Header("Key UI Sprites")]
    [SerializeField] private Sprite zeroKeySprite;
    [SerializeField] private Sprite oneKeySprite;
    [SerializeField] private Sprite twoKeySprite;
    [SerializeField] private Sprite threeKeySprite;

    void Start()
    {
        UpdateKeyUI(); // 初始化显示
    }

    public void CollectKey()
    {
        keysCollected = Mathf.Clamp(keysCollected + 1, 0, totalKeys);
        UpdateKeyUI();
        AudioManager.Instance.Play(SFX.KeyFound);

        if (HasAllKeys())
        {
            // 所有钥匙已收集，可以解锁大门
            // 大门交互逻辑由 MainDoor.TryUnlock 处理
            Debug.Log("[KeyManager] 已集齐全部钥匙！前往大门逃脱！");
        }
    }

    private void UpdateKeyUI()
    {
        if (keyText != null)
        {
            // 使用插值字符串更简洁
            keyText.text = $"Key Number : {keysCollected}";
        }

        if (keyImage != null)
        {
            Sprite sprite = GetKeySprite(keysCollected);
            keyImage.sprite = sprite;
            keyImage.enabled = sprite != null;
        }
    }

    private Sprite GetKeySprite(int keyCount)
    {
        switch (Mathf.Clamp(keyCount, 0, totalKeys))
        {
            case 0:
                return zeroKeySprite;
            case 1:
                return oneKeySprite;
            case 2:
                return twoKeySprite;
            default:
                return threeKeySprite;
        }
    }

    public bool HasAllKeys()
    {
        return keysCollected >= totalKeys;
    }

    public int TotalKeys => totalKeys;
}
