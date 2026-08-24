using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class UIEndingController : MonoBehaviour
    {
        private const string TEXT_GLITCH_TWEEN_ID = "TextGlitch";
        private const string JUMPSCARE_FLICKER_TWEEN_ID = "JumpscareFlicker";

        [Header("Ending Elements")]
        [SerializeField] private GameObject _endingPanel;
        [SerializeField] private Image _endingPanelImage;
        [SerializeField] private CanvasGroup _endingCanvasGroup;
        [SerializeField] private TextMeshProUGUI _endingText;
        [SerializeField] private Image _jumpscareFlashImage;
        [SerializeField] private Transform _cameraShakeTarget;
        [SerializeField] private Camera _endingCamera;

        [Header("Buttons")]
        [SerializeField] private RectTransform _wakeUpAgainButton;
        [SerializeField] private CanvasGroup _wakeUpAgainButtonCanvasGroup;
        [SerializeField] private TextMeshProUGUI _wakeUpAgainButtonText;
        [SerializeField] private RectTransform _returnToMenuButton;
        [SerializeField] private CanvasGroup _returnToMenuButtonCanvasGroup;

        [Header("Configs")]
        [SerializeField] private Game.Configs.EndingConfig _endingConfig;

        private Coroutine _endingRoutine;
        private bool _isActionTriggered;
        private bool _isEndingPlaying;

        private void Start()
        {
            GameEvents.OnGameWon += PlayLoopEnding;
            InitializeEndingUI();
        }

        private void OnDestroy()
        {
            GameEvents.OnGameWon -= PlayLoopEnding;
            KillEndingTweens();
        }

        private void PlayLoopEnding()
        {
            if (_isEndingPlaying)
            {
                return;
            }

            if (_endingRoutine != null)
            {
                StopCoroutine(_endingRoutine);
            }

            _endingRoutine = StartCoroutine(PlayLoopEndingRoutine());
        }

        private IEnumerator PlayLoopEndingRoutine()
        {
            _isEndingPlaying = true;
            _isActionTriggered = false;

            PrepareEndingPanel();

            yield return PlayEndingIntroRoutine();
            yield return PlayEndingLinesRoutine();

            HideEndingText();
            yield return new WaitForSecondsRealtime(0.5f);

            ShowEndingButtons();
            _endingRoutine = null;
        }

        private void InitializeEndingUI()
        {
            if (_endingPanel != null)
            {
                _endingPanel.SetActive(false);
            }

            if (_endingPanelImage != null)
            {
                _endingPanelImage.color = Color.black;
            }

            if (_jumpscareFlashImage != null)
            {
                _jumpscareFlashImage.gameObject.SetActive(false);
            }

            SetButtonActive(_wakeUpAgainButton, _wakeUpAgainButtonCanvasGroup, false);
            SetButtonActive(_returnToMenuButton, _returnToMenuButtonCanvasGroup, false);
            HideEndingText();
        }

        private void PrepareEndingPanel()
        {
            HideGameplayUI();

            if (_endingPanel != null)
            {
                _endingPanel.SetActive(true);
            }

            if (_endingPanelImage != null)
            {
                _endingPanelImage.color = Color.black;
            }

            if (_endingCanvasGroup != null)
            {
                _endingCanvasGroup.DOKill();
                _endingCanvasGroup.alpha = 0f;
            }

            SetButtonActive(_wakeUpAgainButton, _wakeUpAgainButtonCanvasGroup, false);
            SetButtonActive(_returnToMenuButton, _returnToMenuButtonCanvasGroup, false);
            HideEndingText();
        }

        private IEnumerator PlayEndingIntroRoutine()
        {
            float originalFov = 0f;
            bool hasCamera = _endingCamera != null;

            if (hasCamera)
            {
                originalFov = _endingCamera.fieldOfView;
                _endingCamera.DOFieldOfView(10f, 1.5f).SetEase(Ease.InExpo).SetUpdate(true);
            }

            yield return new WaitForSecondsRealtime(1.5f);

            FadeEndingCanvas(1f, 0.2f);
            yield return new WaitForSecondsRealtime(0.2f);

            if (hasCamera)
            {
                _endingCamera.fieldOfView = originalFov;
            }
        }

        private IEnumerator PlayEndingLinesRoutine()
        {
            if (_endingConfig == null || _endingConfig.endingLines == null)
            {
                yield break;
            }

            for (int i = 0; i < _endingConfig.endingLines.Count; i++)
            {
                Game.Configs.EndingLine line = _endingConfig.endingLines[i];
                if (line == null)
                {
                    continue;
                }

                yield return new WaitForSecondsRealtime(line.delayBeforeShow);

                ShowEndingLine(line);
                yield return new WaitForSecondsRealtime(line.showDuration);

                if (line.triggerJumpscareAfter)
                {
                    yield return PlayEndingJumpscareRoutine();
                }
                else
                {
                    FadeEndingText(0f, 0.5f);
                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
        }

        private IEnumerator PlayEndingJumpscareRoutine()
        {
            const float JUMPSCARE_DURATION = 2f;

            HideEndingText();
            PlayEndingJumpscareStartEffects(JUMPSCARE_DURATION);

            yield return new WaitForSecondsRealtime(JUMPSCARE_DURATION);

            StopEndingJumpscareEffects();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private void HideGameplayUI()
        {
            if (UIGameplayManager.Instance != null)
            {
                UIGameplayManager.Instance.HideAllPanels();
            }
        }

        private void ShowEndingLine(Game.Configs.EndingLine line)
        {
            if (_endingText == null)
            {
                return;
            }

            DOTween.Kill(TEXT_GLITCH_TWEEN_ID);
            _endingText.DOKill();
            _endingText.text = GetLocalizedText(line.localizedText);
            _endingText.color = Color.white;
            _endingText.alpha = 0f;
            _endingText.DOFade(1f, 1f).SetUpdate(true);
            _endingText.rectTransform.DOShakeAnchorPos(line.showDuration, new Vector2(3f, 3f), 20, 90f, false, true)
                .SetId(TEXT_GLITCH_TWEEN_ID)
                .SetUpdate(true);
            _endingText.DOFade(0.6f, 0.15f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(TEXT_GLITCH_TWEEN_ID)
                .SetUpdate(true);

            PlayLineSfx(line.soundName);
        }

        private void PlayLineSfx(SoundName soundName)
        {
            if (soundName != SoundName.None)
            {
                PlaySfx(soundName);
            }
        }

        private void PlayEndingJumpscareStartEffects(float jumpscareDuration)
        {
            PlaySfx(SoundName.Entity_ChangeState);
            PlaySfx(SoundName.Entity_Jumpscare);
            ShowJumpscareImage(jumpscareDuration);
            PlayJumpscareFlicker();
            PlayCameraShake(jumpscareDuration);
        }

        private void ShowJumpscareImage(float jumpscareDuration)
        {
            if (_jumpscareFlashImage == null)
            {
                return;
            }

            _jumpscareFlashImage.gameObject.SetActive(true);
            _jumpscareFlashImage.color = Color.white;

            RectTransform monsterRect = _jumpscareFlashImage.rectTransform;
            monsterRect.DOKill();
            monsterRect.localScale = Vector3.one;
            monsterRect.DOScale(Vector3.one * 1.5f, jumpscareDuration).SetEase(Ease.OutExpo).SetUpdate(true);
            monsterRect.DOShakeAnchorPos(jumpscareDuration, new Vector2(50f, 50f), 60, 90f, false, true).SetUpdate(true);
            monsterRect.DOShakeRotation(jumpscareDuration, new Vector3(0f, 0f, 15f), 60, 90f).SetUpdate(true);
        }

        private void PlayJumpscareFlicker()
        {
            if (_jumpscareFlashImage == null)
            {
                return;
            }

            Color targetColor = Color.red;
            if (FilterController.Instance != null)
            {
                targetColor = FilterController.Instance.HazardColor;
            }

            _jumpscareFlashImage.DOColor(targetColor, 0.05f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetId(JUMPSCARE_FLICKER_TWEEN_ID)
                .SetUpdate(true);
        }

        private void PlayCameraShake(float jumpscareDuration)
        {
            if (_cameraShakeTarget != null)
            {
                _cameraShakeTarget.DOShakePosition(jumpscareDuration, 2.5f, 60, 90f).SetUpdate(true);
            }
        }

        private void StopEndingJumpscareEffects()
        {
            DOTween.Kill(JUMPSCARE_FLICKER_TWEEN_ID);

            if (_jumpscareFlashImage != null)
            {
                _jumpscareFlashImage.rectTransform.DOKill();
                _jumpscareFlashImage.color = Color.white;
                _jumpscareFlashImage.gameObject.SetActive(false);
            }

            if (_cameraShakeTarget != null)
            {
                _cameraShakeTarget.DOKill();
            }

            if (_endingPanelImage != null)
            {
                _endingPanelImage.color = Color.black;
            }
        }

        private void ShowEndingButtons()
        {
            if (_wakeUpAgainButtonText != null && _endingConfig != null)
            {
                _wakeUpAgainButtonText.text = GetLocalizedText(_endingConfig.localizedLoopButtonText);
            }

            ShowButton(_wakeUpAgainButton, _wakeUpAgainButtonCanvasGroup);
            ShowButton(_returnToMenuButton, _returnToMenuButtonCanvasGroup);
        }

        private void ShowButton(RectTransform button, CanvasGroup canvasGroup)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(true);

            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 1f).SetUpdate(true);
        }

        private void SetButtonActive(RectTransform button, CanvasGroup canvasGroup, bool isActive)
        {
            if (button != null)
            {
                button.gameObject.SetActive(isActive);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = isActive ? 1f : 0f;
            }
        }

        private void FadeEndingCanvas(float targetAlpha, float duration)
        {
            if (_endingCanvasGroup != null)
            {
                _endingCanvasGroup.DOKill();
                _endingCanvasGroup.DOFade(targetAlpha, duration).SetUpdate(true);
            }
        }

        private void FadeEndingText(float targetAlpha, float duration)
        {
            if (_endingText != null)
            {
                _endingText.DOKill();
                _endingText.DOFade(targetAlpha, duration).SetUpdate(true);
            }
        }

        private void HideEndingText()
        {
            DOTween.Kill(TEXT_GLITCH_TWEEN_ID);

            if (_endingText == null)
            {
                return;
            }

            _endingText.DOKill();
            _endingText.alpha = 0f;
            _endingText.text = string.Empty;
        }

        private void PlaySfx(SoundName soundName)
        {
            try
            {
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlaySFX(soundName);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private string GetLocalizedText(UnityEngine.Localization.LocalizedString localizedString)
        {
            try
            {
                return localizedString.GetLocalizedString();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return string.Empty;
            }
        }

        private void KillEndingTweens()
        {
            DOTween.Kill(TEXT_GLITCH_TWEEN_ID);
            DOTween.Kill(JUMPSCARE_FLICKER_TWEEN_ID);

            if (_endingCanvasGroup != null)
            {
                _endingCanvasGroup.DOKill();
            }

            if (_endingText != null)
            {
                _endingText.DOKill();
                _endingText.rectTransform.DOKill();
            }

            if (_jumpscareFlashImage != null)
            {
                _jumpscareFlashImage.rectTransform.DOKill();
                _jumpscareFlashImage.DOKill();
            }

            if (_cameraShakeTarget != null)
            {
                _cameraShakeTarget.DOKill();
            }
        }

        public void OnClickReplay()
        {
            if (_isActionTriggered)
            {
                return;
            }

            _isActionTriggered = true;
            PlaySfx(SoundName.ButtonClick);

            if (SceneController.Instance != null)
            {
                SceneController.Instance.ReloadGameplayScene();
            }
        }

        public void OnClickReturnToMenu()
        {
            if (_isActionTriggered)
            {
                return;
            }

            _isActionTriggered = true;
            PlaySfx(SoundName.ButtonClick);

            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadMenuScene();
            }
        }
    }
}