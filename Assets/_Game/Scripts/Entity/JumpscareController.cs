using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Managers;
using UnityEngine;

namespace Game.Entity
{
    public class JumpscareController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpriteRenderer _jumpscareSpriteRenderer;
        [SerializeField] private SpriteRenderer _jumpScareBackground;
        [SerializeField] private Transform _cameraShakeTarget;

        [Header("Timing")]
        [SerializeField] private float _startDelay = 0.5f;
        [SerializeField] private float _holdDuration = 1.5f;

        [Header("UI")]
        [SerializeField] private UIGameplayManager _uiGameplayManager;

        private bool _isPlaying;

        private void Start()
        {
            GameEvents.OnJumpscareTriggered += HandleJumpscare;
            SetJumpscareVisualActive(false);
        }

        private void OnDestroy()
        {
            GameEvents.OnJumpscareTriggered -= HandleJumpscare;
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(UnityEngine.KeyCode.J))
            {
                GameEvents.RaiseJumpscareTriggered();
            }
        }
#endif

        private void HandleJumpscare()
        {
            if (_isPlaying)
            {
                return;
            }

            StartCoroutine(PlayJumpscareRoutine());
        }

        private IEnumerator PlayJumpscareRoutine()
        {
            _isPlaying = true;
            if (_uiGameplayManager != null)
                _uiGameplayManager.HideAllPanels();
            yield return new WaitForSecondsRealtime(_startDelay);

            SetJumpscareVisualActive(true);
            PlayJumpscareEffects();

            yield return new WaitForSecondsRealtime(_holdDuration);

            PlayJumpscareEndEffects();
            SetJumpscareVisualActive(false);

            _isPlaying = false;
            GameEvents.RaiseGameLost();
        }

        private void SetJumpscareVisualActive(bool isActive)
        {
            if (_jumpscareSpriteRenderer != null)
            {
                _jumpscareSpriteRenderer.gameObject.SetActive(isActive);
            }

            if (_jumpScareBackground != null)
            {
                _jumpScareBackground.gameObject.SetActive(isActive);
            }
        }

        private void PlayJumpscareEffects()
        {
            PlayJumpscareSfx();
            PlayJumpscareLightFlicker();
            PlayJumpscareCameraShake();
            PlayJumpscareScreenFlash();
        }

        private void PlayJumpscareEndEffects()
        {
            PlayJumpscareVhsEffect();
            PlayJumpscareEndScreenFlash();
        }

        private void PlayJumpscareSfx()
        {
            try
            {
                if (AudioController.Instance != null)
                {
                    AudioController.Instance.PlaySFX(SoundName.Entity_Jumpscare);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PlayJumpscareLightFlicker()
        {
            try
            {
                if (FlickerController.Instance != null)
                {
                    FlickerController.Instance.FlickerFor(FlickerPattern.Strobe, 1f);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PlayJumpscareCameraShake()
        {
            try
            {
                if (_cameraShakeTarget != null)
                {
                    _cameraShakeTarget.DOShakePosition(0.5f, 1f, 25, 90f).SetUpdate(true);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PlayJumpscareScreenFlash()
        {
            try
            {
                if (FilterController.Instance != null)
                {
                    FilterController.Instance.FlashScreen(FilterController.Instance.HazardColor, 0.5f);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PlayJumpscareVhsEffect()
        {
            try
            {
                if (VhsController.Instance != null)
                {
                    VhsController.Instance.PlayVhsEffect();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void PlayJumpscareEndScreenFlash()
        {
            try
            {
                if (FilterController.Instance != null)
                {
                    FilterController.Instance.FlashScreen(Color.white, 0.5f);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
