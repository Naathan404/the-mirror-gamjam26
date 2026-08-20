using UnityEngine;
using Game.Core;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace Game.Minigames
{
    [DefaultExecutionOrder(-5)]
    public abstract class MinigameBaseController : MonoBehaviour
    {
        [Header("Difficulty SO")]
        [SerializeField] protected MinigameDifficultyConfigSO _difficultyConfig;

        [Header("Base Cấu hình")]
        public MinigameType minigameType;
        public Color minigameColor;

        [Header("Base Tham chiếu")]
        public GameObject visualRoot;

        [Header("Fail Penalty Config")]
        [SerializeField] private float _increaseEntityStateChangeAccelaration = 1.2f;

        [Header("Phần thưởng (Mảnh giấy chứa Số)")]
        [Tooltip("Prefab của tờ giấy nhỏ chứa mật mã")]
        public GameObject digitPaperPrefab;
        [Tooltip("Vùng mặt bàn (BoxCollider) để tờ giấy rơi xuống và người chơi kéo thả")]
        public BoxCollider deskSpawnArea;

        // Dữ liệu dùng chung
        protected int secretDigit = -1;
        protected bool isPlaying = false;
        protected bool isFocused = true;

        // Biến cờ khóa tương tác khi đang chạy hiệu ứng Win
        protected bool isCompleting = false;

        // ================= ĐĂNG KÝ EVENT =================
        protected virtual void Start()
        {
            GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
            GameEvents.OnLightFlashed += HandleLightFlashed;
            GameEvents.OnMinigameOpened += HandleMinigameOpened;
            GameEvents.OnMinigameClosed += HandleMinigameClosed;
            GameEvents.OnViewChangeFinished += HandleViewChangeFinished;

            GameEvents.OnDifficultyIncreased += HandleDifficultyIncreased;
            OnDifficultyIncrease(0);

            if (visualRoot != null) visualRoot.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameOpened -= HandleMinigameOpened;
            GameEvents.OnMinigameClosed -= HandleMinigameClosed;
            GameEvents.OnViewChangeFinished -= HandleViewChangeFinished;

            GameEvents.OnDifficultyIncreased -= HandleDifficultyIncreased;
        }

        // ================= XỬ LÝ VÒNG ĐỜI CHUNG =================
        protected void HandlePasscodeGenerated(Dictionary<MinigameType, int> dict)
        {
            if (dict.TryGetValue(minigameType, out int digit))
            {
                secretDigit = digit;
                Debug.Log($"[{minigameType}] Mật mã được giao là: {secretDigit}");
            }
        }

        protected void HandleViewChangeFinished(View currentView) => isFocused = (currentView == View.Desk);

        private void HandleLightFlashed()
        {
            if (!isPlaying || isCompleting) return;
            OnGameReset();
            GameEvents.RaiseMinigameProgressReset(minigameType);
        }

        protected void HandleMinigameOpened(MinigameType type)
        {
            if (type != minigameType || secretDigit == -1) return;

            isCompleting = false;
            visualRoot.SetActive(true);
            isPlaying = true;

            OnGameStart();
        }

        protected void HandleMinigameClosed(MinigameType type)
        {
            if (type != minigameType || !isPlaying) return;

            isPlaying = false;
            isCompleting = false;

            // Hủy các hiệu ứng DOTween đang chạy dở trên visualRoot (nếu có)
            if (visualRoot != null) DOTween.Kill(visualRoot.transform);

            visualRoot.SetActive(false);

            OnGameClosed();
            GameEvents.RaiseMinigameProgressReset(minigameType);
        }

        // ================= API CHO CLASS CON GỌI KHI THẮNG =================

        protected void CompleteMinigame()
        {
            if (!isPlaying || isCompleting) return;
            StartCoroutine(CompleteRoutine());
        }

        // Coroutine chạy hiệu ứng Win
        private IEnumerator CompleteRoutine()
        {
            isCompleting = true;
            Debug.Log($"[{minigameType}] Giải xong! Đang chạy hiệu ứng...");

            // 1. Hiệu ứng giật DOTween (Rung lắc visualRoot)
            if (visualRoot != null)
            {
                // Rung trong 0.5s, lực rung 0.2, vibrato 20
                visualRoot.transform.DOShakePosition(0.5f, new Vector3(0.2f, 0.2f, 0f), 20, 90f, false, true);
            }

            // Đợi hiệu ứng giật xong (cộng thêm 0.1s cho chắc)
            yield return new WaitForSeconds(0.6f);

            // 2. Sinh ra mảnh giấy chứa con số
            SpawnDigitPaper();

            // 3. Bắn event đóng game và nộp mã số
            Debug.Log($"[{minigameType}] Đã nhả giấy. Nộp mã {secretDigit}");
            GameEvents.RaiseMinigameCompleted(minigameType, secretDigit);
            GameEvents.RaiseMinigameClosed(minigameType);
        }

        private void SpawnDigitPaper()
        {
            if (digitPaperPrefab == null || deskSpawnArea == null)
            {
                Debug.LogWarning($"[{minigameType}] Chưa setup Prefab Giấy hoặc Vùng bàn để nhả giấy!");
                return;
            }

            // Sinh giấy ở tâm vùng bàn
            GameObject paper = Instantiate(digitPaperPrefab, deskSpawnArea.transform);

            // Xóc tọa độ Z lên trên một chút để không bị kẹt dưới mặt bàn
            paper.transform.localPosition = new Vector3(0f, 0f, -0.1f);

            // Lắc góc xoay ngẫu nhiên cho tờ giấy rơi tự nhiên
            paper.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

            // Tìm script trên mảnh giấy và truyền con số mật mã vào
            if (paper.TryGetComponent(out DigitPaper paperScript))
            {
                // unique layer để giấy nọ đè giấy kia
                int uniqueLayer = 50 + Random.Range(1, 10);
                Color targetColor = minigameColor;
                paperScript.Initialize(secretDigit.ToString(), uniqueLayer, deskSpawnArea, targetColor);
            }
        }

        protected void HandleDifficultyIncreased(int minigamePassed)
        {
            OnDifficultyIncrease(minigamePassed);
        }

        // ================= CÁC HÀM TRỪU TƯỢNG =================
        protected abstract void OnGameStart();
        protected abstract void OnGameReset();
        protected abstract void OnDifficultyIncrease(int minigamePassed);
        protected virtual void OnGameClosed() { }
        protected virtual void OnFailed()
        {
            GameEvents.RaiseMinigameFailed(_increaseEntityStateChangeAccelaration);
        }
    }
}