using Game.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class AudioController : MonoSingleton<AudioController>
{
    private const float MIN_AUDIBLE_VOLUME = 0.0001f;
    private const float MUTED_DECIBELS = -80f;
    private const float DECIBEL_MULTIPLIER = 20f;

    [FormerlySerializedAs("audioMixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [FormerlySerializedAs("masterVolumeParam")]
    [SerializeField] private string _masterVolumeParam = "MasterVolume";

    [FormerlySerializedAs("bgmVolumeParam")]
    [SerializeField] private string _bgmVolumeParam = "BGMVolume";

    [FormerlySerializedAs("sfxVolumeParam")]
    [SerializeField] private string _sfxVolumeParam = "SFXVolume";

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private List<AudioSource> _sfxSource;

    [FormerlySerializedAs("soundList")]
    [SerializeField] private List<SoundSO> _soundList;

    private readonly Dictionary<SoundName, SoundSO> _sounds = new Dictionary<SoundName, SoundSO>();
    private bool _isMasterMuted;
    private bool _isBgmMuted;
    private bool _isSfxMuted;

    public override void Awake()
    {
        base.Awake();
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _sounds.Clear();

        if (_soundList == null)
        {
            return;
        }

        foreach (SoundSO soundSO in _soundList)
        {
            if (soundSO != null && !_sounds.ContainsKey(soundSO.soundName))
            {
                _sounds.Add(soundSO.soundName, soundSO);
            }
        }
    }

    public void PlayBGM(SoundName soundName)
    {
        if (_bgmSource == null)
        {
            return;
        }

        if (!_sounds.TryGetValue(soundName, out SoundSO soundData))
        {
            return;
        }

        if (soundData.audioClips == null || soundData.audioClips.Count == 0)
        {
            return;
        }

        AudioClip clip = soundData.audioClips[Random.Range(0, soundData.audioClips.Count)];
        _bgmSource.clip = clip;
        _bgmSource.volume = soundData.volume;
        _bgmSource.pitch = Random.Range(soundData.minPitch, soundData.maxPitch);
        ApplyBgmMuteState();
        _bgmSource.Play();
    }

    public void StopBGM()
    {
        if (_bgmSource != null)
        {
            _bgmSource.Stop();
        }
    }

    public void PlaySFX(SoundName soundName)
    {
        if (!_sounds.TryGetValue(soundName, out SoundSO soundData))
        {
            return;
        }

        if (soundData.audioClips == null || soundData.audioClips.Count == 0)
        {
            return;
        }

        AudioSource source = GetAvailableSfxSource();
        if (source == null)
        {
            return;
        }

        AudioClip clip = soundData.audioClips[Random.Range(0, soundData.audioClips.Count)];
        source.clip = clip;
        source.volume = soundData.volume;
        source.pitch = Random.Range(soundData.minPitch, soundData.maxPitch);
        source.mute = ShouldMuteSfx();
        source.Play();
    }

    public void SetMasterVolume(float sliderValue)
    {
        _isMasterMuted = IsMuted(sliderValue);
        SetMixerVolume(_masterVolumeParam, sliderValue);
        ApplyAudioSourceMuteStates();
    }

    public void SetBGMVolume(float sliderValue)
    {
        _isBgmMuted = IsMuted(sliderValue);
        SetMixerVolume(_bgmVolumeParam, sliderValue);
        ApplyBgmMuteState();
    }

    public void SetSFXVolume(float sliderValue)
    {
        _isSfxMuted = IsMuted(sliderValue);
        SetMixerVolume(_sfxVolumeParam, sliderValue);
        ApplySfxMuteState();
    }

    private AudioSource GetAvailableSfxSource()
    {
        if (_sfxSource == null)
        {
            return null;
        }

        for (int i = 0; i < _sfxSource.Count; i++)
        {
            AudioSource source = _sfxSource[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        return null;
    }

    private void ApplyAudioSourceMuteStates()
    {
        ApplyBgmMuteState();
        ApplySfxMuteState();
    }

    private void ApplyBgmMuteState()
    {
        if (_bgmSource != null)
        {
            _bgmSource.mute = ShouldMuteBgm();
        }
    }

    private void ApplySfxMuteState()
    {
        if (_sfxSource == null)
        {
            return;
        }

        bool shouldMuteSfx = ShouldMuteSfx();

        for (int i = 0; i < _sfxSource.Count; i++)
        {
            AudioSource source = _sfxSource[i];
            if (source != null)
            {
                source.mute = shouldMuteSfx;
            }
        }
    }

    private bool ShouldMuteBgm()
    {
        return _isMasterMuted || _isBgmMuted;
    }

    private bool ShouldMuteSfx()
    {
        return _isMasterMuted || _isSfxMuted;
    }

    private bool IsMuted(float sliderValue)
    {
        return sliderValue <= MIN_AUDIBLE_VOLUME;
    }

    private void SetMixerVolume(string paramName, float sliderValue)
    {
        if (_audioMixer == null)
        {
            return;
        }

        float clampedValue = Mathf.Clamp01(sliderValue);
        float db = IsMuted(clampedValue) ? MUTED_DECIBELS : Mathf.Log10(clampedValue) * DECIBEL_MULTIPLIER;
        _audioMixer.SetFloat(paramName, db);
    }
}