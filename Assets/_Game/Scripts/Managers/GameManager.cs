
using Game.Core;
using Game.Utils;

namespace Game.Managers
{
    public sealed class GameManager : MonoSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Playing;
        public bool HasRoomKey { get; private set; } = false;

        private int _minigamePassed = 0;

        public int MinigamePassed => _minigamePassed;

        private void Start()
        {
            CurrentState = GameState.Playing;
            HasRoomKey = false;
            _minigamePassed = 0;

            GameEvents.OnMinigameCompleted += HandleMinigameCompleted;
            GameEvents.OnGameLost += HandleGameLost;
            GameEvents.OnKeyCollected += HandleKeyCollected;
            GameEvents.OnDoorInteracted += TryWinGame;
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;
            GameEvents.OnGameLost -= HandleGameLost;
            GameEvents.OnKeyCollected -= HandleKeyCollected;
            GameEvents.OnDoorInteracted -= TryWinGame;
        }

        #region GameStates
        public void PauseGame()
        {
            CurrentState = GameState.Pause;
        }

        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
        }

        public void SetGameOver()
        {
            CurrentState = GameState.GameOver;
        }
        #endregion

        #region eVENTS
        private void HandleMinigameCompleted(MinigameType _, KeyCode ___)
        {
            _minigamePassed++;
            GameEvents.RaiseDifficultyIncreased(_minigamePassed);
        }
        private void HandleKeyCollected()
        {
            HasRoomKey = true;
        }

        private void HandleGameLost()
        {
            SetGameOver();
        }
        private void TryWinGame()
        {
            if (CurrentState != GameState.Playing) return;

            if (HasRoomKey)
            {
                CurrentState = GameState.GameWon; // Hoặc GameWon nếu Hưn có State này
                // ĐÂY LÀ LÚC PHÁT LỆNH CHO UI CHẠY HIỆU ỨNG
                GameEvents.RaiseGameWon();
            }
            else
            {
                // Thêm âm thanh "Cạch cạch" cửa bị khóa ở đây nếu muốn
            }
        }
        #endregion
    }

    public enum GameState
    {
        Playing,
        Pause,
        GameOver,
        GameWon
    }
}