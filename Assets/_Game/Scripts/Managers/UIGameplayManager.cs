using DG.Tweening;
using Game.Cameras;
using Game.Core;
using Game.Effect;
using Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Managers
{
    public class UIGameplayManager : MonoSingleton<UIGameplayManager>
    {
        [Header("Main Panels")]
        [Header("Lose")]
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private CanvasGroup _loseCanvasGroup;
        [SerializeField] private float _loseAppearDuration = 1f;
        [SerializeField] private RectTransform _replayButton;

        [Header("Gameplay Panels")]
        [SerializeField] private GameObject _mirrorPanel;
        [SerializeField] private GameObject _deskPanel;
        [SerializeField] private GameObject _behindPanel;

        [Header("Rewards")]
        [SerializeField] private CanvasGroup _keyUIGroup;
        [SerializeField] private float _keyUIAppearDuration = 0.5f;
        [Header("Flash Light Buttona")]
        [SerializeField] private Button _lightButton;
        [SerializeField] private Sprite _activateSprite;
        [SerializeField] private Sprite _deactivateSprite;

        [Header("Ending")]
        [SerializeField] private GameObject _endingPanel;
        [SerializeField] private CanvasGroup _endingCanvasGroup;
        [SerializeField] private TextMeshProUGUI _endingText;
        [SerializeField] private RectTransform _wakeUpAgainButton;
        [SerializeField] private Image _jumpscareFlashImage;
        [SerializeField] private Game.Configs.EndingConfig _endingConfig;
        [SerializeField] private AudioSource _audioSource;//Đợi LTN làm sound nhá

        private string[] _loseText = new string[]
        {
            "Wake Up",
            "Return",
            "Again?",
            "Open your eyes",
            "Come back"
        };

        #region Base
        private void Start()
        {
            ActivateView(View.Mirror);
            GameEvents.OnViewChangeStarted += HideAllPanels;
            GameEvents.OnViewChangeFinished += ActivateView;

            GameEvents.OnJumpscareTriggered += HideAllPanels;

            GameEvents.OnGameLost += ShowLosePanel;

            GameEvents.OnKeyCollected += ShowKeyOnUI;
            GameEvents.OnBatteryChargeCompleted += HandleBatteryChargeCompleted;

            GameEvents.OnGameWon += PlayLoopEnding;

            HandleBatteryChargeCompleted();

            _losePanel.gameObject.SetActive(false);
            _replayButton.gameObject.SetActive(false);
            _loseCanvasGroup.alpha = 0f;

            if (_keyUIGroup != null)
            {
                _keyUIGroup.alpha = 0f;
                _keyUIGroup.gameObject.SetActive(false);
            }

            if (_endingPanel != null)
            {
                _endingPanel.SetActive(false);

                if (_endingPanel.TryGetComponent<UnityEngine.UI.Image>(out var panelImage))
                {
                    panelImage.color = Color.black;
                }
            }

            if (_jumpscareFlashImage != null) _jumpscareFlashImage.gameObject.SetActive(false);

            if (_wakeUpAgainButton != null) _wakeUpAgainButton.gameObject.SetActive(false);

            if (_endingText != null)
            {
                _endingText.alpha = 0f;
                _endingText.text = "";
            }
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnViewChangeStarted -= HideAllPanels;
            GameEvents.OnViewChangeFinished -= ActivateView;

            GameEvents.OnJumpscareTriggered += HideAllPanels;

            GameEvents.OnBatteryChargeCompleted -= HandleBatteryChargeCompleted;

            GameEvents.OnGameLost -= ShowLosePanel;

            GameEvents.OnKeyCollected -= ShowKeyOnUI;
            GameEvents.OnGameWon -= PlayLoopEnding;
        }
        #endregion

        #region Panels
        private void HideAllPanels(View _)
        {
            if (_mirrorPanel != null)
                _mirrorPanel.gameObject.SetActive(false);
            if (_deskPanel != null)
                _deskPanel.gameObject.SetActive(false);
            if (_behindPanel != null)
                _behindPanel.gameObject.SetActive(false);
        }

        private void HideAllPanels()
        {
            if (_mirrorPanel != null)
                _mirrorPanel.gameObject.SetActive(false);
            if (_deskPanel != null)
                _deskPanel.gameObject.SetActive(false);
            if (_behindPanel != null)
                _behindPanel.gameObject.SetActive(false);
        }

        private void ActivateView(View view)
        {
            switch (view)
            {
                case View.Mirror:
                    HideAllPanels(view);
                    _mirrorPanel.gameObject.SetActive(true);
                    return;
                case View.Desk:
                    HideAllPanels(view);
                    _deskPanel.gameObject.SetActive(true);
                    return;
                case View.Behind:
                    HideAllPanels(view);
                    _behindPanel.gameObject.SetActive(true);
                    return;
            }
        }
        #endregion

        #region Lose Panel
        private void ShowLosePanel()
        {
            HideAllPanels();
            FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, _loseAppearDuration);
            _losePanel.gameObject.SetActive(true);
            _loseCanvasGroup.DOFade(1f, _loseAppearDuration).SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    _replayButton.TryGetComponent<CanvasGroup>(out var cvg);
                    _replayButton.GetComponentInChildren<TextMeshProUGUI>().text = _loseText[Random.Range(0, _loseText.Length)];
                    cvg.alpha = 0f;
                    _replayButton.gameObject.SetActive(true);
                    cvg.DOFade(1f, 1f);
                });
        }
        #endregion

        // ==========================================
        // [MỚI] HIỆU ỨNG HIỂN THỊ CHÌA KHÓA LÊN UI
        // ==========================================
        #region Inventory
        private void ShowKeyOnUI()
        {
            if (_keyUIGroup != null)
            {
                _keyUIGroup.gameObject.SetActive(true);

                // Reset scale về 0 để chuẩn bị phóng to
                _keyUIGroup.transform.localScale = Vector3.zero;

                // Fade in mượt mà
                _keyUIGroup.DOFade(1f, _keyUIAppearDuration);

                // Hiệu ứng phóng to và nảy nhẹ (OutBack) tạo cảm giác vui nhộn khi nhận đồ
                _keyUIGroup.transform.DOScale(Vector3.one, _keyUIAppearDuration).SetEase(Ease.OutBack);
            }
        }
        #endregion

        #region Button Events
        public void LightFlash()
        {
            GameEvents.RaiseLightFlashed();
            _lightButton.interactable = false;
            _lightButton.GetComponent<Image>().sprite = _deactivateSprite;
        }

        private void HandleBatteryChargeCompleted()
        {
            _lightButton.interactable = true;
            _lightButton.GetComponent<Image>().sprite = _activateSprite;
        }

        public void Replay()
        {
            SceneController.Instance.ReloadGameplayScene();
        }

        private void PlayLoopEnding()
        {
            if (_endingConfig == null) return;

            HideAllPanels();
            _endingPanel.SetActive(true);
            _endingCanvasGroup.alpha = 0f;
            _endingText.alpha = 0f;
            _wakeUpAgainButton.gameObject.SetActive(false);

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
                    // Dọn dẹp hiệu ứng cũ nếu có
                    DOTween.Kill("TextGlitch");

                    _endingText.text = line.text;
                    _endingText.color = Color.white; // Trả về trắng gốc
                    _endingText.DOFade(1f, 1f);

                    // 1. Chữ run rẩy nhẹ (Lắc vị trí biên độ nhỏ: 3 pixel)
                    _endingText.rectTransform.DOShakeAnchorPos(line.showDuration, new Vector2(3f, 3f), 20, 90f, false, true).SetId("TextGlitch");

                    // 2. Chữ chớp mờ Alpha liên tục (Glitch)
                    _endingText.DOFade(0.6f, 0.15f).SetLoops(-1, LoopType.Yoyo).SetId("TextGlitch");

                    if (line.sfx != null && _audioSource != null)
                        _audioSource.PlayOneShot(line.sfx);
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
                        _jumpscareFlashImage.gameObject.SetActive(true);

                        RectTransform monsterRect = _jumpscareFlashImage.rectTransform;

                        // Phóng to lao vào mặt
                        monsterRect.localScale = Vector3.one;
                        monsterRect.DOScale(Vector3.one * 1.5f, jumpscareDuration).SetEase(Ease.OutExpo);

                        // Rung lắc UI bạo lực (50px, 60 nhịp)
                        monsterRect.DOShakeAnchorPos(jumpscareDuration, new Vector2(50f, 50f), 60, 90f, false, true);
                        monsterRect.DOShakeRotation(jumpscareDuration, new Vector3(0f, 0f, 15f), 60, 90f);

                        // Chớp màu Đỏ (Hazard) liên tục trên hình quái vật
                        _jumpscareFlashImage.color = Color.white;
                        _jumpscareFlashImage.DOColor(FilterController.Instance.HazardColor, 0.05f)
                               .SetLoops(-1, LoopType.Yoyo)
                               .SetId("JumpscareFlicker");

                        // Rung Camera 3D để tạo cảm giác chấn động toàn cục
                        Camera.main.transform.DOShakePosition(jumpscareDuration, 2.5f, 60, 90f);
                    });

                    endSeq.AppendInterval(jumpscareDuration);

                    endSeq.AppendCallback(() => {
                        // Triệt tiêu mọi rung lắc & chớp màu
                        DOTween.Kill("JumpscareFlicker");
                        _jumpscareFlashImage.rectTransform.DOKill();
                        Camera.main.transform.DOKill();

                        // Trả màu về mặc định
                        _jumpscareFlashImage.color = Color.white;

                        // Tắt quái vật & Ép nền về đen tuyệt đối
                        _jumpscareFlashImage.gameObject.SetActive(false);
                        _endingPanel.GetComponent<UnityEngine.UI.Image>().color = Color.black;
                    });

                    // Khoảng lặng (0.5s) hụt hẫng trong bóng tối trước khi hiện nút
                    endSeq.AppendInterval(0.5f);
                }
                else
                {
                    endSeq.AppendCallback(() => {
                        _endingText.DOFade(0f, 0.5f);
                    });
                    endSeq.AppendInterval(0.5f);
                }
            }

            // 3. BẢO HIỂM CUỐI CÙNG: Dọn sạch mọi text dính lại trước khi gọi Button
            endSeq.AppendCallback(() => {
                _endingText.DOKill();
                _endingText.alpha = 0f;
                _endingText.text = "";
            });
            endSeq.AppendInterval(0.5f);

            // 4. HIỆN NÚT VÒNG LẶP
            endSeq.AppendCallback(() => {
                _wakeUpAgainButton.GetComponentInChildren<TextMeshProUGUI>().text = _endingConfig.loopButtonText;
                _wakeUpAgainButton.gameObject.SetActive(true);
                _wakeUpAgainButton.GetComponent<CanvasGroup>().DOFade(1f, 1f);
            });
        }
        #endregion
    }
}