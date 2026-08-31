using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using System.Linq;

namespace Game.Minigames.WordSearch
{
    public class WordSearchController : MinigameBaseController
    {
        private enum BoardRevealPattern
        {
            Rows,
            Columns,
            Mosaic
        }

        [Header("Tham chiếu")]
        public WordSearchConfig config;
        public WordSearchLetterItem letterPrefab;
        public Transform gridContainer;
        public Renderer paperBackground;

        [Tooltip("Prefab của mảnh giấy nhỏ")]
        public WordSearchClueItem cluePrefab;

        [Tooltip("Tạo một Empty Object, Add BoxCollider (Chỉnh size bọc lấy vùng bàn), đánh dấu IsTrigger")]
        public BoxCollider clueSpawnArea;

        [Header("Visual FX - Additive")]
        [Tooltip("Mặc định Mosaic để tránh kiểu reveal chéo đã dùng nhiều ở các minigame khác.")]
        [SerializeField] private BoardRevealPattern boardRevealPattern = BoardRevealPattern.Mosaic;
        [SerializeField] private float revealBandDelay = 0.045f;
        [SerializeField] private float mosaicGroupDelay = 0.055f;
        [SerializeField] private float clueSpawnStagger = 0.045f;

        private List<WordSearchLetterItem> allLetters = new List<WordSearchLetterItem>();
        private List<WordSearchClueItem> spawnedClues = new List<WordSearchClueItem>(); // List mới để quản lý
        private List<string> activeWords = new List<string>();
        private int wordsFoundCount = 0;

        // Xử lý kéo chuột
        private bool isDragging = false;
        private List<WordSearchLetterItem> currentDragList = new List<WordSearchLetterItem>();

        protected override void OnGameStart()
        {
            ClearGame();

            VisualFlipType currentFlip = VisualFlipType.None;
            if (config.allowedFlipTypes.Count > 0)
            {
                currentFlip = config.allowedFlipTypes[UnityEngine.Random.Range(0, config.allowedFlipTypes.Count)];
            }

            int currentLanguageId = PlayerPrefs.GetInt("Language", 0);
            Debug.Log($"[WORD SEARCH] Đang load ngôn ngữ số: {currentLanguageId}");

            List<string> selectedWordPool = (currentLanguageId == 1) ? config.wordPoolVN : config.wordPoolEN;
            Debug.Log($"[WORD SEARCH] Số lượng từ trong kho đang bốc: {selectedWordPool.Count}");

            activeWords = selectedWordPool.OrderBy(x => UnityEngine.Random.value).Take(config.wordsToFindPerGame).ToList();
            wordsFoundCount = 0;

            // --- GỌI HÀM SINH GIẤY TỰ ĐỘNG ---
            SpawnClues();

            // 3. THUẬT TOÁN SINH LƯỚI CHỮ & GIẤU TỪ (Generator)
            char[,] letterMatrix = GenerateWordGrid();

            // 4. AUTO-SCALE LƯỚI CHỮ (Kế thừa từ bài CardMatch)
            gridContainer.localScale = Vector3.one;
            float totalWidth = (config.columns * config.cellSize.x) + ((config.columns - 1) * config.spacing.x);
            float totalHeight = (config.rows * config.cellSize.y) + ((config.rows - 1) * config.spacing.y);

            if (paperBackground is SpriteRenderer sr && sr.sprite != null)
            {
                Vector2 ext = sr.sprite.bounds.extents;
                Vector3[] corners = new Vector3[] {
                    new Vector3(-ext.x, -ext.y, 0), new Vector3(ext.x, -ext.y, 0),
                    new Vector3(-ext.x, ext.y, 0), new Vector3(ext.x, ext.y, 0)
                };
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                foreach (var c in corners)
                {
                    Vector3 localPt = gridContainer.InverseTransformPoint(sr.transform.TransformPoint(c));
                    if (localPt.x < minX) minX = localPt.x; if (localPt.x > maxX) maxX = localPt.x;
                    if (localPt.y < minY) minY = localPt.y; if (localPt.y > maxY) maxY = localPt.y;
                }
                float finalScale = Mathf.Min((maxX - minX) * config.paperPadding / totalWidth, (maxY - minY) * config.paperPadding / totalHeight);
                // ==========================================
                // THUẬT TOÁN LẬT GƯƠNG (MIRROR) CHUẨN XÁC:
                // ==========================================
                // Nếu lật ngang -> nhân trục X với -1. Lật dọc -> nhân trục Y với -1
                float scaleX = finalScale * (currentFlip == VisualFlipType.FlipHorizontal ? -1f : 1f);
                float scaleY = finalScale * (currentFlip == VisualFlipType.FlipVertical ? -1f : 1f);
                gridContainer.localScale = new Vector3(scaleX, scaleY, 1f);
            }

            // 5. RẢI CHỮ LÊN GIAO DIỆN
            float startX = -totalWidth / 2f + config.cellSize.x / 2f;
            float startY = totalHeight / 2f - config.cellSize.y / 2f;

            for (int r = 0; r < config.rows; r++)
            {
                for (int c = 0; c < config.columns; c++)
                {
                    float posX = startX + c * (config.cellSize.x + config.spacing.x);
                    float posY = startY - r * (config.cellSize.y + config.spacing.y);

                    WordSearchLetterItem newLetter = Instantiate(letterPrefab, gridContainer);
                    newLetter.transform.localPosition = new Vector3(posX, posY, 0f);

                    newLetter.Initialize(letterMatrix[r, c], new Vector2Int(c, r));

                    // Đăng ký Event chuột
                    newLetter.OnLetterPointerDown += HandlePointerDown;
                    newLetter.OnLetterPointerEnter += HandlePointerEnter;
                    newLetter.OnLetterPointerUp += HandlePointerUp;

                    allLetters.Add(newLetter);

                    // VFX reveal không đi theo đường chéo:
                    // Rows = từng hàng, Columns = từng cột,
                    // Mosaic = 4 nhóm ô xen kẽ kiểu mosaic.
                    newLetter.PlaySpawnEffect(GetBoardRevealDelay(r, c));
                }
            }
        }

