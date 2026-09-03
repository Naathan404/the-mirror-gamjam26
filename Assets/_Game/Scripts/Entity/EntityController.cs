using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Cameras;
using Game.Core;
using Game.Effect;
using Game.Managers;
using UnityEngine;
using KeyCode = Game.Core.KeyCode;


namespace Game.Entity
{
    [DefaultExecutionOrder(-100)]
    public sealed class EntityController : MonoBehaviour
    {
        [Header("Time scale Settings")]
        [SerializeField] private List<TimeViewScale> _timeScaleViews;

        [Header("Move Probability Settings")]
        [SerializeField] private float _baseMoveInterval = 9f;     // x: giây giữa mỗi lần roll ở base
        [SerializeField] private float _minMoveInterval = 3f;    // floor, tránh roll dồn dập vô lý
        [SerializeField] private float _baseMoveChance = 0.15f;    // a: xác suất cơ bản
        [SerializeField] private float _moveChanceCap = 0.5f;
        [SerializeField] private float _moveChancePerMinigame = 0.05f; // +a mỗi minigame hoàn thành

        [Header("Insurance Settings")]
        [SerializeField] private float _baseInsuranceTime = 120f; // bảo hiểm: quá 2 phút không move -> chắc chắn move
        [SerializeField] private float _insuranceTimeStep = 10f;

        [Header("RampRate Settings")]
        [SerializeField] private float _awayRampRate = 0.008f; // giảm nhẹ so với 0.02 cũ
        [SerializeField] private float _awayRampCap = 1.25f;    // giảm nhẹ so với 1.8 cũ
        [SerializeField] private float _timerAwayFromMirror = 0f;

        [Header("State Settings")]
        [SerializeField] private int _minJumpStepWhenGotLightFlashed = 2;
        [SerializeField] private int _maxJumpStepWhenGotLightFlashed = 2;
        [SerializeField] private float _accelaration = 1.15f;
        [SerializeField] private bool _resetTimeWhenJumpState = true;

        [Header("Accelaration Caps")]
        [SerializeField] private float _flashAccelCap = 2.5f;
        [SerializeField] private float _mistakeAccelCap = 3f;

        [Header("Mercy Jump Settings")]
        [SerializeField] private float _mercyAccelThreshold = 2f;
        [SerializeField] private int _mercyJumpStepMin = 2;
        [SerializeField] private int _mercyJumpStepMax = 3;

        [Header("State Readonly")]
        [SerializeField] public int CurrentState { get; private set; }
        [SerializeField] private float _flashAccelaration = 1f;
        [SerializeField] private float _mistakeAccelaration = 1f;
        [SerializeField] private int _minigamesCompleted = 0;
        [SerializeField] private float _rollTimer = 0f;
        [SerializeField] private float _timeSinceLastMove = 0f;

        [Header("Camera View")]
        [SerializeField] private CameraviewController _cameraViewController;


        private View _currentView = View.Mirror;
        private bool _gameOverBuffer = false;

        #region Base
        private void Start()
        {
            CurrentState = GameConstants.ENTITY_START_STATE;
            GameEvents.RaiseEntityStateChanged(CurrentState);

            GameEvents.OnViewChangeFinished += HandleViewChanged;
            GameEvents.OnLightFlashed += HandleLightFlashed;
            GameEvents.OnMinigameFailed += HandleMinigameFailed;
            GameEvents.OnMinigameCompleted += HandleMinigameCompleted;
        }

        private void OnDestroy()
        {
            GameEvents.OnViewChangeFinished -= HandleViewChanged;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameFailed -= HandleMinigameFailed;
            GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;
        }
        #endregion

        #region States change
        private void Update()
        {
            if (GameManager.Instance.CurrentState != GameState.Playing)
                return;

            UpdateRoll();
#if UNITY_EDITOR
            if (Input.GetKeyDown(UnityEngine.KeyCode.Space))
            {
                GameEvents.RaiseLightFlashed();
                FilterController.Instance.FlashScreen(FilterController.Instance.FlashColor);
                Camera.main.transform.DOShakePosition(0.5f, 0.35f, 12, 45f);
            }
#endif
        }

