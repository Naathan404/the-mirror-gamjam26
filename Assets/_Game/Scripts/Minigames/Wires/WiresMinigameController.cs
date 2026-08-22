using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Minigames.Maze;
using UnityEngine;

namespace Game.Minigames.Wires
{
    public class WiresMinigameController : MinigameBaseController
    {
        #region Serialize fields
        [Header("Configs")]
        [SerializeField] private WiresConfig _config;

        [Header("Socket")]
        [SerializeField] private WireSocket _leftSocketPrefab;
        [SerializeField] private WireSocket _rightSocketPrefab;
        [SerializeField] private Transform _leftSocketsContainer;
        [SerializeField] private Transform _rightSocketsContainer;

        [Header("Hint Indicator")]
        [SerializeField] private WireSocket _hintIndicatorPrefab;
        [SerializeField] private Transform _hintIndicatorsContainer;
        [Range(0f, 0.3f)]
        [SerializeField] private float _hintExtraOffsetPercent = 0.08f;

        [Header("Layout")]
        [Range(0f, 0.45f)]
        [SerializeField] private float _horizontalPaddingPercent = 0.1f;
        [Range(0f, 0.45f)]
        [SerializeField] private float _verticalPaddingPercent = 0.12f;
        [SerializeField] private float _socketZOffset = -0.1f;
        [SerializeField] private List<GameObject> _mistakeWarnings;


        [Header("Visual")]
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private LineRenderer _linePrefab;
        [SerializeField] private Transform _linesContainer;
        [Range(0f, 1f)]
        [SerializeField] private float _curveBulge = 0.2f;
        [SerializeField] private int _curveSegments = 24;

        [Header("Interact")]
        [SerializeField] private LayerMask _socketLayerMask;
        [SerializeField] private float _raycastMaxDistance = 5f;

        [Header("Penalty")]
        [SerializeField] private bool _enablePenalty = true;
        [SerializeField] private float _mistakeFlashDuration = 0.35f;
        [SerializeField] private Color _mistakeColor = Color.red;
        [SerializeField] private Color _pendingDragColor = Color.white;

        #endregion

        public int WireCount => _config.WireCount;
        public int HiddenCount => _config.HiddenCount;

        #region Private
        private readonly Dictionary<WireSocket, LineRenderer> _activeLines = new();
        private WireSocket _draggingFrom;
        private LineRenderer _draggingLine;
        private int _connectedCount = 0;
        private int _mistakeCount = 0;
        private int _maxMistakeCount = 1;

        private readonly List<WireSocket> _leftSockets = new();
        private readonly List<WireSocket> _rightSockets = new();
        private readonly List<WireSocket> _hintIndicators = new();
        private readonly Dictionary<ColorId, ColorId> _requiredMatch = new();
        private readonly List<ColorId> _forcedHiddenColors = new();
        private readonly List<ColorId> _leftColorOrder = new();
        #endregion

        #region Event
        protected override void OnGameStart()
        {
            WarmUp();
            ResetVisualsAndState();
            AssignRandomColors();
            SetVisibleWireSockets(true);
            ApplyForcedDeductionHints();
            HideMistakeWarningPanel();
        }

        protected override void OnGameReset()
        {
            ResetVisualsAndState();
            DestroyAllSockets();
            _mistakeCount = 0;
            HideMistakeWarningPanel();
            OnGameStart();
        }

        protected override void OnGameClosed()
        {
            ResetVisualsAndState();
            DestroyAllSockets();
        }
        #endregion

        private void Update()
        {
            if (!isPlaying || !isFocused) return;
            HandleInput();
        }

        #region Core Logic
        private void WarmUp()
        {
            _leftSockets.Clear();
            _rightSockets.Clear();
            _hintIndicators.Clear();

            int count = Mathf.Min(WireCount, _config.ColorCount);

            List<Vector3> leftPositions = ComputeSocketPositions(count, WireSide.Left);
            List<Vector3> rightPositions = ComputeSocketPositions(count, WireSide.Right);
            List<Vector3> hintPositions = ComputeHintPositions(count);

            for (int i = 0; i < count; i++)
            {
                WireSocket left = Instantiate(_leftSocketPrefab, _leftSocketsContainer);
                WireSocket right = Instantiate(_rightSocketPrefab, _rightSocketsContainer);

                left.transform.position = leftPositions[i];
                right.transform.position = rightPositions[i];

                left.Initial(WireSide.Left);
                right.Initial(WireSide.Right);

                _leftSockets.Add(left);
                _rightSockets.Add(right);

                left.transform.localScale = _config.Scale;
                right.transform.localScale = _config.Scale;

                left.gameObject.SetActive(false);
                right.gameObject.SetActive(false);

                if (_hintIndicatorPrefab != null && _hintIndicatorsContainer != null)
                {
                    WireSocket hint = Instantiate(_hintIndicatorPrefab, _hintIndicatorsContainer);
                    hint.transform.position = hintPositions[i];
                    _hintIndicators.Add(hint);
                    hint.gameObject.SetActive(false);
                }
            }
        }

