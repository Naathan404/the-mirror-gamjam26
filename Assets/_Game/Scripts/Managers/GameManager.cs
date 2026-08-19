
using Game.Core;
using KingCat.Base;

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

        #endregion
    }

    public enum GameState
    {
        Playing,
        Pause,
        GameOver
    }
}