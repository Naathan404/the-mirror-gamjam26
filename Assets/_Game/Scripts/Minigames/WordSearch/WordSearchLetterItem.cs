using UnityEngine;
using TMPro;
using System;
using System.Collections;

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

        public char Letter { get; private set; }
        public Vector2Int GridPos { get; private set; }
        public bool IsFound { get; private set; } // Đã nằm trong từ được giải chưa?

        private Vector3 originalLocalPos;

        public void Initialize(char letter, Vector2Int gridPos)
        {
            Letter = letter;
            GridPos = gridPos;
            IsFound = false;
            letterText.text = letter.ToString();

            // Xóa màu nền ban đầu
            backgroundRenderer.color = new Color(0, 0, 0, 0);

            originalLocalPos = transform.localPosition;
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
    }
}