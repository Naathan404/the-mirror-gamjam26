using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Effect;
using UnityEngine;

namespace Game.Minigames.Maze
{
    public class MazeController : MinigameBaseController
    {
        [Header("Tham chiếu Maze")]
        public MazeConfig mazeConfig;
        public MazeRenderer mazeRenderer;

        [Tooltip("Prefab của người chơi")]
        public MazePlayer playerPrefab;

        [Tooltip("Prefab của thực thể (Có thể dùng lại MazePlayer nhưng đổi màu đen/đỏ)")]
        public MazePlayer entityPrefab;

        private MazeData currentMazeData;
        private MazePlayer currentPlayerInstance;
        private List<MazePlayer> activeEntities = new List<MazePlayer>();

        private bool isShaking = false; // Khóa input khi đang bị rung lắc do thua

        // 1. CHẠY KHI GAME MỞ LÊN (HOẶC RESET BẢN ĐỒ MỚI)
        protected override void OnGameStart()
        {
            ClearEntities();

            currentMazeData = MazeGenerator.Generate(mazeConfig.mazeWidth, mazeConfig.mazeHeight);
            mazeRenderer.RenderMaze(currentMazeData);

            if (currentPlayerInstance == null)
                currentPlayerInstance = Instantiate(playerPrefab, mazeRenderer.mazeTilemap.layoutGrid.transform);

            currentPlayerInstance.transform.rotation = mazeRenderer.paperQuad.rotation;

            SpawnEntities(); // Sinh thực thể
            OnGameReset();

            visualRoot.gameObject.SetActive(true);
        }

        // 2. CHẠY KHI BỊ GIẬT ĐÈN (Restore vị trí nhưng không đổi map)
        protected override void OnGameReset()
        {
            if (currentPlayerInstance != null && currentMazeData != null)
            {
                Vector3 startWorldPos = mazeRenderer.GetWorldPosition(currentMazeData.StartPos);
                currentPlayerInstance.Initialize(currentMazeData.StartPos, startWorldPos);
            }
        }

        // 3. THUẬT TOÁN SINH THỰC THỂ
        private void SpawnEntities()
        {
            for (int i = 0; i < mazeConfig.entityCount; i++)
            {
                Vector2Int spawnPos = Vector2Int.zero;
                bool valid = false;
                int attempts = 0;

                while (!valid && attempts < 100)
                {
                    attempts++;
                    int rx = Random.Range(0, mazeConfig.mazeWidth);
                    int ry = Random.Range(0, mazeConfig.mazeHeight);
                    Vector2Int testPos = new Vector2Int(rx, ry);

                    // Tính khoảng cách Manhattan tới Player
                    int distToPlayer = Mathf.Abs(testPos.x - currentMazeData.StartPos.x) + Mathf.Abs(testPos.y - currentMazeData.StartPos.y);

                    if (distToPlayer >= mazeConfig.safeDistanceFromPlayer)
                    {
                        // Kiểm tra khoảng cách với các thực thể khác đã sinh
                        bool tooCloseToOthers = false;
                        foreach (var entity in activeEntities)
                        {
                            int distToEntity = Mathf.Abs(testPos.x - entity.CurrentGridPos.x) + Mathf.Abs(testPos.y - entity.CurrentGridPos.y);
                            if (distToEntity < mazeConfig.safeDistanceBetweenEntities)
                            {
                                tooCloseToOthers = true; break;
                            }
                        }

                        if (!tooCloseToOthers)
                        {
                            spawnPos = testPos;
                            valid = true;
                        }
                    }
                }

                // Khởi tạo thực thể
                MazePlayer newEntity = Instantiate(entityPrefab, mazeRenderer.mazeTilemap.layoutGrid.transform);
                newEntity.transform.rotation = mazeRenderer.paperQuad.rotation;
                newEntity.Initialize(spawnPos, mazeRenderer.GetWorldPosition(spawnPos));
                activeEntities.Add(newEntity);
            }
        }

        // 4. LOGIC RIÊNG: ĐIỀU KHIỂN & KIỂM TRA VA CHẠM
        private void Update()
        {
            if (!isPlaying || !isFocused || currentPlayerInstance == null || isShaking)
                return;

            // Chặn input nếu Player hoặc BẤT KỲ Thực thể nào đang di chuyển
            if (currentPlayerInstance.IsMoving) return;
            foreach (var ent in activeEntities) if (ent.IsMoving) return;

            if (Input.GetKeyDown(KeyCode.W)) TryMove(PathDirection.Up, new Vector2Int(0, 1));
            else if (Input.GetKeyDown(KeyCode.S)) TryMove(PathDirection.Down, new Vector2Int(0, -1));
            else if (Input.GetKeyDown(KeyCode.A)) TryMove(PathDirection.Left, new Vector2Int(-1, 0));
            else if (Input.GetKeyDown(KeyCode.D)) TryMove(PathDirection.Right, new Vector2Int(1, 0));
        }

