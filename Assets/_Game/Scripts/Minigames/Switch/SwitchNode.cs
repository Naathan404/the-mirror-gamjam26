using System;
using UnityEngine;
using DG.Tweening;

namespace Game.Minigames
{
    [RequireComponent(typeof(BoxCollider))]
    public class SwitchNode : MonoBehaviour
    {
        public int ID { get; private set; }
        public bool IsOn { get; private set; }

        public Action<SwitchNode> OnNodeClicked;
        public Action<SwitchNode, bool> OnNodeHovered;

        [Header("Công tắc")]
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite _spriteOn;
        [SerializeField] private Sprite _spriteOff;
        [SerializeField] private SpriteRenderer _glowObject;

        public void Init(int id)
        {
            ID = id;
            transform.localPosition = new Vector3(transform.localPosition.x, 0.01f, transform.localPosition.z);
        }

        public void SetState(bool isOn, bool playTween = true)
        {
            IsOn = isOn;
            _renderer.sprite = IsOn ? _spriteOn : _spriteOff;

            float targetAlpha = isOn ? 1f : 0f;

            if (playTween)
            {
                transform.DOPunchScale(new Vector3(-0.1f, -0.1f, 0f), 0.15f);

                _glowObject.DOFade(targetAlpha, 0.2f);
            }
            else
            {
                Color c = _glowObject.color;
                c.a = targetAlpha;
                _glowObject.color = c;
            }
        }

        private void OnMouseEnter() => OnNodeHovered?.Invoke(this, true);
        private void OnMouseExit() => OnNodeHovered?.Invoke(this, false);
        private void OnMouseDown() => OnNodeClicked?.Invoke(this);
        public void PlaySpawnEffect(float delay)
        {
            Vector3 originalScale = transform.localScale;
            transform.localScale = Vector3.zero;

            transform.DOScale(originalScale, 0.4f)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
        }
    }
}