using System.Collections.Generic;
using UnityEngine;

namespace Game.Minigames.Maze
{
    public static class MazeGenerator
    {
        // Struct nội bộ để tiện việc tính toán hướng đi trong thuật toán
        private struct MoveInfo
        {
            public Vector2Int offset;         // Độ lệch tọa độ (vd: 0, 1)
            public PathDirection direction;   // Bit hướng đi của ô hiện tại
            public PathDirection opposite;    // Bit hướng đi của ô hàng xóm (để phá tường 2 phía)
        }

        // Định nghĩa 4 hướng di chuyển để thuật toán duyệt qua
        private static readonly MoveInfo[] Moves = new MoveInfo[]
        {
            new MoveInfo { offset = new Vector2Int(0, 1),  direction = PathDirection.Up,    opposite = PathDirection.Down },
            new MoveInfo { offset = new Vector2Int(0, -1), direction = PathDirection.Down,  opposite = PathDirection.Up },
            new MoveInfo { offset = new Vector2Int(-1, 0), direction = PathDirection.Left,  opposite = PathDirection.Right },
            new MoveInfo { offset = new Vector2Int(1, 0),  direction = PathDirection.Right, opposite = PathDirection.Left }
        };

        /// <summary>
        /// Sinh mê cung ngẫu nhiên dựa trên thuật toán DFS Backtracking
        /// </summary>
        public static MazeData Generate(int width, int height, float loopChance = 0.1f)
        {
            MazeData data = new MazeData(width, height);

            // Mảng đánh dấu các ô đã đi qua
            bool[,] visited = new bool[width, height];

            // Stack dùng để quay lui (Backtracking)
            Stack<Vector2Int> stack = new Stack<Vector2Int>();

            // Bắt đầu đào từ ô Start (0, 0)
            Vector2Int currentPos = data.StartPos;
            visited[currentPos.x, currentPos.y] = true;
            stack.Push(currentPos);

            // Thuật toán chạy đến khi Stack rỗng (đã duyệt hết mọi ngóc ngách)
            while (stack.Count > 0)
            {
                currentPos = stack.Peek();

                // Tìm tất cả các hàng xóm chưa được thăm
                List<MoveInfo> unvisitedNeighbors = GetUnvisitedNeighbors(currentPos, visited, width, height);

                if (unvisitedNeighbors.Count > 0)
                {
                    // 1. Chọn ngẫu nhiên một hướng để đi
                    MoveInfo chosenMove = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                    Vector2Int nextPos = currentPos + chosenMove.offset;

                    // 2. Phá tường giữa ô hiện tại và ô hàng xóm (Cộng bit)
                    // Ví dụ: Đi lên thì ô hiện tại thêm bit Up, ô phía trên thêm bit Down
                    data.SetCell(currentPos.x, currentPos.y, data.Grid[currentPos.x, currentPos.y] | (int)chosenMove.direction);
                    data.SetCell(nextPos.x, nextPos.y, data.Grid[nextPos.x, nextPos.y] | (int)chosenMove.opposite);

                    // 3. Đánh dấu đã thăm và đẩy vào Stack để tiếp tục đào từ ô đó
                    visited[nextPos.x, nextPos.y] = true;
                    stack.Push(nextPos);
                }
                else
                {
                    // Nếu không còn hàng xóm nào chưa thăm (Ngõ cụt) -> Lùi lại (Pop)
                    stack.Pop();
                }
            }

            AddLoops(data, width, height, loopChance);

            return data;
        }

        private static void AddLoops(MazeData data, int width, int height, float loopChance)
        {
            for(int x = 0; x < width; x++)
            {
                for(int y = 0; y < height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    foreach(var move in Moves)
                    {
                        Vector2Int next = pos + move.offset;

                        if (next.x < 0 || next.x >= width || next.y < 0 || next.y >= height)
                            continue;

                        bool alreadyConnected = (data.Grid[x, y] & (int)move.direction) != 0;

                        if (alreadyConnected) continue;

                        if (Random.value <= loopChance)
                        {
                            data.SetCell(x, y, data.Grid[x, y] | (int)move.direction);
                            data.SetCell(next.x, next.y, data.Grid[next.x, next.y] | (int)move.opposite);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Quét 4 hướng xung quanh xem có ô nào nằm trong map và chưa đi qua không
        /// </summary>
        private static List<MoveInfo> GetUnvisitedNeighbors(Vector2Int pos, bool[,] visited, int width, int height)
        {
            List<MoveInfo> neighbors = new List<MoveInfo>();

            foreach (var move in Moves)
            {
                Vector2Int nextPos = pos + move.offset;

                // Kiểm tra xem tọa độ mới có lọt ra ngoài bản đồ không
                if (nextPos.x >= 0 && nextPos.x < width && nextPos.y >= 0 && nextPos.y < height)
                {
                    // Nếu chưa thăm thì đưa vào danh sách ứng viên
                    if (!visited[nextPos.x, nextPos.y])
                    {
                        neighbors.Add(move);
                    }
                }
            }

            return neighbors;
        }
    }
}