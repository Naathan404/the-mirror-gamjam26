using UnityEngine;
using Game.Core;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System;
using Game.Effect;

namespace Game.Minigames.Laser
{
    public class LaserMinigameController : MinigameBaseController
    {
        [Header("Configs")]
        [SerializeField] private LaserConfigSO _config;


        [Header("Background")]
        [SerializeField] private SpriteRenderer _background;
        [Range(0.1f, 1f)] [SerializeField] private float _gridAreaWidthPercent = 0.8f;
        [Range(0.1f, 1f)] [SerializeField] private float _gridAreaHeightPercent = 0.8f;

        [Header("Prefab & Layer")]
        [SerializeField] private LaserCell _cellPrefab;
        [SerializeField] private float _prefabReferenceCellSize = 1f;
        [Range(0.1f, 1f)] [SerializeField] private float _cellFillPercent = 0.8f;
        [SerializeField] private LayerMask _cellLayerMask;

        [Header("Sinh Level")]
        //[SerializeField] private int _minStraightRunBetweenTurns = 2;

        [Header("Play Laser Button")]
        [SerializeField] private Collider _fireButtonCollider;

        [Header("Visual")]
        [SerializeField] private LineRenderer _laserLine;
        [SerializeField] private Color _laserColor = Color.red;

        [Header("Visual FX - Additive")]
        [SerializeField] private float _cellSpawnStagger = 0.012f;
        [SerializeField] private float _fireButtonPunchScale = 0.10f;
        [SerializeField] private float _fireButtonPunchDuration = 0.18f;
        [SerializeField] private float _warningPunchScale = 0.16f;
        [SerializeField] private float _warningPunchDuration = 0.22f;
        [SerializeField] private float _successWaveStagger = 0.025f;

        [Header("Warnings")]
        [SerializeField] private List<GameObject> _mistakeWarnings;
        [SerializeField] private Color _mistakeColor;
        [SerializeField] private float _mistakeFlashDuration;

        [Header("Generate")]
        [SerializeField] private int _temp = 200;

        private LaserCell[,] _cells;
        private Vector2Int _gunPos;
        private int _bulbTotalCount;
        private int _mistakeCount = 0;
        private bool _isFiring = false;
        private bool _puzzleSolved = false;
        private Transform _cellRoot;

        private Tween _fireButtonTween;
        private readonly Dictionary<Transform, Tween> _warningTweens = new();

        #region  Base
        protected override void OnGameStart()
        {
            _mistakeCount = 0;
            _puzzleSolved = false;
            if (_laserLine != null) _laserLine.positionCount = 0;
            HideMistakeWarningPanel();
            GeneratePuzzle();
        }

        protected override void OnGameReset()
        {
            StopAllCoroutines();
            _isFiring = false;
            _mistakeCount = 0;
            _puzzleSolved = false;
            if (_laserLine != null) _laserLine.positionCount = 0;

            GeneratePuzzle();
        }

        protected override void OnGameClosed()
        {
            StopAllCoroutines();
            _isFiring = false;
            ClearGrid();
        }

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            _config = _difficultyConfig.GetMinigameConfig<LaserConfigSO>(minigamePassed);
        }

        private void Update()
        {
            if (!isPlaying || isCompleting || !isFocused || _isFiring) return;

            if (Input.GetMouseButtonDown(0))
                HandleClick();

#if UNITY_EDITOR
            if (Input.GetKeyDown(UnityEngine.KeyCode.G))
            {
                GeneratePuzzle();
            }
#endif
        }
        #endregion

