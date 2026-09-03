using DG.Tweening;
using Game.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.Effect
{
    public class FilterController : MonoSingleton<FilterController>
    {
        [Header("References")]
        [SerializeField] private Volume _globalVolume;

        public Color HazardColor = new Color(1f, 0.3f, 0.3f);
        public Color AdvantageColor = new Color(0.3f, 1f, 0.3f);
        public Color FlashColor = new Color(2f, 2f, 2f);
        
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;
        private float _defaultVignetteIntensity;
        private Color _defaultVignetteColor;
        private bool _isDefaultCached = false;

        public override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _globalVolume = FindAnyObjectByType<Volume>();

            if (_globalVolume != null)
            {
                if (_globalVolume.profile.TryGet(out _colorAdjustments))
                {
                    _colorAdjustments.colorFilter.value = Color.white;
                    _colorAdjustments.saturation.value = 0f;
                    _colorAdjustments.contrast.value = 0f;
                }

                if (_globalVolume.profile.TryGet(out _vignette))
                {
                    if (!_isDefaultCached)
                    {
                        _defaultVignetteIntensity = _vignette.intensity.value;
                        _defaultVignetteColor = _vignette.color.value;
                        _isDefaultCached = true;
                    }
                    else
                    {
                        _vignette.intensity.value = _defaultVignetteIntensity;
                        _vignette.color.value = _defaultVignetteColor;
                    }
                }
            }
            else
            {
                Debug.LogWarning("[FilterController] Scene này không có Volume!");
            }
        }


        /// <summary>
        /// Hàm nhấp nháy màn hình
        /// </summary>
        /// <param name="targetColor": Màu nhấp nháy></param>
        /// <param name="flashDuration": Thời gian nhấp nháy></param>
        public void FlashScreen(Color targetColor, float flashDuration = 0.5f)
        {
            if(_colorAdjustments == null) return;

            DOTween.Kill(_colorAdjustments);
            _colorAdjustments.colorFilter.value = targetColor;

            DOTween.To(
                () => _colorAdjustments.colorFilter.value, 
                x => _colorAdjustments.colorFilter.value = x, 
                Color.white, 
                flashDuration
            )
            .SetEase(Ease.OutQuad)
            .SetTarget(_colorAdjustments);
        }

        /// <summary>
        /// Hàm chớp viền camera
        /// </summary>
        /// <param name="targetColor">Màu của viền</param>
        /// <param name="maxIntensity">Độ dày của viền</param>
        /// <param name="flashDuration">Thời gian viền mờ dần về 0</param>
        public void FlashVignette(Color targetColor, float maxIntensity = 0.4f, float flashDuration = 0.4f)
        {
            if (_vignette == null) return;
            DOTween.Kill(_vignette);

            _vignette.color.value = targetColor;
            _vignette.intensity.value = maxIntensity;

            DOTween.To(
                () => _vignette.intensity.value, 
                x => _vignette.intensity.value = x, 
                _defaultVignetteIntensity, 
                flashDuration
            )
            .SetEase(Ease.OutQuad)
            .SetTarget(_vignette).WaitForCompletion();

            DOTween.To(
                () => _vignette.color.value, 
                x => _vignette.color.value = x, 
                _defaultVignetteColor, 
                0.2f
            )
            .SetEase(Ease.OutQuad)
            .SetTarget(_vignette);
        }

        public void PlayEyeClosedVignetteEffect(Color color, float duration = 0.5f)
        {
            if (_vignette == null) return;
            DOTween.Kill(_vignette);

            _vignette.color.value = color;
            _vignette.intensity.value = 2f;

            DOTween.To(
                () => _vignette.intensity.value, 
                x => _vignette.intensity.value = x, 
                _defaultVignetteIntensity, 
                duration
            )
            .SetEase(Ease.InCubic);
        }

        public void PlayEyeOpenedVignetteEffect(float duration = 0.5f)
        {
            if (_vignette == null) return;
            DOTween.Kill(_vignette);
            
            _vignette.color.value = Color.black;
            _vignette.intensity.value = 2f;

            DOTween.To(
                () => _vignette.intensity.value,
                x => _vignette.intensity.value = x,
                _defaultVignetteIntensity,
                duration
            )
            .SetEase(Ease.OutCubic);
        }

        /// <summary>
        /// Bật/Tắt chế độ tập trung bằng cách làm tối dày 4 góc màn hình
        /// </summary>
        /// <param name="active">True để tối góc, False để trả về bình thường</param>
        /// <param name="duration">Thời gian chuyển đổi</param>
        public void SetFocusMode(bool active, float duration = 0.5f)
        {
            if (_vignette == null) return;
            DOTween.Kill(_vignette);

            float targetIntensity = active ? 0.8f : _defaultVignetteIntensity; 
            Color targetColor = active ? Color.black : _defaultVignetteColor;

            DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, targetIntensity, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(_vignette);

            DOTween.To(() => _vignette.color.value, x => _vignette.color.value = x, targetColor, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(_vignette);
        }

        public void SetDramaticFilter(bool isBlackAndWhite = true)
        {
            if(_colorAdjustments == null) return;

            _colorAdjustments.saturation.value = 0f;
            _colorAdjustments.contrast.value = 0f;
            DOTween.Kill(_colorAdjustments);
            
            float targetSat = isBlackAndWhite ? -100f : 0f;
            float targetContrast = isBlackAndWhite ? 20f : 0f;

            DOTween.To(() => _colorAdjustments.saturation.value, x => _colorAdjustments.saturation.value = x, targetSat, 1f);

            DOTween.To(() => _colorAdjustments.contrast.value, x => _colorAdjustments.contrast.value = x, targetContrast, 1f);
        }
    }
}
