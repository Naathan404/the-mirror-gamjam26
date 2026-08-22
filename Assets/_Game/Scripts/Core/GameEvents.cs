using System;
using Game.Cameras;
using UnityEngine;

/// <summary>
/// EVENT BUS TRUNG TÂM — giao tiếp giữa 3 hệ thống chính của game:
/// CameraViewController, EntityController, MinigameController, LightController.
///
/// NGUYÊN TẮC LÀM VIỆC NHÓM:
/// - Đây là file DUY NHẤT tất cả cùng cần đọc/hiểu trước khi code.
/// - Khi cần thêm 1 sự kiện mới, THẢO LUẬN NHÓM trước rồi mới thêm vào đây,
///   tránh mỗi người tự thêm field riêng gây trùng lặp / conflict.
/// - Các hệ thống KHÔNG được giữ tham chiếu trực tiếp tới nhau
///   (VD: MinigameController không được gọi thẳng EntityController.SetState()).
///   Mọi giao tiếp giữa hệ thống PHẢI đi qua GameEvents.
/// - File này gần như không đổi sau ngày 1 → hạn chế tối đa merge conflict.
/// </summary>
namespace Game.Core
{
    
    public static class GameEvents
    {
        // ================= CAMERA / VIEW =================

        /// <summary>Bắn khi người chơi BẮT ĐẦU chuyển view (dùng cho SFX xoay, khóa input UI...).</summary>
        public static event Action<View> OnViewChangeStarted;
        public static void RaiseViewChangeStarted(View view) => OnViewChangeStarted?.Invoke(view);

        /// <summary>Bắn khi camera đã xoay xong, chính thức đứng ở view mới.
        /// EntityController lắng nghe cái này để biết tốc độ tiến (chậm ở Mirror, nhanh ở Desk/Behind).</summary>
        public static event Action<View> OnViewChangeFinished;
        public static void RaiseViewChangeFinished(View view) => OnViewChangeFinished?.Invoke(view);


        // ================= ENTITY =================

        /// <summary>Bắn mỗi khi thực thể đổi state (5 -> 0). UI/Audio lắng nghe để đổi hình ảnh trong gương.</summary>
        public static event Action<int> OnEntityStateChanged;
        public static void RaiseEntityStateChanged(int newState) => OnEntityStateChanged?.Invoke(newState);

        /// <summary>Bắn khi thực thể vào State 1 .</summary>
        public static event Action OnEntityDangerZoneEntered;
        public static void RaiseEntityDangerZoneEntered() => OnEntityDangerZoneEntered?.Invoke();

        /// <summary>Bắn khi thực thể đạt State 0 → THUA / jumpscare.</summary>
        public static event Action OnJumpscareTriggered;
        public static void RaiseJumpscareTriggered() => OnJumpscareTriggered?.Invoke();


        // ================= LIGHT / BATTERY =================

        /// <summary>Bắn khi người chơi giật đèn THÀNH CÔNG (đủ pin).
        /// EntityController lắng nghe để lùi 2 state + tăng tốc độ +15%.
        /// MinigameController lắng nghe để reset minigame đang dở.</summary>
        public static event Action OnLightFlashed;
        public static void RaiseLightFlashed() => OnLightFlashed?.Invoke();

        /// <summary>Bắn khi người chơi bấm nút sạc pin (bắt đầu timer sạc ngầm).</summary>
        public static event Action OnBatteryChargeStarted;
        public static void RaiseBatteryChargeStarted() => OnBatteryChargeStarted?.Invoke();

        /// <summary>Bắn khi pin sạc xong, đèn sẵn sàng dùng lại. UI lắng nghe để đổi icon pin.</summary>
        public static event Action OnBatteryChargeCompleted;
        public static void RaiseBatteryChargeCompleted() => OnBatteryChargeCompleted?.Invoke();

