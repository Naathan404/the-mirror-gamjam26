using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;


namespace Game.Entity
{
    [DefaultExecutionOrder(-100)]
    public sealed class EntityController : MonoBehaviour
    {
        [Header("Time scale Settings")]
        [SerializeField] private List<TimeViewScale> _timeScaleViews;

        [Header("Timer Settings")]
        [SerializeField] private float _maxTimerTime = 100f;
        [SerializeField] private float _currentTimeScale;
        [SerializeField] private float _timer = 0f;


        private int _currentState;
        
        #region Base
        private void Start()
        {
            _currentState = GameConstants.ENTITY_MAX_STATE;
            _currentTimeScale = GetTimeScaleByView(GameConstants.START_VIEW);
            _currentState = GameConstants.ENTITY_START_STATE;

            GameEvents.RaiseEntityStateChanged(_currentState);

            // register event
            GameEvents.OnViewChangeFinished += HandleViewChange;
        }

        private void OnDestroy()
        {
            GameEvents.OnViewChangeFinished -= HandleViewChange;
        }
        #endregion

        private void Update()
        {
            UpdateTimer();
        }

        private void UpdateTimer()
        {
            _timer += Time.deltaTime * _currentTimeScale;
            if (_timer > _maxTimerTime)
            {
                _timer = -1f;
                _currentState--;
                GameEvents.RaiseEntityStateChanged(_currentState);
            }
        }

        #region Handle Events
        private void HandleViewChange(View view)
        {
            _currentTimeScale = GetTimeScaleByView(view);
        }
        #endregion

        #region Helpers
        private float GetTimeScaleByView(View view)
        {
            foreach(var timeview in _timeScaleViews)
            {
                if (timeview.BaseView == view)
                    return timeview.TimeScale;
            }
            return 1f;
        }
        #endregion
    }

    [Serializable]
    public class TimeViewScale
    {
        public View BaseView = View.Mirror;
        public float TimeScale = 1f;
    }
}