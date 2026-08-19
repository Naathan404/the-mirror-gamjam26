using System.Collections.Generic;
using Game.Core;
using Game.Minigames.CardMatch;
using Game.Minigames.Maze;
using Game.Minigames.WordSearch;
using UnityEngine;

namespace Game.Minigames
{
    [CreateAssetMenu(fileName = "Minigame Difficulty Config", menuName = "Game/Minigames/Mng Difficulty Config")]
    public class MinigameDifficultyConfigSO : ScriptableObject
    {
        
        [Header("Maze")]
        [SerializeField] private MazeConfig[] _mazeConfigs = new MazeConfig[GameConstants.NUMBER_OF_MINIGAMES];

        [Header("Cards Match")]
        [SerializeField] private CardMatchConfig[] _cardsConfigs = new CardMatchConfig[GameConstants.NUMBER_OF_MINIGAMES];

        [Header("Wires")]
        [SerializeField] private WiresConfig[] _wiresConfigs = new WiresConfig[GameConstants.NUMBER_OF_MINIGAMES];

        [Header("Words Search")]
        [SerializeField] private WordSearchConfig[] _wordsConfigs = new WordSearchConfig[GameConstants.NUMBER_OF_MINIGAMES];

        public T GetMinigameConfig<T>(int minigamePassed) where T : class
        {
            object array = typeof(T) switch
            {
                var t when t == typeof(MazeConfig) => _mazeConfigs,
                var t when t == typeof(CardMatchConfig) => _cardsConfigs,
                var t when t == typeof(WiresConfig) => _wiresConfigs,
                var t when t == typeof(WordSearchConfig) => _wordsConfigs,
                _ => null
            };

            if (array is T[] typedArray && typedArray.Length > 0)
            {
                int index = UnityEngine.Mathf.Clamp(minigamePassed, 0, typedArray.Length - 1);
                return typedArray[index];
            }

            Debug.LogError($"[ConfigManager] Không tìm thấy config cho type: {typeof(T).Name}");
            return null;
        }
    }
    
}
