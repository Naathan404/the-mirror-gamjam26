using System.Collections;
using DG.Tweening;
using Game.Cameras;
using Game.Core;
using Game.Effect;
using Game.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

namespace Game.Managers
{
    public class UIGameplayManager : MonoSingleton<UIGameplayManager>
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject _mirrorPanel;
        [SerializeField] private GameObject _deskPanel;
        [SerializeField] private GameObject _behindPanel;
        [SerializeField] private GameObject _inventoryPanel;
        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _rulePanel;
        [Header("Blocker")]
        [Tooltip("Kéo thả khối RaycastBlocker trong Camera vào đây")]
        [SerializeField] private GameObject _raycastBlocker;

        [Header("Lose Panel")]
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private CanvasGroup _loseCanvasGroup;
        [SerializeField] private float _loseAppearDuration = 1f;
        [SerializeField] private RectTransform _replayButton;
        [SerializeField] private CanvasGroup _replayButtonCanvasGroup;
        [SerializeField] private TextMeshProUGUI _replayButtonText;
        [SerializeField] private RectTransform _quitButton;
        [SerializeField] private CanvasGroup _quitButtonCanvasGroup;

        [Header("Lose Text Config")]
        [Tooltip("Danh sách các câu ngẫu nhiên khi thua. Nhấn + để thêm, rồi chọn Table/Key tương ứng.")]
        [SerializeField] private LocalizedString[] _loseTexts;

        [Header("Rewards")]
        [SerializeField] private CanvasGroup _keyUIGroup;
        [SerializeField] private float _keyUIAppearDuration = 0.5f;

        [Header("Flash Light Button")]
        [SerializeField] private Button _lightButton;
        [SerializeField] private Image _lightButtonImage;
        [SerializeField] private Sprite _activateSprite;
        [SerializeField] private Sprite _deactivateSprite;

        private Coroutine _showReplayButtonRoutine;

