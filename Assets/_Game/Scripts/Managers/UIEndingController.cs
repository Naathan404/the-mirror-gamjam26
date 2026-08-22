using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class UIEndingController : MonoBehaviour
    {
        [Header("Ending Elements")]
        [SerializeField] private GameObject _endingPanel;
        [SerializeField] private CanvasGroup _endingCanvasGroup;
        [SerializeField] private TextMeshProUGUI _endingText;
        [SerializeField] private Image _jumpscareFlashImage;

        [Header("Buttons")]
        [SerializeField] private RectTransform _wakeUpAgainButton; // Nút chơi lại
        [SerializeField] private RectTransform _returnToMenuButton; // Nút về Menu

        [Header("Configs")]
        [SerializeField] private Game.Configs.EndingConfig _endingConfig;

        private bool _isActionTriggered = false; // Ngăn chặn bấm 2 nút cùng lúc

        private void Start()
        {
            GameEvents.OnGameWon += PlayLoopEnding;

            // Setup dọn dẹp ban đầu
            if (_endingPanel != null)
            {
                _endingPanel.SetActive(false);
                if (_endingPanel.TryGetComponent<UnityEngine.UI.Image>(out var panelImage))
                    panelImage.color = Color.black;
            }

            if (_jumpscareFlashImage != null) _jumpscareFlashImage.gameObject.SetActive(false);
            if (_wakeUpAgainButton != null) _wakeUpAgainButton.gameObject.SetActive(false);
            if (_returnToMenuButton != null) _returnToMenuButton.gameObject.SetActive(false);

            if (_endingText != null)
            {
                _endingText.alpha = 0f;
                _endingText.text = "";
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnGameWon -= PlayLoopEnding;
        }

        private void PlayLoopEnding()
        {
            if (_endingConfig == null) return;

            // Nhờ UIGameplayManager tắt các UI rác đi
            if (UIGameplayManager.Instance != null)
            {
                UIGameplayManager.Instance.HideAllPanels();
            }

            _endingPanel.SetActive(true);
            _endingCanvasGroup.alpha = 0f;
            _endingText.alpha = 0f;
            _wakeUpAgainButton.gameObject.SetActive(false);
            if (_returnToMenuButton != null) _returnToMenuButton.gameObject.SetActive(false);

            Camera mainCam = Camera.main;
            float originalFOV = mainCam.fieldOfView;
            Sequence endSeq = DOTween.Sequence();

            // 1. Hút FOV & Tối màn hình
            endSeq.Append(mainCam.DOFieldOfView(10f, 1.5f).SetEase(Ease.InExpo));
            endSeq.Append(_endingCanvasGroup.DOFade(1f, 0.2f));
            endSeq.AppendCallback(() => mainCam.fieldOfView = originalFOV);

            // 2. VÒNG LẶP ĐỘNG
            foreach (var line in _endingConfig.endingLines)
            {
                endSeq.AppendInterval(line.delayBeforeShow);

                endSeq.AppendCallback(() => {
                    DOTween.Kill("TextGlitch");
                    _endingText.text = line.text;
                    _endingText.color = Color.white;
                    _endingText.DOFade(1f, 1f);
                    _endingText.rectTransform.DOShakeAnchorPos(line.showDuration, new Vector2(3f, 3f), 20, 90f, false, true).SetId("TextGlitch");
                    _endingText.DOFade(0.6f, 0.15f).SetLoops(-1, LoopType.Yoyo).SetId("TextGlitch");

                    if (line.soundName != SoundName.None && AudioController.Instance != null)
                    {
                        AudioController.Instance.PlaySFX(line.soundName);
                    }
                });

                endSeq.AppendInterval(line.showDuration);

                // KIỂM TRA JUMPSCARE
                if (line.triggerJumpscareAfter)
                {
                    float jumpscareDuration = 2f;

                    endSeq.AppendCallback(() => {
                        _endingText.DOKill();
                        _endingText.alpha = 0f;
                        _endingText.text = "";
                    });

                    endSeq.AppendCallback(() => {

                        // [SỬA 3]: Tiếng nổ Jumpscare
                        if (AudioController.Instance != null)
                        {
                            // Hưn có thể thay đổi Enum này sau nếu đã thêm SoundName.Jumpscare vào
                            AudioController.Instance.PlaySFX(SoundName.Entity_ChangeState);
                        }

                        _jumpscareFlashImage.gameObject.SetActive(true);
                        RectTransform monsterRect = _jumpscareFlashImage.rectTransform;
                        monsterRect.localScale = Vector3.one;
                        monsterRect.DOScale(Vector3.one * 1.5f, jumpscareDuration).SetEase(Ease.OutExpo);
                        monsterRect.DOShakeAnchorPos(jumpscareDuration, new Vector2(50f, 50f), 60, 90f, false, true);
                        monsterRect.DOShakeRotation(jumpscareDuration, new Vector3(0f, 0f, 15f), 60, 90f);

                        _jumpscareFlashImage.color = Color.white;
                        _jumpscareFlashImage.DOColor(FilterController.Instance.HazardColor, 0.05f)
                               .SetLoops(-1, LoopType.Yoyo).SetId("JumpscareFlicker");

                        Camera.main.transform.DOShakePosition(jumpscareDuration, 2.5f, 60, 90f);
                    });

                    endSeq.AppendInterval(jumpscareDuration);

                    endSeq.AppendCallback(() => {
                        DOTween.Kill("JumpscareFlicker");
                        _jumpscareFlashImage.rectTransform.DOKill();
                        Camera.main.transform.DOKill();
                        _jumpscareFlashImage.color = Color.white;
                        _jumpscareFlashImage.gameObject.SetActive(false);
                        _endingPanel.GetComponent<UnityEngine.UI.Image>().color = Color.black;
                    });

                    endSeq.AppendInterval(0.5f);
                }
                else
                {
                    endSeq.AppendCallback(() => _endingText.DOFade(0f, 0.5f));
                    endSeq.AppendInterval(0.5f);
                }
            }

            endSeq.AppendCallback(() => {
                _endingText.DOKill();
                _endingText.alpha = 0f;
                _endingText.text = "";
            });
            endSeq.AppendInterval(0.5f);

            // 4. HIỆN 2 NÚT LỰA CHỌN
            endSeq.AppendCallback(() => {
                _wakeUpAgainButton.GetComponentInChildren<TextMeshProUGUI>().text = _endingConfig.loopButtonText;
                _wakeUpAgainButton.gameObject.SetActive(true);
                _wakeUpAgainButton.GetComponent<CanvasGroup>().DOFade(1f, 1f);

                if (_returnToMenuButton != null)
                {
                    _returnToMenuButton.gameObject.SetActive(true);
                    _returnToMenuButton.GetComponent<CanvasGroup>().DOFade(1f, 1f);
                }
            });
        }

        // ==========================================
        // SỰ KIỆN KHI BẤM NÚT
        // ==========================================
        public void OnClickReplay()
        {
            if (_isActionTriggered) return;
            _isActionTriggered = true;

            // Phát tiếng bấm nút
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            SceneController.Instance.ReloadGameplayScene();
        }

        public void OnClickReturnToMenu()
        {
            if (_isActionTriggered) return;
            _isActionTriggered = true;

            // Phát tiếng bấm nút
            if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            SceneController.Instance.LoadMenuScene();
        }
    }
}