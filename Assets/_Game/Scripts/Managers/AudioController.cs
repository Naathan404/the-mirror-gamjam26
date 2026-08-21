using Game.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioController : MonoSingleton<AudioController>
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string bgmVolumeParam = "BGMVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private List<AudioSource> _sfxSource;
    [SerializeField] private List<SoundSO> soundList;

    private Dictionary<SoundName, SoundSO> _sounds;

    public override void Awake()
    {
        base.Awake();
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _sounds = new Dictionary<SoundName, SoundSO>();
        foreach (var soundSO in soundList)
        {
            if (soundSO != null && !_sounds.ContainsKey(soundSO.soundName))
            {
                _sounds.Add(soundSO.soundName, soundSO);
            }
        }
    }

    public void PlayBGM(SoundName soundName)
    {
        if (_sounds.TryGetValue(soundName, out SoundSO soundData))
        {
            if (soundData.audioClips == null || soundData.audioClips.Count == 0) return;

            AudioClip clip = soundData.audioClips[Random.Range(0, soundData.audioClips.Count)]; // pick random clip
            _bgmSource.clip = clip;
            _bgmSource.volume = soundData.volume;
            _bgmSource.pitch = Random.Range(soundData.minPitch, soundData.maxPitch); // random pitch in range
            _bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        _bgmSource.Stop();
    }

    public void PlaySFX(SoundName soundName)
    {
        if (_sounds.TryGetValue(soundName, out SoundSO soundData))
        {
            if (soundData.audioClips == null || soundData.audioClips.Count == 0) return;

            AudioClip clip = soundData.audioClips[Random.Range(0, soundData.audioClips.Count)];
            AudioSource source = _sfxSource.Find(s => !s.isPlaying);
            if (source != null)
            {
                source.clip = clip;
                source.volume = soundData.volume;
                source.pitch = Random.Range(soundData.minPitch, soundData.maxPitch);
                source.Play();
            }
        }
    }

    public void SetMasterVolume(float sliderValue)
    {
        SetMixerVolume(masterVolumeParam, sliderValue);
    }

    public void SetBGMVolume(float sliderValue)
    {
        SetMixerVolume(bgmVolumeParam, sliderValue);
    }

    public void SetSFXVolume(float sliderValue)
    {
        SetMixerVolume(sfxVolumeParam, sliderValue);
    }

    private void SetMixerVolume(string paramName, float sliderValue)
    {
        float db = sliderValue > 0 ? Mathf.Log10(sliderValue) * 20f : -80f;
        audioMixer.SetFloat(paramName, db);
    }
}