using UnityEngine;
using System;
using System.Collections;

namespace Game.Minigames.CardMatch
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CardItem : MonoBehaviour
    {
        public event Action<CardItem> OnCardClicked;

        public int CardID { get; private set; }
        public bool IsFaceUp { get; private set; }
        public bool IsMatched { get; private set; }

        private SpriteRenderer _spriteRenderer;
        private Sprite _faceSprite;
        private Sprite _backSprite;
        private float _flipDuration;
        private bool _isAnimating;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
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

        public void FlipUp() => StartCoroutine(FlipRoutine(true));
        public void FlipDown() => StartCoroutine(FlipRoutine(false));

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
        }

        public void SetMatched()
        {
            IsMatched = true;
            // Đổi màu hơi tối đi để báo hiệu đã ghép xong
            _spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }
}