using UnityEngine;

namespace Game.Minigames
{
    [CreateAssetMenu(fileName = "MastermindConfig", menuName = "Game/Minigames/Mastermind Config")]
    public class MastermindConfig : ScriptableObject
    {
        [Header("Difficulty")]
        [Tooltip("Guess Slots")]
        [Range(2, 10)]
        public int CodeLength = 4;

        [Tooltip("Availabel Symbols")]
        [Range(2, 10)]
        public int SymbolCount = 5;

        [Tooltip("Temp Amount")]
        public int MaxAttempts = 10;

        public bool AllowDuplicateSymbols = false;

        [Header("Symbols")]
        public Sprite[] SymbolSprites;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (SymbolSprites != null && SymbolCount > SymbolSprites.Length)
            {
                Debug.LogWarning($"[{name}] SymbolCount ({SymbolCount}) vượt quá số sprite hiện có ({SymbolSprites.Length}).");
            }

            if (!AllowDuplicateSymbols && CodeLength > SymbolCount)
            {
                Debug.LogWarning($"[{name}] CodeLength ({CodeLength}) > SymbolCount ({SymbolCount}) mà không cho lặp ký hiệu — không thể tạo mã hợp lệ!");
            }
        }
#endif
    }
}