        private void TryMove(PathDirection playerDir, Vector2Int playerOffset)
        {
            Vector2Int playerOldPos = currentPlayerInstance.CurrentGridPos;
            Vector2Int playerNextPos = playerOldPos;
            bool playerMoved = false;
            AudioController.Instance.PlaySFX(SoundName.Maze_Moving);
            // 1. Tính toán vị trí tương lai của Player
            if (currentMazeData.IsPathOpen(playerOldPos, playerDir))
            {
                playerNextPos = playerOldPos + playerOffset;
                currentPlayerInstance.MoveTo(playerNextPos, mazeRenderer.GetWorldPosition(playerNextPos));
                playerMoved = true;
            }

            // 2. TÍNH TOÁN NHÁP VỊ TRÍ CỦA THỰC THỂ (Chưa di chuyển vội)
            PathDirection enemyDir = GetOppositeDirection(playerDir);
            Vector2Int enemyOffset = -playerOffset;

            Vector2Int[] enemyOldPositions = new Vector2Int[activeEntities.Count];
            Vector2Int[] enemyNextPositions = new Vector2Int[activeEntities.Count];

            for (int i = 0; i < activeEntities.Count; i++)
            {
                enemyOldPositions[i] = activeEntities[i].CurrentGridPos;
                if (currentMazeData.IsPathOpen(enemyOldPositions[i], enemyDir))
                {
                    enemyNextPositions[i] = enemyOldPositions[i] + enemyOffset;
                }
                else
                {
                    enemyNextPositions[i] = enemyOldPositions[i]; // Đụng tường thì đứng im
                }
            }

            // 3. THUẬT TOÁN CHỐNG DỒN TOA (TRAFFIC JAM RESOLVER)
            // Quét liên tục, nếu 2 đứa có chung 1 đích đến, đứa nào định đi tới sẽ bị hủy bước đi
            bool isResolved;
            do
            {
                isResolved = true;
                for (int i = 0; i < activeEntities.Count; i++)
                {
                    for (int j = i + 1; j < activeEntities.Count; j++)
                    {
                        if (enemyNextPositions[i] == enemyNextPositions[j])
                        {
                            // Đứa i là đứa định đi tới -> Hủy
                            if (enemyNextPositions[i] != enemyOldPositions[i])
                            {
                                enemyNextPositions[i] = enemyOldPositions[i];
                                isResolved = false; // Phải quét lại vì có thể gây kẹt xe dây chuyền
                            }
                            // Đứa j là đứa định đi tới -> Hủy
                            if (enemyNextPositions[j] != enemyOldPositions[j])
                            {
                                enemyNextPositions[j] = enemyOldPositions[j];
                                isResolved = false; // Phải quét lại
                            }
                        }
                    }
                }
            } while (!isResolved);

            // 4. CHÍNH THỨC DI CHUYỂN & KIỂM TRA ĐỤNG NGƯỜI CHƠI
            bool collisionDetected = false;

            for (int i = 0; i < activeEntities.Count; i++)
            {
                var entity = activeEntities[i];
                Vector2Int oldPos = enemyOldPositions[i];
                Vector2Int nextPos = enemyNextPositions[i];

                // Nếu có sự thay đổi tọa độ sau khi nháp xong thì mới cho entity đi
                if (nextPos != oldPos)
                {
                    entity.MoveTo(nextPos, mazeRenderer.GetWorldPosition(nextPos));
                }

                // Kiểm tra 2 luật chết với người chơi
                if (playerNextPos == nextPos)
                {
                    collisionDetected = true;
                }
                else if (playerNextPos == oldPos && nextPos == playerOldPos)
                {
                    collisionDetected = true;
                }
            }

            // 5. XỬ LÝ HẬU QUẢ THẮNG / THUA
            if (collisionDetected)
            {
                AudioController.Instance.PlaySFX(SoundName.Maze_Fail);
                FilterController.Instance.FlashScreen(FilterController.Instance.HazardColor, 0.5f);
                Camera.main.transform.DOShakePosition(0.5f, 1f, 15, 90f);
                mazeRenderer.paperQuad.DOShakePosition(0.3f, 0.5f, 10, 90f);
                StartCoroutine(FailAndRestartRoutine());
            }
            else if (playerMoved && playerNextPos == currentMazeData.EndPos)
            {
                AudioController.Instance.PlaySFX(SoundName.Maze_Success);
                CompleteMinigame(); // Win
            }
        }

        private PathDirection GetOppositeDirection(PathDirection dir)
        {
            if (dir == PathDirection.Up) return PathDirection.Down;
            if (dir == PathDirection.Down) return PathDirection.Up;
            if (dir == PathDirection.Left) return PathDirection.Right;
            if (dir == PathDirection.Right) return PathDirection.Left;
            return PathDirection.Up;
        }

        // 5. HIỆU ỨNG RUNG LẮC VÀ ĐỔI MAP
        private IEnumerator FailAndRestartRoutine()
        {
            isShaking = true;

            // Đợi 1 chút xíu để player nhìn thấy mình đâm sầm vào quái (0.15s)
            yield return new WaitForSeconds(0.15f);

            // Rung tờ giấy kịch liệt
            Vector3 originalLocalPos = mazeRenderer.paperQuad.localPosition;
            float elapsed = 0f;
            float duration = 0.4f;
            float magnitude = 0.5f;

            while (elapsed < duration)
            {
                // float x = originalLocalPos.x + Random.Range(-1f, 1f) * magnitude;
                // float y = originalLocalPos.y + Random.Range(-1f, 1f) * magnitude;
                // mazeRenderer.paperQuad.localPosition = new Vector3(x, y, originalLocalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            mazeRenderer.paperQuad.localPosition = originalLocalPos;
            isShaking = false;

            // Xóa sổ và sinh map mới hoàn toàn
            OnGameStart();
        }

        private void ClearEntities()
        {
            foreach (var ent in activeEntities)
            {
                if (ent != null) Destroy(ent.gameObject);
            }
            activeEntities.Clear();
        }

        protected override void OnGameClosed() => ClearEntities();

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            mazeConfig = _difficultyConfig.GetMinigameConfig<MazeConfig>(minigamePassed);
        }
    }
}