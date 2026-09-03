using Game.Utils;
using UnityEngine.SceneManagement;
using UnityEngine;
using DG.Tweening;
using Game.Effect;
using System;

namespace Game.Managers
{
    public class SceneController : MonoSingleton<SceneController>
    {
        [SerializeField] private string _gameplaySceneName = "_CoreScene";
        [SerializeField] private string _menuSceneName = "MenuScene";

        [Tooltip("Thời gian nhắm/mở mắt mặc định nếu không truyền tham số")]
        [SerializeField] private float _defaultTransitionTime = 1.5f;

        private bool _isLoading = false;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _isLoading = false;
            Invoke(nameof(AutoOpenEye), 0.1f);
        }

        private void AutoOpenEye()
        {
            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeOpenedVignetteEffect(_defaultTransitionTime);
            }
        }

        public void ReloadGameplayScene(float transitionTime = -1f)
        {
            TransitionAndLoad(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex), transitionTime);
        }

        public void LoadGameplayScene(float transitionTime = -1f)
        {
            TransitionAndLoad(() => SceneManager.LoadScene(_gameplaySceneName), transitionTime);
        }

        public void LoadMenuScene(float transitionTime = -1f)
        {
            TransitionAndLoad(() => SceneManager.LoadScene(_menuSceneName), transitionTime);
        }

        private void TransitionAndLoad(Action loadAction, float duration)
        {
            if (_isLoading) return;
            _isLoading = true;

            float t = duration > 0 ? duration : _defaultTransitionTime;

            if (FilterController.Instance != null)
            {
                FilterController.Instance.PlayEyeClosedVignetteEffect(Color.black, t);

                DOVirtual.DelayedCall(t + 0.1f, () => {
                    loadAction?.Invoke();
                });
            }
            else
            {
                loadAction?.Invoke();
            }
        }
    }
}