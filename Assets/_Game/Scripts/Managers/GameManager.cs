
using KingCat.Base;

namespace Game.Managers
{
    public sealed class GameManager : MonoSingleton<GameManager>
    {
        public GameState CurrentState { get; private set; } = GameState.Playing;

        private void Start()
        {
            CurrentState = GameState.Playing;
        }

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
    }

    public enum GameState
    {
        Playing,
        Pause,
        GameOver
    }
}