        #region Inputs
        private void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (_fireButtonCollider != null &&
                Physics.Raycast(ray, out RaycastHit fireHit, 100f) &&
                fireHit.collider == _fireButtonCollider)
            {
                AudioController.Instance.PlaySFX(SoundName.Lazors_Gun);
                PlayFireButtonEffect();
                StartCoroutine(FireLaserRoutine());
                return;
            }

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _cellLayerMask))
            {
                if (hit.collider.TryGetComponent(out LaserCell cell))
                {
                    cell.TryRotate();
                }
            }
        }
        #endregion

        #region Generator
        private void GeneratePuzzle()
        {
            ClearGrid();

            int n = _config.gridSize;
            _cells = new LaserCell[n, n];

            Transform parent = visualRoot != null ? visualRoot.transform : transform;
            Transform gridReference = _background != null ? _background.transform : parent;
            _cellRoot = CreateCellRoot(parent, gridReference);

            Vector3 gridOriginLocal;
            float cellSize;

            if (_background != null && _background.sprite != null)
            {
                Bounds spriteBounds = _background.sprite.bounds;
                Vector3 backgroundScale = _background.transform.lossyScale;
                float areaW = spriteBounds.size.x * Mathf.Abs(backgroundScale.x) * _gridAreaWidthPercent;
                float areaH = spriteBounds.size.y * Mathf.Abs(backgroundScale.y) * _gridAreaHeightPercent;
                cellSize = Mathf.Min(areaW / n, areaH / n);

                float totalW = cellSize * n;
                float totalH = cellSize * n;
                gridOriginLocal = new Vector3(-totalW * 0.5f + cellSize * 0.5f, totalH * 0.5f - cellSize * 0.5f, -0.02f);
            }
            else
            {
                cellSize = _config != null ? _config.fallbackCellSize : 0.6f;
                gridOriginLocal = Vector3.zero;
            }

            float cellScale = _prefabReferenceCellSize > 0f ? cellSize * _cellFillPercent / _prefabReferenceCellSize : _cellFillPercent;

            // Random walk sinh đường đi hợp lệ, kèm validate bằng cách mô phỏng bắn thử với cấu hình
            // "đã giải đúng" - nếu mô phỏng không sáng hết đèn (do path tự cắt chính nó hay bug khác)
            // thì huỷ toàn bộ (path + mirror + bulb) và random lại từ đầu, KHÔNG chỉ random lại path suông.
            List<Vector2Int> path = null;
            List<LaserDirection> pathDirs = null;
            LaserDirection solvedGunDir = default;
            var pathMirrorOrientation = new Dictionary<Vector2Int, MirrorOrientation>();
            var bulbCells = new HashSet<Vector2Int>();
            bool generationSucceeded = false;
            int genAttempts = 0;

            while (genAttempts < _temp)
            {
                genAttempts++;

                if (!TryGenerateSolvablePath(n, out path, out pathDirs))
                    continue;

                solvedGunDir = pathDirs[0];
                pathMirrorOrientation.Clear();
                bulbCells.Clear();

                for (int i = 1; i < path.Count - 1; i++)
                {
                    LaserDirection incoming = pathDirs[i - 1];
                    LaserDirection outgoing = pathDirs[i];

                    if (incoming != outgoing)
                    {
                        var orientation = LaserDirectionUtil.FindOrientationFor(incoming, outgoing);
                        pathMirrorOrientation[path[i]] = orientation ?? MirrorOrientation.Slash;
                    }
                }

                var straightCandidates = new List<Vector2Int>();
                for (int i = 1; i < path.Count; i++)
                {
                    if (!pathMirrorOrientation.ContainsKey(path[i]))
                        straightCandidates.Add(path[i]);
                }
                Shuffle(straightCandidates);
                int bulbCandidateCount = Mathf.Min(_config.bulbCount, straightCandidates.Count);
                for (int i = 0; i < bulbCandidateCount; i++)
                    bulbCells.Add(straightCandidates[i]);

                // Bước validate then chốt: mô phỏng bắn thử với gun + mirror ở ĐÚNG hướng đã giải,
                // xác nhận tia thực sự sáng hết đèn trước khi ra khỏi lưới / kẹt vòng lặp.
                if (bulbCells.Count > 0 && ValidateSolution(path, solvedGunDir, pathMirrorOrientation, bulbCells, n))
                {
                    generationSucceeded = true;
                    break;
                }
            }

            if (!generationSucceeded)
            {
                Debug.LogError("[LaserPuzzle] Không thể sinh level giải được sau nhiều lần thử, kiểm tra lại config (n quá nhỏ so với bulbCount/mirrorCount).");
                return;
            }

            _gunPos = path[0];
            _bulbTotalCount = bulbCells.Count;

            // Tập hợp ô KHÔNG nằm trên đường đi để đặt đá + gương decoy
            var pathSet = new HashSet<Vector2Int>(path);
            var freeCells = new List<Vector2Int>();
            for (int x = 0; x < n; x++)
                for (int y = 0; y < n; y++)
                    if (!pathSet.Contains(new Vector2Int(x, y)))
                        freeCells.Add(new Vector2Int(x, y));
            Shuffle(freeCells);

            int stoneCount = Mathf.Min(_config.stoneCount, freeCells.Count);
            var stoneCells = new HashSet<Vector2Int>();
            for (int i = 0; i < stoneCount; i++) stoneCells.Add(freeCells[i]);

            int decoyCount = Mathf.Min(_config.decoyMirrorCount, freeCells.Count - stoneCount);
            var decoyMirrorCells = new HashSet<Vector2Int>();
            for (int i = stoneCount; i < stoneCount + decoyCount; i++) decoyMirrorCells.Add(freeCells[i]);

            // Spawn toàn bộ lưới
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    var pos = new Vector2Int(x, y);
                    LaserCell cellObj = Instantiate(_cellPrefab, _cellRoot);
                    Vector3 cellOffset = new Vector3(x * cellSize, -y * cellSize, 0f);
                    cellObj.transform.localPosition = gridOriginLocal + cellOffset;
                    cellObj.transform.localScale = Vector3.one * cellScale;

                    LaserCellType type;
                    if (pos == _gunPos) type = LaserCellType.Gun;
                    else if (bulbCells.Contains(pos)) type = LaserCellType.Bulb;
                    else if (pathMirrorOrientation.ContainsKey(pos)) type = LaserCellType.Mirror;
                    else if (stoneCells.Contains(pos)) type = LaserCellType.Stone;
                    else if (decoyMirrorCells.Contains(pos)) type = LaserCellType.Mirror;
                    else type = LaserCellType.Empty;

                    cellObj.Setup(type, x, y);

                    if (type == LaserCellType.Gun)
                    {
                        LaserDirection scrambled;
                        do { scrambled = (LaserDirection)UnityEngine.Random.Range(0, 4); }
                        while (scrambled == solvedGunDir);
                        cellObj.SetGunFacing(scrambled);
                    }
                    else if (type == LaserCellType.Mirror)
                    {
                        MirrorOrientation solvedOrientation = pathMirrorOrientation.TryGetValue(pos, out var o)
                            ? o
                            : (MirrorOrientation)UnityEngine.Random.Range(0, 2);

                        MirrorOrientation scrambled = pathMirrorOrientation.ContainsKey(pos)
                            // Gương THẬT SỰ cần cho lời giải -> luôn xáo sang orientation còn lại
                            ? (solvedOrientation == MirrorOrientation.Slash ? MirrorOrientation.Backslash : MirrorOrientation.Slash)
                            // Gương decoy -> random tự do, không quan trọng
                            : (MirrorOrientation)UnityEngine.Random.Range(0, 2);

                        cellObj.SetMirrorOrientation(scrambled);
                    }

                    // Additive spawn VFX only. Logic/type/orientation phía trên giữ nguyên.
                    cellObj.PlaySpawnEffect((x + y) * _cellSpawnStagger);

                    _cells[x, y] = cellObj;
                }
            }
        }

        private Transform CreateCellRoot(Transform parent, Transform gridReference)
        {
            GameObject rootObject = new GameObject("LaserCellsRoot");
            Transform root = rootObject.transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(gridReference.position, gridReference.rotation);
            root.localScale = Vector3.one;
            return root;
        }

        private bool TryGenerateSolvablePath(int n, out List<Vector2Int> path, out List<LaserDirection> dirs)
        {
            path = new List<Vector2Int>();
            dirs = new List<LaserDirection>();

            // Chọn ô gun ở biên + hướng bắn vào trong lưới
            bool horizontalEdge = UnityEngine.Random.value < 0.5f;
            Vector2Int gunPos;
            LaserDirection dir;

            if (horizontalEdge)
            {
                int x = UnityEngine.Random.value < 0.5f ? 0 : n - 1;
                int y = UnityEngine.Random.Range(0, n);
                gunPos = new Vector2Int(x, y);
                dir = x == 0 ? LaserDirection.Right : LaserDirection.Left;
            }
            else
            {
                int y = UnityEngine.Random.value < 0.5f ? 0 : n - 1;
                int x = UnityEngine.Random.Range(0, n);
                gunPos = new Vector2Int(x, y);
                dir = y == 0 ? LaserDirection.Down : LaserDirection.Up;
            }

            path.Add(gunPos);
            Vector2Int current = gunPos;
            int mirrorsUsed = 0;
            int requiredMirrors = _config.requiredMirrorCount;
            int requiredStraightCells = _config.bulbCount;
            int straightCellsPassed = 0;

            int stepsSinceLastTurn = 0;

            // Theo dõi các ô ĐÃ đi qua để đảm bảo đường đi không tự cắt chính nó.
            // Nếu path tự cắt (đi lại đúng 1 ô cũ), yêu cầu hướng của lần ghé đầu sẽ bị lần
            // ghé sau ghi đè -> gương đó không thể đáp ứng cả 2 yêu cầu -> level không giải được.
            var visitedCells = new HashSet<Vector2Int> { gunPos };

            int maxSteps = n * n * 2;
            int steps = 0;

            while (steps < maxSteps)
            {
                steps++;
                Vector2Int next = current + LaserDirectionUtil.ToVector(dir);

                if (next.x < 0 || next.x >= n || next.y < 0 || next.y >= n)
                {
                    dirs.Add(dir);
                    break;
                }

                if (visitedCells.Contains(next))
                {
                    // Sắp tự cắt chính nó -> huỷ lần thử này, để bên ngoài random lại từ đầu
                    return false;
                }
                visitedCells.Add(next);

                path.Add(next);
                dirs.Add(dir);
                current = next;
                stepsSinceLastTurn++;

                bool needMoreStraight = straightCellsPassed < requiredStraightCells;

                bool canStillTurn = mirrorsUsed < requiredMirrors && stepsSinceLastTurn >= _config.minStraightBetweenTurn;

                float turnChance = needMoreStraight ? 0.25f : 0.55f;
                bool shouldTurn = canStillTurn && UnityEngine.Random.value < turnChance;

                if (shouldTurn)
                {
                    LaserDirection newDir = UnityEngine.Random.value < 0.5f
                        ? LaserDirectionUtil.RotateClockwise(dir)
                        : LaserDirectionUtil.RotateClockwise(LaserDirectionUtil.RotateClockwise(LaserDirectionUtil.RotateClockwise(dir)));

                    Vector2Int test = current + LaserDirectionUtil.ToVector(newDir);
                    if (test.x >= 0 && test.x < n && test.y >= 0 && test.y < n)
                    {
                        dir = newDir;
                        mirrorsUsed++;
                    }
                    else
                    {
                        straightCellsPassed++;
                    }
                }
                else
                {
                    straightCellsPassed++;
                }

                if (mirrorsUsed >= requiredMirrors && straightCellsPassed >= requiredStraightCells)
                {
                    while (steps < maxSteps)
                    {
                        steps++;
                        Vector2Int exitNext = current + LaserDirectionUtil.ToVector(dir);
                        if (exitNext.x < 0 || exitNext.x >= n || exitNext.y < 0 || exitNext.y >= n)
                        {
                            dirs.Add(dir);
                            break;
                        }

                        if (visitedCells.Contains(exitNext))
                        {
                            // Tự cắt chính nó ngay cả ở đoạn thẳng cuối -> huỷ lần thử này
                            return false;
                        }
                        visitedCells.Add(exitNext);

                        path.Add(exitNext);
                        dirs.Add(dir);
                        current = exitNext;
                        straightCellsPassed++;
                    }
                    break;
                }
            }

            bool ok = mirrorsUsed >= requiredMirrors && straightCellsPassed >= requiredStraightCells && path.Count >= 2;
            return ok;
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // Mô phỏng bắn tia với gun + mirror ở ĐÚNG hướng đã tính là lời giải, xác nhận tia thực sự
        // sáng hết toàn bộ bulbCells trước khi ra khỏi lưới hoặc kẹt vòng lặp. Đây là lưới an toàn
        // cuối cùng: dù logic sinh path ở trên có bug gì (tự cắt, tính sai orientation, v.v...),
        // puzzle vẫn không bao giờ được chấp nhận nếu không thực sự giải được.
        private bool ValidateSolution(List<Vector2Int> path, LaserDirection solvedGunDir,
            Dictionary<Vector2Int, MirrorOrientation> pathMirrorOrientation, HashSet<Vector2Int> bulbCells, int n)
        {
            var visited = new HashSet<(int, int, LaserDirection)>();
            Vector2Int current = path[0];
            LaserDirection dir = solvedGunDir;
            var litBulbs = new HashSet<Vector2Int>();
            int safety = n * n * 4;

            while (safety-- > 0)
            {
                Vector2Int next = current + LaserDirectionUtil.ToVector(dir);

                if (next.x < 0 || next.x >= n || next.y < 0 || next.y >= n)
                    return false; // ra biên trước khi sáng hết đèn -> không hợp lệ

                if (!visited.Add((next.x, next.y, dir)))
                    return false; // kẹt vòng lặp giữa các gương -> không hợp lệ

                current = next;

                if (bulbCells.Contains(current))
                {
                    litBulbs.Add(current);
                    if (litBulbs.Count >= bulbCells.Count)
                        return true; // sáng hết đèn -> hợp lệ, dừng ngay tại đây
                }

                if (pathMirrorOrientation.TryGetValue(current, out var orientation))
                    dir = LaserDirectionUtil.Reflect(orientation, dir);
                // không phải mirror/bulb -> đi thẳng, không đổi hướng
            }

            return false;
        }

        #endregion

        #region Laser
        private IEnumerator FireLaserRoutine()
        {
            _isFiring = true;

            foreach (var cell in _cells) cell?.ResetLitVisual();

            var beamCells = new List<LaserCell>();
            var visited = new HashSet<(int, int, LaserDirection)>();

            LaserCell gunCell = _cells[_gunPos.x, _gunPos.y];
            Vector2Int current = _gunPos;
            LaserDirection dir = gunCell.gunFacing;
            beamCells.Add(gunCell);

            bool succeeded = false;
            bool failed = false; // trúng đá HOẶC chạm thành 
            int n = _config.gridSize;
            int safety = n * n * 4;
            int litCount = 0;

            while (safety-- > 0)
            {
                Vector2Int next = current + LaserDirectionUtil.ToVector(dir);

                if (next.x < 0 || next.x >= n || next.y < 0 || next.y >= n)
                {
                    // Chạm thành / ra khỏi biên lưới trước khi giải xong -> tính lỗi
                    failed = true;
                    break;
                }

                if (!visited.Add((next.x, next.y, dir)))
                {
                    // Vòng lặp vô hạn giữa các gương -> tính lỗi
                    failed = true;
                    break;
                }

                LaserCell cell = _cells[next.x, next.y];
                beamCells.Add(cell);
                current = next;

                if (cell.cellType == LaserCellType.Stone)
                {
                    
                    failed = true;
                    break;
                }
                else if (cell.cellType == LaserCellType.Bulb && !cell.IsLightUp)
                {
                    
                    litCount++;
                    cell.IsLightUp = true;
                    if (litCount >= _bulbTotalCount && _bulbTotalCount > 0)
                    {
                        succeeded = true;
                        break;
                    }
                }
                else if (cell.cellType == LaserCellType.Mirror)
                {
                    dir = LaserDirectionUtil.Reflect(cell.mirrorOrientation, dir);
                }
            }

            yield return StartCoroutine(AnimateBeam(beamCells));

            if (succeeded)
            {
                _puzzleSolved = true;
                PlaySuccessWave(beamCells);
                yield return new WaitForSeconds(0.3f);
                CompleteMinigame();
            }
            else if (failed)
            {
                if (beamCells.Count > 0)
                {
                    LaserCell impactCell = beamCells[beamCells.Count - 1];
                    if (impactCell != null && impactCell.cellType == LaserCellType.Stone)
                        impactCell.PlayBlockedEffect();
                }

                // GIỮ NGUYÊN toàn bộ fail feedback đang có.
                AudioController.Instance.PlaySFX(SoundName.Laser_Block);
                FilterController.Instance.FlashScreen(Color.gray, 0.3f);
                Camera.main.transform.DOShakePosition(0.3f, 0.5f, 5, 45f);
                RegisterMistake();
            }

            _isFiring = false;
        }

        private IEnumerator AnimateBeam(List<LaserCell> cellsAlongBeam)
        {
            if (_laserLine == null) yield break;

            _laserLine.useWorldSpace = true;
            _laserLine.startColor = _laserColor;
            _laserLine.endColor = _laserColor;
            _laserLine.positionCount = 0;

            float delay = _config != null ? _config.laserTravelTimePerCell : 0.05f;

            for (int i = 0; i < cellsAlongBeam.Count; i++)
            {
                LaserCell cell = cellsAlongBeam[i];

                _laserLine.positionCount = i + 1;
                _laserLine.SetPosition(i, cell.transform.position);

                if (cell.cellType == LaserCellType.Bulb)
                {
                    AudioController.Instance.PlaySFX(SoundName.Laser_Light);
                    cell.SetLit(true);
                }
                else
                {
                    cell.PlayBeamPassEffect();
                }
                    

                yield return new WaitForSeconds(delay);
            }
        }
        #endregion

        #region Visual FX - Additive
        private void PlayFireButtonEffect()
        {
            if (_fireButtonCollider == null) return;

            Transform target = _fireButtonCollider.transform;

            _fireButtonTween?.Kill(true);
            _fireButtonTween = target.DOPunchScale(
                target.localScale * _fireButtonPunchScale,
                _fireButtonPunchDuration,
                5,
                0.55f);
        }

        private void PlayWarningEffect(GameObject warning)
        {
            if (warning == null) return;

            Transform target = warning.transform;

            if (_warningTweens.TryGetValue(target, out Tween oldTween))
                oldTween?.Kill(true);

            Tween tween = target.DOPunchScale(
                target.localScale * _warningPunchScale,
                _warningPunchDuration,
                6,
                0.55f);

            _warningTweens[target] = tween;
        }

        private void PlaySuccessWave(List<LaserCell> beamCells)
        {
            if (beamCells == null) return;

            for (int i = 0; i < beamCells.Count; i++)
            {
                LaserCell cell = beamCells[i];
                if (cell == null) continue;

                cell.PlaySuccessEffect(i * _successWaveStagger);
            }
        }
        #endregion

        #region Reset
        private void RegisterMistake()
        {
            _mistakeCount++;
            

            int maxMistakes = _config != null ? _config.maxMistakes : 2;

            for (int i = 0; i < _mistakeCount; i++)
            {
                _mistakeWarnings[i].gameObject.SetActive(true);
            }

            int newestWarningIndex = _mistakeCount - 1;
            if (newestWarningIndex >= 0 && newestWarningIndex < _mistakeWarnings.Count)
                PlayWarningEffect(_mistakeWarnings[newestWarningIndex]);

            if (_mistakeCount >= maxMistakes)
            {
                OnGameReset();
                OnFailed();
                GameEvents.RaiseMinigameProgressReset(minigameType);

                FilterController.Instance.FlashScreen(_mistakeColor, _mistakeFlashDuration);
                Camera.main.transform.DOShakePosition(_mistakeFlashDuration, 0.5f, 20, 90f);

                HideMistakeWarningPanel();
            }
            else
            {
                StartCoroutine(ResetLaserAfterMistakeRoutine());
            }
        }

        private IEnumerator ResetLaserAfterMistakeRoutine()
        {
            yield return new WaitForSeconds(_mistakeFlashDuration);

            if (_laserLine != null) _laserLine.positionCount = 0;
            foreach (var cell in _cells) cell?.ResetLitVisual();
        }

        private void HideMistakeWarningPanel()
        {
            foreach(var o in _mistakeWarnings) o.SetActive(false);
        }

        private void ClearGrid()
        {
            _fireButtonTween?.Kill(true);

            foreach (var pair in _warningTweens)
                pair.Value?.Kill(true);
            _warningTweens.Clear();

            if (_cells != null)
            {
                foreach (LaserCell cell in _cells)
                {
                    if (cell != null)
                    {
                        Destroy(cell.gameObject);
                    }
                }

                _cells = null;
            }

            if (_cellRoot != null)
            {
                Destroy(_cellRoot.gameObject);
                _cellRoot = null;
            }
        }
        #endregion
    }
}