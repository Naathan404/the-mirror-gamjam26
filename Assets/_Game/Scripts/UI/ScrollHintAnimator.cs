using UnityEngine;
using DG.Tweening;

namespace Game.Minigames.Waveform
{
    public class ScrollHintAnimator : MonoBehaviour
    {
        [Header("Thành phần Visual")]
        [SerializeField] private SpriteRenderer _upArrow;
        [SerializeField] private SpriteRenderer _downArrow;
        [SerializeField] private SpriteRenderer _mouse;

        [Header("Cài đặt Nảy (Mũi tên)")]
        [Tooltip("Khoảng cách nảy tới/lui (Trục Z)")]
        [SerializeField] private float _punchForce = 0.2f;
        [Tooltip("Thời gian nảy")]
        [SerializeField] private float _punchDuration = 0.15f;

        [Header("Cài đặt Trượt (Con chuột)")]
        [Tooltip("Khoảng cách trượt của con chuột (Trục Z)")]
        [SerializeField] private float _mouseMoveDistance = 0.15f;

        private Vector3 _mouseOriginalPos;

        private void Start()
        {
            if (_mouse != null)
            {
                _mouseOriginalPos = _mouse.transform.localPosition;
            }
        }

        private void Update()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll > 0)
            {
                if (_upArrow != null) AnimateFeedback(_upArrow, -_punchForce); 
                if (_mouse != null) AnimateMouse(-_mouseMoveDistance);
            }
            else if (scroll < 0)
            {
                if (_downArrow != null) AnimateFeedback(_downArrow, _punchForce); 
                if (_mouse != null) AnimateMouse(_mouseMoveDistance);
            }
        }

        private void AnimateFeedback(SpriteRenderer arrow, float moveZ)
        {
            arrow.transform.DOKill(true);

            arrow.transform.DOPunchPosition(new Vector3(0, 0, -moveZ), _punchDuration, 1, 0);

            arrow.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), _punchDuration, 1, 0);
        }

        private void AnimateMouse(float moveZ)
        {
            _mouse.transform.DOKill();

            _mouse.transform.DOLocalMoveZ(_mouseOriginalPos.z - moveZ, 0.1f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    _mouse.transform.DOLocalMoveZ(_mouseOriginalPos.z, 0.2f).SetEase(Ease.InOutSine);
                });
        }
    }
}