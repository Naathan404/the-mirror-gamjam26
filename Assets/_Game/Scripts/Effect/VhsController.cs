using System.Collections;
using DG.Tweening;
using Game.Core;
using UnityEngine;

namespace Game.Effect
{
    public class VhsController : MonoBehaviour
    {
        [Header("Vhs")]
        [SerializeField] private SpriteRenderer _vhsSpriteRenderer;
        [SerializeField] private float _minVhsDuration = 0.2f;
        [SerializeField] private float _maxVhsDuration = 1f;

        private bool _isPlaying = false;
        private View _currentView = View.Mirror;

        private void Start()
        {
            GameEvents.OnEntityStateChanged += PlayVhsEffect;
            GameEvents.OnViewChangeFinished += HandleViewChanged;

            _vhsSpriteRenderer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            GameEvents.OnEntityStateChanged -= PlayVhsEffect;
            GameEvents.OnViewChangeFinished -= HandleViewChanged;
        }

        private void PlayVhsEffect(int _)
        {
            if (_isPlaying || _currentView != View.Mirror) return;

            _isPlaying = true;

            if (_vhsSpriteRenderer != null && !_vhsSpriteRenderer.gameObject.activeSelf)
            {
                float duration = Random.Range(_minVhsDuration, _maxVhsDuration);
                _vhsSpriteRenderer.gameObject.SetActive(true);
                _vhsSpriteRenderer.transform.DOShakePosition(duration, 0.5f, 30, 90f);
                StartCoroutine(VhsEffectRoutine(duration));
            }
        }

        private IEnumerator VhsEffectRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _vhsSpriteRenderer.gameObject.SetActive(false);
            _isPlaying = false;
        }

        private void HandleViewChanged(View view)
        {
            _currentView = view;
        }
    }
}