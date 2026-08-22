using System;
using UnityEngine;
using DG.Tweening;

namespace Game.Systems.Lock
{
    [RequireComponent(typeof(Collider))]
    public class KeyboxButton : MonoBehaviour
    {
        public Transform buttonMesh;
        public Vector3 pushAxis = Vector3.back;
        public float pressDepth = 0.03f;
        public float pressDuration = 0.1f;

        [SerializeField] private bool _isCheckButton = false;

        [Header("Materials")]
        [SerializeField] private MeshRenderer buttonRenderer;
        [SerializeField] private Material _redMAT;
        [SerializeField] private Material _yellowMAT;

        public event Action OnClicked;

        private Vector3 originalLocalPos;
        private Transform targetTransform;
        private Sequence pressTween;

        private void Start()
        {
            targetTransform = buttonMesh != null ? buttonMesh : transform;
            originalLocalPos = targetTransform.localPosition;

            if (buttonRenderer == null)
            {
                buttonRenderer = targetTransform.GetComponent<MeshRenderer>();
            }
        }

        private void OnMouseDown()
        {
            AnimatePress();
            if (_isCheckButton)
                AudioController.Instance.PlaySFX(SoundName.Button3DClick);
            else
                AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            OnClicked?.Invoke();
        }

        private void AnimatePress()
        {
            pressTween?.Kill();

            Vector3 pressedPos = originalLocalPos + pushAxis.normalized * pressDepth;


            if (buttonRenderer != null && _yellowMAT != null)
            {
                buttonRenderer.material = _yellowMAT;
            }

            pressTween = DOTween.Sequence();

            pressTween.Append(targetTransform.DOLocalMove(pressedPos, pressDuration).SetEase(Ease.OutQuad))
                      .Append(targetTransform.DOLocalMove(originalLocalPos, pressDuration).SetEase(Ease.InQuad))
                      .OnComplete(() =>
                      {
                          if (buttonRenderer != null && _redMAT != null)
                          {
                              buttonRenderer.material = _redMAT;
                          }
                      });
        }

        private void OnDestroy()
        {
            pressTween?.Kill();
        }
    }
}