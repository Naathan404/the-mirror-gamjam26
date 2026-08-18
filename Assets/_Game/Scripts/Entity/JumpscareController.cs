using System.Collections;
using DG.Tweening;
using Game.Core;
using Game.Effect;
using Game.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Entity
{
    public class JumpscareController : MonoBehaviour
    {
        [Header("References")]

        [SerializeField] private SpriteRenderer _jumpscareSpriteRenderer;

        [Header("Timing")]
        [SerializeField] private float _holdDuration = 1.5f;

        private void Start()
        {
            GameEvents.OnJumpscareTriggered += HandleJumpscare;

            _jumpscareSpriteRenderer.gameObject.SetActive(false);
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

            UnityEngine.Camera.main.transform.DOShakePosition(0.5f, 1f, 25, 90f);

            FilterController.Instance.FlashScreen(FilterController.Instance.HazardColor, 0.5f);
            
            yield return new WaitForSeconds(_holdDuration);
        }
    }    
}