        private float GetBoardRevealDelay(int row, int column)
        {
            switch (boardRevealPattern)
            {
                case BoardRevealPattern.Rows:
                    // Cả một hàng hiện cùng lúc, rồi tới hàng kế tiếp.
                    return row * revealBandDelay;

                case BoardRevealPattern.Columns:
                    // Cả một cột hiện cùng lúc, rồi tới cột kế tiếp.
                    return column * revealBandDelay;

                case BoardRevealPattern.Mosaic:
                default:
                    // Chia board thành 4 nhóm xen kẽ:
                    // (even,even) -> (odd,odd) -> (even,odd) -> (odd,even)
                    // Không tạo cảm giác quét từ góc chéo.
                    int group;
                    bool evenRow = (row & 1) == 0;
                    bool evenColumn = (column & 1) == 0;

                    if (evenRow && evenColumn) group = 0;
                    else if (!evenRow && !evenColumn) group = 1;
                    else if (evenRow) group = 2;
                    else group = 3;

                    return group * mosaicGroupDelay;
            }
        }

        // --- CÁC HÀM XỬ LÝ KÉO THẢ CHUỘT ---
        private void HandlePointerDown(WordSearchLetterItem letter)
        {
            if (!isPlaying) return;
            isDragging = true;
            currentDragList.Clear();
            AddToDragList(letter);
        }

        private void HandlePointerEnter(WordSearchLetterItem letter)
        {
            if (!isPlaying || !isDragging) return;
            // Nếu chữ này chưa có trong list kéo thì thêm vào (tránh bị kéo lùi lại ô cũ)
            if (!currentDragList.Contains(letter))
            {
                AddToDragList(letter);
            }
        }

        private void AddToDragList(WordSearchLetterItem letter)
        {
            currentDragList.Add(letter);
            letter.SetHighlightColor(config.highlightColor); // Nhuộm đen nhạt

            // VFX additive, không đổi currentDragList hay màu highlight.
            letter.PlaySelectionEffect();
        }

