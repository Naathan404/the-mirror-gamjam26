using UnityEngine;

namespace Game.Minigames.CardMatch
{
    [CreateAssetMenu(fileName = "NewCardMatchConfig", menuName = "Game/Minigames/Card Match Config")]
    public class CardMatchConfig : ScriptableObject
    {
        [Header("Kích thước bàn chơi")]
        [Min(2)] public int rows = 2;
        [Min(2)] public int columns = 3;

        [Header("Thư viện ảnh (Sprite 2D)")]
        [Tooltip("Mặt sau của lá bài (Dùng chung)")]
        public Sprite cardBackSprite;

        [Tooltip("Danh sách các hình mặt trước (Cần tối thiểu bằng số cặp bài)")]
        public Sprite[] cardFaceSprites;

        [Header("Cài đặt Thời gian")]
        [Tooltip("Thời gian cho xem bài trước khi úp xuống lúc mới vào game (giây)")]
        public float previewTime = 2.0f;

        [Tooltip("Thời gian chờ úp bài lại nếu lật sai (giây)")]
        public float delayBeforeFlipBack = 1.0f;

        [Tooltip("Tốc độ hiệu ứng lật bài (giây)")]
        public float flipAnimationDuration = 0.2f;

        [Header("Cài đặt Lưới (Grid)")]
        [Tooltip("Kích thước vật lý của 1 lá bài (Width, Height)")]
        public Vector2 cardSize = new Vector2(1f, 1.5f);

        [Tooltip("Khoảng cách giữa các lá bài")]
        public Vector2 spacing = new Vector2(0.2f, 0.2f);

        [Tooltip("Khoảng lề an toàn (Tính theo tỷ lệ %. Ví dụ 0.9 nghĩa là chừa 10% lề)")]
        [Range(0.5f, 1f)] public float paperPadding = 0.9f;
    }
}