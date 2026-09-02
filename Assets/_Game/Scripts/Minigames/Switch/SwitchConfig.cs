using UnityEngine;

namespace Game.Minigames
{
    [CreateAssetMenu(fileName = "SwitchConfig", menuName = "Game/Minigames/Switch Config")]
    public class SwitchConfig : ScriptableObject
    {
        [Header("Cấu hình Đồ thị (Graph)")]
        [Tooltip("Số lượng công tắc trên bàn")]
        public int nodeCount = 7;

        [Tooltip("Khoảng cách tối thiểu giữa 2 công tắc để không bị đè lên nhau")]
        public float minNodeDistance = 1.2f;

        [Tooltip("Số dây nối tối đa cho 1 công tắc (Giữ ở mức 2-4 để không bị rối)")]
        public int maxEdgesPerNode = 3;

        [Header("Cấu hình Giải đố")]
        [Tooltip("Số lần game tự bấm ngầm để xáo trộn (Càng cao càng cần nhiều bước để giải)")]
        public int shuffleSteps = 4;
    }
}