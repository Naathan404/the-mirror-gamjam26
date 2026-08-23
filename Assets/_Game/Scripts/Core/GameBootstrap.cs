using System.Collections;
using Game.Managers; // Gọi AudioController
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Game.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            GameEvents.ClearAllListeners();
        }

        private void Start()
        {
            int savedLang = PlayerPrefs.GetInt("Language", 0);
            StartCoroutine(InitLanguage(savedLang));

            if (AudioController.Instance != null)
            {
                AudioController.Instance.SetMasterVolume(PlayerPrefs.GetFloat("MasterVol", 1f));
                AudioController.Instance.SetBGMVolume(PlayerPrefs.GetFloat("BGMVol", 1f));
                AudioController.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVol", 1f));
            }
        }

        private IEnumerator InitLanguage(int localeID)
        {
            yield return LocalizationSettings.InitializationOperation;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        }
    }
}