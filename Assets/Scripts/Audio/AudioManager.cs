using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[AudioManager]");
                instance = go.AddComponent<AudioManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("配置")]
    [SerializeField] private AudioConfig config;

    [Header("3D 音效池")]
    public int maxPositionalSources = 16;

    [Header("调试")]
    [SerializeField] private BGM currentBGM = BGM.None;
    [SerializeField] private AmbientRoom currentAmbient = AmbientRoom.None;

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBGMSource;
    private AudioSource ambientSource;
    private AudioSource sfxSource;

    private AudioSource[] positionalPool;
    private int positionalPoolIndex;

    private Dictionary<SFX, SoundEntry> sfxLookup;
    private Dictionary<BGM, SoundEntry> bgmLookup;
    private Dictionary<AmbientRoom, SoundEntry> ambientLookup;

    private Coroutine bgmFadeCoroutine;
    private Coroutine ambientFadeCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (instance == null)
        {
            var go = new GameObject("[AudioManager]");
            instance = go.AddComponent<AudioManager>();
            DontDestroyOnLoad(go);
            instance.Init();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        Init();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Init()
    {
        if (sfxLookup != null) return;

        CreateAudioSources();
        BuildLookups();
    }

    private void CreateAudioSources()
    {
        bgmSourceA = CreateSource("BGM_A");
        bgmSourceB = CreateSource("BGM_B");
        activeBGMSource = bgmSourceA;

        ambientSource = CreateSource("Ambient");

        sfxSource = CreateSource("SFX");

        // 创建 3D 音效对象池
        positionalPool = new AudioSource[maxPositionalSources];
        for (int i = 0; i < maxPositionalSources; i++)
        {
            positionalPool[i] = CreateSource("Pos3D_" + i);
            positionalPool[i].spatialBlend = 1f; // 3D 音效
        }
        positionalPoolIndex = 0;
    }

    private AudioSource CreateSource(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        return source;
    }

    private void BuildLookups()
    {
        sfxLookup = new Dictionary<SFX, SoundEntry>();
        bgmLookup = new Dictionary<BGM, SoundEntry>();
        ambientLookup = new Dictionary<AmbientRoom, SoundEntry>();

        if (config == null)
        {
            Debug.LogWarning("[AudioManager] AudioConfig 未分配，音效系统无法工作。请在 Inspector 中设置 AudioConfig。");
            return;
        }

        if (config.sfxEntries != null)
        {
            foreach (var e in config.sfxEntries)
            {
                e.sound.Init();
                sfxLookup[e.key] = e.sound;
            }
        }

        if (config.bgmEntries != null)
        {
            foreach (var e in config.bgmEntries)
            {
                e.sound.Init();
                bgmLookup[e.key] = e.sound;
            }
        }

        if (config.ambientEntries != null)
        {
            foreach (var e in config.ambientEntries)
            {
                e.sound.Init();
                ambientLookup[e.key] = e.sound;
            }
        }
    }

    // ===== 公共 API =====

    /// <summary>
    /// 播放一次性 2D 音效
    /// </summary>
    public void Play(SFX sfx)
    {
        if (!TryGetSFXEntry(sfx, out var entry) || entry.clip == null) return;

        float pitch = entry.GetRandomPitch();
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(entry.clip, entry.volume * GetSFXVolume());
    }

    /// <summary>
    /// 在指定位置播放 3D 音效（对象池复用，不创建临时 GameObject）
    /// </summary>
    public void PlayAtPosition(SFX sfx, Vector3 position)
    {
        if (!TryGetSFXEntry(sfx, out var entry) || entry.clip == null) return;
        if (positionalPool == null || positionalPool.Length == 0) return;

        // 轮询选择一个 AudioSource（正在播放的会被抢占，实现复用）
        AudioSource source = positionalPool[positionalPoolIndex];
        positionalPoolIndex = (positionalPoolIndex + 1) % positionalPool.Length;

        source.transform.position = position;
        source.clip = entry.clip;
        source.volume = entry.volume * GetSFXVolume();
        source.pitch = entry.GetRandomPitch();
        source.spatialBlend = 1f;
        source.Play();
    }

    /// <summary>
    /// 播放 BGM（自动交叉淡入淡出）
    /// </summary>
    public void PlayBGM(BGM bgm, float fadeDuration = -1f)
    {
        if (bgm == currentBGM) return;
        currentBGM = bgm;

        if (fadeDuration < 0f)
            fadeDuration = config != null ? config.bgmCrossFadeDuration : 1.5f;

        if (bgmFadeCoroutine != null)
            StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(bgm, fadeDuration));
    }

    /// <summary>
    /// 切换房间环境音（自动淡入淡出）
    /// </summary>
    public void PlayAmbient(AmbientRoom room, float fadeDuration = -1f)
    {
        if (room == currentAmbient) return;
        currentAmbient = room;

        if (fadeDuration < 0f)
            fadeDuration = config != null ? config.ambientFadeDuration : 0.5f;

        if (ambientFadeCoroutine != null)
            StopCoroutine(ambientFadeCoroutine);
        ambientFadeCoroutine = StartCoroutine(FadeAmbient(room, fadeDuration));
    }

    /// <summary>
    /// 停止 BGM
    /// </summary>
    public void StopBGM(float fadeDuration = -1f)
    {
        PlayBGM(BGM.None, fadeDuration);
    }

    /// <summary>
    /// 停止环境音
    /// </summary>
    public void StopAmbient(float fadeDuration = -1f)
    {
        PlayAmbient(AmbientRoom.None, fadeDuration);
    }

    // ===== 音量控制 =====

    public void SetMasterVolume(float volume)
    {
        if (config != null) config.masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetBGMVolume(float volume)
    {
        if (config != null) config.bgmVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void SetSFXVolume(float volume)
    {
        if (config != null) config.sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetAmbientVolume(float volume)
    {
        if (config != null) config.ambientVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    private float GetMasterVolume() => config != null ? config.masterVolume : 1f;
    private float GetBGMVolume() => config != null ? config.bgmVolume * config.masterVolume : 0.7f;
    private float GetSFXVolume() => config != null ? config.sfxVolume * config.masterVolume : 1f;
    private float GetAmbientVolume() => config != null ? config.ambientVolume * config.masterVolume : 0.5f;

    private void ApplyVolumes()
    {
        if (activeBGMSource != null)
            activeBGMSource.volume = GetBGMVolume();
        if (ambientSource != null)
            ambientSource.volume = GetAmbientVolume();
    }

    // ===== 内部实现 =====

    private bool TryGetSFXEntry(SFX sfx, out SoundEntry entry)
    {
        entry = null;
        if (sfxLookup == null) BuildLookups();
        return sfxLookup != null && sfxLookup.TryGetValue(sfx, out entry);
    }

    private IEnumerator CrossFadeBGM(BGM bgm, float duration)
    {
        AudioSource fadeOutSource = activeBGMSource;
        AudioSource fadeInSource = (activeBGMSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        activeBGMSource = fadeInSource;

        // 设置新 BGM
        if (bgmLookup.TryGetValue(bgm, out var entry) && entry.clip != null)
        {
            fadeInSource.clip = entry.clip;
            fadeInSource.loop = entry.loop;
            fadeInSource.volume = 0f;
            fadeInSource.pitch = 1f;
            fadeInSource.Play();
        }
        else
        {
            // 没有对应 BGM，直接淡出
            fadeInSource = null;
        }

        float timer = 0f;
        float startFadeOutVol = fadeOutSource.volume;
        float targetBGMVol = GetBGMVolume();

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = timer / duration;

            fadeOutSource.volume = Mathf.Lerp(startFadeOutVol, 0f, t);
            if (fadeInSource != null)
                fadeInSource.volume = Mathf.Lerp(0f, targetBGMVol, t);

            yield return null;
        }

        fadeOutSource.Stop();
        fadeOutSource.volume = 0f;
        if (fadeInSource != null)
            fadeInSource.volume = targetBGMVol;
    }

    private IEnumerator FadeAmbient(AmbientRoom room, float duration)
    {
        float startVol = ambientSource.volume;
        float targetVol = GetAmbientVolume();

        // 淡出当前
        if (startVol > 0f)
        {
            float timer = 0f;
            while (timer < duration * 0.5f)
            {
                timer += Time.unscaledDeltaTime;
                ambientSource.volume = Mathf.Lerp(startVol, 0f, timer / (duration * 0.5f));
                yield return null;
            }
        }

        ambientSource.Stop();

        // 设置并淡入新环境音
        if (ambientLookup.TryGetValue(room, out var entry) && entry.clip != null)
        {
            ambientSource.clip = entry.clip;
            ambientSource.loop = true;
            ambientSource.volume = 0f;
            ambientSource.Play();

            float timer = 0f;
            while (timer < duration * 0.5f)
            {
                timer += Time.unscaledDeltaTime;
                ambientSource.volume = Mathf.Lerp(0f, targetVol, timer / (duration * 0.5f));
                yield return null;
            }
            ambientSource.volume = targetVol;
        }
    }
}
