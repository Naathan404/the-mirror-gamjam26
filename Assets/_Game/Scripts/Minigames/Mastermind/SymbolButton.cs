using DG.Tweening;
using Game.Core;
using UnityEngine;

namespace Game.Minigames
{
    public class SymbolButton : MonoBehaviour
    {
        public int SymbolId { get; private set; }

        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _transform;
        [SerializeField] private Vector3 _moveOffset = Vector3.zero;
        [SerializeField] private float _moveduration = 1f;

        [Header("Visual Effects")]
        [SerializeField, Range(0.1f, 1f)] private float _spawnStartScale = 0.55f;
        [SerializeField] private float _spawnDuration = 0.22f;
        [SerializeField] private float _selectPunchScale = 0.14f;
        [SerializeField] private float _selectPunchDuration = 0.18f;
        [SerializeField] private float _restorePunchScale = 0.1f;
        [SerializeField] private float _restorePunchDuration = 0.16f;

        private bool _isMoving = false;
        private Vector3 _iconBaseScale = Vector3.one;
        private Tween _iconScaleTween;

        private void Awake()
        {
            if (_iconRenderer != null)
                _iconBaseScale = _iconRenderer.transform.localScale;
        }

        public void Init(int symbolId, Sprite icon)
        {
            SymbolId = symbolId;

            if (_iconRenderer != null)
            {
                _iconRenderer.sprite = icon;
                _iconRenderer.transform.localScale = _iconBaseScale;
            }
        }

        public void SetInteractable(bool isActive)
        {
            if (isActive)
            {
                _iconRenderer.color = Color.white;
            }
            else
            {
                _iconRenderer.color = Color.gray;
            }

            _collider.enabled = isActive;
        }

        public void PlaySpawnEffect(float delay = 0f)
        {
            if (_iconRenderer == null) return;

            Transform iconTransform = _iconRenderer.transform;

            if (_iconScaleTween != null && _iconScaleTween.IsActive())
                _iconScaleTween.Kill();

            iconTransform.localScale = _iconBaseScale * _spawnStartScale;

            _iconScaleTween = iconTransform
                .DOScale(_iconBaseScale, _spawnDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
        }

        public void PlayRestoreEffect()
        {
            if (_iconRenderer == null) return;

            if (_iconScaleTween != null && _iconScaleTween.IsActive())
                _iconScaleTween.Kill();

            _iconScaleTween = _iconRenderer.transform.DOPunchScale(
                Vector3.one * _restorePunchScale,
                _restorePunchDuration,
                5,
                0.5f
            );
        }

        private void PlaySelectEffect()
        {
            if (_iconRenderer == null) return;

            if (_iconScaleTween != null && _iconScaleTween.IsActive())
                _iconScaleTween.Kill();

            _iconScaleTween = _iconRenderer.transform.DOPunchScale(
                Vector3.one * _selectPunchScale,
                _selectPunchDuration,
                5,
                0.5f
            );
        }

        private void OnMouseDown()
        {
            if (_isMoving) return;
            _isMoving = true;

            PlaySelectEffect();

            Vector3 pos = _transform.position;

            _transform.DOKill();
            _transform.DOMove(pos + _moveOffset, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _transform.DOMove(pos, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    GameEvents.RaiseBatteryChargeStarted();
                    _isMoving = false;
                });
            });
        }
    }
}
