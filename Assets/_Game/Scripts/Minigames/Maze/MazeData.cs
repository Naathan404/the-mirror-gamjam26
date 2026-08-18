using System;
using UnityEngine;

namespace Game.Minigames.Maze
{
    // Cờ bitmask định nghĩa 4 hướng. 
    [Flags]
    public enum PathDirection
    {
        None = 0,
        Up = 1 << 0,    // 1
        Down = 1 << 1,  // 2
        Left = 1 << 2,  // 4
        Right = 1 << 3  // 8
    }

    public class MazeData
    {
        // Kích thước của mê cung
        public int Width { get; private set; }
        public int Height { get; private set; }

        // Mảng 2 chiều lưu trữ giá trị Bitmask (từ 0 đến 15) của từng ô
        public int[,] Grid { get; private set; }

        // Tọa độ xuất phát và đích đến
        public Vector2Int StartPos { get; set; }
        public Vector2Int EndPos { get; set; }

        /// <summary>
        /// Constructor khởi tạo dữ liệu mê cung rỗng
        /// </summary>
        public MazeData(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            // Khởi tạo mảng, mặc định các phần tử sẽ có giá trị 0 (tường kín 4 mặt)
            Grid = new int[width, height];

            // Mặc định Start ở góc dưới trái, End ở góc trên phải, sau này sinh thuật toán sửa sau
            StartPos = new Vector2Int(0, 0);
            EndPos = new Vector2Int(width - 1, height - 1);
        }

        /// <summary>
        /// Kiểm tra xem tọa độ có bị lọt ra ngoài phạm vi ma trận không
        /// </summary>
        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        /// <summary>
        /// Đứng ở ô 'pos', muốn đi theo hướng 'dir' có được không?
        /// </summary>
        public bool IsPathOpen(Vector2Int pos, PathDirection dir)
        {
            if (!IsValidPosition(pos)) return false;

            int cellValue = Grid[pos.x, pos.y];

            return ((PathDirection)cellValue).HasFlag(dir);
        }

        /// <summary>
        /// Hàm hỗ trợ để set giá trị cho ô (dùng cho class MazeGenerator lúc đào đường)
        /// </summary>
        public void SetCell(int x, int y, int value)
        {
            if (IsValidPosition(new Vector2Int(x, y)))
            {
                Grid[x, y] = value;
            }
        }
    }
}