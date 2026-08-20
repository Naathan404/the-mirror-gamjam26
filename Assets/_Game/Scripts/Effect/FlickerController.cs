using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Utils;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Effect
{

    public enum FlickerPattern
    {
        Subtle,     // Rung nhẹ liên tục 
        Nervous,    // Nhấp nháy nhanh, biên độ lớn 
        Dying,      // Yếu dần theo thời gian 
        Strobe,     // Chớp tắt cực nhanh 
        PowerOut    // Chớp vài nhịp rồi tắt hẳn 
    }

    public class FlickerController : MonoSingleton<FlickerController>
    {
        [Header("References")]
        [SerializeField] private Light2D[] _targetLights;
        [SerializeField] private Light[] _target3DLights;

        [Header("Settings")]
        [SerializeField] private float _minIntensityFloor = 0.05f;
        [SerializeField] private float _briefFlickerDuration = 0.35f; 

        [Header("Ambient Random Flicker")]
        [Tooltip("Chớp ngẫu nhiên định kỳ để tạo không khí, không liên quan gì tới entity")]
        [SerializeField] private bool _enableAmbientFlicker = true;
        [SerializeField] private float _ambientMinInterval = 10f;
        [SerializeField] private float _ambientMaxInterval = 25f;
        [Range(0f, 1f)]
        [SerializeField] private float _ambientDoubleFlashChance = 0.3f; // Xác suất chớp 2 nhịp liên tiếp thay vì 1

        private float[] _originalIntensities;
        private float[] _original3DIntensities;
        private Coroutine _flickerRoutine;
        private Coroutine _ambientRoutine;
        private bool _isFlickering;

        private void Start()
        {
            GameEvents.OnEntityStateChanged += OnEntityStateChanged;

            if (_targetLights == null || _targetLights.Length == 0 || _target3DLights == null || _target3DLights.Length == 0)
            {
                Debug.LogError("[FlickerController] Chưa gán _targetLights");
                return;
            }

            _originalIntensities = new float[_targetLights.Length];
            _original3DIntensities = new float[_target3DLights.Length];
            for (int i = 0; i < _targetLights.Length; i++)
            {
                if (_targetLights[i] != null)
                    _originalIntensities[i] = _targetLights[i].intensity;
                if (_target3DLights[i] != null)
                    _original3DIntensities[i] = _target3DLights[i].intensity;
            }

            if (_enableAmbientFlicker)
                _ambientRoutine = StartCoroutine(AmbientFlickerRoutine());
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnEntityStateChanged -= OnEntityStateChanged;
        }


        public void SetAmbientFlickerEnabled(bool enabled)
        {
            _enableAmbientFlicker = enabled;

            if (!enabled && _ambientRoutine != null)
            {
                StopCoroutine(_ambientRoutine);
                _ambientRoutine = null;
            }
            else if (enabled && _ambientRoutine == null)
            {
                _ambientRoutine = StartCoroutine(AmbientFlickerRoutine());
            }
        }

        #region Flickers


        public void StartFlicker(FlickerPattern pattern)
        {
            if (_isFlickering && _flickerRoutine != null)
                StopCoroutine(_flickerRoutine);

            _isFlickering = true;
            _flickerRoutine = StartCoroutine(FlickerRoutine(pattern, -1f));
        }


        public void FlickerFor(FlickerPattern pattern, float duration)
        {
            if (_isFlickering && _flickerRoutine != null)
                StopCoroutine(_flickerRoutine);

            _isFlickering = true;
            _flickerRoutine = StartCoroutine(FlickerRoutine(pattern, duration));
        }


        public void StopFlicker(float restoreDuration = 0.3f)
        {
            if (_flickerRoutine != null)
            {
                StopCoroutine(_flickerRoutine);
                _flickerRoutine = null;
            }
            _isFlickering = false;

            for (int i = 0; i < _targetLights.Length; i++)
            {
                var light = _targetLights[i];
                if (light == null) continue;

                DOTween.Kill(light);
                float target = _originalIntensities[i];
                DOTween.To(() => light.intensity, x => light.intensity = x, target, restoreDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(light);
            }
            for (int i = 0; i < _target3DLights.Length; i++)
            {
                var light = _target3DLights[i];
                if (light == null) continue;

                DOTween.Kill(light);
                float target = _original3DIntensities[i];
                DOTween.To(() => light.intensity, x => light.intensity = x, target, restoreDuration)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(light);
            }

        }


        public void SingleFlash(float downTime = 0.08f, float upTime = 0.15f)
        {
            StartCoroutine(SingleFlashRoutine(downTime, upTime));
        }


        public void OnEntityStateChanged(int newState)
        {
            switch (newState)
            {
                case 6: // bình thường / entity còn xa
                    StopFlicker();
                    break;
                case 5:
                case 4:
                case 3:
                case 2:
                    StartFlicker(FlickerPattern.Subtle);
                    break;

                case 1:
                    FlickerFor(FlickerPattern.Nervous, _briefFlickerDuration);
                    break;

                case 0:
                case -1:
                    break;

                default:
                    StopFlicker();
                    break;
            }
        }

        #endregion

        #region Routines

        private IEnumerator FlickerRoutine(FlickerPattern pattern, float duration)
        {
            float elapsed = 0f;

            while (duration < 0f || elapsed < duration)
            {
                switch (pattern)
                {
                    case FlickerPattern.Subtle:
                        yield return DoFlickerStep(0.8f, 1f, 0.08f, 0.18f);
                        break;

                    case FlickerPattern.Nervous:
                        yield return DoFlickerStep(0.35f, 1f, 0.03f, 0.1f);
                        break;

                    case FlickerPattern.Dying:
                        float t = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
                        float ceiling = Mathf.Lerp(1f, _minIntensityFloor, t);
                        yield return DoFlickerStep(_minIntensityFloor, ceiling, 0.05f, 0.15f);
                        break;

                    case FlickerPattern.Strobe:
                        yield return DoFlickerStep(_minIntensityFloor, 1f, 0.02f, 0.05f);
                        break;

                    case FlickerPattern.PowerOut:
                        yield return DoFlickerStep(_minIntensityFloor, 1f, 0.05f, 0.1f);
                        break;
                }

                elapsed += Time.deltaTime;

                if (pattern == FlickerPattern.PowerOut && elapsed > 0.6f)
                {
                    SetIntensityImmediate(0f);
                    _isFlickering = false;
                    yield break;
                }
            }

            if (duration >= 0f)
                StopFlicker();
        }

        private IEnumerator DoFlickerStep(float minMul, float maxMul, float minHold, float maxHold)
        {
            float mul = Random.Range(minMul, maxMul);
            SetIntensityScaled(mul);

            float hold = Random.Range(minHold, maxHold);
            yield return new WaitForSeconds(hold);
        }

        /// <summary>
        /// Chạy nền suốt game, chớp đèn ngẫu nhiên theo khoảng thời gian random để tạo không khí
        /// 
        /// </summary>
        private IEnumerator AmbientFlickerRoutine()
        {
            while (true)
            {
                float wait = Random.Range(_ambientMinInterval, _ambientMaxInterval);
                yield return new WaitForSeconds(wait);

                if (_isFlickering) continue;

                SingleFlash();

                if (Random.value < _ambientDoubleFlashChance)
                {
                    yield return new WaitForSeconds(0.2f);
                    if (!_isFlickering) SingleFlash();
                }
            }
        }

        private IEnumerator SingleFlashRoutine(float downTime, float upTime)
        {
            SetIntensityImmediate(_minIntensityFloor);
            yield return new WaitForSeconds(downTime);

            for (int i = 0; i < _targetLights.Length; i++)
            {
                var light = _targetLights[i];
                if (light == null) continue;

                DOTween.Kill(light);
                float target = _originalIntensities[i];
                DOTween.To(() => light.intensity, x => light.intensity = x, target, upTime)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(light);
            }

            for (int i = 0; i < _target3DLights.Length; i++)
            {
                var light = _target3DLights[i];
                if (light == null) continue;

                DOTween.Kill(light);
                float target = _original3DIntensities[i];
                DOTween.To(() => light.intensity, x => light.intensity = x, target, upTime)
                    .SetEase(Ease.OutQuad)
                    .SetTarget(light);
            }
        }

        private void SetIntensityScaled(float multiplier)
        {
            for (int i = 0; i < _targetLights.Length; i++)
            {
                if (_targetLights[i] == null) continue;
                _targetLights[i].intensity = _originalIntensities[i] * multiplier;
            }

            for (int i = 0; i < _target3DLights.Length; i++)
            {
                if (_target3DLights[i] == null) continue;
                _target3DLights[i].intensity = _original3DIntensities[i] * multiplier;
            }
        }

        private void SetIntensityImmediate(float value)
        {
            foreach (var light in _targetLights)
            {
                if (light == null) continue;
                light.intensity = value;
            }
            foreach (var light in _target3DLights)
            {
                if (light == null) continue;
                light.intensity = value;
            }
        }

        #endregion
    }
}