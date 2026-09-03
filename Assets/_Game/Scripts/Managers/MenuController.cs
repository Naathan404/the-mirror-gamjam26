using DG.Tweening;
using Game.Effect;
using Game.Managers;
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Menu
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private CanvasGroup _menuCanvasGroup;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _tutorialPanel;
        [SerializeField] private SettingsController _settingController;

        [Header("Cinematic Elements")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _mirrorTarget;
        [SerializeField] private SpriteRenderer _shadowSprite;
        [SerializeField] private Sprite _humanShadow;
        [SerializeField] private Sprite _monsterShadow;

        [Header("Timing Configs")]
        [Tooltip("Thời gian hiệu ứng nhắm mắt kéo xuống")]
        [SerializeField] private float _eyeCloseDuration = 1f;

        private bool _isStarting = false;
        private Sequence _glitchSequence;

        private void Start()
        {
            _settingsPanel?.SetActive(false);
            _tutorialPanel?.SetActive(false);
            _settingController.WarmUp();
            SetShadow(false, 0.4f);
        }

        private void OnDestroy()
        {
            _glitchSequence?.Kill();
        }

        public void OnClickFaceYourself()
        {
            if (_isStarting) return;
            _isStarting = true;

            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            if (_settingsPanel != null) _settingsPanel.SetActive(false);
            if (_tutorialPanel != null) _tutorialPanel.SetActive(false);

            Sequence seq = DOTween.Sequence();

            seq.InsertCallback(0f, () =>
            {
                _menuCanvasGroup.interactable = false;
                _menuCanvasGroup.DOFade(0f, 0.5f);
                AudioController.Instance.PlayBGM(SoundName.Menu_Whisper);
            });

            // --- NHỊP CHỚP 1 ---
            seq.InsertCallback(1.0f, () => CloseEyes(0.15f));
            seq.InsertCallback(1.2f, () => { SetShadow(true, 0.5f); OpenEyes(0.1f); });

            // --- NHỊP CHỚP 2 ---
            seq.InsertCallback(1.4f, () => CloseEyes(0.1f));
            seq.InsertCallback(1.5f, () => { SetShadow(false, 0.4f); OpenEyes(0.2f); });

            // --- NHỊP CHỚP 3 ---
            seq.InsertCallback(3.0f, () => CloseEyes(0.15f));
            seq.InsertCallback(3.2f, () => { SetShadow(true, 0.6f); OpenEyes(0.1f); });

            // --- NHỊP CHỚP 4 ---
            seq.InsertCallback(3.3f, () => CloseEyes(0.1f));
            seq.InsertCallback(3.4f, () => { SetShadow(false, 0.4f); OpenEyes(0.3f); });

            // --- NHỊP CHỚP 5 (CÚ CHỐT) ---
            seq.InsertCallback(4.8f, () => CloseEyes(0.2f));
            seq.InsertCallback(5.0f, () =>
            {
                SetShadow(true, 0.85f);
                _shadowSprite.transform.DOShakePosition(1.5f, 0.05f, 20);

                AudioController.Instance.StopBGM();
                AudioController.Instance.PlaySFX(SoundName.Menu_Glitch);

                OpenEyes(0.2f);

                if (_cameraTransform != null && _mirrorTarget != null)
                    _cameraTransform.DORotateQuaternion(_mirrorTarget.rotation, 1.0f).SetEase(Ease.InOutSine);
            });

            seq.InsertCallback(6.1f, () => SceneController.Instance.LoadGameplayScene(0.15f));
        }

        private void CloseEyes(float duration)
        {
            if (FilterController.Instance != null)
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, duration);
        }

        private void OpenEyes(float duration)
        {
            if (FilterController.Instance != null)
                FilterController.Instance.PlayEyeOpenedVignetteEffect(duration);
        }

        private void SetShadow(bool isMonster, float alpha)
        {
            if (_shadowSprite != null)
            {
                _shadowSprite.sprite = isMonster ? _monsterShadow : _humanShadow;
                _shadowSprite.color = new Color(0f, 0f, 0f, alpha);
            }
        }

        public void OnClickSettings()
        {
            if (_isStarting || _settingsPanel == null) return;
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            // Tắt bảng Tutorial (Nếu đang mở) để tránh đè lên nhau
            if (_tutorialPanel != null) _tutorialPanel.SetActive(false);

            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
            UpdateMainMenuVisibility();
        }

        public void OnCloseSettings()
        {
            if (_isStarting || _settingsPanel == null) return;
            _settingsPanel.SetActive(false);
            UpdateMainMenuVisibility();
        }
        public void OnClickTutorial()
        {
            if (_isStarting || _tutorialPanel == null) return;
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            // Tắt bảng Setting (Nếu đang mở) để tránh đè lên nhau
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            _tutorialPanel.SetActive(!_tutorialPanel.activeSelf);
            UpdateMainMenuVisibility();
        }

        public void OnCloseTutorial()
        {
            if (_isStarting || _tutorialPanel == null) return;
            _tutorialPanel.SetActive(false);
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            UpdateMainMenuVisibility();
        }

        private void UpdateMainMenuVisibility()
        {
            if (_menuCanvasGroup == null) return;

            bool isAnyPanelOpen = (_settingsPanel != null && _settingsPanel.activeSelf) ||
                                  (_tutorialPanel != null && _tutorialPanel.activeSelf);

            if (isAnyPanelOpen)
            {
                _menuCanvasGroup.alpha = 0f;
                _menuCanvasGroup.interactable = false;
                _menuCanvasGroup.blocksRaycasts = false;
            }
            else
            {
                _menuCanvasGroup.alpha = 1f;
                _menuCanvasGroup.interactable = true;
                _menuCanvasGroup.blocksRaycasts = true;
            }
        }

        public void OnClickQuit()
        {
            if (_isStarting) return;
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            if (FilterController.Instance != null)
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, _eyeCloseDuration);

            DOVirtual.DelayedCall(_eyeCloseDuration + 0.5f, () => Application.Quit());
        }
    }
}