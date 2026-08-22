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
        [SerializeField] private GameObject _mirrorPanel;
        [SerializeField] private GameObject _deskPanel;
        [SerializeField] private GameObject _behindPanel;

        [Header("Lose Panel")]
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private CanvasGroup _loseCanvasGroup;
        [SerializeField] private float _loseAppearDuration = 1f;
        [SerializeField] private RectTransform _replayButton;

        [Header("Rewards")]
        [SerializeField] private CanvasGroup _keyUIGroup;
        [SerializeField] private float _keyUIAppearDuration = 0.5f;

        [Header("Flash Light Button")]
        [SerializeField] private Button _lightButton;
        [SerializeField] private Sprite _activateSprite;
        [SerializeField] private Sprite _deactivateSprite;

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
            GameEvents.OnViewChangeStarted += HideAllPanels;
            GameEvents.OnViewChangeFinished += ActivateView;
            GameEvents.OnJumpscareTriggered += HideAllPanels;
            GameEvents.OnGameLost += ShowLosePanel;
            GameEvents.OnKeyCollected += ShowKeyOnUI;
            GameEvents.OnDoorInteracted += HideKeyOnUI;
            GameEvents.OnBatteryChargeCompleted += HandleBatteryChargeCompleted;

            HandleBatteryChargeCompleted();

            _losePanel.gameObject.SetActive(false);
            _replayButton.gameObject.SetActive(false);
            _loseCanvasGroup.alpha = 0f;

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
            GameEvents.OnGameLost -= ShowLosePanel;
            GameEvents.OnKeyCollected -= ShowKeyOnUI;
            GameEvents.OnDoorInteracted -= HideKeyOnUI;
            GameEvents.OnBatteryChargeCompleted -= HandleBatteryChargeCompleted;
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
        #endregion

        #region Lose Panel & Inventory
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
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            SceneController.Instance.ReloadGameplayScene();
        }
        #endregion
    }
}