        private void HandlePointerUp(WordSearchLetterItem letter)
        {
            if (!isPlaying || !isDragging) return;
            isDragging = false;

            // Chuyển danh sách ô kéo thành 1 chuỗi chữ (Ví dụ: kéo qua 4 ô S-O-U-L -> "SOUL")
            string formedWord = "";
            foreach (var item in currentDragList) formedWord += item.Letter;

            // Từ người chơi kéo có thể đúng chiều hoặc ngược chiều (VD kéo từ phải sang trái)
            string reversedWord = new string(formedWord.Reverse().ToArray());

            bool isCorrect = false;
            string foundTarget = "";

            if (activeWords.Contains(formedWord))
            {
                isCorrect = true; foundTarget = formedWord;
            }
            else if (activeWords.Contains(reversedWord))
            {
                isCorrect = true; foundTarget = reversedWord;
            }

            if (isCorrect)
            {
                foreach (var item in currentDragList) item.SetFoundColor(config.foundColor);

                // QUÉT DANH SÁCH GIẤY MỚI ĐỂ GẠCH BỎ
                foreach (var clue in spawnedClues)
                {
                    if (clue.TargetWord == foundTarget) clue.MarkAsFound();
                    AudioController.Instance.PlaySFX(SoundName.Word_Success);
                }

                activeWords.Remove(foundTarget);
                wordsFoundCount++;

                if (wordsFoundCount >= config.wordsToFindPerGame) CompleteMinigame();

            }
            else
            {
                // Trả lại màu trong suốt và báo rung lắc
                foreach (var item in currentDragList)
                {
                    item.ClearHighlight();
                    item.Shake(config.shakeDuration, config.shakeMagnitude);

                    // Shake position cũ vẫn giữ nguyên; effect mới chỉ rung rotation + tint chữ.
                    item.PlayWrongEffect();
                }

                AudioController.Instance.PlaySFX(SoundName.Word_Fail);
            }

            currentDragList.Clear();
        }

        // --- THUẬT TOÁN NHÉT TỪ VÀO LƯỚI ---
        private char[,] GenerateWordGrid()
        {
            char[,] grid = new char[config.rows, config.columns];
            for (int r = 0; r < config.rows; r++)
                for (int c = 0; c < config.columns; c++)
                    grid[r, c] = '-'; // Trống

            // ==========================================
            // ĐÃ SỬA: Chỉ giữ lại 2 hướng (Ngang xuôi chiều, Dọc xuôi chiều)
            // {0, 1} là đi sang Phải. {1, 0} là đi xuống Dưới.
            // ==========================================
            int[][] directions = new int[][] {
                new int[] { 0, 1 },
                new int[] { 1, 0 }
            };

            foreach (string word in activeWords)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 100) // Tránh lặp vô hạn
                {
                    attempts++;
                    int r = UnityEngine.Random.Range(0, config.rows);
                    int c = UnityEngine.Random.Range(0, config.columns);
                    int[] dir = directions[UnityEngine.Random.Range(0, directions.Length)];

                    if (CanPlaceWord(grid, word, r, c, dir[0], dir[1]))
                    {
                        for (int i = 0; i < word.Length; i++)
                            grid[r + i * dir[0], c + i * dir[1]] = word[i];
                        placed = true;
                    }
                }
                if (!placed) Debug.LogWarning($"[WordSearch] Lưới quá chật, không thể nhét từ: {word}");
            }

            // Điền rác (A-Z) vào các ô còn trống '-'
            for (int r = 0; r < config.rows; r++)
                for (int c = 0; c < config.columns; c++)
                    if (grid[r, c] == '-')
                        grid[r, c] = (char)('A' + UnityEngine.Random.Range(0, 26));

