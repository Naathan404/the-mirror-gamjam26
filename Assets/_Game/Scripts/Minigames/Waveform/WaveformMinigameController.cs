using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using UnityEngine;

namespace Game.Minigames.Waveform
{
    public sealed class WaveformMinigameController : MinigameBaseController
    {
        [Header("Config")]
        [SerializeField] private WaveformConfigSO _config;

        [Header("Visual")]
        [SerializeField] private WaveformRenderer _targetRenderer; 
        [SerializeField] private WaveformRenderer _playerRenderer; 
        [SerializeField] private List<GameObject> _mistakeWarnings;
        [SerializeField] private SpriteRenderer _background;

        [Header("Mistakes")]
        [SerializeField] private Color _mistakeColor = Color.red;
        [SerializeField] private float _mistakeFlashDuration = 0.5f;

        [Header("Dial Prefabs")]
        [SerializeField] private WaveformDial _ampDialPrefab;
        [SerializeField] private WaveformDial _freqDialPrefab;
        [SerializeField] private Transform _dialContainer;
        [SerializeField] private float _dialSpacing = 1.2f;
        [SerializeField] private float _dialSurfaceOffset = 0.02f;
        [SerializeField] private WaveformDial[] _dials;

        [Header("Row Anchors")]
        [SerializeField] private Transform _targetRowAnchor;
        [SerializeField] private Transform _playerRowAnchor;
        [SerializeField] private Transform _ampRowAnchor;
        [SerializeField] private Transform _freqRowAnchor;

        [Header("Amplitude Visual Scale")]
        [SerializeField] private float _rowHalfHeight = 0.25f; // giới hạn biên độ vẽ trong mỗi hàng


        private SineComponent[] _target;
        private SineComponent[] _player;
        private int _mistakeCount;

        private float xMin, xMax;

        private System.Action<WaveformDial>[] _dialHandlers;

        #region Base
        protected override void OnDifficultyIncrease(int minigamePassed)
        {
             _config = _difficultyConfig.GetMinigameConfig<WaveformConfigSO>(minigamePassed);
        }

        protected override void OnGameStart()
        {
            ComputeBounds(out xMin, out xMax);
            Generate();
            HideMistakeWarningPanel();
        }

        protected override void OnGameReset()
        {
            Generate();
        }

        protected override void OnGameClosed()
        {
            base.OnGameClosed();
            foreach (var dial in _dials) dial.OnValueChanged -= OnDialChanged;
        }
        #endregion

        private float GetLocalRowY(Transform anchor)
        {
            Vector3 localPoint = _background.transform.InverseTransformPoint(anchor.position);
            return localPoint.y;
        }

        private void RebindDialListeners()
        {
            foreach (var dial in _dials)
            {
                dial.OnValueChanged -= OnDialChanged;
                dial.OnValueChanged += OnDialChanged;
            }
        }

        #region Generator
        private void SpawnDials()
        {
            foreach (Transform child in _dialContainer) Destroy(child.gameObject);

            int count = _config.WaveComponentCount;
            _dials = new WaveformDial[count * 2];

            ComputeBounds(out float xMinB, out float xMaxB);
            float ampRowY = GetLocalRowY(_ampRowAnchor);
            float freqRowY = GetLocalRowY(_freqRowAnchor);
            float colStep = (xMaxB - xMinB) / (count + 1);

            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                float localX = xMinB + colStep * (i + 1);

                var ampDial = Instantiate(_ampDialPrefab, _dialContainer);
                ampDial.SetIndex(i, WaveformDial.ParamType.Amplitude);
                PlaceOnSurface(ampDial.transform, localX, ampRowY);
                _dials[idx++] = ampDial;

                var freqDial = Instantiate(_freqDialPrefab, _dialContainer);
                freqDial.SetIndex(i, WaveformDial.ParamType.Frequency);
                PlaceOnSurface(freqDial.transform, localX, freqRowY);
                _dials[idx++] = freqDial;
            }

            foreach (var dial in _dials) dial.OnValueChanged += OnDialChanged;

            RebindDialListeners();
        }

