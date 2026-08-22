using Game.Effect;
using Game.Managers;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace Game.UI
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Language Buttons")]
        [SerializeField] private Button _btnEnglish;
        [SerializeField] private UIButton _uiBtnEnglish; // Script hiệu ứng của nút EN

        [Space(10)]
        [SerializeField] private Button _btnVietnamese;
        [SerializeField] private UIButton _uiBtnVietnamese; // Script hiệu ứng của nút VN

        // 0 = English, 1 = Tiếng Việt (Khớp với mảng của Hưn)
        private int _currentLanguageId = 0;

        private void Start()
        {
            float masterVol = PlayerPrefs.GetFloat("MasterVol", 1f);
            float bgmVol = PlayerPrefs.GetFloat("BGMVol", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);

            if (_masterSlider != null) _masterSlider.value = masterVol;
            if (_bgmSlider != null) _bgmSlider.value = bgmVol;
            if (_sfxSlider != null) _sfxSlider.value = sfxVol;

            // ĐĂNG KÝ SỰ KIỆN SLIDER
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(SetSFXVolume);

            // ĐĂNG KÝ SỰ KIỆN 2 NÚT NGÔN NGỮ
            if (_btnEnglish != null) _btnEnglish.onClick.AddListener(() => SetLanguage(0));
            if (_btnVietnamese != null) _btnVietnamese.onClick.AddListener(() => SetLanguage(1));

            // Tải ngôn ngữ đã lưu & Khởi tạo UI
            _currentLanguageId = PlayerPrefs.GetInt("Language", 0);
            UpdateLanguageUI();
            StartCoroutine(BlinkAndChangeLanguage(_currentLanguageId));

            ApplySavedVolumes();
        }

        public void CloseSettings()
        {
            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            gameObject.SetActive(false);
        }

        // ==========================================
        // XỬ LÝ ÂM THANH
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

        private void SetLanguage(int languageId)
        {
            if (_currentLanguageId == languageId) return;

            _currentLanguageId = languageId;
            PlayerPrefs.SetInt("Language", _currentLanguageId);
            PlayerPrefs.Save();

            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            StartCoroutine(BlinkAndChangeLanguage(_currentLanguageId));
        }

        private void UpdateLanguageUI()
        {
            bool isEnglish = (_currentLanguageId == 0);

            // 1. Tắt chức năng click của nút đang được chọn (để tránh spam click)
            if (_btnEnglish != null) _btnEnglish.interactable = !isEnglish;
            if (_btnVietnamese != null) _btnVietnamese.interactable = isEnglish;

            // 2. Kích hoạt hiệu ứng mờ/nhỏ cho nút đang được chọn
            if (_uiBtnEnglish != null) _uiBtnEnglish.SetLockedState(isEnglish);
            if (_uiBtnVietnamese != null) _uiBtnVietnamese.SetLockedState(!isEnglish);
        }

        private System.Collections.IEnumerator BlinkAndChangeLanguage(int localeID)
        {
            float blinkDuration = 0.25f; 

            // 1. NHẮM MẮT LẠI (Màn hình tối đi)
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, blinkDuration);
            }

            yield return new WaitForSeconds(blinkDuration);

            // 2. BẮT ĐẦU ĐỔI CHỮ (TRONG LÚC MÀN HÌNH ĐANG ĐEN)
            UpdateLanguageUI();

            yield return LocalizationSettings.InitializationOperation;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];

            yield return new WaitForSeconds(0.5f);

            // 3. MỞ MẮT RA (Chữ đã được đổi xong hoàn hảo)
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeOpenedVignetteEffect(blinkDuration);
            }
        }
    }
}