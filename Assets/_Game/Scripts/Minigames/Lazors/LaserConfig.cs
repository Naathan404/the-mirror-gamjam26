using UnityEngine;

namespace Game.Minigames.Laser
{
    [CreateAssetMenu(fileName = "LaserConfig", menuName = "Game/Minigames/Laser Grid Config")]
    public class LaserConfigSO : ScriptableObject
    {

        [Min(3)] public int gridSize;
        [Min(1)] public int bulbCount;
        [Min(0)] public int requiredMirrorCount;
        [Min(0)] public int decoyMirrorCount;
        [Min(0)] public int stoneCount;

        public int minStraightBetweenTurn = 2;

        [Tooltip("Số lần bắn trúng đá tối đa trước khi bị reset toàn bộ minigame")]
        public int maxMistakes = 2;

        public float fallbackCellSize = 0.6f;

        public float laserTravelTimePerCell = 0.05f;
    }
}