        private void SetVisibleWireSockets(bool active)
        {
            foreach(Transform child in _leftSocketsContainer)
            {
                child.gameObject.SetActive(active);
            }

            foreach(Transform child in _rightSocketsContainer)
            {
                child.gameObject.SetActive(active);
            }

            if (_hintIndicatorsContainer != null)
            {
                foreach (Transform child in _hintIndicatorsContainer)
                {
                    child.gameObject.SetActive(active);
                }
            }
        }

        private void DestroyAllSockets()
        {
            foreach(Transform child in _leftSocketsContainer)
            {
                Destroy(child.gameObject);
            }

            foreach(Transform child in _rightSocketsContainer)
            {
                Destroy(child.gameObject);
            }

            if (_hintIndicatorsContainer != null)
            {
                foreach (Transform child in _hintIndicatorsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private List<Vector3> ComputeHintPositions(int count)
        {
            var positions = new List<Vector3>(count);
            if (_background == null || _background.sprite == null || count <= 0) return positions;

            Vector2 localExtents = _background.sprite.bounds.extents;
            float halfWidth = localExtents.x;
            float halfHeight = localExtents.y;

            float insetX = halfWidth * _horizontalPaddingPercent;
            float extraOffset = halfWidth * _hintExtraOffsetPercent;
            float xLocal = -halfWidth + insetX - extraOffset;

            float insetY = halfHeight * _verticalPaddingPercent;
            float topYLocal = halfHeight - insetY;
            float bottomYLocal = -halfHeight + insetY;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float yLocal = Mathf.Lerp(topYLocal, bottomYLocal, t);

                Vector3 localPoint = new Vector3(xLocal, yLocal, _socketZOffset);
                positions.Add(_background.transform.TransformPoint(localPoint));
            }

            return positions;
        }

        private List<Vector3> ComputeSocketPositions(int count, WireSide side)
        {
            var positions = new List<Vector3>(count);
            if (_background == null || _background.sprite == null || count <= 0) return positions;

            Vector2 localExtents = _background.sprite.bounds.extents;

            float halfWidth = localExtents.x;
            float halfHeight = localExtents.y;

            float insetX = halfWidth * _horizontalPaddingPercent;
            float insetY = halfHeight * _verticalPaddingPercent;

            float xLocal = side == WireSide.Left
                ? -halfWidth + insetX
                : halfWidth - insetX;

            float topYLocal = halfHeight - insetY;
            float bottomYLocal = -halfHeight + insetY;

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float yLocal = Mathf.Lerp(topYLocal, bottomYLocal, t);

                Vector3 localPoint = new Vector3(xLocal, yLocal, _socketZOffset);
                positions.Add(_background.transform.TransformPoint(localPoint));
            }

            return positions;
        }

        private void AssignRandomColors()
        {
            int count = Mathf.Min(WireCount, _config.ColorCount);

            var allColors = new List<ColorId>((ColorId[])System.Enum.GetValues(typeof(ColorId)));
            var pickedColors = new List<ColorId>(count);
            while (pickedColors.Count < count && allColors.Count > 0)
            {
                int r = Random.Range(0, allColors.Count);
                pickedColors.Add(allColors[r]);
                allColors.RemoveAt(r);
            }

            int forcedDeductionCount = Mathf.Clamp(_config.ForcedDeductionCount, 0, 2);

            _requiredMatch.Clear();
            _forcedHiddenColors.Clear();

            if (forcedDeductionCount == 2 && pickedColors.Count >= 4)
            {
                var pairCandidates = Shuffle(pickedColors);
                ColorId a = pairCandidates[0];
                ColorId b = pairCandidates[1];

                _requiredMatch[a] = b;
                _requiredMatch[b] = a;
                _forcedHiddenColors.Add(a);
                _forcedHiddenColors.Add(b);

                var remaining = pickedColors.FindAll(c => !c.Equals(a) && !c.Equals(b));
                foreach (var kvp in GenerateRequiredMatchMap(remaining))
                {
                    _requiredMatch[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                foreach (var kvp in GenerateRequiredMatchMap(pickedColors))
                {
                    _requiredMatch[kvp.Key] = kvp.Value;
                }

                if (forcedDeductionCount == 1 && pickedColors.Count >= 2)
                {
                    // k=1 luôn AN TOÀN với bất kỳ màu nào, không cần cấu trúc
                    // đặc biệt gì - vì chỉ còn đúng 1 màu nguồn + 1 màu đích
                    // trống, bị ép ghép với nhau, và không thể là tự-nối-
                    // chính-nó do map gốc vốn đã là derangement toàn cục.
                    _forcedHiddenColors.Add(pickedColors[Random.Range(0, pickedColors.Count)]);
                }
            }

            List<ColorId> leftOrders = Shuffle(pickedColors);
            List<ColorId> rightOrders = Shuffle(pickedColors);

            _leftColorOrder.Clear();

            for (int i = 0; i < count; i++)
            {
                _leftSockets[i].SetColor(leftOrders[i]);
                _rightSockets[i].SetColor(rightOrders[i]);
                _leftColorOrder.Add(leftOrders[i]);

                if (i < _hintIndicators.Count)
                {
                    ColorId requiredTarget = _requiredMatch[leftOrders[i]];
                    _hintIndicators[i].SetColor(requiredTarget);
                }
            }
        }


        private Dictionary<ColorId, ColorId> GenerateRequiredMatchMap(List<ColorId> colors)
        {
            var map = new Dictionary<ColorId, ColorId>();
            int n = colors.Count;

            if (n <= 1)
            {
                foreach (var c in colors) map[c] = c;
                return map;
            }

            int[] perm = new int[n];
            for (int i = 0; i < n; i++) perm[i] = i;

            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }

            for (int i = 0; i < n; i++)
            {
                map[colors[i]] = colors[perm[i]];
            }

            return map;
        }

        private List<T> Shuffle<T>(IEnumerable<T> source)
        {
            var list = new List<T>(source);
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }
        #endregion

        #region Input
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var socket = RaycastSocket();
                if (socket != null && socket.Side == WireSide.Left && !socket.IsConnected)
                {
                    AudioController.Instance.PlaySFX(SoundName.ButtonClick);
                    BeginDrag(socket);
                }
            }
            else if (Input.GetMouseButton(0) && _draggingFrom != null)
            {
                UpdateDragPreview();
            }
            else if (Input.GetMouseButtonUp(0) && _draggingFrom != null)
            {
                EndDrag();
            }
        }