        #region Base
        private void Start()
        {
            GameEvents.OnViewChangeStarted += HideAllPanels;
            GameEvents.OnViewChangeFinished += ActivateView;
            GameEvents.OnJumpscareTriggered += HideAllPanels;
            GameEvents.OnJumpscareTriggered += HideInventoryPanel;
            GameEvents.OnGameLost += ShowLosePanel;
            GameEvents.OnKeyCollected += ShowKeyOnUI;
            GameEvents.OnDoorInteracted += HideKeyOnUI;
            GameEvents.OnBatteryChargeCompleted += HandleBatteryChargeCompleted;

            HandleBatteryChargeCompleted();

            if (_losePanel != null)
            {
                _losePanel.gameObject.SetActive(false);
            }

            if (_settingsPanel != null)
            {
                _settingsPanel.SetActive(false);
            }

            if (_rulePanel != null)
            {
                _rulePanel.SetActive(false);
            }

            if (_raycastBlocker != null)
            {
                _raycastBlocker.SetActive(false);
            }

            if (_replayButton != null)
            {
                _replayButton.gameObject.SetActive(false);
            }

            if (_loseCanvasGroup != null)
            {
                _loseCanvasGroup.alpha = 0f;
            }

            if (_replayButtonCanvasGroup != null)
            {
                _replayButtonCanvasGroup.alpha = 0f;
            }

            if (_quitButton != null)
            {
                _quitButton.gameObject.SetActive(false);
            }

            if (_quitButtonCanvasGroup != null)
            {
                _quitButtonCanvasGroup.alpha = 0f;
            }

            if (_keyUIGroup != null)
            {
                _keyUIGroup.alpha = 0f;
                _keyUIGroup.gameObject.SetActive(false);
            }

            ActivateView(View.Mirror);
        }
#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnViewChangeStarted -= HideAllPanels;
            GameEvents.OnViewChangeFinished -= ActivateView;
            GameEvents.OnJumpscareTriggered -= HideAllPanels;
            GameEvents.OnJumpscareTriggered -= HideInventoryPanel;
            GameEvents.OnGameLost -= ShowLosePanel;
            GameEvents.OnKeyCollected -= ShowKeyOnUI;
            GameEvents.OnDoorInteracted -= HideKeyOnUI;
            GameEvents.OnBatteryChargeCompleted -= HandleBatteryChargeCompleted;
        }

        private void Update()
        {
            if (GameManager.Instance.CurrentState == GameState.GameOver ||
                GameManager.Instance.CurrentState == GameState.GameWon)
            {
                return;
            }

            if (Input.GetKeyDown(UnityEngine.KeyCode.Escape))
            {
                if (_rulePanel != null && _rulePanel.activeSelf)
                {
                    CloseRule();
                }
                else
                {
                    ToggleSettings();
                }
            }
        }
        #endregion

        #region Panels
        private void HideAllPanels(View _) => HideAllPanels();

        public void HideAllPanels()
        {
            if (_mirrorPanel != null) _mirrorPanel.gameObject.SetActive(false);
            if (_deskPanel != null) _deskPanel.gameObject.SetActive(false);
            if (_behindPanel != null) _behindPanel.gameObject.SetActive(false);
        }

        private void HideInventoryPanel()
        {
            if (_inventoryPanel != null) _inventoryPanel.gameObject.SetActive(false);
        }

        private void ActivateView(View view)
        {
            HideAllPanels();
            switch (view)
            {
                case View.Mirror:
                    if (_mirrorPanel != null) _mirrorPanel.gameObject.SetActive(true);
                    break;
                case View.Desk:
                    if (_deskPanel != null) _deskPanel.gameObject.SetActive(true);
                    break;
                case View.Behind:
                    if (_behindPanel != null) _behindPanel.gameObject.SetActive(true);
                    break;
            }
        }

        public void ToggleSettings()
        {
            if (_settingsPanel == null) return;

            bool isOpening = !_settingsPanel.activeSelf;
            _settingsPanel.SetActive(isOpening);

            if (_raycastBlocker != null)
            {
                _raycastBlocker.SetActive(isOpening);
            }
            if (isOpening)
            {
                GameManager.Instance.PauseGame();

                if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            }
            else
            {
                GameManager.Instance.ResumeGame();

                if (AudioController.Instance != null) AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            }
        }
        public void CloseSettings()
        {
            if (_settingsPanel != null && _settingsPanel.activeSelf)
            {
                ToggleSettings();
            }
        }
        public void OpenRule()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            if (_rulePanel != null)
            {
                _rulePanel.transform.SetAsLastSibling(); 
                _rulePanel.SetActive(true);
            }
        }

        public void CloseRule()
        {
            if (AudioController.Instance != null)
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);

            if (_rulePanel != null) _rulePanel.SetActive(false);
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }
        #endregion

        #region Lose Panel & Inventory
        private void ShowLosePanel()
        {
            HideAllPanels();
            HideInventoryPanel();

            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, _loseAppearDuration);
            }

            if (_losePanel != null)
            {
                _losePanel.transform.SetAsLastSibling();
                _losePanel.gameObject.SetActive(true);
            }

            if (_loseCanvasGroup != null)
            {
                _loseCanvasGroup.DOKill();
                _loseCanvasGroup.alpha = 0f;
                _loseCanvasGroup.interactable = true;
                _loseCanvasGroup.blocksRaycasts = true;
                _loseCanvasGroup.DOFade(1f, _loseAppearDuration).SetEase(Ease.OutQuart).SetUpdate(true);
            }

            if (_showReplayButtonRoutine != null)
            {
                StopCoroutine(_showReplayButtonRoutine);
            }

            _showReplayButtonRoutine = StartCoroutine(ShowReplayButtonRoutine());

            if (_settingsPanel != null && _settingsPanel.activeSelf)
            {
                _settingsPanel.SetActive(false);
                Time.timeScale = 1f;
            }
        }

        private IEnumerator ShowReplayButtonRoutine()
        {
            yield return new WaitForSecondsRealtime(_loseAppearDuration);

            if (_replayButtonText != null && _loseTexts != null && _loseTexts.Length > 0)
            {
                int randomIndex = Random.Range(0, _loseTexts.Length);

                var asyncOp = _loseTexts[randomIndex].GetLocalizedStringAsync();
                yield return asyncOp;

                _replayButtonText.text = asyncOp.Result;
            }

            ShowLoseButtons();
            _showReplayButtonRoutine = null;
        }

        private void ShowLoseButtons()
        {
            if (_replayButton != null)
            {
                _replayButton.gameObject.SetActive(true);
                if (_replayButtonCanvasGroup != null)
                {
                    _replayButtonCanvasGroup.DOKill();
                    _replayButtonCanvasGroup.alpha = 0f;
                    _replayButtonCanvasGroup.DOFade(1f, 3f).SetUpdate(true);
                }
            }

            if (_quitButton != null)
            {
                _quitButton.gameObject.SetActive(true);
                if (_quitButtonCanvasGroup != null)
                {
                    _quitButtonCanvasGroup.DOKill();
                    _quitButtonCanvasGroup.alpha = 0f;
                    _quitButtonCanvasGroup.DOFade(1f, 3f).SetUpdate(true);
                }
            }
        }

        private void ShowKeyOnUI()
        {
            if (_keyUIGroup != null)
            {
                _keyUIGroup.gameObject.SetActive(true);
                _keyUIGroup.transform.localScale = Vector3.zero;
                _keyUIGroup.DOFade(1f, _keyUIAppearDuration);
                _keyUIGroup.transform.DOScale(Vector3.one, _keyUIAppearDuration).SetEase(Ease.OutBack);
            }
        }

        private void HideKeyOnUI()
        {
            if (GameManager.Instance.HasRoomKey && _keyUIGroup != null && _keyUIGroup.gameObject.activeSelf)
            {
                _keyUIGroup.DOFade(0f, 0.3f);
                _keyUIGroup.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
                    .OnComplete(() => _keyUIGroup.gameObject.SetActive(false));
            }
        }
        #endregion

        #region Button Events
        public void LightFlash()
        {
            GameEvents.RaiseLightFlashed();

            if (_lightButton != null)
            {
                _lightButton.interactable = false;
            }

            SetLightButtonSprite(_deactivateSprite);
        }

        private void HandleBatteryChargeCompleted()
        {
            if (_lightButton != null)
            {
                _lightButton.interactable = true;
            }

            SetLightButtonSprite(_activateSprite);
        }

        private void SetLightButtonSprite(Sprite sprite)
        {
            if (_lightButtonImage != null)
            {
                _lightButtonImage.sprite = sprite;
            }
        }

        public void Replay()
        {
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            SceneController.Instance.ReloadGameplayScene();
        }
        #endregion
    }
}