using Game.Camera;
using Game.Core;
using KingCat.Base;
using UnityEngine;

namespace Game.Managers
{
    public class UIGameplayManager : MonoSingleton<UIGameplayManager>
    {
        [Header("Panels")]
        [SerializeField] private GameObject _mirrorPanel;
        [SerializeField] private GameObject _deskPanel;
        [SerializeField] private GameObject _behindPanel;

        #region Base
        private void OnEnable()
        {
            GameEvents.OnViewChangeStarted += HideAllPanels;
            GameEvents.OnViewChangeFinished += ActivateView;
        }
        
        private void OnDisable()
        {
            GameEvents.OnViewChangeStarted -= HideAllPanels;
            GameEvents.OnViewChangeFinished -= ActivateView;
        }

        private void Start()
        {
            ActivateView(View.Mirror);
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
    }
    
}
