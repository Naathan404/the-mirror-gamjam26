using DG.Tweening;
using Game.Effect;
using Game.Managers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Menu
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private CanvasGroup _menuCanvasGroup;
        [SerializeField] private GameObject _settingsPanel;

        [Header("Cinematic Elements")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _mirrorTarget;
        [SerializeField] private SpriteRenderer _shadowSprite;
        [SerializeField] private Sprite _humanShadow;
        [SerializeField] private Sprite _monsterShadow;

        [Header("Timing Configs")]
        [Tooltip("Thời gian hiệu ứng nhắm mắt kéo xuống")]
        [SerializeField] private float _eyeCloseDuration = 1f;

        [Header("Scene Settings")]
        [SerializeField] private string _gameplaySceneName = "GameplayScene";

        private bool _isStarting = false;
        private Sequence _glitchSequence;

        private void Start()
        {
            SetShadow(false, 0.4f); // Khởi đầu là bóng người, mờ 0.4
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

            // ==============================================================
            // TIMELINE KỊCH BẢN 8 GIÂY (Phục vụ file Whisper dài 7 giây)
            // ==============================================================
            Sequence seq = DOTween.Sequence();

            // 0.0s: Bắt đầu mờ UI và phát tiếng xì xào 7 giây
            seq.InsertCallback(0f, () =>
            {
                _menuCanvasGroup.interactable = false;
                _menuCanvasGroup.DOFade(0f, 0.5f);
                AudioController.Instance.PlayBGM(SoundName.Menu_Whisper);
            });

            // --- NHỊP CHỚP 1 ---
            seq.InsertCallback(1.0f, () => CloseEyes(0.15f));
            seq.InsertCallback(1.2f, () => { SetShadow(true, 0.5f); OpenEyes(0.1f); }); // Thấy quái vật xẹt qua

            // --- NHỊP CHỚP 2 ---
            seq.InsertCallback(1.4f, () => CloseEyes(0.1f));
            seq.InsertCallback(1.5f, () => { SetShadow(false, 0.4f); OpenEyes(0.2f); }); // Nhìn lại thì là bóng người

            // --- NHỊP CHỚP 3 (Khoảng lặng hoang mang) ---
            seq.InsertCallback(3.0f, () => CloseEyes(0.15f));
            seq.InsertCallback(3.2f, () => { SetShadow(true, 0.6f); OpenEyes(0.1f); }); // Quái vật lại xuất hiện

            // --- NHỊP CHỚP 4 ---
            seq.InsertCallback(3.3f, () => CloseEyes(0.1f));
            seq.InsertCallback(3.4f, () => { SetShadow(false, 0.4f); OpenEyes(0.3f); }); // Trở về bình thường

            // --- NHỊP CHỚP 5 (CÚ CHỐT) ---
            seq.InsertCallback(4.8f, () => CloseEyes(0.2f));
            seq.InsertCallback(5.0f, () =>
            {
                SetShadow(true, 0.85f);
                _shadowSprite.transform.DOShakePosition(1.5f, 0.05f, 20);

                AudioController.Instance.StopBGM();
                AudioController.Instance.PlaySFX(SoundName.Menu_Glitch);

                OpenEyes(0.2f);

                // Cùng lúc đó, Camera ngẩng lên gương (mất 1.0 giây)
                if (_cameraTransform != null && _mirrorTarget != null)
                    _cameraTransform.DORotateQuaternion(_mirrorTarget.rotation, 1.0f).SetEase(Ease.InOutSine);
            });

            // Ở giây 6.0 là camera vừa ngẩng lên tới gương.
            // Để người chơi nhìn thấy hình phản chiếu trống rỗng trong tích tắc (0.1 giây), rồi sập màn hình tối đen cái "rụp" cực nhanh (0.15 giây) và ném vào game luôn.
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

        // ==========================================

        public void OnClickSettings()
        {
            if (_isStarting || _settingsPanel == null) return;
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
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