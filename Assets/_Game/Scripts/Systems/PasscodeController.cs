using UnityEngine;
using Game.Core;
using System.Collections.Generic;

namespace Game.Systems.Lock
{
    // 1. TẠO STRUCT ĐỂ UNITY HIỂN THỊ ĐƯỢC CẶP KEY-VALUE
    [System.Serializable]
    public struct MinigameDigitPair
    {
        public MinigameType minigame;
        public int digit;
    }

    public class PasscodeController : MonoBehaviour
    {
        public int requiredDigits = 4;

        // Dữ liệu gốc (Chạy logic)
        private Dictionary<MinigameType, int> minigameDigitMap;
        private Dictionary<MinigameType, int> collectedDigits;

        // 2. LIST ĐỂ PHẢN CHIẾU LÊN INSPECTOR (Chỉ dùng để xem)
        [Header("Góc nhìn trộm (Debug View)")]
        public List<MinigameDigitPair> debugMinigameDigitMap = new List<MinigameDigitPair>();
        public List<MinigameDigitPair> debugCollectedDigits = new List<MinigameDigitPair>();

        private void OnEnable()
        {
            GameEvents.OnMinigameCompleted += HandleMinigameCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;
        }
        private void Start()
        {
            GenerateNewPasscode();
        }

        public void GenerateNewPasscode()
        {
            minigameDigitMap = new Dictionary<MinigameType, int>();
            collectedDigits = new Dictionary<MinigameType, int>();

            minigameDigitMap.Add(MinigameType.Maze, Random.Range(0, 10));
            minigameDigitMap.Add(MinigameType.CardMatch, Random.Range(0, 10));
            minigameDigitMap.Add(MinigameType.Wires, Random.Range(0, 10));
            minigameDigitMap.Add(MinigameType.WordSearch, Random.Range(0, 10));

            // Cập nhật lên Inspector ngay sau khi tạo
            SyncDictionaryToLists();

            GameEvents.RaisePasscodeGenerated(minigameDigitMap); // Lưu ý: Cần đổi kiểu dữ liệu Event này sang Dictionary<MinigameType, int> nhé
        }

        private void HandleMinigameCompleted(MinigameType minigameType, int digit)
        {
            // Đặt trạm gác BÊN NGOÀI lệnh if để xem tín hiệu có tới cửa không
            Debug.Log($"[PasscodeController] ⚡ ĐÃ NHẬN TÍN HIỆU TỪ MÊ CUNG {minigameType} VỚI SỐ {digit}!");

            if (!collectedDigits.ContainsKey(minigameType))
            {
                collectedDigits.Add(minigameType, digit);
                Debug.Log($"[PasscodeController] 🔑 Đã lưu thành công số {digit} vào danh sách.");
                SyncDictionaryToLists();

                if (collectedDigits.Count >= requiredDigits)
                {
                    Unlock();
                }
            }
            else
            {
                // Nếu chui vào đây, nghĩa là game bị nộp đúp hoặc báo cáo 2 lần
                Debug.LogWarning($"[PasscodeController] ❌ TỪ CHỐI LƯU: Game {minigameType} đã nộp mã trước đó rồi!");
            }
        }

        private void Unlock()
        {
            Debug.Log("[PasscodeController] 🎉 ĐÃ ĐỦ 3 MÃ SỐ! ĐANG BẮN LỆNH MỞ KHÓA!");
            GameEvents.RaiseLockUnlocked();
        }

        // 3. HÀM ĐỒNG BỘ: Chép data từ Dictionary sang List để Inspector thấy được
        private void SyncDictionaryToLists()
        {
            debugMinigameDigitMap.Clear();
            foreach (var kvp in minigameDigitMap)
            {
                debugMinigameDigitMap.Add(new MinigameDigitPair { minigame = kvp.Key, digit = kvp.Value });
            }

            debugCollectedDigits.Clear();
            foreach (var kvp in collectedDigits)
            {
                debugCollectedDigits.Add(new MinigameDigitPair { minigame = kvp.Key, digit = kvp.Value });
            }
        }
    }
}