        // private WireSocket RaycastSocket()
        // {
        //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        //     if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxDistance, _socketLayerMask))
        //     {
        //         return hit.collider.GetComponent<WireSocket>();
        //     }
        //     return null;
        // }

        private WireSocket RaycastSocket()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Debug.DrawRay(ray.origin, ray.direction * _raycastMaxDistance, Color.red, 2f);

            if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxDistance, _socketLayerMask))
            {
                Debug.Log($"[Wires] Raycast trúng: {hit.collider.name} (layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                return hit.collider.GetComponent<WireSocket>();
            }

            Debug.Log("[Wires] Raycast KHÔNG trúng gì cả");
            return null;
        }

        private bool RaycastPoint(out Vector3 point)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                point = default;
                return false;
            }

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, _raycastMaxDistance, _socketLayerMask))
            {
                point = hit.point;
                return true;
            }

            if (_draggingFrom != null)
            {
                Plane plane = new Plane(-cam.transform.forward, _draggingFrom.AnchorPosition);
                if (plane.Raycast(ray, out float enter))
                {
                    point = ray.GetPoint(enter);
                    return true;
                }
            }

            point = default;
            return false;
        }
        #endregion

        #region Dragging
        private void BeginDrag(WireSocket from)
        {
            from.transform.DOKill();
            from.transform.localScale = _config.Scale;

            from.transform.DOPunchScale(0.2f * _config.Scale, 0.3f);
            _draggingFrom = from;
            _draggingLine = Instantiate(_linePrefab, _linesContainer);
            _draggingLine.positionCount = _curveSegments;
            _draggingLine.startColor = _config.GetColorById(from.ColorId);
            _draggingLine.endColor = _config.GetColorById(from.ColorId);
        }

        private void UpdateDragPreview()
        {
            if (_draggingLine == null) return;
            Vector3 end = RaycastPoint(out Vector3 hitPoint) ? hitPoint : _draggingFrom.AnchorPosition;
            DrawCurve(_draggingLine, _draggingFrom.AnchorPosition, end);
        }

        private void EndDrag()
        {
            var target = RaycastSocket();

            if (_draggingLine != null)
            {
                Destroy(_draggingLine.gameObject);
            }
            _draggingLine = null;

            if (target != null && target.Side == WireSide.Right && !target.IsConnected)
            {
                TryConnect(_draggingFrom, target);
            }

            _draggingFrom = null;
        }
        #endregion

        #region Connect
        private bool TryConnect(WireSocket from, WireSocket to)
        {
            bool isCorrect = _requiredMatch.TryGetValue(from.ColorId, out ColorId requiredTarget)
                && requiredTarget == to.ColorId;

            if (isCorrect)
            {
                from.IsConnected = true;
                to.IsConnected = true;

                from.transform.DOPunchScale(0.2f * from.OriginalScale, 0.2f);
                to.transform.DOPunchScale(0.2f * to.OriginalScale, 0.2f);

                var line = Instantiate(_linePrefab, _linesContainer);
                line.positionCount = _curveSegments;

                line.startColor = _config.GetColorById(from.ColorId);
                line.endColor = _config.GetColorById(to.ColorId);

                DrawCurve(line, from.AnchorPosition, to.AnchorPosition);
                _activeLines[from] = line;

                _connectedCount++;
                AudioController.Instance.PlaySFX(SoundName.Wire_Success);

                int count = Mathf.Min(WireCount, _config.ColorCount);

                if (_connectedCount >= count)
                {
                    CompleteMinigame();
                }
                return true;
            }
            else
            {
                if (_enablePenalty)
                {
                    FilterController.Instance.FlashScreen(_mistakeColor, _mistakeFlashDuration);
                    Camera.main.transform.DOShakePosition(_mistakeFlashDuration, 0.5f, 20, 90f);
                    to.transform.DOShakePosition(_mistakeFlashDuration, 0.8f, 15, 45);

                    _mistakeCount++;
                    CheckMistakeCountToReset();
                }
                AudioController.Instance.PlaySFX(SoundName.Wire_Fail);
                return false;
            }
        }
        #endregion

        #region Draw
        private void DrawCurve(LineRenderer line, Vector3 start, Vector3 end)
        {
            Vector3 mid = (start + end) * 0.5f;
            var cam = Camera.main;

            // Lệch điểm giữa theo hướng vuông góc với đoạn thẳng start-end
            // để tạo độ cong, đồng thời offset ngẫu nhiên nhẹ theo trục lên/xuống
            // -> nhiều dây cong khác nhau sẽ đan chéo nhau
            Vector3 dir = (end - start).normalized;
            Vector3 perpendicular = Vector3.Cross(dir, cam != null ? cam.transform.forward : Vector3.forward).normalized;

            float bulgeAmount = Vector3.Distance(start, end) * _curveBulge;
            Vector3 controlPoint = mid + perpendicular * bulgeAmount;

            for (int i = 0; i < _curveSegments; i++)
            {
                float t = i / (float)(_curveSegments - 1);
                Vector3 point = QuadraticBezier(start, controlPoint, end, t);
                line.SetPosition(i, point);
            }
        }

        private Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            float u = 1f - t;
            return (u * u * a) + (2f * u * t * b) + (t * t * c);
        }
        #endregion

        #region Reset
        private void ResetVisualsAndState()
        {
            foreach (var kvp in _activeLines)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _activeLines.Clear();

            if (_draggingLine != null) Destroy(_draggingLine.gameObject);
            _draggingLine = null;
            _draggingFrom = null;

            _connectedCount = 0;

            foreach (var s in _leftSockets) s.ResetSocket();
            foreach (var s in _rightSockets) s.ResetSocket();
        }

        private void CheckMistakeCountToReset()
        {
            for (int i = 0; i < _mistakeCount; i++)
            {
                _mistakeWarnings[i].SetActive(true);
            }

            if (_mistakeCount == 1)
            {
                HideRandomSockets(_rightSockets, HiddenCount);
            }

            if (_mistakeCount > _maxMistakeCount)
            {
                OnFailed();

                _mistakeCount = 0;
                ResetVisualsAndState();
                AssignRandomColors();
                // SetVisibleWireSockets(true);
                ApplyForcedDeductionHints();
                HideMistakeWarningPanel();
            }
        }

        private void ApplyForcedDeductionHints()
        {
            if (_hintIndicators.Count == 0) return;

            foreach (var hint in _hintIndicators)
            {
                hint.gameObject.SetActive(true);
            }

            if (_forcedHiddenColors.Count == 0) return;

            for (int i = 0; i < _leftColorOrder.Count && i < _hintIndicators.Count; i++)
            {
                if (_forcedHiddenColors.Contains(_leftColorOrder[i]))
                {
                    _hintIndicators[i].gameObject.SetActive(false);
                }
            }
        }
        
        private void HideRandomSockets(List<WireSocket> sockets, int hiddenCount)
        {
            hiddenCount = Mathf.Clamp(hiddenCount, 0, sockets.Count);

            // shuffle Fisher-Yates
            List<int> indices = new List<int>(sockets.Count);
            for (int i = 0; i < sockets.Count; i++)
                indices.Add(i);

            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            for (int i = 0; i < hiddenCount; i++)
            {
                sockets[indices[i]].SetHidden();
            }
        }

        private void HideMistakeWarningPanel()
        {
            foreach(var o in _mistakeWarnings) o.SetActive(false);
        }

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            _config = _difficultyConfig.GetMinigameConfig<WiresConfig>(minigamePassed);
        }
        #endregion
    }
}