        private void PlaceOnSurface(Transform t, float localX, float localY)
        {
            Vector3 localPoint = new Vector3(localX, localY, -0.02f); // lùi nhẹ theo pháp tuyến bề mặt
            t.position = _background.transform.TransformPoint(localPoint);
            t.rotation = _background.transform.rotation; // áp đúng góc nghiêng mặt bàn
        }

        private void Generate()
        {
            if (_dials == null || _dials.Length != _config.WaveComponentCount * 2)
                SpawnDials();

            _target = WaveformGenerator.Generate(_config);
            _player = new SineComponent[_config.WaveComponentCount];

            float targetRowY = GetLocalRowY(_targetRowAnchor);
            float playerRowY = GetLocalRowY(_playerRowAnchor);

            _targetRenderer.Draw(_target, _background.transform, xMin, xMax, _config.DomainHalfWidth, targetRowY, _rowHalfHeight);

            for (int i = 0; i < _config.WaveComponentCount; i++)
            {
                _player[i] = new SineComponent
                {
                    Amplitude = _config.AmplitudeRange.x,
                    Frequency = _config.FrequencyRange.x,
                    Phase = _target[i].Phase
                };
            }

            foreach (var dial in _dials)
            {
                float initVal = dial.Type == WaveformDial.ParamType.Amplitude
                    ? _config.AmplitudeRange.x
                    : _config.FrequencyRange.x;
                dial.Init(_config, initVal);
            }

            _playerRenderer.Draw(_player, _background.transform, xMin, xMax, _config.DomainHalfWidth, playerRowY, _rowHalfHeight);
            _mistakeCount = 0;
        }


        private void ComputeBounds(out float xMin, out float xMax)
        {
            Vector2 extents = _background.sprite.bounds.extents;
            const float margin = 0.8f;
            xMin = -extents.x * margin;
            xMax = extents.x * margin;
        }

        #endregion

        #region interact
        public void OnConfirmPressed()
        {
            float error = WaveformMath.CalculateError(_target, _player, xMin, xMax, _config.SampleResolution);

            if (error <= _config.MatchErrorTolerance)
            {
                CompleteMinigame();
            }
            else
            {
                FilterController.Instance.FlashScreen(_mistakeColor, _mistakeFlashDuration);
                Camera.main.transform.DOShakePosition(_mistakeFlashDuration, 0.5f, 20, 90f);

                _mistakeCount++;
                AudioController.Instance.PlaySFX(SoundName.Waveform_Fail);
                for(int i = 0; i < _mistakeCount; i++)
                {
                    _mistakeWarnings[i].gameObject.SetActive(true);
                }
                
                if (_mistakeCount >= _config.MaxMistakes)
                {
                    OnFailed();
                    _mistakeCount = 0;
                }
            }
        }

        private void HideMistakeWarningPanel()
        {
            foreach(var o in _mistakeWarnings) o.SetActive(false);
        }
        #endregion

        #region Dial Change
        private void OnDialChanged(WaveformDial dial)
        {
            var wave = _player[dial.WaveIndex];
            if (dial.Type == WaveformDial.ParamType.Amplitude)
                wave.Amplitude = dial.CurrentValue;
            else
                wave.Frequency = dial.CurrentValue;
            _player[dial.WaveIndex] = wave;

            float playerRowY = GetLocalRowY(_playerRowAnchor);
            _playerRenderer.Draw(_player, _background.transform, xMin, xMax, _config.DomainHalfWidth, playerRowY, _rowHalfHeight);
        }
        #endregion

        #region debug
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(UnityEngine.KeyCode.W))
            {
                Debug.Log("[Test] Force OnGameStart");
                OnGameStart(); 
            }

            if (Input.GetKeyDown(UnityEngine.KeyCode.R))
            {
                Debug.Log("[Test] Force Regenerate");
                Generate();
            }
        }
        private void OnGUI()
        {
            if (_player == null) return;
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            for (int i = 0; i < _player.Length; i++)
            {
                GUILayout.Label($"Wave {i}: Amp={_player[i].Amplitude:F2} Freq={_player[i].Frequency:F2}");
            }
            GUILayout.EndArea();
        }
#endif
        #endregion
    }
}