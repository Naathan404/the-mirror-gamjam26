using System.Collections;
using Game.Managers; // Gọi AudioController
using Game.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Game.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private SoundName _soundName = SoundName.BGM_Gameplay_1;
        [SerializeField] private bool _playBgmOnAwake = false;
        [SerializeField] private SettingsController _settingController;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ClearStaleEventListeners()
        {
            GameEvents.ClearAllListeners();
        }

        private void Start()
        {
            if (_settingController != null)
                _settingController.WarmUp();
            int savedLang = PlayerPrefs.GetInt("Language", 0);
            StartCoroutine(InitLanguage(savedLang));

            if (AudioController.Instance != null)
            {
                AudioController.Instance.SetMasterVolume(PlayerPrefs.GetFloat("MasterVol", 1f));
                AudioController.Instance.SetBGMVolume(PlayerPrefs.GetFloat("BGMVol", 1f));
                AudioController.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVol", 1f));

                if (_playBgmOnAwake)
                {
                    AudioController.Instance.PlayBGM(_soundName);
                }
            }
        }

        private IEnumerator InitLanguage(int localeID)
        {
            yield return LocalizationSettings.InitializationOperation;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        }
    }
}