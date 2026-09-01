using UnityEngine;

namespace Game.Minigames.Maze
{
    [CreateAssetMenu(fileName = "NewMazeConfig", menuName = "Game/Minigames/Maze Config")]
    public class MazeConfig : ScriptableObject
    {
        [Header("Kích thước Mê cung")]
        [Tooltip("Chiều rộng (Số ô)")]
        [Min(3)] // Ràng buộc số nhỏ nhất để thuật toán không bị lỗi
        public int mazeWidth = 8;

        [Tooltip("Chiều cao (Số ô)")]
        [Min(3)]
        public int mazeHeight = 8;

        public float loopChance = 0.1f;

        [Header("Cài đặt Thực thể")]
        public int entityCount = 3; // Số lượng thực thể sinh ra
        public int safeDistanceFromPlayer = 5; // Khoảng cách an toàn tối thiểu (tính theo số ô) so với người chơi
        public int safeDistanceBetweenEntities = 3; // Khoảng cách giãn cách giữa các thực thể với nhau
    }
}