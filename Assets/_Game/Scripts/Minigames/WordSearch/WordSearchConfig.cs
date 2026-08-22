using System.Collections.Generic;
using UnityEngine;

namespace Game.Minigames.WordSearch
{
    public enum VisualFlipType
    {
        None,
        FlipHorizontal,
        FlipVertical
    }

    [CreateAssetMenu(fileName = "NewWordSearchConfig", menuName = "Game/Minigames/Word Search Config")]
    public class WordSearchConfig : ScriptableObject
    {
        [Header("Lưới chữ")]
        public int rows = 8;
        public int columns = 8;
        public Vector2 cellSize = new Vector2(1f, 1f);
        public Vector2 spacing = new Vector2(0.1f, 0.1f);
        [Range(0.5f, 1f)] public float paperPadding = 0.9f;

        [Header("Cài đặt Lật ngược (Duality)")]
        public List<VisualFlipType> allowedFlipTypes = new List<VisualFlipType> { VisualFlipType.None, VisualFlipType.FlipHorizontal, VisualFlipType.FlipVertical };

        [Header("Kho từ khóa (English)")]
        [Tooltip("Từ vựng Tiếng Anh (Chữ IN HOA)")]
        public List<string> wordPoolEN = new List<string> { "SOUL", "DEMON", "DUAL", "MIRROR", "DEATH" };

        [Header("Kho từ khóa (Tiếng Việt)")]
        [Tooltip("Từ vựng Tiếng Việt (BẮT BUỘC: Không dấu, không khoảng trắng. VD: LINHHON, ACQUY)")]
        public List<string> wordPoolVN = new List<string> { "LINHHON", "ACQUY", "KEP", "GUONG", "CAICHET" };

        public int wordsToFindPerGame = 3;
        // ==========================================

        [Header("Cài đặt Mảnh giấy manh mối (Clues)")]
        public float clueSafeRadius = 1.2f;
        [Range(0f, 90f)] public float maxClueRotation = 45f;

        [Header("Màu sắc & Hiệu ứng")]
        public Color highlightColor = new Color(0f, 0f, 0f, 0.5f);
        public Color foundColor = new Color(0f, 0f, 0f, 0.9f);
        public float shakeDuration = 0.2f;
        public float shakeMagnitude = 0.1f;
    }
}