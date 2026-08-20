using Game.Utils;
using UnityEngine.SceneManagement;

namespace Game.Managers
{
    public class SceneController : MonoSingleton<SceneController>
    {
        public void ReloadGameplayScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            SceneManager.LoadScene(currentScene.buildIndex);
        }
    }
}