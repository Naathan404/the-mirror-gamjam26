using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using DG.Tweening;
using System.Collections;

namespace Game.Minigames
{
    public class SwitchMinigameController : MinigameBaseController
    {
        [Header("Switch Settings")]
        [SerializeField] private SwitchNode _nodePrefab;
        [SerializeField] private Material _lineMaterial;

        [Tooltip("Kéo BoxCollider vào đây làm vùng giới hạn sinh công tắc (Mặt X - Z)")]
        [SerializeField] private BoxCollider _spawnAreaCollider;

        [Header("Wire Colors (Màu sắc dây)")]
        [SerializeField] private Color _idleWireColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        [SerializeField] private Color _hoverWireColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

        [ColorUsage(true, true)]
        [SerializeField] private Color _pulseWireColor = new Color(0f, 1f, 1f, 1f);

        [Header("Visual FX - Additive")]
        [SerializeField] private float _nodeSpawnStagger = 0.05f;
        [SerializeField] private float _wireSpawnStagger = 0.02f;
        private SwitchConfig _currentConfig;

        private List<SwitchNode> _nodes = new List<SwitchNode>();
        private List<SwitchWire> _wires = new List<SwitchWire>();

        private Dictionary<SwitchNode, List<SwitchNode>> _graph = new Dictionary<SwitchNode, List<SwitchNode>>();
        private Dictionary<string, SwitchWire> _wireMap = new Dictionary<string, SwitchWire>();

        private bool _isProcessingTurn = false;
        private bool _isSpawning = false;

        protected override void OnGameStart()
        {
            GenerateBoard();
        }

        protected override void OnGameReset()
        {
            ClearBoard();
            GenerateBoard();
        }

        protected override void OnDifficultyIncrease(int minigamePassed)
        {
            _currentConfig = _difficultyConfig.GetMinigameConfig<SwitchConfig>(minigamePassed);
            if (_currentConfig == null)
            {
                Debug.LogError("[SwitchMinigame] Không tìm thấy SwitchConfig!");
            }
        }

        private void GenerateBoard()
        {
            ClearBoard();
            if (_currentConfig == null) return;

            _isSpawning = true;

            Vector3 worldCenter = _spawnAreaCollider != null ?
                _spawnAreaCollider.transform.TransformPoint(_spawnAreaCollider.center) : Vector3.zero;

            for (int i = 0; i < _currentConfig.nodeCount; i++)
            {
                SwitchNode newNode = Instantiate(_nodePrefab, visualRoot.transform);

                Vector3 jitter = new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), 0, UnityEngine.Random.Range(-0.1f, 0.1f));
                newNode.transform.position = worldCenter + jitter;

                newNode.Init(i);
                newNode.OnNodeClicked += HandleNodeClicked;
                newNode.OnNodeHovered += HandleNodeHovered;

                _nodes.Add(newNode);
                _graph[newNode] = new List<SwitchNode>();
            }

            // KẾT NỐI LOGIC ĐỒ THỊ
            GenerateLogicEdges();
            ScramblePuzzle();
            CheckWinCondition();

