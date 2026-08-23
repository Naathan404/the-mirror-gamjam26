using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    public class SplashScreenController : MonoBehaviour
    {
        [SerializeField] private SplashSlide[] _slides;
        [SerializeField] private string _nextSceneName = "MainMenu";
        [SerializeField] private bool _allowSkipOnClick = true;

        private bool _skipRequested;

        private void Start()
        {
            foreach (var slide in _slides)
                slide.CanvasGroup.alpha = 0f;

            StartCoroutine(PlaySequence());
        }

        private void Update()
        {
            if (_allowSkipOnClick && Input.GetMouseButtonDown(0))
                _skipRequested = true;
        }

        private IEnumerator PlaySequence()
        {
            foreach (var slide in _slides)
            {
                if (_skipRequested) break;

                yield return slide.CanvasGroup.DOFade(1f, slide.FadeInDuration).WaitForCompletion();

                float elapsed = 0f;
                while (elapsed < slide.HoldDuration && !_skipRequested)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                yield return slide.CanvasGroup.DOFade(0f, slide.FadeOutDuration).WaitForCompletion();
            }

            SceneManager.LoadScene(_nextSceneName);
        }
    }
}