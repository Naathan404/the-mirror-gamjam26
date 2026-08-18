using UnityEngine;

namespace Game.Minigames.Maze
{
    // Kế thừa class cha
    public class MazeController : MinigameBaseController
    {
        [Header("Tham chiếu Maze")]
        public MazeConfig mazeConfig;
        public MazeRenderer mazeRenderer;
        public MazePlayer playerPrefab;

        private MazeData currentMazeData;
        private MazePlayer currentPlayerInstance;

        // 1. CHẠY KHI GAME MỞ LÊN
        protected override void OnGameStart()
        {
            currentMazeData = MazeGenerator.Generate(mazeConfig.mazeWidth, mazeConfig.mazeHeight);
            mazeRenderer.RenderMaze(currentMazeData);

            if (currentPlayerInstance == null)
                currentPlayerInstance = Instantiate(playerPrefab, mazeRenderer.mazeTilemap.layoutGrid.transform);

            currentPlayerInstance.transform.rotation = mazeRenderer.paperQuad.rotation;
            OnGameReset();
        }

        // 2. CHẠY KHI BỊ GIẬT ĐÈN (Hoặc lúc mới vào)
        protected override void OnGameReset()
        {
            if (currentPlayerInstance != null && currentMazeData != null)
            {
                Vector3 startWorldPos = mazeRenderer.GetWorldPosition(currentMazeData.StartPos);
                currentPlayerInstance.Initialize(currentMazeData.StartPos, startWorldPos);
            }
        }

        // 3. LOGIC RIÊNG: ĐIỀU KHIỂN
        private void Update()
        {
            if (!isPlaying || !isFocused || currentPlayerInstance == null || currentPlayerInstance.IsMoving)
                return;

            if (Input.GetKeyDown(KeyCode.W)) TryMove(PathDirection.Up, new Vector2Int(0, 1));
            else if (Input.GetKeyDown(KeyCode.S)) TryMove(PathDirection.Down, new Vector2Int(0, -1));
            else if (Input.GetKeyDown(KeyCode.A)) TryMove(PathDirection.Left, new Vector2Int(-1, 0));
            else if (Input.GetKeyDown(KeyCode.D)) TryMove(PathDirection.Right, new Vector2Int(1, 0));
        }

        private void TryMove(PathDirection dir, Vector2Int offset)
        {
            Vector2Int currentPos = currentPlayerInstance.CurrentGridPos;

            if (currentMazeData.IsPathOpen(currentPos, dir))
            {
                Vector2Int nextPos = currentPos + offset;
                currentPlayerInstance.MoveTo(nextPos, mazeRenderer.GetWorldPosition(nextPos));

                // KIỂM TRA ĐIỀU KIỆN THẮNG
                if (nextPos == currentMazeData.EndPos)
                {
                    CompleteMinigame();
                }
            }
        }
    }
}