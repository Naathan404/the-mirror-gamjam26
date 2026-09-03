using Game.Effect;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.Rendering;

namespace Game.UI
{
    public class SettingsController : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Effect Setting")]
        [SerializeField] private Slider _blurSlider;
        [SerializeField] private Slider _scanlineSlider;
        [SerializeField] private Slider _distortionSlider;
        [SerializeField] private Material _crtMat;
        [SerializeField] private Volume _volume;
        private LensDistortion _lensDistortion;
        private static readonly int BLUR_ID = Shader.PropertyToID("_Blur_Offset");
        private static readonly int SCANLINE_ID = Shader.PropertyToID("_Number_Of_Scan_Lines");

        [Header("Reset Buttons")]
        [SerializeField] private Button _resetSetting;

        [Header("Language Buttons")]
        [SerializeField] private Button _btnEnglish;
        [SerializeField] private UIButton _uiBtnEnglish;

        [Space(10)]
        [SerializeField] private Button _btnVietnamese;
        [SerializeField] private UIButton _uiBtnVietnamese; // Script hiệu ứng của nút VN
        [Header("Gameplay Settings")]
        [SerializeField] private Toggle _tutorialToggle;

        private int _currentLanguageId = 0;

        private void OnEnable()
        {
            // Cập nhật giá trị lên Slider UI
            float masterVol = PlayerPrefs.GetFloat("MasterVol", 1f);
            float bgmVol = PlayerPrefs.GetFloat("BGMVol", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVol", 1f);
            float blurVal = PlayerPrefs.GetFloat("BLURVal", 0.0015f);
            int scanline = PlayerPrefs.GetInt("SCANLINEVal", 400);
            float distortion = PlayerPrefs.GetFloat("DISTORTIONVal", 0.3f);

            if (_masterSlider != null) _masterSlider.SetValueWithoutNotify(masterVol);
            if (_bgmSlider != null) _bgmSlider.SetValueWithoutNotify(bgmVol);
            if (_sfxSlider != null) _sfxSlider.SetValueWithoutNotify(sfxVol);

            if (_blurSlider != null) _blurSlider.SetValueWithoutNotify(blurVal);
            if (_scanlineSlider != null) _scanlineSlider.SetValueWithoutNotify(scanline);
            if (_distortionSlider != null) _distortionSlider.SetValueWithoutNotify(distortion);

            if (_volume != null && _volume.profile.TryGet(out _lensDistortion))
            {
                _lensDistortion.intensity.overrideState = true;
            }
            else
            {
                Debug.LogError("Chưa gán Volume hoặc chưa thêm Lens Distortion vào Volume Profile!");
            }

            SetMasterVolume(masterVol);
            SetBGMVolume(bgmVol);
            SetSFXVolume(sfxVol);
            SetBlurValue(blurVal);
            SetScanlineValue(scanline);
            SetDistortionValue(distortion);

            _currentLanguageId = PlayerPrefs.GetInt("Language", 0);
            UpdateLanguageUI();

            if (_tutorialToggle != null)
            {
                _tutorialToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("TutorialEnabled", 1) == 1);
            }
        }

        private void Start()
        {
            if (_masterSlider != null) _masterSlider.onValueChanged.AddListener(SetMasterVolume);
            if (_bgmSlider != null) _bgmSlider.onValueChanged.AddListener(SetBGMVolume);
            if (_sfxSlider != null) _sfxSlider.onValueChanged.AddListener(SetSFXVolume);

            if (_blurSlider != null) _blurSlider.onValueChanged.AddListener(SetBlurValue);
            if (_scanlineSlider != null) _scanlineSlider.onValueChanged.AddListener(SetScanlineValue);
            if (_distortionSlider != null) _distortionSlider.onValueChanged.AddListener(SetDistortionValue);

            if (_resetSetting != null) _resetSetting.onClick.AddListener(ResetSetting);

            if (_btnEnglish != null) _btnEnglish.onClick.AddListener(() => SetLanguage(0));
            if (_btnVietnamese != null) _btnVietnamese.onClick.AddListener(() => SetLanguage(1));

            if (_tutorialToggle != null)
                _tutorialToggle.onValueChanged.AddListener(SetTutorialState);
        }

        private void ResetSetting()
        {
            SetMasterVolume(1f);
            SetBGMVolume(1f);
            SetSFXVolume(1f);
            SetBlurValue(0.0015f);
            SetScanlineValue(400);
            SetDistortionValue(0.3f);

            if (_masterSlider != null) _masterSlider.value = 1f;
            if (_bgmSlider != null) _bgmSlider.value = 1f;
            if (_sfxSlider != null) _sfxSlider.value = 1f;

            if (_blurSlider != null) _blurSlider.value = 0.0015f;
            if (_scanlineSlider != null) _scanlineSlider.value = 400;
            if (_distortionSlider != null) _distortionSlider.value = 0.3f;
        }

        public void CloseSettings()
        {
            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            if (Managers.UIGameplayManager.Instance != null)
            {
                Managers.UIGameplayManager.Instance.CloseSettings();
            }
            else
            {
                Menu.MenuController menuController = UnityEngine.Object.FindAnyObjectByType<Menu.MenuController>();

                if (menuController != null)
                {
                    menuController.OnCloseSettings();
                }
                else
                {
                    gameObject.SetActive(false);
                    Time.timeScale = 1f;
                }
            }
        }
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

        private void SetBlurValue(float value)
        {
            value = Mathf.Clamp(value, 0f, 0.05f);
            _crtMat.SetFloat(BLUR_ID, value);
            PlayerPrefs.SetFloat("BLURVal", value);
        }

        private void SetScanlineValue(float value)
        {
            int normalized = Mathf.RoundToInt(value);
            normalized = Mathf.Clamp(normalized, 0, 1000);
            _crtMat.SetInt(SCANLINE_ID, normalized);
            PlayerPrefs.SetInt("SCANLINEVal", normalized);
        }

        private void SetDistortionValue(float value)
        {
            _lensDistortion.intensity.value = Mathf.Clamp(value, 0.1f, 0.6f);
            PlayerPrefs.SetFloat("DISTORTIONVal", value);
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

            // Tắt chức năng click của nút đang được chọn
            if (_btnEnglish != null) _btnEnglish.interactable = !isEnglish;
            if (_btnVietnamese != null) _btnVietnamese.interactable = isEnglish;

            // Kích hoạt hiệu ứng mờ/nhỏ cho nút đang được chọn
            if (_uiBtnEnglish != null) _uiBtnEnglish.SetLockedState(isEnglish);
            if (_uiBtnVietnamese != null) _uiBtnVietnamese.SetLockedState(!isEnglish);
        }

        private System.Collections.IEnumerator BlinkAndChangeLanguage(int localeID)
        {
            float blinkDuration = 0.25f;

            // 1. NHẮM MẮT LẠI
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, blinkDuration);
            }

            yield return new WaitForSeconds(blinkDuration);

            // 2. CẬP NHẬT UI TRONG LÚC ĐEN MÀN
            UpdateLanguageUI();

            yield return LocalizationSettings.InitializationOperation;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];

            yield return new WaitForSeconds(0.5f);

            // 3. MỞ MẮT RA
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeOpenedVignetteEffect(blinkDuration);
            }
        }

        private void SetTutorialState(bool isOn)
        {
            // Lưu xuống hệ thống: 1 là Bật, 0 là Tắt
            PlayerPrefs.SetInt("TutorialEnabled", isOn ? 1 : 0);
            PlayerPrefs.Save();

            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);
        }
    }
}