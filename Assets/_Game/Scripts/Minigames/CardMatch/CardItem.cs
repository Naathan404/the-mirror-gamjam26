using UnityEngine;
using System;
using System.Collections;
using DG.Tweening;

namespace Game.Minigames.CardMatch
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardItem : MonoBehaviour
    {
        public event Action<CardItem> OnCardClicked;

        public int CardID { get; private set; }
        public bool IsFaceUp { get; private set; }
        public bool IsMatched { get; private set; }

        [Header("Visual FX - Additive")]
        [SerializeField] private float _spawnFadeDuration = 0.18f;
        [SerializeField] private float _spawnRotationOffset = 6f;
        [SerializeField] private float _flipSettleDuration = 0.14f;
        [SerializeField] private float _flipSettleRotation = 4f;
        [SerializeField] private float _matchedPunchScale = 0.12f;
        [SerializeField] private float _matchedPunchDuration = 0.22f;
        [SerializeField] private float _mismatchShakeDuration = 0.22f;
        [SerializeField] private float _mismatchShakeStrength = 7f;
        [SerializeField] private Color _mismatchTint = new Color(1f, 0.72f, 0.72f, 1f);

        private SpriteRenderer _spriteRenderer;
        private Sprite _faceSprite;
        private Sprite _backSprite;
        private float _flipDuration;
        private bool _isAnimating;

        private Color _defaultColor;
        private Vector3 _baseScale;
        private Quaternion _baseLocalRotation;

        private Tween _spawnTween;
        private Tween _flipSettleTween;
        private Tween _matchedTween;
        private Tween _mismatchTween;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();

            // Chỉ lưu trạng thái visual gốc để VFX luôn trả object về đúng trạng thái ban đầu.
            _defaultColor = _spriteRenderer.color;
            _baseScale = transform.localScale;
            _baseLocalRotation = transform.localRotation;
        }

        public void Initialize(int id, Sprite face, Sprite back, float flipDuration)
        {
            CardID = id;
            _faceSprite = face;
            _backSprite = back;
            _flipDuration = flipDuration;

            IsFaceUp = true;
            IsMatched = false;
            _isAnimating = false;

            // Mặc định lúc mới sinh ra là ngửa (để xem trước)
            _spriteRenderer.sprite = _faceSprite;
        }

        private void OnMouseDown()
        {
            if (!IsFaceUp && !IsMatched && !_isAnimating)
            {
                OnCardClicked?.Invoke(this);
            }
        }

        public void FlipUp()
        {
            FinishSpawnVisualIfNeeded();
            AudioController.Instance.PlaySFX(SoundName.Card_Flip_Up);
            StartCoroutine(FlipRoutine(true));
        }

        public void FlipDown()
        {
            FinishSpawnVisualIfNeeded();
            AudioController.Instance.PlaySFX(SoundName.Card_Flip_Down);
            StartCoroutine(FlipRoutine(false));
        }

        private IEnumerator FlipRoutine(bool toFaceUp)
        {
            _isAnimating = true;
            float halfDuration = _flipDuration / 2f;
            Vector3 scale = transform.localScale;

            // Bước 1: Bóp trục X từ 1 về 0
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                scale.x = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
                transform.localScale = scale;
                yield return null;
            }

            // Bước 2: Đổi hình ở khoảnh khắc lá bài mỏng nhất (Scale X = 0)
            _spriteRenderer.sprite = toFaceUp ? _faceSprite : _backSprite;
            IsFaceUp = toFaceUp;

            // Bước 3: Mở trục X từ 0 lên 1
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                scale.x = Mathf.Lerp(0f, 1f, elapsed / halfDuration);
                transform.localScale = scale;
                yield return null;
            }

            scale.x = 1f;
            transform.localScale = scale;
            _isAnimating = false;

            // VFX bổ sung: chỉ punch rotation SAU KHI coroutine flip cũ đã hoàn tất.
            // Không tween scale ở đây để tránh xung đột với logic flip hiện tại.
            PlayFlipSettleEffect(toFaceUp);
        }

        public void SetMatched()
        {
            IsMatched = true;

            _flipSettleTween?.Kill();
            transform.localRotation = _baseLocalRotation;

            // Giữ nguyên visual cũ: đổi màu hơi tối đi để báo hiệu đã ghép xong.
            Color matchedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            _spriteRenderer.color = matchedColor;

            // VFX bổ sung chỉ chạy sau khi match đã được xác nhận.
            _matchedTween?.Kill();
            transform.localScale = _baseScale;
            _matchedTween = transform
                .DOPunchScale(_baseScale * _matchedPunchScale, _matchedPunchDuration, 6, 0.55f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = _baseScale);
        }

        public void PlaySpawnEffect(float delay)
        {
            if (_spriteRenderer == null) return;

            _spawnTween?.Kill();

            Color targetColor = _defaultColor;
            Color startColor = targetColor;
            startColor.a = 0f;
            _spriteRenderer.color = startColor;

            float direction = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, _spawnRotationOffset * direction);

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(Mathf.Max(0f, delay));
            sequence.Append(_spriteRenderer.DOFade(targetColor.a, _spawnFadeDuration).SetEase(Ease.OutQuad));
            sequence.Join(transform
                .DOLocalRotate(_baseLocalRotation.eulerAngles, _spawnFadeDuration)
                .SetEase(Ease.OutBack));

            sequence.OnComplete(() =>
            {
                if (!IsMatched)
                    _spriteRenderer.color = _defaultColor;

                transform.localRotation = _baseLocalRotation;
            });

            _spawnTween = sequence;
        }

        private void PlayFlipSettleEffect(bool toFaceUp)
        {
            _flipSettleTween?.Kill();

            transform.localRotation = _baseLocalRotation;

            float direction = toFaceUp ? 1f : -1f;
            _flipSettleTween = transform
                .DOPunchRotation(
                    new Vector3(0f, 0f, _flipSettleRotation * direction),
                    _flipSettleDuration,
                    5,
                    0.45f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localRotation = _baseLocalRotation);
        }

        public void PlayMismatchEffect()
        {
            if (IsMatched || _spriteRenderer == null) return;

            _mismatchTween?.Kill();
            _flipSettleTween?.Kill();

            transform.localRotation = _baseLocalRotation;

            Sequence sequence = DOTween.Sequence();

            sequence.Join(transform
                .DOShakeRotation(
                    _mismatchShakeDuration,
                    new Vector3(0f, 0f, _mismatchShakeStrength),
                    12,
                    70f));

            sequence.Join(_spriteRenderer.DOColor(_mismatchTint, _mismatchShakeDuration * 0.45f));
            sequence.Append(_spriteRenderer.DOColor(_defaultColor, _mismatchShakeDuration * 0.35f));

            sequence.OnComplete(() =>
            {
                transform.localRotation = _baseLocalRotation;

                if (!IsMatched)
                    _spriteRenderer.color = _defaultColor;
            });

            _mismatchTween = sequence;
        }


        private void FinishSpawnVisualIfNeeded()
        {
            if (_spawnTween == null || !_spawnTween.IsActive()) return;

            _spawnTween.Kill();
            _spawnTween = null;

            if (!IsMatched)
                _spriteRenderer.color = _defaultColor;

            transform.localRotation = _baseLocalRotation;
        }

        public void PlaySuccessEffect(float delay)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(Mathf.Max(0f, delay));
            sequence.Append(transform
                .DOPunchScale(_baseScale * (_matchedPunchScale * 0.75f), _matchedPunchDuration, 5, 0.45f)
                .SetEase(Ease.OutQuad));

            sequence.OnComplete(() => transform.localScale = _baseScale);
        }

        private void OnDestroy()
        {
            _spawnTween?.Kill();
            _flipSettleTween?.Kill();
            _matchedTween?.Kill();
            _mismatchTween?.Kill();
        }
    }
}
