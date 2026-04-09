using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Audio;

public class DE_SoundManager : MonoBehaviour
{
    public static DE_SoundManager soundManager{get; private set;}
    public AudioMixer mixer;

    [Header("#BGM")]
    [SerializeField] private AudioClip[] bgmSound;
    [SerializeField, Range(0f, 1f)] private float[] bgmVolumes;
    public int bgmChannel = 2;
    AudioSource[] bgmPLayer;
    int bgmChannelIndex;
    int currentBGMIndex = -1;
    Coroutine bgmCoroutine;

    [Header("#SFX")]
    [SerializeField] private AudioClip[] sfxSounds;
    [SerializeField, Range(0f, 1f)] private float[] sfxVolumes;
    public int channels;
    AudioSource[] sfxPLayer; 
    int SFXchannelIndex;

    [Header("Inspector Guide (Read Only)")]
    [SerializeField, TextArea(4, 12)] private string bgmIndexGuide;
    [SerializeField, TextArea(10, 40)] private string sfxIndexGuide;
    [SerializeField] private bool autoResizeArraysToEnumCount = false;

    [Header("오디오 믹서 연결")]
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    //사운드 추가하고 넣기
    public enum bgm {} 
    public enum sfx {}

    void Awake()
    {
        if(soundManager == null)
        {
            soundManager = this;
            DontDestroyOnLoad(gameObject);
            Init();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        MixerSetMasterVolume(1f);
        MixerSetBGMVolume(1f);
        MixerSetSFXVolume(1f);
    }

    void Init()
    {
        //배경음 초기화
        GameObject bgmObject = new GameObject("BGM");
        bgmObject.transform.parent = transform;
        bgmPLayer = new AudioSource[bgmChannel];

        for (int i=0; i<bgmPLayer.Length; i++)
        {
            bgmPLayer[i] = bgmObject.AddComponent<AudioSource>();
            bgmPLayer[i].outputAudioMixerGroup = bgmGroup;
            bgmPLayer[i].playOnAwake = false;
            bgmPLayer[i].loop = true;
            bgmPLayer[i].priority = 0;
        }

        //효과음 초기화
        GameObject sfxobject = new GameObject("SFX");
        sfxobject.transform.parent = transform;
        sfxPLayer = new AudioSource[channels];

        for (int i=0; i<sfxPLayer.Length; i++)
        {
            sfxPLayer[i] = sfxobject.AddComponent<AudioSource>();
            sfxPLayer[i].outputAudioMixerGroup = sfxGroup;
            sfxPLayer[i].playOnAwake = false;
            sfxPLayer[i].priority = 128;
        }
    }

    private float GetBgmVolume(bgm type)
    {
        int index = (int)type;
        if (bgmVolumes == null || index < 0 || index >= bgmVolumes.Length)
            return 1f;
        return bgmVolumes[index];
    }

    private float GetSfxVolume(sfx type)
    {
        int index = (int)type;
        if (sfxVolumes == null || index < 0 || index >= sfxVolumes.Length)
            return 1f;
        return sfxVolumes[index];
    }

    public void PlayBGM(bgm bgm, float fadeTime = 1.5f)
    {
        if (bgmSound.Length <= (int)bgm) return;

        if (currentBGMIndex >= 0 &&
            bgmPLayer[currentBGMIndex].clip == bgmSound[(int)bgm])
        {
            return;
        }

        float targetVolume = GetBgmVolume(bgm);

        int nextIndex = (currentBGMIndex + 1) % bgmPLayer.Length;
        AudioSource next = bgmPLayer[nextIndex];
        AudioSource current = currentBGMIndex >= 0 ? bgmPLayer[currentBGMIndex] : null;

        next.clip = bgmSound[(int)bgm];

        if (current == null)
        {
            next.volume = targetVolume;
            next.Play();
            currentBGMIndex = nextIndex;
            return;
        }

        next.volume = 0f;
        next.Play();

        if (bgmCoroutine != null)
            StopCoroutine(bgmCoroutine);

        bgmCoroutine = StartCoroutine(CrossFade(current, next, fadeTime, targetVolume));
        currentBGMIndex = nextIndex;
    }
    
    public void PlaySFX(sfx sfx)
    {
        float volume = GetSfxVolume(sfx);

        for (int i = 0; i < sfxPLayer.Length; i++)
        {
            int loopIndex = (i + SFXchannelIndex) % sfxPLayer.Length;
            if (sfxPLayer[loopIndex].isPlaying) continue;

            SFXchannelIndex = (loopIndex + 1) % sfxPLayer.Length;
            sfxPLayer[loopIndex].clip = sfxSounds[(int)sfx];
            sfxPLayer[loopIndex].volume = volume;
            sfxPLayer[loopIndex].Play();
            break;
        }
    }

    IEnumerator CrossFade(AudioSource from, AudioSource to, float duration, float targetVolume = 1f)
    {
        if (duration <= 0f)
        {
            if (from != null) { from.Stop(); from.volume = 0f; }
            if (to != null) to.volume = targetVolume;
            yield break;
        }

        float fromVolume = from != null ? from.volume : 1f;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            if (to != null)   to.volume   = Mathf.Lerp(0f, targetVolume, t);
            if (from != null) from.volume = Mathf.Lerp(fromVolume, 0f, t);

            yield return null;
        }

        if (from != null) { from.Stop(); from.volume = 0f; }
        if (to != null)   to.volume = targetVolume;
    }