            return grid;
        }

        private void SpawnClues()
        {
            if (clueSpawnArea == null)
            {
                Debug.LogError("[WordSearch] Chưa kéo BoxCollider vùng sinh giấy (Clue Spawn Area)!");
                return;
            }

            List<Vector3> usedLocalPositions = new List<Vector3>();

            for (int i = 0; i < activeWords.Count; i++)
            {
                Vector3 validLocalPos = Vector3.zero;
                bool foundSpot = false;

                // Bắt đầu với bán kính lý tưởng (to nhất) từ Config
                float currentSafeRadius = config.clueSafeRadius;

                // Vòng lặp thích ứng: Ép buộc phải tìm ra chỗ bằng cách giảm dần tiêu chuẩn
                while (!foundSpot && currentSafeRadius > 0.1f)
                {
                    // Thử 50 lần ở bán kính hiện tại
                    for (int attempt = 0; attempt < 50; attempt++)
                    {
                        // Sinh tọa độ ngẫu nhiên bên trong giới hạn của BoxCollider
                        float rx = UnityEngine.Random.Range(-clueSpawnArea.size.x / 2f, clueSpawnArea.size.x / 2f) + clueSpawnArea.center.x;
                        float ry = UnityEngine.Random.Range(-clueSpawnArea.size.y / 2f, clueSpawnArea.size.y / 2f) + clueSpawnArea.center.y;

                        Vector3 testPos = new Vector3(rx, ry, clueSpawnArea.center.z);

                        // Kiểm tra va chạm với các tờ giấy đã sinh trước đó
                        bool isOverlapping = false;
                        foreach (var usedPos in usedLocalPositions)
                        {
                            if (Vector3.Distance(testPos, usedPos) < currentSafeRadius)
                            {
                                isOverlapping = true;
                                break;
                            }
                        }

                        // Nếu khoảng cách an toàn, chốt ngay tọa độ này
                        if (!isOverlapping)
                        {
                            validLocalPos = testPos;
                            foundSpot = true;
                            break;
                        }
                    }

                    // ĐIỂM ĂN TIỀN LÀ ĐÂY: Nếu 50 lần thử đều thất bại do bàn quá chật,
                    // tự động bóp nhỏ khoảng cách an toàn đi 10% rồi thử lại vòng lặp!
                    if (!foundSpot)
                    {
                        currentSafeRadius *= 0.9f;
                    }
                }

                // Ghi nhận tọa độ vào danh sách đã dùng
                usedLocalPositions.Add(validLocalPos);

                // Sinh giấy và đặt nó làm con của vùng Spawn
                WordSearchClueItem newClue = Instantiate(cluePrefab, clueSpawnArea.transform);
                newClue.transform.localPosition = validLocalPos;

                // Lắc góc ngẫu nhiên quanh trục Z
                float randRot = UnityEngine.Random.Range(-config.maxClueRotation, config.maxClueRotation);
                newClue.transform.localRotation = Quaternion.Euler(0f, 0f, randRot);
                
                // i = 0 -> tầng 10. i = 1 -> tầng 20. i = 2 -> tầng 30...
                int uniqueSortingBase = 10 + (i * 10);

                // Truyền số tầng này vào hàm Initialize
                newClue.Initialize(activeWords[i], uniqueSortingBase, clueSpawnArea);

                spawnedClues.Add(newClue);

                // Clue chỉ pop/fade tại vị trí random sẵn có, không bay từ góc hay đổi position.
                newClue.PlaySpawnEffect(i * clueSpawnStagger);
            }
        }

        private bool CanPlaceWord(char[,] grid, string word, int r, int c, int dr, int dc)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int nr = r + i * dr;
                int nc = c + i * dc;
                // Nếu vượt ra ngoài lưới, hoặc ô đó đã có chữ nhưng không trùng khớp
                if (nr < 0 || nr >= config.rows || nc < 0 || nc >= config.columns) return false;
                if (grid[nr, nc] != '-' && grid[nr, nc] != word[i]) return false;
            }
            return true;
        }

        private void ClearGame()
        {
            foreach (var letter in allLetters)
                if (letter != null) Destroy(letter.gameObject);
            allLetters.Clear();

            // XÓA CÁC TỜ GIẤY CŨ
            foreach (var clue in spawnedClues)
                if (clue != null) Destroy(clue.gameObject);
            spawnedClues.Clear();

            currentDragList.Clear();
            isDragging = false;
        }

        protected override void OnGameClosed() => ClearGame();
        protected override void OnGameReset() { if (isPlaying) OnGameStart(); }

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            config = _difficultyConfig.GetMinigameConfig<WordSearchConfig>(minigamePassed);
        }
    }
}