using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Effect;
using UnityEngine;
using UnityEngine.UI;

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
        [Header("Nút điều khiển UI")]
        public Button btnUp;
        public Button btnDown;
        public Button btnLeft;
        public Button btnRight;

        private MazeData currentMazeData;
        private MazePlayer currentPlayerInstance;
        private List<MazePlayer> activeEntities = new List<MazePlayer>();

        private bool isShaking = false; // Khóa input khi đang bị rung lắc do thua

        protected override void Start()
        {
            base.Start();
            if (btnUp != null) btnUp.onClick.AddListener(ExecuteMoveUp);
            if (btnDown != null) btnDown.onClick.AddListener(ExecuteMoveDown);
            if (btnLeft != null) btnLeft.onClick.AddListener(ExecuteMoveLeft);
            if (btnRight != null) btnRight.onClick.AddListener(ExecuteMoveRight);
        }

        // 1. CHẠY KHI GAME MỞ LÊN (HOẶC RESET BẢN ĐỒ MỚI)
        protected override void OnGameStart()
        {
            ClearEntities();

            currentMazeData = MazeGenerator.Generate(mazeConfig.mazeWidth, mazeConfig.mazeHeight, mazeConfig.loopChance);
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
            if (!CanReceiveInput()) return;

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) ExecuteMoveUp();
            else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) ExecuteMoveDown();
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) ExecuteMoveLeft();
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) ExecuteMoveRight();
        }
        private bool CanReceiveInput()
        {
            if (!isPlaying || !isFocused || currentPlayerInstance == null || isShaking)
                return false;

            if (currentPlayerInstance.IsMoving) return false;
            foreach (var ent in activeEntities) if (ent.IsMoving) return false;

            return true;
        }
        private void TryMove(PathDirection playerDir, Vector2Int playerOffset)
        {
            Vector2Int playerOldPos = currentPlayerInstance.CurrentGridPos;
            Vector2Int playerNextPos = playerOldPos;
            bool playerMoved = false;
            // 1. Tính toán vị trí tương lai của Player
            if (currentMazeData.IsPathOpen(playerOldPos, playerDir))
            {
                playerNextPos = playerOldPos + playerOffset;
                currentPlayerInstance.MoveTo(playerNextPos, mazeRenderer.GetWorldPosition(playerNextPos));
                AudioController.Instance.PlaySFX(SoundName.Maze_Moving);
                playerMoved = true;
            }
            else
            {
                // VFX bổ sung: feedback khi player bấm vào tường. Không thay đổi logic di chuyển.
                currentPlayerInstance.PlayBlockedEffect();
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

                    // VFX bổ sung, entity vẫn đứng nguyên đúng như logic cũ.
                    activeEntities[i].PlayBlockedEffect();
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
            List<MazePlayer> collidingEntities = new List<MazePlayer>(); // chỉ dùng cho VFX

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
                    collidingEntities.Add(entity);
                }
                else if (playerNextPos == oldPos && nextPos == playerOldPos)
                {
                    collisionDetected = true;
                    collidingEntities.Add(entity);
                }
            }

            // 5. XỬ LÝ HẬU QUẢ THẮNG / THUA
            if (collisionDetected)
            {
                // VFX bổ sung trước các shake/flash cũ.
                currentPlayerInstance.PlayHitEffect();
                foreach (var entity in collidingEntities)
                    if (entity != null) entity.PlayHitEffect();

                FilterController.Instance.FlashScreen(FilterController.Instance.HazardColor, 0.5f);
                Camera.main.transform.DOShakePosition(0.5f, 1f, 15, 90f);
                mazeRenderer.paperQuad.DOShakePosition(0.3f, 0.5f, 10, 90f);
                OnFailed();
                StartCoroutine(FailAndRestartRoutine());
            }
            else if (playerMoved && playerNextPos == currentMazeData.EndPos)
            {
                // VFX bổ sung, không delay logic win.
                currentPlayerInstance.PlaySuccessEffect();
                AudioController.Instance.PlaySFX(SoundName.Maze_Success);
                CompleteMinigame(); // Win
            }
        }

        public void ExecuteMoveUp()
        {
            if (!CanReceiveInput()) return;
            TryMove(PathDirection.Up, new Vector2Int(0, 1));
            AnimateButtonPress(btnUp);
        }

        public void ExecuteMoveDown()
        {
            if (!CanReceiveInput()) return;
            TryMove(PathDirection.Down, new Vector2Int(0, -1));
            AnimateButtonPress(btnDown);
        }

        public void ExecuteMoveLeft()
        {
            if (!CanReceiveInput()) return;
            TryMove(PathDirection.Left, new Vector2Int(-1, 0));
            AnimateButtonPress(btnLeft);
        }

        public void ExecuteMoveRight()
        {
            if (!CanReceiveInput()) return;
            TryMove(PathDirection.Right, new Vector2Int(1, 0));
            AnimateButtonPress(btnRight);
        }

        private void AnimateButtonPress(Button btn)
        {
            if (btn == null) return;

            btn.transform.DOKill();
            btn.transform.localScale = Vector3.one;

            btn.transform.DOPunchScale(new Vector3(-0.2f, -0.2f, 0f), 0.15f, 1);
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