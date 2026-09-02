using UnityEngine;
using Game.Core;
using System.Collections.Generic;
using KeyCode = Game.Core.KeyCode;
using Game.Utils;
using System.Linq;

namespace Game.Systems.Lock
{
    [System.Serializable]
    public struct MinigameDigitPair
    {
        public MinigameType Minigame;
        public KeyCode KeyCode;
    }

    [System.Serializable]
    public struct MinigameToggle
    {
        public MinigameType Minigame;
        public bool IsEnabled;
    }

    public class PasscodeController : MonoSingleton<PasscodeController>
    {
        public int requiredDigits = 4;

        [Header("Cấu hình Game xuất hiện (Check để cho phép)")]
        public List<MinigameToggle> minigamePool = new List<MinigameToggle>()
        {
            new MinigameToggle { Minigame = MinigameType.Maze, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.CardMatch, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.Wires, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.WordSearch, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.Lazors, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.Waveform, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.Mastermind, IsEnabled = true },
            new MinigameToggle { Minigame = MinigameType.Switch, IsEnabled = true }
        };

        private Dictionary<MinigameType, KeyCode> minigameDigitMap = new();
        private Dictionary<MinigameType, KeyCode> collectedDigits = new();

        [Header("Góc nhìn trộm (Debug View)")]
        public List<MinigameDigitPair> debugMinigameDigitMap = new List<MinigameDigitPair>();
        public List<MinigameDigitPair> debugCollectedDigits = new List<MinigameDigitPair>();

        private List<KeyShape> keyShapes = new List<KeyShape>()
        {
            KeyShape.Square,
            KeyShape.Cross,
            KeyShape.Triangle,
            KeyShape.Circle
        };

        private List<KeyColor> keyColors = new List<KeyColor>()
        {
            KeyColor.Red,
            KeyColor.Blue,
            KeyColor.Yellow,
            KeyColor.Green
        };

        private List<MinigameType> minigameTypes = new List<MinigameType>();

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
            minigameTypes = minigamePool.Where(g => g.IsEnabled).Select(g => g.Minigame).ToList();

            if (minigameTypes.Count < GameConstants.NUMBER_OF_MINIGAMES)
            {
                Debug.LogError($"[PasscodeController] CẢNH BÁO: Bạn chỉ bật {minigameTypes.Count} game, nhưng GameConstants yêu cầu sinh {GameConstants.NUMBER_OF_MINIGAMES} game!");
            }

            keyShapes = ShuffleHelper.Shuffle(keyShapes);
            keyColors = ShuffleHelper.Shuffle(keyColors);
            minigameTypes = ShuffleHelper.Shuffle(minigameTypes);

            GenerateNewPasscode();
        }

        public void GenerateNewPasscode()
        {
            minigameDigitMap.Clear();
            List<int> uniqueDigits = Enumerable.Range(0, 10).OrderBy(x => Random.value).ToList();

            int gamesToGenerate = Mathf.Min(GameConstants.NUMBER_OF_MINIGAMES, minigameTypes.Count);

            for (int i = 0; i < gamesToGenerate; i++)
            {
                int code = uniqueDigits[i];
                minigameDigitMap.Add(minigameTypes[i], new KeyCode(code, keyShapes[i], keyColors[i]));
            }

            SyncDictionaryToLists();
        }

        public Dictionary<MinigameType, KeyCode> GetCurrentPasscodeMap()
        {
            return minigameDigitMap;
        }

        private void HandleMinigameCompleted(MinigameType minigameType, KeyCode digit)
        {
            if (!collectedDigits.ContainsKey(minigameType))
            {
                collectedDigits.Add(minigameType, digit);
                Debug.Log($"[PasscodeController] 🔑 Đã lưu thành công số {digit} vào danh sách.");
                SyncDictionaryToLists();
            }
            else
            {
                Debug.LogWarning($"[PasscodeController] ❌ TỪ CHỐI LƯU: Game {minigameType} đã nộp mã trước đó rồi!");
            }
        }

        private void SyncDictionaryToLists()
        {
            debugMinigameDigitMap.Clear();
            foreach (var kvp in minigameDigitMap)
            {
                debugMinigameDigitMap.Add(new MinigameDigitPair { Minigame = kvp.Key, KeyCode = kvp.Value });
            }

            debugCollectedDigits.Clear();
            foreach (var kvp in collectedDigits)
            {
                debugCollectedDigits.Add(new MinigameDigitPair { Minigame = kvp.Key, KeyCode = kvp.Value });
            }
        }
    }
}