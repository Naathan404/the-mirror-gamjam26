using System;
using System.Collections.Generic;
using Game.Core;
using Game.Minigames.CardMatch;
using Game.Minigames.Laser;
using Game.Minigames.Maze;
using Game.Minigames.Waveform;
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

        [Header("Lazors")]
        [SerializeField] private LaserConfigSO[] _lazorsConfigs = new LaserConfigSO[GameConstants.NUMBER_OF_MINIGAMES];

        [Header("Waveforms")]
        [SerializeField] private WaveformConfigSO[] _waveFormConfigs = new WaveformConfigSO[GameConstants.NUMBER_OF_MINIGAMES];

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _enableDebug = false;
        [Range(0, 3)]
        [SerializeField] private int _debugDifficultLevel = 0;
#endif

        public T GetMinigameConfig<T>(int minigamePassed) where T : class
        {
            object array = typeof(T) switch
            {
                var t when t == typeof(MazeConfig) => _mazeConfigs,
                var t when t == typeof(CardMatchConfig) => _cardsConfigs,
                var t when t == typeof(WiresConfig) => _wiresConfigs,
                var t when t == typeof(WordSearchConfig) => _wordsConfigs,
                var t when t == typeof(LaserConfigSO) => _lazorsConfigs,
                var t when t == typeof(WaveformConfigSO) => _waveFormConfigs,
                _ => null
            };

#if UNITY_EDITOR
            if (_enableDebug)
            {
                if (array is T[] typedArray1 && typedArray1.Length > 0)
                {
                    return typedArray1[_debugDifficultLevel];
                }
            }
#endif

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
