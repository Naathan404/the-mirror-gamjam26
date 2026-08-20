using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Utils;
using UnityEngine;

namespace Game.Effect
{
    public class VhsController : MonoSingleton<VhsController>
    {
        [Header("Vhs")]
        [SerializeField] private SpriteRenderer _vhsEntityChangeStateSpriteRenderer;
        [SerializeField] private SpriteRenderer _vhsRandomFlickerSpriteRenderer;

        [SerializeField] private float _minVhsDuration = 0.2f;
        [SerializeField] private float _maxVhsDuration = 1f;

        [Header("Ambient Random Glitch")]
        [SerializeField] private bool _enableAmbientGlitch = true;
        [SerializeField] private float _ambientMinInterval = 12f;
        [SerializeField] private float _ambientMaxInterval = 30f;
        [SerializeField] private float _ambientGlitchMinDuration = 0.1f;
        [SerializeField] private float _ambientGlitchMaxDuration = 0.35f;

        private bool _isPlaying = false;
        private bool _isAmbientPlaying = false;
        private View _currentView = View.Mirror;

        private void Start()
        {
            GameEvents.OnEntityStateChanged += PlayVhsEffect;
            GameEvents.OnViewChangeFinished += HandleViewChanged;

            _vhsEntityChangeStateSpriteRenderer.gameObject.SetActive(false);

            if (_vhsRandomFlickerSpriteRenderer != null)
            {
                _vhsRandomFlickerSpriteRenderer.gameObject.SetActive(false);

                if (_enableAmbientGlitch)
                    StartCoroutine(AmbientGlitchRoutine());
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnEntityStateChanged -= PlayVhsEffect;
            GameEvents.OnViewChangeFinished -= HandleViewChanged;
        }

        public void PlayVhsEffect()
        {
            if (_vhsEntityChangeStateSpriteRenderer != null && !_vhsEntityChangeStateSpriteRenderer.gameObject.activeSelf)
            {
                float duration = Random.Range(_minVhsDuration, _maxVhsDuration);
                _vhsEntityChangeStateSpriteRenderer.gameObject.SetActive(true);
                _vhsEntityChangeStateSpriteRenderer.transform.DOShakePosition(0.2f, 0.2f, 15, 90f);
                StartCoroutine(VhsEffectRoutine(duration));
            }
        }

        private void PlayVhsEffect(int state)
        {
            if (state <= 0) return;
            if (_isPlaying || _currentView != View.Mirror) return;
            _isPlaying = true;

            if (_vhsEntityChangeStateSpriteRenderer != null && !_vhsEntityChangeStateSpriteRenderer.gameObject.activeSelf)
            {
                float duration = Random.Range(_minVhsDuration, _maxVhsDuration);
                _vhsEntityChangeStateSpriteRenderer.gameObject.SetActive(true);
                _vhsEntityChangeStateSpriteRenderer.transform.DOShakePosition(duration, 0.5f, 30, 90f);
                StartCoroutine(VhsEffectRoutine(duration));
            }
        }

        private IEnumerator VhsEffectRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _vhsEntityChangeStateSpriteRenderer.gameObject.SetActive(false);
            _isPlaying = false;
        }

        private void HandleViewChanged(View view)
        {
            _currentView = view;
        }

        /// <summary>
        /// Chạy nền suốt game, glitch hình ngẫu nhiên theo khoảng thời gian random để tạo không khí.
        /// hiệu ứng này ddeerte thuần túy tạo cảm giác
        /// bất an, không mang ý nghĩa cảnh báo gì cho người chơi.
        /// </summary>
        private IEnumerator AmbientGlitchRoutine()
        {
            while (true)
            {
                float wait = Random.Range(_ambientMinInterval, _ambientMaxInterval);
                yield return new WaitForSeconds(wait);

                if (_isPlaying || _isAmbientPlaying) continue;

                _isAmbientPlaying = true;

                float duration = Random.Range(_ambientGlitchMinDuration, _ambientGlitchMaxDuration);
                _vhsRandomFlickerSpriteRenderer.gameObject.SetActive(true);
                _vhsRandomFlickerSpriteRenderer.transform.DOShakePosition(duration, 0.3f, 20, 60f);

                yield return new WaitForSeconds(duration);

                _vhsRandomFlickerSpriteRenderer.gameObject.SetActive(false);
                _isAmbientPlaying = false;
            }
        }
    }
}