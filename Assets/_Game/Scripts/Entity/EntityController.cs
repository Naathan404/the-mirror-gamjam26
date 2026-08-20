using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Managers;
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
        [SerializeField] private bool _resetTimeWhenJumpState = true;

        [Header("RampRate Settings")]
        [SerializeField] private float _awayRampRate = 0.02f;
        [SerializeField] private float _awayRampCap = 1.8f;
        [SerializeField] private float _timerAwayFromMirror = 0f;

        [Header("State Settings")]
        [SerializeField] private int _jumpStepWhenGotLightFlashed = 2;
        [SerializeField] private float _accelaration = 1.15f;

        [Header("State Readonly")]
        [SerializeField] public int CurrentState { get; private set; }
        [SerializeField] private float _baseTimeScaleAccelaration = 1f;


        private View _currentView = View.Mirror;
        private bool _gameOverBuffer = false;
        
        #region Base
        private void Start()
        {
            CurrentState = GameConstants.ENTITY_MAX_STATE;
            _currentTimeScale = GetTimeScaleByView(GameConstants.START_VIEW);
            CurrentState = GameConstants.ENTITY_START_STATE;

            GameEvents.RaiseEntityStateChanged(CurrentState);

            // register event
            GameEvents.OnViewChangeFinished += HandleViewChanged;
            GameEvents.OnLightFlashed += HandleLightFlashed;
            GameEvents.OnMinigameFailed += HandleMinigameFailed;
        }

        private void OnDestroy()
        {
            GameEvents.OnViewChangeFinished -= HandleViewChanged;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameFailed -= HandleMinigameFailed;
        }
        #endregion

        #region States change
        private void Update()
        {
            UpdateTimer();
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space))
            {
                GameEvents.RaiseLightFlashed();
                FilterController.Instance.FlashScreen(FilterController.Instance.FlashColor);
                Camera.main.transform.DOShakePosition(0.5f, 0.35f, 12, 45f);
            }
#endif
        }

        private void UpdateTimer()
        {
            if (_gameOverBuffer)
            {
                if (_currentView == View.Mirror)
                {
                    GameEvents.RaiseJumpscareTriggered();
                    GameManager.Instance.SetGameOver();
                    _gameOverBuffer = false;
                }
                return;
            }

            if (GameManager.Instance.CurrentState == GameState.GameOver) 
                return;

            if (_currentView != View.Mirror)
            {
                _timerAwayFromMirror += Time.deltaTime;
            }

            _timer += Time.deltaTime * _currentTimeScale * GetAwayMultiplier();

            if (_timer > _maxTimerTime)
            {
                _timer = -1f;
                CurrentState--;
                GameEvents.RaiseEntityStateChanged(CurrentState);

                if (CurrentState == 0)
                {
                    if (_currentView == View.Mirror)
                    {
                        GameEvents.RaiseJumpscareTriggered();   
                        GameManager.Instance.SetGameOver();
                    }
                    else
                    {
                        _gameOverBuffer = true;
                    }
                }
            }
        }
        
        private void JumpToState(int state)
        {
            CurrentState = state;
            GameEvents.RaiseEntityStateChanged(state);
            _currentTimeScale = GetTimeScaleByView(_currentView);

            if (_resetTimeWhenJumpState)
            {
                _timer = 0f;
            }
        }

        private void SetTimeScaleAccelaration(float factor)
        {
            if (factor <= 0) return;

            _baseTimeScaleAccelaration *= factor;
        }

        private float GetAwayMultiplier()
        {
            if (_currentView == View.Mirror) return 1f;
            return Mathf.Min(1f + _timerAwayFromMirror * _awayRampRate, _awayRampCap);
        }
        #endregion

        #region Handle Events
        private void HandleViewChanged(View view)
        {
            _currentTimeScale = GetTimeScaleByView(view);
            _currentView = view;
            if (view == View.Mirror)
            {
                _timerAwayFromMirror = 0f;
            }
        }

        private void HandleLightFlashed()
        {
            SetTimeScaleAccelaration(_accelaration);
            int clamp = Mathf.Clamp(CurrentState + _jumpStepWhenGotLightFlashed, 0, GameConstants.ENTITY_MAX_STATE);
            JumpToState(clamp);
        }

        private void HandleMinigameFailed(float accel)
        {
            SetTimeScaleAccelaration(accel);
            _currentTimeScale = GetTimeScaleByView(_currentView);
        }
        #endregion

        #region Helpers
        private float GetTimeScaleByView(View view)
        {
            foreach(var timeview in _timeScaleViews)
            {
                if (timeview.BaseView == view)
                    return timeview.TimeScale * _baseTimeScaleAccelaration;
            }
            return 1f * _baseTimeScaleAccelaration;
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