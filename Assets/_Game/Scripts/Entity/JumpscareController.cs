using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using UnityEngine;

namespace Game.Entity
{
    public class JumpscareController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField] private SpriteRenderer _jumpscareSpriteRenderer;
        [SerializeField] private SpriteRenderer _jumpScareBackground;

        [Header("Timing")]
        [SerializeField] private float _holdDuration = 1.5f;

        private void Start()
        {
            GameEvents.OnJumpscareTriggered += HandleJumpscare;

            _jumpscareSpriteRenderer.gameObject.SetActive(false);
            _jumpScareBackground.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            GameEvents.OnJumpscareTriggered -= HandleJumpscare;
        }


#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J))
                GameEvents.RaiseJumpscareTriggered();
        }
#endif


        private void HandleJumpscare()
        {
            StartCoroutine(PlayJumpscare());
        }

        private IEnumerator PlayJumpscare()
        {
            _jumpscareSpriteRenderer.gameObject.SetActive(true);
            _jumpScareBackground.gameObject.SetActive(true);

            FlickerController.Instance.FlickerFor(FlickerPattern.Strobe, 1f);

            UnityEngine.Camera.main.transform.DOShakePosition(0.5f, 1f, 25, 90f);

            FilterController.Instance.FlashScreen(FilterController.Instance.HazardColor, 0.5f);
            
            yield return new WaitForSeconds(_holdDuration);
            
            VhsController.Instance.PlayVhsEffect();
            FilterController.Instance.FlashScreen(Color.white, 0.5f);
            _jumpscareSpriteRenderer.gameObject.SetActive(false);

            GameEvents.RaiseGameLost();
        }
    }    
}
