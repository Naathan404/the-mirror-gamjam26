using System;
using Game.Core;
using Game.Managers;
using Mono.Cecil;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Cameras
{
    public class CameraviewController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _mirrorTarget;
        [SerializeField] private Transform _deskTarget;
        [SerializeField] private Transform _behindTarget;

        [Header("Transition Settings")]
        /// thời gian chuyển từ view này sang view khác
        [SerializeField] private float _transitionDuration = 0.5f;
        // ease curve
        [SerializeField] private AnimationCurve _easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // khóa input lúc chuyển view 
        [SerializeField] private bool _lookInputDurationTransition = true;
        
        /// <summary>View hiện tại người chơi đang đứng ở đó (sau khi transition xong).</summary>
        public View CurrentView { get; private set; } = View.Mirror;
        /// <summary>True nếu camera đang trong quá trình xoay.</summary>
        public bool IsTransitioning { get; private set; } = false;


        #region Private fields
        private Quaternion _startRotation;
        private Quaternion _targetRotation;
        private float _transitionTimer;
        #endregion

        #region Base
        private void Awake()
        {
            if (_cameraTransform == null) _cameraTransform = transform;
        }

        private void Start()
        {
            // Bắt đầu game ở view Gương.
            if (_mirrorTarget != null)
            {
                _cameraTransform.rotation = _mirrorTarget.rotation;
            }
            CurrentView = View.Mirror;
        }

        private void Update()
        {
            if (!IsTransitioning) return;
    
            _transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionTimer / _transitionDuration);
            float easedT = _easeCurve.Evaluate(t);
    
            _cameraTransform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, easedT);
    
            if (t >= 1f)
            {
                IsTransitioning = false;
                GameEvents.RaiseViewChangeFinished(CurrentView);
            }
        }
        #endregion

        #region Buttons
        public void SwitchToMirror()
        {
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            RequestSwitch(View.Mirror, _mirrorTarget);
        }
        public void SwitchToDesk()
        {
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            RequestSwitch(View.Desk, _deskTarget);
        }
        public void SwitchToBehind()
        {
            AudioController.Instance.PlaySFX(SoundName.ButtonClick);
            RequestSwitch(View.Behind, _behindTarget);
        }
        #endregion

        #region Switch Views
        public void RequestSwitchTo(View view)
        {
            Transform target = view switch
            {
                View.Mirror => _mirrorTarget,
                View.Desk => _deskTarget,
                View.Behind => _behindTarget,
                _ => null
            };

            RequestSwitch(view, target);
        }
        private void RequestSwitch(View view, Transform target)
        {
            if (target == null)
            {
                Debug.LogError("[CameraviewController] Missing target to switch view");
                return;
            }

            if (_lookInputDurationTransition && IsTransitioning) return;

            if (!IsTransitioning && CurrentView == view) return;

            _startRotation = _cameraTransform.rotation;
            _targetRotation = target.rotation;
            _transitionTimer = 0f;
            IsTransitioning = true;
            CurrentView = view;

            GameEvents.RaiseViewChangeStarted(view);
        }

        #endregion
    }
}

