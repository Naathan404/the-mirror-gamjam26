using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Managers; // Gọi AudioController
using UnityEngine.Localization.Settings;

namespace Game.UI
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Language Settings")]
        [SerializeField] private Button _languageButton;
        [SerializeField] private TextMeshProUGUI _languageText;

        private int _currentLanguageId = 0;
        private string[] _languages = {"English", "Tiếng Việt" };

        private void Start()
        {
            // Trả lại giá trị 1f (tương đương 100% max volume của hệ thống 0->1)
            float masterVol = PlayerPrefs.GetFloat("MasterVol", 1f);
            float bgmVol = PlayerPrefs.GetFloat("BGMVol", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);

            // Cập nhật giá trị lên UI
            if (_masterSlider != null) _masterSlider.value = masterVol;
            if (_bgmSlider != null) _bgmSlider.value = bgmVol;
            if (_sfxSlider != null) _sfxSlider.value = sfxVol;

            // Tải ngôn ngữ đã lưu
            _currentLanguageId = PlayerPrefs.GetInt("Language", 0);
            UpdateLanguageUI();

            // ĐĂNG KÝ SỰ KIỆN KHI KÉO SLIDER
            if (_masterSlider != null)
                _masterSlider.onValueChanged.AddListener(SetMasterVolume);

            if (_bgmSlider != null)
                _bgmSlider.onValueChanged.AddListener(SetBGMVolume);

            if (_sfxSlider != null)
                _sfxSlider.onValueChanged.AddListener(SetSFXVolume);

            if (_languageButton != null)
                _languageButton.onClick.AddListener(ToggleLanguage);

            ApplySavedVolumes();
        }

        public void CloseSettings()
        {
            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            gameObject.SetActive(false);
        }

        // ==========================================
        // XỬ LÝ ÂM THANH (Truyền giá trị 0.0 -> 1.0 cho AudioController)
        // ==========================================
        private void SetMasterVolume(float value)
        {
            if (AudioController.Instance != null) AudioController.Instance.SetMasterVolume(value);
            PlayerPrefs.SetFloat("MasterVol", value);
        }

        private void SetBGMVolume(float value)
        {
            if (AudioController.Instance != null) AudioController.Instance.SetBGMVolume(value);
            PlayerPrefs.SetFloat("BGMVol", value);
        }

        private void SetSFXVolume(float value)
        {
            if (AudioController.Instance != null) AudioController.Instance.SetSFXVolume(value);
            PlayerPrefs.SetFloat("SFXVol", value);
        }

        private void ApplySavedVolumes()
        {
            if (AudioController.Instance != null)
            {
                AudioController.Instance.SetMasterVolume(PlayerPrefs.GetFloat("MasterVol", 1f));
                AudioController.Instance.SetBGMVolume(PlayerPrefs.GetFloat("BGMVol", 1f));
                AudioController.Instance.SetSFXVolume(PlayerPrefs.GetFloat("SFXVol", 1f));
            }
        }

        // ==========================================
        // XỬ LÝ NGÔN NGỮ (Tích hợp Unity Localization)
        // ==========================================
        private void ToggleLanguage()
        {
            _currentLanguageId = (_currentLanguageId + 1) % _languages.Length;
            PlayerPrefs.SetInt("Language", _currentLanguageId);

            UpdateLanguageUI();

            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            // [LỆNH MỚI]: Đổi ngôn ngữ của Unity Localization Package
            StartCoroutine(ChangeLocale(_currentLanguageId));
        }

        private void UpdateLanguageUI()
        {
            if (_languageText != null)
            {
                _languageText.text = _languages[_currentLanguageId];
            }
        }

        // Coroutine chờ đổi ngôn ngữ (Vì Unity Localization khởi tạo bất đồng bộ)
        private System.Collections.IEnumerator ChangeLocale(int localeID)
        {
            yield return LocalizationSettings.InitializationOperation;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        }
    }
}