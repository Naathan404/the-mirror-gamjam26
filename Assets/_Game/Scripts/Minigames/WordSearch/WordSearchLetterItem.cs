using UnityEngine;
using TMPro;
using System;
using System.Collections;
using DG.Tweening;

namespace Game.Minigames.WordSearch
{
    [RequireComponent(typeof(BoxCollider))]
    public class WordSearchLetterItem : MonoBehaviour
    {
        public event Action<WordSearchLetterItem> OnLetterPointerDown;
        public event Action<WordSearchLetterItem> OnLetterPointerEnter;
        public event Action<WordSearchLetterItem> OnLetterPointerUp;

        [SerializeField] private TMP_Text letterText;
        [SerializeField] private SpriteRenderer backgroundRenderer; // Màu nền của ô

        [Header("Visual FX - Additive")]
        [SerializeField] private float spawnDuration = 0.22f;
        [SerializeField, Range(0.1f, 1f)] private float spawnStartScale = 0.72f;
        [SerializeField] private float selectPunchScale = 0.08f;
        [SerializeField] private float selectPunchDuration = 0.12f;
        [SerializeField] private float foundPunchScale = 0.14f;
        [SerializeField] private float foundPunchDuration = 0.20f;
        [SerializeField] private float wrongRotationStrength = 8f;
        [SerializeField] private float wrongRotationDuration = 0.20f;
        [SerializeField] private Color wrongTextTint = new Color(0.65f, 0.05f, 0.05f, 1f);

        public char Letter { get; private set; }
        public Vector2Int GridPos { get; private set; }
        public bool IsFound { get; private set; } // Đã nằm trong từ được giải chưa?

        private Vector3 originalLocalPos;

        // VFX cache - không tham gia logic gameplay
        private Vector3 baseLocalScale;
        private Quaternion baseLocalRotation;
        private Color baseTextColor;
        private Tween spawnTween;
        private Tween scaleTween;
        private Tween rotationTween;
        private Tween textColorTween;

        public void Initialize(char letter, Vector2Int gridPos)
        {
            Letter = letter;
            GridPos = gridPos;
            IsFound = false;
            letterText.text = letter.ToString();

            // Xóa màu nền ban đầu
            backgroundRenderer.color = new Color(0, 0, 0, 0);

            originalLocalPos = transform.localPosition;

            // Chỉ cache trạng thái visual gốc.
            baseLocalScale = transform.localScale;
            baseLocalRotation = transform.localRotation;
            baseTextColor = letterText.color;
        }

        /// <summary>
        /// Reveal từng ô. Chỉ tác động scale + alpha chữ, không đổi position,
        /// GridPos, IsFound hay event input.
        /// </summary>
        public void PlaySpawnEffect(float delay)
        {
            spawnTween?.Kill();

            transform.localScale = baseLocalScale * spawnStartScale;

            Color textColor = baseTextColor;
            Color hiddenTextColor = textColor;
            hiddenTextColor.a = 0f;
            letterText.color = hiddenTextColor;

            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(Mathf.Max(0f, delay));
            seq.Append(transform
                .DOScale(baseLocalScale, spawnDuration)
                .SetEase(Ease.OutBack));
            seq.Join(letterText
                .DOFade(textColor.a, spawnDuration * 0.75f)
                .SetEase(Ease.OutQuad));

            seq.OnComplete(() =>
            {
                transform.localScale = baseLocalScale;
                letterText.color = baseTextColor;
            });

            spawnTween = seq;
        }

        /// <summary>
        /// Feedback khi con trỏ vừa thêm ô này vào đường kéo.
        /// Không thay đổi highlight color.
        /// </summary>
        public void PlaySelectionEffect()
        {
            spawnTween?.Kill();
            scaleTween?.Kill();

            transform.localScale = baseLocalScale;
            letterText.color = baseTextColor;

            scaleTween = transform
                .DOPunchScale(
                    baseLocalScale * selectPunchScale,
                    selectPunchDuration,
                    4,
                    0.45f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = baseLocalScale);
        }

        /// <summary>
        /// Feedback visual khi từ đúng. Màu found vẫn được set ngay bởi logic cũ.
        /// </summary>
        public void PlayFoundEffect()
        {
            spawnTween?.Kill();
            scaleTween?.Kill();

            transform.localScale = baseLocalScale;
            letterText.color = baseTextColor;

            scaleTween = transform
                .DOPunchScale(
                    baseLocalScale * foundPunchScale,
                    foundPunchDuration,
                    6,
                    0.55f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.localScale = baseLocalScale);
        }

        /// <summary>
        /// Feedback bổ sung khi chọn sai. Shake position cũ vẫn chạy riêng.
        /// Effect này chỉ dùng rotation + màu chữ nên không tranh transform.position.
        /// </summary>
        public void PlayWrongEffect()
        {
            if (IsFound) return;

            rotationTween?.Kill();
            textColorTween?.Kill();

            transform.localRotation = baseLocalRotation;
            letterText.color = baseTextColor;

            rotationTween = transform
                .DOShakeRotation(
                    wrongRotationDuration,
                    new Vector3(0f, 0f, wrongRotationStrength),
                    10,
                    70f)
                .OnComplete(() => transform.localRotation = baseLocalRotation);

            Sequence colorSeq = DOTween.Sequence();
            colorSeq.Append(letterText.DOColor(wrongTextTint, wrongRotationDuration * 0.45f));
            colorSeq.Append(letterText.DOColor(baseTextColor, wrongRotationDuration * 0.55f));
            textColorTween = colorSeq;
        }

        // --- CÁC SỰ KIỆN CHUỘT CỦA UNITY ---
        private void OnMouseDown() => OnLetterPointerDown?.Invoke(this);
        private void OnMouseEnter() => OnLetterPointerEnter?.Invoke(this);
        private void OnMouseUp() => OnLetterPointerUp?.Invoke(this);

        // --- ĐỔI MÀU ---
        public void SetHighlightColor(Color color)
        {
            if (!IsFound) backgroundRenderer.color = color;
        }

        public void SetFoundColor(Color color)
        {
            IsFound = true;
            backgroundRenderer.color = color;

            // VFX additive: logic/state/màu cũ vẫn được áp dụng ngay như trước.
            PlayFoundEffect();
        }

        public void ClearHighlight()
        {
            if (!IsFound) backgroundRenderer.color = new Color(0, 0, 0, 0);
        }

        // --- HIỆU ỨNG RUNG LẮC (SAI) ---
        public void Shake(float duration, float magnitude)
        {
            if (!IsFound) StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float x = originalLocalPos.x + UnityEngine.Random.Range(-1f, 1f) * magnitude;
                transform.localPosition = new Vector3(x, originalLocalPos.y, originalLocalPos.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = originalLocalPos;
        }

        private void OnDestroy()
        {
            spawnTween?.Kill();
            scaleTween?.Kill();
            rotationTween?.Kill();
            textColorTween?.Kill();
        }
    }
}