        private void UpdateRoll()
        {
            if (_gameOverBuffer)
            {
                // if (_currentView != View.Behind)
                // {
                //     GameEvents.RaiseJumpscareTriggered();
                //     GameManager.Instance.SetGameOver();
                //     _gameOverBuffer = false;
                // }
                if (_cameraViewController != null)
                {
                    _cameraViewController.SwitchToMirrorImmediately();      
                    GameEvents.RaiseJumpscareTriggered();
                    GameManager.Instance.SetGameOver();
                    _gameOverBuffer = false;
                    return;
                }
                else
                {
                    if (_currentView == View.Mirror)
                    {
                        GameEvents.RaiseJumpscareTriggered();
                        GameManager.Instance.SetGameOver();
                        _gameOverBuffer = false;
                    }
                    return;
                }
            }

            if (GameManager.Instance.CurrentState == GameState.GameOver)
                return;

            if (_currentView != View.Mirror)
            {
                _timerAwayFromMirror += Time.deltaTime;
            }

            _rollTimer += Time.deltaTime;
            _timeSinceLastMove += Time.deltaTime;

            float interval = GetCurrentInterval();
            float insurance = GetInsuranceTime();
            bool insuranceTriggered = _timeSinceLastMove >= insurance;

            if (_rollTimer >= interval)
            {
                _rollTimer = 0f;
                bool success = insuranceTriggered || UnityEngine.Random.value < GetCurrentChance();
                if (success)
                {
                    AdvanceState();
                }
            }
        }

        private void AdvanceState()
        {
            CurrentState--;
            _timeSinceLastMove = 0f;
            GameEvents.RaiseEntityStateChanged(CurrentState);

            if (_currentView == View.Mirror)
            {
                AudioController.Instance.PlaySFX(SoundName.Entity_ChangeState);
            }

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

        private int GetJumpStep()
        {
            float totalAccel = _flashAccelaration * _mistakeAccelaration;
            if (totalAccel >= _mercyAccelThreshold)
            {
                return UnityEngine.Random.Range(_mercyJumpStepMin, _mercyJumpStepMax + 1);
            }
            return UnityEngine.Random.Range(_minJumpStepWhenGotLightFlashed, _maxJumpStepWhenGotLightFlashed + 1);
        }

        private void JumpToState(int state)
        {
            CurrentState = Mathf.Clamp(state, 0, GameConstants.ENTITY_MAX_STATE);
            GameEvents.RaiseEntityStateChanged(CurrentState);

            if (_resetTimeWhenJumpState)
            {
                _rollTimer = 0f;
                _timeSinceLastMove = 0f;
            }
        }

        private void SetFlashAccelaration(float factor)
        {
            if (factor <= 0) return;
            _flashAccelaration = Mathf.Min(_flashAccelaration * factor, _flashAccelCap);
        }

        private void SetMistakeAccelaration(float factor)
        {
            if (factor <= 0) return;
            _mistakeAccelaration = Mathf.Min(_mistakeAccelaration * factor, _mistakeAccelCap);
        }

        private float GetAwayMultiplier()
        {
            if (_currentView == View.Mirror) return 1f;
            return Mathf.Min(1f + _timerAwayFromMirror * _awayRampRate, _awayRampCap);
        }

        private float GetIntervalMultiplierByView(View view)
        {
            foreach (var tv in _timeScaleViews)
            {
                if (tv.BaseView == view)
                    return tv.TimeScale;
            }
            return 1f;
        }

        private float GetCurrentInterval()
        {
            float totalAccel = _flashAccelaration * _mistakeAccelaration;
            float viewMultiplier = GetIntervalMultiplierByView(_currentView);
            float awayMultiplier = GetAwayMultiplier();

            float interval = _baseMoveInterval / (viewMultiplier * totalAccel * awayMultiplier);
            return Mathf.Max(interval, _minMoveInterval);
        }

        private float GetInsuranceTime()
        {
            return _baseInsuranceTime - (_insuranceTimeStep * _minigamesCompleted);
        }

        private float GetCurrentChance()
        {
            float chance = _baseMoveChance + (_minigamesCompleted * _moveChancePerMinigame);
            return Mathf.Min(chance, _moveChanceCap);
        }
        #endregion

        #region Handle Events
        private void HandleViewChanged(View view)
        {
            _currentView = view;
            if (view == View.Mirror)
            {
                _timerAwayFromMirror = 0f;
            }
        }

        private void HandleLightFlashed()
        {
            SetFlashAccelaration(_accelaration);
            int clamp = Mathf.Clamp(CurrentState + GetJumpStep(), 0, GameConstants.ENTITY_MAX_STATE);
            JumpToState(clamp);
        }

        private void HandleMinigameFailed(float accel)
        {
            SetMistakeAccelaration(accel);
        }

        private void HandleMinigameCompleted(MinigameType _, KeyCode __)
        {
            _minigamesCompleted++;
            _rollTimer = 0f; 
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