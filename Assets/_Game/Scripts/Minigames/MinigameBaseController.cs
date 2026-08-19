using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Minigames
{
    public abstract class MinigameBaseController : MonoBehaviour
    {
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

            if (visualRoot != null) visualRoot.SetActive(false);
        }

        protected virtual void OnDestroy()
        {
            GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameOpened -= HandleMinigameOpened;
            GameEvents.OnMinigameClosed -= HandleMinigameClosed;
            GameEvents.OnViewChangeFinished -= HandleViewChangeFinished;
        }

        // ================= XỬ LÝ VÒNG ĐỜI CHUNG =================
        private void HandlePasscodeGenerated(Dictionary<MinigameType, int> dict)
        {
            if (dict.TryGetValue(minigameType, out int digit))
            {
                secretDigit = digit;
                Debug.Log($"{secretDigit}");
            }
        }

        private void HandleViewChangeFinished(View currentView) => isFocused = (currentView == View.Desk);

        private void HandleLightFlashed()
        {
            if (!isPlaying) return;
            OnGameReset();
            GameEvents.RaiseMinigameProgressReset(minigameType);
        }

        private void HandleMinigameOpened(MinigameType type)
        {
            if (type != minigameType || secretDigit == -1) return;

            visualRoot.SetActive(true);
            isPlaying = true;

            OnGameStart();
        }

        private void HandleMinigameClosed(MinigameType type)
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

        // ================= CÁC HÀM TRỪU TƯỢNG =================
        protected abstract void OnGameStart();
        protected abstract void OnGameReset();
        protected virtual void OnGameClosed() { }
    }
}