    public void MixerSetMasterVolume(float value)
    {
        mixer.SetFloat("Master", ConvertToDb(value));
    }

    public void MixerSetSFXVolume(float value)
    {
        mixer.SetFloat("SFX", ConvertToDb(value));
    }

    public void MixerSetBGMVolume(float value)
    {
        mixer.SetFloat("BGM", ConvertToDb(value));
    }

    float ConvertToDb(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20;
    }

    public int GetBgmEnumCount() => Enum.GetNames(typeof(bgm)).Length;
    public int GetSfxEnumCount() => Enum.GetNames(typeof(sfx)).Length;

    private void OnValidate()
    {
        bgmChannel = Mathf.Max(1, bgmChannel);
        channels   = Mathf.Max(1, channels);

        UpdateInspectorGuides();

        if (!autoResizeArraysToEnumCount) return;

        ResizeArraysToEnumCounts();
        autoResizeArraysToEnumCount = false;
    }

    private void UpdateInspectorGuides()
    {
        bgmIndexGuide = BuildEnumGuideText<bgm>(bgmSound != null ? bgmSound.Length : 0);
        sfxIndexGuide = BuildEnumGuideText<sfx>(sfxSounds != null ? sfxSounds.Length : 0);
    }

    private void ResizeArraysToEnumCounts()
    {
        ResizeAudioClipArray(ref bgmSound,  GetBgmEnumCount());
        ResizeFloatArray    (ref bgmVolumes, GetBgmEnumCount());
        ResizeAudioClipArray(ref sfxSounds,  GetSfxEnumCount());
        ResizeFloatArray    (ref sfxVolumes, GetSfxEnumCount());
        UpdateInspectorGuides();
    }

    private static void ResizeAudioClipArray(ref AudioClip[] array, int newSize)
    {
        if (newSize < 0) newSize = 0;
        AudioClip[] previous = array;
        AudioClip[] resized  = new AudioClip[newSize];
        if (previous != null)
            Array.Copy(previous, resized, Mathf.Min(previous.Length, resized.Length));
        array = resized;
    }

    private static void ResizeFloatArray(ref float[] array, int newSize, float defaultValue = 1f)
    {
        if (newSize < 0) newSize = 0;
        float[] previous = array;
        float[] resized  = new float[newSize];
        for (int i = 0; i < resized.Length; i++)
            resized[i] = defaultValue;
        if (previous != null)
            Array.Copy(previous, resized, Mathf.Min(previous.Length, resized.Length));
        array = resized;
    }

    private static string BuildEnumGuideText<TEnum>(int currentArraySize) where TEnum : Enum
    {
        string[] names = Enum.GetNames(typeof(TEnum));
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.AppendLine($"Array Size: {currentArraySize} / Enum Count: {names.Length}");
        for (int i = 0; i < names.Length; i++)
            builder.AppendLine($"{i} : {names[i]}");
        return builder.ToString();
    }
}