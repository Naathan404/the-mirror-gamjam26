using DG.Tweening;
using Game.Cameras;
using Game.Core;
using Game.Effect;
using Game.Utils;
using TMPro;
using UnityEngine;

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

        private string[] _loseText = new string[]
        {
            "Wake Up",
            "Return",
            "Again?",
            "Open your eyes",
            "Come back"
        };

        #region Base
        // private void Star()
        // {
        //     GameEvents.OnViewChangeStarted += HideAllPanels;
        //     GameEvents.OnViewChangeFinished += ActivateView;
        // }
        
        // private void OnDisable()
        // {
        //     GameEvents.OnViewChangeStarted -= HideAllPanels;
        //     GameEvents.OnViewChangeFinished -= ActivateView;
        // }

        private void Start()
        {
            ActivateView(View.Mirror);
            GameEvents.OnViewChangeStarted += HideAllPanels;
            GameEvents.OnViewChangeFinished += ActivateView;
            
            GameEvents.OnJumpscareTriggered += HideAllPanels;

            GameEvents.OnGameLost += ShowLosePanel;

            _losePanel.gameObject.SetActive(false);
            _replayButton.gameObject.SetActive(false);
            _loseCanvasGroup.alpha = 0f;
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnViewChangeStarted -= HideAllPanels;
            GameEvents.OnViewChangeFinished -= ActivateView;

            GameEvents.OnJumpscareTriggered += HideAllPanels;

            GameEvents.OnGameLost -= ShowLosePanel;
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
            switch(view)
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

        #region Button Events
        public void LightFlash()
        {
            GameEvents.RaiseLightFlashed();
        }

        public void Replay()
        {
            SceneController.Instance.ReloadGameplayScene();
        }
        #endregion
    }
    
}
