using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Systems.Lock;
using Mono.Cecil.Cil;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KeyCode = Game.Core.KeyCode;

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
        public KeyColor KColor;
        public KeyShape Shape;

        [Header("Base Tham chiếu")]
        public GameObject visualRoot;

        [Header("Fail Penalty Config")]
        [SerializeField] private float _increaseEntityStateChangeAccelaration = 1.2f;

        [Header("Phần thưởng (Mảnh giấy chứa Số)")]
        [Tooltip("Prefab của tờ giấy nhỏ chứa mật mã")]
        public GameObject digitPaperPrefab;
        public BoxCollider deskSpawnArea;

        protected int secretDigit = -1;
        protected bool isPlaying = false;
        protected bool isFocused = true;

        protected bool isCompleting = false;

        protected virtual void Start()
        {
            //GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
            GameEvents.OnLightFlashed += HandleLightFlashed;
            GameEvents.OnMinigameOpened += HandleMinigameOpened;
            GameEvents.OnMinigameClosed += HandleMinigameClosed;
            GameEvents.OnViewChangeFinished += HandleViewChangeFinished;

            GameEvents.OnDifficultyIncreased += HandleDifficultyIncreased;
            OnDifficultyIncrease(0);

            if (visualRoot != null) visualRoot.SetActive(false);
            Invoke(nameof(FetchSecretCode), 0.15f);
        }

        protected virtual void OnDestroy()
        {
            //GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
            GameEvents.OnLightFlashed -= HandleLightFlashed;
            GameEvents.OnMinigameOpened -= HandleMinigameOpened;
            GameEvents.OnMinigameClosed -= HandleMinigameClosed;
            GameEvents.OnViewChangeFinished -= HandleViewChangeFinished;

            GameEvents.OnDifficultyIncreased -= HandleDifficultyIncreased;
        }

        private void FetchSecretCode()
        {
            if (PasscodeController.Instance == null)
            {
                Debug.LogError($"[{minigameType}] LỖI NGHIÊM TRỌNG: Không tìm thấy PasscodeController!");
                return;
            }

            var dict = PasscodeController.Instance.GetCurrentPasscodeMap();

            if (dict != null && dict.TryGetValue(minigameType, out KeyCode code))
            {
                secretDigit = code.Digit;
                minigameColor = code.GetColor();
                Shape = code.Shape;
                KColor = code.KColor;

                Debug.Log($"[{minigameType}] Tự động kéo mã thành công: {secretDigit}");
            }
            else
            {
                Debug.Log($"[{minigameType}] Ván này không được bốc trúng. Tự động đi ngủ!");

                gameObject.SetActive(false);
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
            if (type != minigameType)
            {
                return;
            }

            if (visualRoot == null)
            {
                Debug.LogError($"[{minigameType}] Cannot open minigame because visual root is not assigned.");
                return;
            }

            if (secretDigit == -1)
            {
                Debug.LogWarning($"[{minigameType}] Opening minigame before passcode data is assigned. Check PasscodeController if this minigame should reward a digit.");
            }

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

            if (visualRoot != null) DOTween.Kill(visualRoot.transform);

            visualRoot.SetActive(false);

            OnGameClosed();
            GameEvents.RaiseMinigameProgressReset(minigameType);
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
        }


        protected void CompleteMinigame()
        {
            if (!isPlaying || isCompleting) return;
            
            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            isCompleting = true;
            Debug.Log($"[{minigameType}] Giải xong! Đang chạy hiệu ứng...");

            if (visualRoot != null)
            {
                visualRoot.transform.DOShakePosition(0.5f, new Vector3(0.2f, 0.2f, 0f), 20, 90f, false, true);
            }

            yield return new WaitForSeconds(0.6f);

            if (!HasAssignedPasscode())
            {
                Debug.LogWarning($"[{minigameType}] Completed without passcode data. Closing minigame without rewarding a digit.");
                GameEvents.RaiseMinigameClosed(minigameType);
                yield break;
            }

            SpawnDigitPaper();

            Debug.Log($"[{minigameType}] Đã nhả giấy. Nộp mã {secretDigit}");
            GameEvents.RaiseMinigameCompleted(minigameType, new KeyCode(secretDigit, Shape, KColor));
            GameEvents.RaiseMinigameClosed(minigameType);
        }

        private bool HasAssignedPasscode()
        {
            return secretDigit >= 0;
        }

        private void SpawnDigitPaper()
        {
            if (digitPaperPrefab == null || deskSpawnArea == null)
            {
                Debug.LogWarning($"[{minigameType}] Chưa setup Prefab Giấy hoặc Vùng bàn để nhả giấy!");
                return;
            }

            // Sinh giấy ở mặt bàn
            GameObject paper = Instantiate(digitPaperPrefab, deskSpawnArea.transform);

            Vector3 center = deskSpawnArea.center;
            Vector3 size = deskSpawnArea.size;

            float randomX = Random.Range(center.x - (size.x / 2f) * 0.2f, center.x + (size.x / 2f) * 0.2f);
            float randomY = Random.Range(center.y - (size.y / 2f) * 0.2f, center.y + (size.y / 2f) * 0.2f);

            paper.transform.localPosition = new Vector3(randomX, randomY, -0.1f);

            paper.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));


            if (paper.TryGetComponent(out DigitPaper paperScript))
            {
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
            GameEvents.RaiseMinigameClosed(minigameType);
            GameEvents.RaiseMinigameFailed(_increaseEntityStateChangeAccelaration);
            AudioController.Instance.PlaySFX(SoundName.Minigame_Fail);
            FilterController.Instance.FlashVignette(Color.white, 1f, 2f);
        }
    }
}