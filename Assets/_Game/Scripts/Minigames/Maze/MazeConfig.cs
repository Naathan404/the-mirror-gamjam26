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
    }
}