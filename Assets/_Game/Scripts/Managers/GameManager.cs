
using Game.Core;
using Game.Utils;

namespace Game.Managers
{
    public sealed class GameManager : MonoSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Playing;
        
        private int _minigamePassed = 0;

        private void Start()
        {
            CurrentState = GameState.Playing;

            GameEvents.OnMinigameCompleted += HandleMinigameCompleted;
            GameEvents.OnGameLost += HandleGameLost;
        }

#pragma warning disable CS0114 // Member hides inherited member; missing override keyword
        private void OnDestroy()
#pragma warning restore CS0114 // Member hides inherited member; missing override keyword
        {
            GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;
            GameEvents.OnGameLost -= HandleGameLost;
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
        private void HandleMinigameCompleted(MinigameType _, int ___)
        {
            _minigamePassed++;
            GameEvents.RaiseDifficultyIncreased(_minigamePassed);
        }

        private void HandleGameLost()
        {
            SetGameOver();
        }
        #endregion
    }

    public enum GameState
    {
        Playing,
        Pause,
        GameOver
    }
}