using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.Configs
{
    [System.Serializable]
    public class EndingLine
    {
        [Header("Nội dung thoại")]
        [Tooltip("Chọn Table và Key cho câu thoại này")]
        public LocalizedString localizedText;

        [Header("Cấu hình hiệu ứng")]
        [Tooltip("Âm thanh phát ra cùng lúc hiện câu thoại (Ví dụ: Tiếng mở cửa, kính vỡ...)")]
        public SoundName soundName = SoundName.None;

        [Tooltip("Thời gian CHỜ trước khi câu thoại này hiện ra")]
        public float delayBeforeShow = 1.5f;

        [Tooltip("Thời gian câu thoại nằm trên màn hình")]
        public float showDuration = 3f;

        [Tooltip("Tích vào đây nếu muốn NỔ JUMPSCARE ngay sau câu thoại này")]
        public bool triggerJumpscareAfter = false;
    }

    [CreateAssetMenu(fileName = "EndingConfig", menuName = "Game/Configs/Ending Config")]
    public class EndingConfig : ScriptableObject
    {
        [Header("Kịch bản WIN (The Loop)")]
        public List<EndingLine> endingLines; // Danh sách vô hạn các câu thoại

        [Header("UI Text (Nút chơi lại)")]
        public LocalizedString localizedLoopButtonText; // Thay cho string cũ
    }
}