        // ================= MINIGAME / MÃ KHÓA =================
        /// <summary>
        /// Bắn 1 LẦN DUY NHẤT lúc bắt đầu run, do LockController tự random rồi phát ra.
        /// Truyền 1 Dictionary ánh xạ minigameId -> chữ số (0-9) mà minigame đó sẽ trả về
        /// khi hoàn thành. VD: {"maze": 7, "cardmatch": 2, "wire": 9}.
        /// Mỗi MinigameController lắng nghe event này, tự tra cứu digit của riêng mình
        /// theo đúng minigameId, rồi dùng số đó khi bắn OnMinigameCompleted lúc chơi xong.
        /// LockController là nguồn chân lý (source of truth) duy nhất cho đáp án —
        /// minigame KHÔNG tự random số của mình.
        /// </summary>
        public static event Action<System.Collections.Generic.Dictionary<MinigameType, KeyCode>> OnPasscodeGenerated;
        public static void RaisePasscodeGenerated(System.Collections.Generic.Dictionary<MinigameType, KeyCode> minigameDigitMap)
            => OnPasscodeGenerated?.Invoke(minigameDigitMap);

        /// <summary>Bắn khi một Object trong môi trường (vd: quyển sách) yêu cầu mở minigame.</summary>
        public static event Action<MinigameType> OnMinigameOpened;
        public static void RaiseMinigameOpened(MinigameType type) => OnMinigameOpened?.Invoke(type);

        /// <summary>Bắn khi người chơi bấm nút thoát hoặc minigame tự đóng.</summary>
        public static event Action<MinigameType> OnMinigameClosed;
        public static void RaiseMinigameClosed(MinigameType type) => OnMinigameClosed?.Invoke(type);

        /// <summary>Bắn khi 1 minigame hoàn thành, trả về (id minigame, chữ số nhận được).
        /// LockController lắng nghe để cộng số vào hộp mã.</summary>
        public static event Action<MinigameType, KeyCode> OnMinigameCompleted;
        public static void RaiseMinigameCompleted(MinigameType minigameType, KeyCode code) => OnMinigameCompleted?.Invoke(minigameType, code);

        /// <summary>Bắn khi tiến trình 1 minigame bị reset (do giật đèn hoặc chuyển minigame khác).</summary>
        public static event Action<MinigameType> OnMinigameProgressReset;
        public static void RaiseMinigameProgressReset(MinigameType minigameType) => OnMinigameProgressReset?.Invoke(minigameType);

        /// <summary> Bắn khi tăng độ khó của minigame lên </sumary>
        public static event Action<int> OnDifficultyIncreased;
        public static void RaiseDifficultyIncreased(int minigamePassed) => OnDifficultyIncreased?.Invoke(minigamePassed);

        /// <summary>
        /// Bắn sự kiện này khi một minigame bị fail. Dùng để tắng tốc độ chuyển state của entity coi như hình phatjj
        /// </summary>
        public static event Action<float> OnMinigameFailed;
        public static void RaiseMinigameFailed(float increased) => OnMinigameFailed?.Invoke(increased);

        /// <summary>Bắn khi đủ 3 số, hộp khóa tự mở, chìa khóa xuất hiện.</summary>
        public static event Action OnLockUnlocked;
        public static void RaiseLockUnlocked() => OnLockUnlocked?.Invoke();

        /// <summary>Nhặt chìa khóa sau khi hộp mở.</summary>
        public static event Action OnKeyCollected;
        public static void RaiseKeyCollected() => OnKeyCollected?.Invoke();

        // ================= WIN / LOSE =================

        public static event Action OnGameWon;
        public static void RaiseGameWon() => OnGameWon?.Invoke();

        public static event Action OnGameLost;
        public static void RaiseGameLost() => OnGameLost?.Invoke();


        public static void ClearAllListeners()
        {
            OnViewChangeStarted = null;
            OnViewChangeFinished = null;
            OnPasscodeGenerated = null;
            OnEntityStateChanged = null;
            OnEntityDangerZoneEntered = null;
            OnJumpscareTriggered = null;
            OnLightFlashed = null;
            OnBatteryChargeStarted = null;
            OnBatteryChargeCompleted = null;
            OnMinigameCompleted = null;
            OnMinigameProgressReset = null;
            OnLockUnlocked = null;
            OnGameWon = null;
            OnGameLost = null;
        }
    }
}