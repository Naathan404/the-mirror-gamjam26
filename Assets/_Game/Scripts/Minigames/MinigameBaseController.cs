using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Minigames
{
    [DefaultExecutionOrder(-5)]
    public abstract class MinigameBaseController : MonoBehaviour
    {
        [Header("Difficulty SO")]
        [SerializeField] protected MinigameDifficultyConfigSO _difficultyConfig;

        [Header("Base Cấu hình")]
        public MinigameType minigameType;

        [Header("Base Tham chiếu")]
        public GameObject visualRoot;

        // Dữ liệu dùng chung
        protected int secretDigit = -1;
        protected bool isPlaying = false;
        protected bool isFocused = true;

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
                Debug.Log($"{secretDigit}");
            }
        }

        protected void HandleViewChangeFinished(View currentView) => isFocused = (currentView == View.Desk);

        private void HandleLightFlashed()
        {
            if (!isPlaying) return;
            OnGameReset();
            GameEvents.RaiseMinigameProgressReset(minigameType);
        }

        protected void HandleMinigameOpened(MinigameType type)
        {
            if (type != minigameType || secretDigit == -1) return;

            visualRoot.SetActive(true);
            isPlaying = true;

            OnGameStart();
        }

        protected void HandleMinigameClosed(MinigameType type)
        {
            if (type != minigameType || !isPlaying) return;

            isPlaying = false;
            visualRoot.SetActive(false);

            OnGameClosed();
            GameEvents.RaiseMinigameProgressReset(minigameType);
        }

        // ================= API CHO CLASS CON GỌI KHI THẮNG =================

        protected void CompleteMinigame()
        {
            if (!isPlaying) return;

            Debug.Log($"[{minigameType}] Giải xong! Đang nộp mã {secretDigit}");
            GameEvents.RaiseMinigameCompleted(minigameType, secretDigit);
            GameEvents.RaiseMinigameClosed(minigameType);
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
    }
}