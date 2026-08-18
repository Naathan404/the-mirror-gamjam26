using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Minigames.Maze
{
    public class MazeController : MonoBehaviour
    {
        [Header("Cài đặt Minigame")]
        public MinigameType minigameType = MinigameType.Maze;
        public int mazeWidth = 8;
        public int mazeHeight = 8;

        [Header("Tham chiếu")]
        public GameObject visualRoot;
        public MazeRenderer mazeRenderer;
        public MazePlayer playerPrefab;

        // Dữ liệu nội bộ
        private MazeData currentMazeData;
        private MazePlayer currentPlayerInstance;
        private int secretDigit = -1;
        private bool isPlaying = false;
        private bool isFocused = true;

        // ================= ĐĂNG KÝ EVENT =================
        private void OnEnable()
        {
            GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
            GameEvents.OnLightFlashed += HandleLightFlashed;
            GameEvents.OnMinigameOpened += HandleMinigameOpened;
            GameEvents.OnMinigameClosed += HandleMinigameClosed;
            GameEvents.OnViewChangeFinished += HandleViewChangeFinished;
        }

        private void OnDisable()
        {
            GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameOpened -= HandleMinigameOpened;
            GameEvents.OnMinigameClosed -= HandleMinigameClosed;
            GameEvents.OnViewChangeFinished -= HandleViewChangeFinished;
        }

        private void Start()
        {
            // Chỉ cần tắt VisualRoot là mọi thứ (kể cả nút Close 3D bên trong) sẽ tự ẩn
            if (visualRoot != null)
            {
                visualRoot.SetActive(false);
            }
        }

        // ================= XỬ LÝ LẮNG NGHE EVENT =================
        private void HandlePasscodeGenerated(Dictionary<string, int> dict)
        {
            if (dict.TryGetValue(minigameType.ToString(), out int digit))
            {
                secretDigit = digit;
                visualRoot.SetActive(false);
            }
        }

        private void HandleViewChangeFinished(View currentView) => isFocused = (currentView == View.Desk);

        private void HandleLightFlashed()
        {
            if (!isPlaying || currentPlayerInstance == null) return;

            ResetPlayerPosition();
            GameEvents.RaiseMinigameProgressReset(minigameType.ToString());
        }

        private void HandleMinigameOpened(MinigameType type)
        {
            if (type != minigameType) return;

            visualRoot.SetActive(true); // Bật lưới, giấy, và cả nút Close 3D

            currentMazeData = MazeGenerator.Generate(mazeWidth, mazeHeight);
            mazeRenderer.RenderMaze(currentMazeData);

            if (currentPlayerInstance == null)
                currentPlayerInstance = Instantiate(playerPrefab, mazeRenderer.mazeTilemap.layoutGrid.transform);

            currentPlayerInstance.transform.rotation = mazeRenderer.paperQuad.rotation;
            ResetPlayerPosition();

            isPlaying = true;
        }

        private void HandleMinigameClosed(MinigameType type)
        {
            if (type != minigameType || !isPlaying) return;

            isPlaying = false;
            visualRoot.SetActive(false); // Tắt sạch sẽ
            GameEvents.RaiseMinigameProgressReset(minigameType.ToString());
        }

        // ================= LOGIC ĐIỀU KHIỂN =================

        private void ResetPlayerPosition()
        {
            Vector3 startWorldPos = mazeRenderer.GetWorldPosition(currentMazeData.StartPos);
            currentPlayerInstance.Initialize(currentMazeData.StartPos, startWorldPos);
        }

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
                CheckWinCondition(nextPos);
            }
        }

        private void CheckWinCondition(Vector2Int pos)
        {
            if (pos == currentMazeData.EndPos)
            {
                GameEvents.RaiseMinigameCompleted(minigameType.ToString(), secretDigit);
                GameEvents.RaiseMinigameClosed(minigameType);
            }
        }
    }
}