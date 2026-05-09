using System;
using UnityEngine;

[Serializable]
public class SoundEntry
{
    [Tooltip("音效片段")]
    public AudioClip clip;

    [Tooltip("音量")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("随机音高变化范围（0 = 无变化）")]
    [Range(0f, 0.5f)]
    public float pitchVariance = 0f;

    [Tooltip("是否循环")]
    public bool loop = false;

    public void Init()
    {
    }

    public float GetRandomPitch()
    {
        if (pitchVariance <= 0f) return 1f;
        return 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance);
    }
}
