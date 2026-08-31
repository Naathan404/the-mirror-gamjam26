using UnityEngine;
using System.Collections;
using DG.Tweening;

namespace Game.Minigames.Maze
{
    public class MazePlayer : MonoBehaviour
    {
        [Header("Cài đặt Di chuyển")]
        [Tooltip("Tốc độ trượt của nét bút (càng lớn càng nhanh)")]
        public float moveSpeed = 5f;

        [Header("Visual FX - Additive")]
        [SerializeField] private SpriteRenderer _visualRenderer;
        [SerializeField] private float _spawnDuration = 0.18f;
        [SerializeField] private float _spawnStartScale = 0.72f;
        [SerializeField] private float _movePunchScale = 0.10f;
        [SerializeField] private float _movePunchDuration = 0.16f;
        [SerializeField] private float _blockedPunchScale = 0.12f;
        [SerializeField] private float _blockedPunchDuration = 0.18f;
        [SerializeField] private Color _hitColor = new Color(1f, 0.35f, 0.35f, 1f);
        [SerializeField] private float _hitDuration = 0.25f;
        [SerializeField] private float _successPunchScale = 0.18f;
        [SerializeField] private float _successPunchDuration = 0.30f;

        private Vector3 _baseScale;
        private Color _baseColor = Color.white;
        private Tween _scaleTween;
        private Tween _colorTween;

        private void Awake()
        {
            _baseScale = transform.localScale;

            if (_visualRenderer == null)
                _visualRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_visualRenderer != null)
                _baseColor = _visualRenderer.color;
        }

        // Vị trí hiện tại trên lưới Data
        public Vector2Int CurrentGridPos { get; private set; }

        // Cờ kiểm tra xem bút có đang trượt dở không (để chặn spam phím)
        public bool IsMoving { get; private set; }

        /// <summary>
        /// Khởi tạo vị trí ban đầu khi bắt đầu game hoặc khi bị Reset
        /// </summary>
        public void Initialize(Vector2Int startGridPos, Vector3 startWorldPos)
        {
            CurrentGridPos = startGridPos;
            transform.position = startWorldPos;

            // Xoay nhân vật nằm bẹp xuống giấy giống như EndMarker
            transform.rotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;

            IsMoving = false;

            // VFX bổ sung, không thay đổi vị trí/state vừa Initialize.
            PlaySpawnEffect();
        }

        /// <summary>
        /// Lệnh di chuyển sang ô mới (Gọi từ MazeController)
        /// </summary>
        public void MoveTo(Vector2Int newGridPos, Vector3 targetWorldPos)
        {
            if (IsMoving) return;

            CurrentGridPos = newGridPos;

            // VFX scale chạy song song, không can thiệp position của SmoothMoveRoutine.
            PlayMoveEffect();
            StartCoroutine(SmoothMoveRoutine(targetWorldPos));
        }

        /// <summary>
        /// Coroutine giúp nét bút trượt mượt mà thay vì giật cục (teleport)
        /// </summary>
        private IEnumerator SmoothMoveRoutine(Vector3 targetPos)
        {
            IsMoving = true;

            // Dùng Vector3.MoveTowards để di chuyển đều đặn
            while (Vector3.Distance(transform.position, targetPos) > 0.001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                // Đợi frame tiếp theo rồi chạy tiếp vòng lặp
                yield return null;
            }

            // Đảm bảo đến đích chính xác 100%
            transform.position = targetPos;
            IsMoving = false;
        }

        public void PlaySpawnEffect()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();

            transform.localScale = _baseScale * _spawnStartScale;
            _scaleTween = transform.DOScale(_baseScale, _spawnDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => transform.localScale = _baseScale);

            if (_visualRenderer != null)
            {
                Color startColor = _baseColor;
                startColor.a = 0f;
                _visualRenderer.color = startColor;
                _colorTween = _visualRenderer.DOColor(_baseColor, _spawnDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        public void PlayMoveEffect()
        {
            _scaleTween?.Kill();
            transform.localScale = _baseScale;
            _scaleTween = transform.DOPunchScale(
                    _baseScale * _movePunchScale,
                    _movePunchDuration,
                    4,
                    0.45f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = _baseScale);
        }

        public void PlayBlockedEffect()
        {
            if (IsMoving) return;

            _scaleTween?.Kill();
            transform.localScale = _baseScale;
            _scaleTween = transform.DOPunchScale(
                    _baseScale * -_blockedPunchScale,
                    _blockedPunchDuration,
                    5,
                    0.55f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = _baseScale);
        }

        public void PlayHitEffect()
        {
            _scaleTween?.Kill();
            transform.localScale = _baseScale;
            _scaleTween = transform.DOPunchScale(
                    _baseScale * 0.20f,
                    _hitDuration,
                    7,
                    0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = _baseScale);

            if (_visualRenderer != null)
            {
                _colorTween?.Kill();
                Sequence sequence = DOTween.Sequence();
                sequence.Append(_visualRenderer.DOColor(_hitColor, _hitDuration * 0.35f));
                sequence.Append(_visualRenderer.DOColor(_baseColor, _hitDuration * 0.65f));
                sequence.OnComplete(() => _visualRenderer.color = _baseColor);
                _colorTween = sequence;
            }
        }

        public void PlaySuccessEffect()
        {
            _scaleTween?.Kill();
            transform.localScale = _baseScale;
            _scaleTween = transform.DOPunchScale(
                    _baseScale * _successPunchScale,
                    _successPunchDuration,
                    6,
                    0.45f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => transform.localScale = _baseScale);
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
        }
    }
}