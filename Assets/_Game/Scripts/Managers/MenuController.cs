using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.SceneManagement;
using Game.Effect; // BẮT BUỘC PHẢI CÓ ĐỂ GỌI FILTERCONTROLLER

namespace Game.Menu
{
    public class MenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private CanvasGroup _menuCanvasGroup;
        [SerializeField] private GameObject _settingsPanel;

        // ĐÃ XÓA _fadeOverlay VÌ SẼ DÙNG FILTERCONTROLLER

        [Header("Cinematic Elements")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _mirrorTarget;
        [SerializeField] private SpriteRenderer _shadowSprite;
        [SerializeField] private Sprite _humanShadow;
        [SerializeField] private Sprite _monsterShadow;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _whisperSound;
        [SerializeField] private AudioClip _glitchSound; // Thêm tiếng xẹt điện/nhiễu sóng

        [Header("Timing Configs")]
        [SerializeField] private float _shadowShakeDuration = 0.8f; // Kéo dài thời gian run rẩy để diễn Glitch
        [SerializeField] private float _monsterFlashDuration = 0.15f;
        [SerializeField] private float _cameraPanDuration = 1.5f;
        [Tooltip("Thời gian hiệu ứng nhắm mắt kéo xuống")]
        [SerializeField] private float _eyeCloseDuration = 1f;

        [Header("Scene Settings")]
        [SerializeField] private string _gameplaySceneName = "GameplayScene";

        private bool _isStarting = false;
        private Sequence _glitchSequence; // Lưu trữ Sequence để dọn dẹp nếu cần

        private void Start()
        {
            if (_shadowSprite != null && _humanShadow != null)
            {
                _shadowSprite.sprite = _humanShadow;
                _shadowSprite.color = new Color(0f, 0f, 0f, 0.4f);
            }

            // Gọi hiệu ứng Mở Mắt lúc mới load Menu Scene
            if (FilterController.Instance != null)
            {
                // Giả sử FilterController của Hưn có hàm PlayEyeOpened hoặc dùng Vignette ngược lại
                // FilterController.Instance.PlayEyeOpenedVignetteEffect(Color.black, 1.5f);
            }
        }

        private void OnDestroy()
        {
            // Dọn dẹp Tween nếu chuyển scene đột ngột
            _glitchSequence?.Kill();
        }

        public void OnClickFaceYourself()
        {
            if (_isStarting) return;
            _isStarting = true;
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            Sequence seq = DOTween.Sequence();

            // 1. Tắt Menu UI
            seq.AppendCallback(() => _menuCanvasGroup.interactable = false);
            seq.Append(_menuCanvasGroup.DOFade(0f, 0.3f));

            // 2. CHỚP MẮT LẦN 1 (Nhắm lại nhanh)
            seq.AppendCallback(() => FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, 0.15f));
            seq.AppendInterval(0.2f); // Đợi màn hình đen hẳn

            // 3. TRÁO QUÁI VẬT & MỞ MẮT RA
            seq.AppendCallback(() => {
                if (_shadowSprite != null && _monsterShadow != null)
                {
                    _shadowSprite.sprite = _monsterShadow;
                    _shadowSprite.color = new Color(0f, 0f, 0f, 0.85f); // Đậm, rõ ràng

                    if (_audioSource && _glitchSound) _audioSource.PlayOneShot(_glitchSound);

                    // Rung nhẹ quái vật 1 chút cho thêm phần kì dị
                    _shadowSprite.transform.DOShakePosition(0.5f, 0.05f, 20);
                }

                // Mở mắt ra (nhanh)
                FilterController.Instance.PlayEyeOpenedVignetteEffect(0.15f);
            });

            // Cho người chơi 0.8 giây để "đứng hình" nhìn con quái vật trên bàn
            seq.AppendInterval(0.8f);

            // 4. TỪ TỪ NGẨNG CAMERA LÊN GƯƠNG
            if (_cameraTransform != null && _mirrorTarget != null)
            {
                seq.Append(_cameraTransform.DORotateQuaternion(_mirrorTarget.rotation, 1.2f).SetEase(Ease.InOutSine));
            }

            // 5. CHỚP MẮT LẦN 2 (Nhắm lại từ từ để chuyển Scene)
            seq.AppendCallback(() => FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, 0.4f));
            seq.AppendInterval(0.5f); // Đợi tối đen hoàn toàn

            // 6. CHUYỂN SCENE
            seq.AppendCallback(() => {
                SceneManager.LoadScene(_gameplaySceneName);
            });
        }

        // ==========================================
        // HÀM TẠO HIỆU ỨNG GLITCH BÓNG NGƯỜI
        // ==========================================
        private void PlayShadowGlitchEffect()
        {
            _glitchSequence = DOTween.Sequence();

            // Lắc bạo lực (Vị trí)
            _glitchSequence.Append(_shadowSprite.transform.DOShakePosition(_shadowShakeDuration, new Vector3(0.05f, 0f, 0.05f), 30, 90f, false, true));

            // Chớp giật (Alpha) - Tạo cảm giác như tín hiệu bị đứt đoạn
            _glitchSequence.Insert(0f, _shadowSprite.DOFade(0.1f, 0.05f).SetLoops(-1, LoopType.Yoyo));

            // Nhảy Scale (Biến dạng kích thước chớp nhoáng)
            _glitchSequence.InsertCallback(0.2f, () => _shadowSprite.transform.localScale = new Vector3(1.1f, 0.9f, 1f));
            _glitchSequence.InsertCallback(0.3f, () => _shadowSprite.transform.localScale = Vector3.one);
            _glitchSequence.InsertCallback(0.5f, () => _shadowSprite.transform.localScale = new Vector3(0.8f, 1.2f, 1f));
            _glitchSequence.InsertCallback(0.6f, () => _shadowSprite.transform.localScale = Vector3.one);
        }

        // ==========================================

        public void OnClickSettings()
        {
            if (_isStarting || _settingsPanel == null) return;
            _settingsPanel.SetActive(!_settingsPanel.activeSelf);
        }

        public void OnClickQuit()
        {
            if (_isStarting) return;

            // Dùng nhắm mắt để thoát game luôn cho ngầu
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, _eyeCloseDuration);
            }

            DOVirtual.DelayedCall(_eyeCloseDuration + 0.5f, () => Application.Quit());
        }
    }
}