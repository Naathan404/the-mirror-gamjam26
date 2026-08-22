using Game.Utils;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Game.Managers
{
    public class SceneController : MonoSingleton<SceneController>
    {
        [SerializeField] private string _gameplaySceneName = "_CoreScene";
        [SerializeField] private string _menuSceneName = "MenuScene";

        public void ReloadGameplayScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoadGameplayScene()
        {
            SceneManager.LoadScene(_gameplaySceneName);
        }

        public void LoadMenuScene()
        {
            SceneManager.LoadScene(_menuSceneName);
        }
    }
}