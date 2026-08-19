using System.Collections.Generic;
using UnityEngine;

namespace Game.Minigames.WordSearch
{
    public enum VisualFlipType
    {
        None,           // Bình thường
        FlipHorizontal, // Lật ngang (trái - phải)
        FlipVertical    // Lật dọc (trên - xuống)
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
        [Tooltip("Hệ thống sẽ bốc random 1 kiểu trong danh sách này mỗi khi chơi")]
        public List<VisualFlipType> allowedFlipTypes = new List<VisualFlipType> { VisualFlipType.None, VisualFlipType.FlipHorizontal, VisualFlipType.FlipVertical };

        [Header("Từ khóa")]
        [Tooltip("Kho từ vựng để game bốc ngẫu nhiên giấu vào lưới")]
        public List<string> wordPool = new List<string> { "SOUL", "DEMON", "DUAL", "MIRROR", "DEATH" };
        public int wordsToFindPerGame = 3;

        [Header("Cài đặt Mảnh giấy manh mối (Clues)")]
        [Tooltip("Khoảng cách tối thiểu giữa 2 mảnh giấy để không đè lên nhau")]
        public float clueSafeRadius = 1.2f;

        [Tooltip("Góc xoay tối đa (Ví dụ 45 nghĩa là sẽ xoay ngẫu nhiên từ -45 đến 45 độ)")]
        [Range(0f, 90f)] public float maxClueRotation = 45f;

        [Header("Màu sắc & Hiệu ứng")]
        public Color highlightColor = new Color(0f, 0f, 0f, 0.5f); // Đen nhạt khi đang kéo chuột
        public Color foundColor = new Color(0f, 0f, 0f, 0.9f);     // Đen đậm khi tìm đúng
        public float shakeDuration = 0.2f;
        public float shakeMagnitude = 0.1f;
    }
}