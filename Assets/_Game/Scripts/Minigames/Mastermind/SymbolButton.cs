using DG.Tweening;
using Game.Core;
using UnityEngine;

namespace Game.Minigames
{
    public class SymbolButton : MonoBehaviour
    {
        public int SymbolId { get; private set; }
        [SerializeField] private SpriteRenderer _iconRenderer;
        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _transform;
        [SerializeField] private Vector3 _moveOffset = Vector3.zero;
        [SerializeField] private float _moveduration = 1f;

        private bool _isMoving = false;

        public void Init(int symbolId, Sprite icon)
        {
            SymbolId = symbolId;
            if (_iconRenderer != null) _iconRenderer.sprite = icon;
        }

        public void SetInteractable(bool isActive)
        {
            if (isActive)
            {
                _iconRenderer.color = Color.white;
            }
            else
                _iconRenderer.color = Color.gray;
            _collider.enabled = isActive;
        }

        private void OnMouseDown()
        {
            if (_isMoving) return;
            _isMoving = true;

            Vector3 pos = _transform.position;

            _transform.DOKill();
            _transform.DOMove(pos + _moveOffset, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                _transform.DOMove(pos, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
                {
                    GameEvents.RaiseBatteryChargeStarted();
                    _isMoving = false;  
                });

            });
                
        }
    }
    
}