            // Hiệu ứng
            ApplyForceDirectedLayout();
            DrawVisualWires();
            StartCoroutine(SpawnEffectRoutine());
        }

        private void GenerateLogicEdges()
        {
            for (int i = 1; i < _nodes.Count; i++)
            {
                List<SwitchNode> validTargets = new List<SwitchNode>();
                for (int j = 0; j < i; j++)
                {
                    if (_graph[_nodes[j]].Count < _currentConfig.maxEdgesPerNode)
                    {
                        validTargets.Add(_nodes[j]);
                    }
                }

                SwitchNode targetNode = validTargets.Count > 0
                    ? validTargets[UnityEngine.Random.Range(0, validTargets.Count)]
                    : _nodes[UnityEngine.Random.Range(0, i)];

                _graph[_nodes[i]].Add(targetNode);
                _graph[targetNode].Add(_nodes[i]);
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                int attempts = 0;

                while (_graph[_nodes[i]].Count < _currentConfig.maxEdgesPerNode && attempts < 15)
                {
                    attempts++;
                    SwitchNode randomNode = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];

                    if (randomNode != _nodes[i]
                        && !_graph[_nodes[i]].Contains(randomNode)
                        && _graph[randomNode].Count < _currentConfig.maxEdgesPerNode)
                    {
                        _graph[_nodes[i]].Add(randomNode);
                        _graph[randomNode].Add(_nodes[i]);
                    }
                }
            }
        }

        private void ApplyForceDirectedLayout()
        {
            if (_spawnAreaCollider == null || _nodes.Count == 0) return;

            Vector3 areaSize = _spawnAreaCollider.size;
            Vector3 areaCenter = _spawnAreaCollider.center;

            float minX = areaCenter.x - (areaSize.x / 2f) + 0.3f;
            float maxX = areaCenter.x + (areaSize.x / 2f) - 0.3f;
            float minZ = areaCenter.z - (areaSize.z / 2f) + 0.3f;
            float maxZ = areaCenter.z + (areaSize.z / 2f) - 0.3f;

            float usableArea = (maxX - minX) * (maxZ - minZ);
            float k = Mathf.Sqrt(usableArea / _nodes.Count) * 1.5f;
            float temperature = areaSize.x / 3f;

            Vector3[] localPos = new Vector3[_nodes.Count];

            for (int i = 0; i < _nodes.Count; i++)
            {
                float randX = UnityEngine.Random.Range(minX, maxX);
                float randZ = UnityEngine.Random.Range(minZ, maxZ);
                localPos[i] = new Vector3(randX, areaCenter.y, randZ);
            }

            for (int iter = 0; iter < 120; iter++)
            {
                Vector3[] displacements = new Vector3[_nodes.Count];

                for (int i = 0; i < _nodes.Count; i++)
                {
                    for (int j = i + 1; j < _nodes.Count; j++)
                    {
                        Vector3 delta = localPos[i] - localPos[j];
                        delta.y = 0;
                        float dist = delta.magnitude;

                        if (dist < 0.05f) { delta = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized * 0.05f; dist = delta.magnitude; }

                        Vector3 force = (delta / dist) * (k * k / dist);
                        displacements[i] += force;
                        displacements[j] -= force;
                    }

                    Vector3 toCenter = areaCenter - localPos[i];
                    toCenter.y = 0;
                    displacements[i] += toCenter * 0.03f;

                    float wallMargin = k * 0.6f;
                    float wallForce = k * 0.2f;

                    float dLeft = localPos[i].x - minX; if (dLeft < wallMargin) displacements[i].x += (wallMargin - dLeft) * wallForce;
                    float dRight = maxX - localPos[i].x; if (dRight < wallMargin) displacements[i].x -= (wallMargin - dRight) * wallForce;
                    float dBottom = localPos[i].z - minZ; if (dBottom < wallMargin) displacements[i].z += (wallMargin - dBottom) * wallForce;
                    float dTop = maxZ - localPos[i].z; if (dTop < wallMargin) displacements[i].z -= (wallMargin - dTop) * wallForce;
                }

                for (int i = 0; i < _nodes.Count; i++)
                {
                    foreach (var neighbor in _graph[_nodes[i]])
                    {
                        int j = _nodes.IndexOf(neighbor);
                        if (i >= j) continue;

                        Vector3 delta = localPos[i] - localPos[j];
                        delta.y = 0;
                        float dist = Mathf.Max(delta.magnitude, 0.05f);

                        Vector3 force = (delta / dist) * (dist * dist / (k * 1.2f));
                        displacements[i] -= force;
                        displacements[j] += force;
                    }
                }

                ApplyDisplacements(localPos, displacements, temperature, minX, maxX, minZ, maxZ, areaCenter.y);
                temperature *= 0.95f;
            }

            // PHASE 2 & 3: ÉP KHOẢNG CÁCH & BẺ CONG 3 ĐIỂM THẲNG HÀNG
            float safeDistance = k * 0.8f;
            float lineSafeDist = k * 0.4f;
            temperature = safeDistance / 2f;

            for (int iter = 0; iter < 50; iter++)
            {
                Vector3[] displacements = new Vector3[_nodes.Count];

                for (int i = 0; i < _nodes.Count; i++)
                {
                    for (int j = i + 1; j < _nodes.Count; j++)
                    {
                        Vector3 delta = localPos[i] - localPos[j];
                        delta.y = 0;
                        float dist = delta.magnitude;

                        if (dist < safeDistance)
                        {
                            if (dist < 0.01f) { delta = new Vector3(0.1f, 0, 0.1f); dist = delta.magnitude; }
                            Vector3 force = (delta / dist) * (safeDistance - dist);
                            displacements[i] += force;
                            displacements[j] -= force;
                        }
                    }
                }

                for (int i = 0; i < _nodes.Count; i++)
                {
                    for (int u = 0; u < _nodes.Count; u++)
                    {
                        foreach (var neighbor in _graph[_nodes[u]])
                        {
                            int v = _nodes.IndexOf(neighbor);
                            if (u >= v) continue;
                            if (i == u || i == v) continue;

                            Vector3 closestPoint = GetClosestPointOnSegment(localPos[i], localPos[u], localPos[v]);
                            Vector3 delta = localPos[i] - closestPoint;
                            delta.y = 0;
                            float dist = delta.magnitude;

                            if (dist < lineSafeDist)
                            {
                                if (dist < 0.001f) { delta = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized * 0.01f; dist = delta.magnitude; }

                                Vector3 push = (delta / dist) * (lineSafeDist - dist);
                                displacements[i] += push;

                                displacements[u] -= push * 0.5f;
                                displacements[v] -= push * 0.5f;
                            }
                        }
                    }
                }

                ApplyDisplacements(localPos, displacements, temperature, minX, maxX, minZ, maxZ, areaCenter.y);
                temperature *= 0.9f;
            }

            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].transform.position = _spawnAreaCollider.transform.TransformPoint(localPos[i]);
            }
        }

        /// <summary>
        /// Thuật toán tìm điểm nằm trên đoạn thẳng AB sao cho gần điểm P nhất
        /// </summary>
        private Vector3 GetClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
            t = Mathf.Clamp01(t);
            return a + t * ab;
        }

        private void ApplyDisplacements(Vector3[] localPos, Vector3[] displacements, float temp, float minX, float maxX, float minZ, float maxZ, float fixY)
        {
            for (int i = 0; i < localPos.Length; i++)
            {
                Vector3 disp = displacements[i];
                float dist = disp.magnitude;

                if (dist > 0)
                {
                    localPos[i] += (disp / dist) * Mathf.Min(dist, temp);
                }

                localPos[i].x = Mathf.Clamp(localPos[i].x, minX, maxX);
                localPos[i].y = fixY;
                localPos[i].z = Mathf.Clamp(localPos[i].z, minZ, maxZ);
            }
        }

        private void DrawVisualWires()
        {
            HashSet<string> drawnWires = new HashSet<string>();
            foreach (var nodeA in _nodes)
            {
                foreach (var nodeB in _graph[nodeA])
                {
                    string key = GetWireKey(nodeA, nodeB);
                    if (drawnWires.Contains(key)) continue;

                    GameObject wireObj = new GameObject($"Wire_{nodeA.ID}_{nodeB.ID}");
                    wireObj.transform.SetParent(visualRoot.transform);
                    wireObj.transform.localPosition = Vector3.zero;

                    SwitchWire wire = wireObj.AddComponent<SwitchWire>();
                    wire.Init(nodeA, nodeB, _lineMaterial, _idleWireColor, _hoverWireColor, _pulseWireColor);

                    _wires.Add(wire);
                    _wireMap[key] = wire;
                    drawnWires.Add(key);
                }
            }
        }

        private void ClearBoard()
        {
            foreach (var node in _nodes) if (node != null) Destroy(node.gameObject);
            foreach (var wire in _wires) if (wire != null) Destroy(wire.gameObject);

            _nodes.Clear();
            _wires.Clear();
            _graph.Clear();
            _wireMap.Clear();
            _isProcessingTurn = false;
        }

        private string GetWireKey(SwitchNode a, SwitchNode b)
        {
            return a.ID < b.ID ? $"{a.ID}_{b.ID}" : $"{b.ID}_{a.ID}";
        }

        private void ScramblePuzzle()
        {
            int targetSteps = Mathf.Min(_currentConfig.minimunStepsToSolve, _nodes.Count);
            int maxAttemptsPerGraph = 100;
            int maxGraphRegenerations = 20;

            for (int graphAttempt = 0; graphAttempt < maxGraphRegenerations; graphAttempt++)
            {
                if (graphAttempt > 0)
                {
                    foreach (var node in _nodes)
                    {
                        if (_graph.ContainsKey(node))
                        {
                            _graph[node].Clear();
                        }
                    }

                    GenerateLogicEdges();
                }

                int bestStepsFound = -1;
                Dictionary<SwitchNode, bool> bestNodeStates = new Dictionary<SwitchNode, bool>();

                for (int attempt = 0; attempt < maxAttemptsPerGraph; attempt++)
                {
                    foreach (var node in _nodes)
                    {
                        node.SetState(true, false);
                    }

                    List<SwitchNode> availableNodes = new List<SwitchNode>(_nodes);
                    int currentShuffleSteps = Mathf.Min(targetSteps, availableNodes.Count);

                    for (int i = 0; i < currentShuffleSteps; i++)
                    {
                        int randomIndex = UnityEngine.Random.Range(0, availableNodes.Count);
                        SwitchNode randomNode = availableNodes[randomIndex];
                        availableNodes.RemoveAt(randomIndex);

                        randomNode.SetState(!randomNode.IsOn, false);
                        foreach (var neighbor in _graph[randomNode])
                        {
                            neighbor.SetState(!neighbor.IsOn, false);
                        }
                    }

                    int currentMinSteps = GetMinimumStepsToSolve();

                    if (currentMinSteps == targetSteps)
                    {
                        return;
                    }

                    if (currentMinSteps > bestStepsFound)
                    {
                        bestStepsFound = currentMinSteps;
                        bestNodeStates.Clear();
                        foreach (var node in _nodes)
                        {
                            bestNodeStates[node] = node.IsOn;
                        }
                    }
                }

            }

            if (_nodes.Count > 0 && GetMinimumStepsToSolve() == 0)
            {
                SwitchNode forcedNode = _nodes[UnityEngine.Random.Range(0, _nodes.Count)];
                forcedNode.SetState(!forcedNode.IsOn, false);
                foreach (var neighbor in _graph[forcedNode])
                {
                    neighbor.SetState(!neighbor.IsOn, false);
                }
            }
        }

        private int GetMinimumStepsToSolve()
        {
            int n = _nodes.Count;
            if (n > 31) return -1; // Vượt quá giới hạn bit của kiểu int

            int[] toggleMasks = new int[n];
            for (int i = 0; i < n; i++)
            {
                toggleMasks[i] |= (1 << i);
                foreach (var neighbor in _graph[_nodes[i]])
                {
                    int j = _nodes.IndexOf(neighbor);
                    toggleMasks[i] |= (1 << j);
                }
            }

            int startState = 0;
            for (int i = 0; i < n; i++)
            {
                if (!_nodes[i].IsOn)
                {
                    startState |= (1 << i);
                }
            }

            if (startState == 0) return 0;

            Queue<int> queue = new Queue<int>();
            Dictionary<int, int> distances = new Dictionary<int, int>();

            queue.Enqueue(startState);
            distances[startState] = 0;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int currentDist = distances[current];

                for (int i = 0; i < n; i++)
                {
                    int nextState = current ^ toggleMasks[i];

                    if (!distances.ContainsKey(nextState))
                    {
                        distances[nextState] = currentDist + 1;

                        if (nextState == 0) return distances[nextState];

                        queue.Enqueue(nextState);
                    }
                }
            }

            return -1;
        }

        private IEnumerator SpawnEffectRoutine()
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                _nodes[i].PlaySpawnEffect(i * _nodeSpawnStagger);
            }

            yield return new WaitForSeconds((_nodes.Count * _nodeSpawnStagger) + 0.3f);

            for (int i = 0; i < _wires.Count; i++)
            {
                _wires[i].PlaySpawnEffect(i * _wireSpawnStagger);
            }

            yield return new WaitForSeconds((_wires.Count * _wireSpawnStagger) + 0.3f);

            _isSpawning = false;
        }

        private void HandleNodeHovered(SwitchNode node, bool isEnter)
        {
            if (isCompleting || !isPlaying || _isProcessingTurn || _isSpawning) return;

            foreach (var neighbor in _graph[node])
            {
                if (_wireMap.TryGetValue(GetWireKey(node, neighbor), out SwitchWire wire))
                {
                    wire.SetHoverState(isEnter);
                }
            }
        }

        private void HandleNodeClicked(SwitchNode clickedNode)
        {
            if (_isProcessingTurn || isCompleting || !isPlaying || _isSpawning) return;

            HandleNodeHovered(clickedNode, false);

            _isProcessingTurn = true;

            clickedNode.SetState(!clickedNode.IsOn);
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            AudioController.Instance.PlaySFX(SoundName.ChargeCompleted);

            List<SwitchNode> neighbors = _graph[clickedNode];
            if (neighbors.Count == 0)
            {
                CheckWinCondition();
                return;
            }

            int pulsesArrived = 0;
            foreach (var neighbor in neighbors)
            {
                SwitchWire wire = _wireMap[GetWireKey(clickedNode, neighbor)];

                wire.ShootPulse(clickedNode, () =>
                {
                    neighbor.SetState(!neighbor.IsOn);
                    pulsesArrived++;

                    if (pulsesArrived >= neighbors.Count)
                    {
                        CheckWinCondition();
                    }
                });
            }
        }

        private void CheckWinCondition()
        {
            bool allOn = true;
            Debug.Log("[SWITCH] Số bước tối thiểu để thắng: " + GetMinimumStepsToSolve());
            foreach (var node in _nodes)
            {
                if (!node.IsOn)
                {
                    allOn = false;
                    break;
                }
            }

            if (allOn)
            {
                CompleteMinigame();
            }
            else
            {
                _isProcessingTurn = false;
            }
        }
    }
}