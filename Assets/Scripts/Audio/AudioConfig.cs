using System;
using UnityEngine;

[Serializable]
public class SFXEntry
{
    public SFX key;
    public SoundEntry sound;
}

[Serializable]
public class BGMEntry
{
    public BGM key;
    public SoundEntry sound;
}

[Serializable]
public class AmbientEntry
{
    public AmbientRoom key;
    public SoundEntry sound;
}

[CreateAssetMenu(fileName = "AudioConfig", menuName = "Audio/Audio Config")]
public class AudioConfig : ScriptableObject
{
    [Header("全局音量")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.7f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float ambientVolume = 0.5f;

    [Header("BGM 设置")]
    public float bgmCrossFadeDuration = 1.5f;

    [Header("环境音设置")]
    public float ambientFadeDuration = 0.5f;

    [Header("BGM 配置")]
    public BGMEntry[] bgmEntries;

    [Header("环境音配置")]
    public AmbientEntry[] ambientEntries;

    [Header("音效配置")]
    public SFXEntry[] sfxEntries;
}
