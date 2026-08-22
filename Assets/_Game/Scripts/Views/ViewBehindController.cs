using System;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Systems.Lock;
using UnityEngine;
using KeyCode = Game.Core.KeyCode;

namespace Game.Views
{
    public class ViewBehindController : MonoBehaviour
    {
        [SerializeField] private List<ShapeSpriteEntry> _shapes;
        [SerializeField] private List<ShapeColorEntry> _colors;

        [Header("Light")]
        [SerializeField] private LightFlashEffect _flashEffect;
        [SerializeField] private SpriteRenderer _lightRenderer;
        [SerializeField] private Sprite _lightNormalSprite;
        [SerializeField] private Sprite _lightBreakSprite;

        [Header("Swing Settings")]
        [SerializeField] private Transform _pivot;
        [SerializeField] private float _swingAngle = 20f;    // Góc đung đưa tối đa
        [SerializeField] private float _swingDuration = 1.5f; // Thời gian đung đưa đến khi dừng
        [SerializeField] private int _vibrato = 6;            // Số lần lắc qua lại
        [SerializeField] private float _elasticity = 0.5f;
        [SerializeField] private GameObject _lightShatters;

        [Header("Background")]
        [SerializeField] private SpriteRenderer _backgroundRdr;
        [SerializeField] private Sprite _bgNormalSprite;
        [SerializeField] private Sprite _bgFlashSprite;

        private Tween _swingTween;

        private void Start()
        {
            //GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
            GameEvents.OnLightFlashed += TriggerFlash;

            _lightRenderer.sprite = _lightNormalSprite;
            _backgroundRdr.sprite = _bgNormalSprite;
            _lightShatters.SetActive(false);

            Invoke(nameof(SetupPasscodeHints), 0.15f);
        }

        private void OnDestroy()
        {
            //GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
            GameEvents.OnLightFlashed -= TriggerFlash;
        }

        private void SetupPasscodeHints()
        {
            if (PasscodeController.Instance == null)
            {
                Debug.LogWarning("[ViewBehind] Không tìm thấy PasscodeController để lấy gợi ý!");
                return;
            }

            var dic = PasscodeController.Instance.GetCurrentPasscodeMap();
            if (dic == null) return;

            foreach (var kvp in dic)
            {
                var sprite = GetSprite(kvp.Value.Shape);
                if (sprite != null)
                {
                    sprite.color = GetColor(kvp.Value.KColor);
                }
            }
        }

        public void TriggerFlash()
        {
            if (_flashEffect == null) return;

            _flashEffect.PlayLightFlash(isBreak =>
            {
                if (_lightRenderer != null)
                {
                    _lightRenderer.sprite = isBreak ? _lightBreakSprite : _lightNormalSprite;
                }

                if (_lightShatters != null)
                {
                    _lightShatters.SetActive(isBreak);
                }

                if (_backgroundRdr != null)
                {
                    _backgroundRdr.sprite = isBreak ? _bgFlashSprite : _bgNormalSprite;
                }
            });

            PlaySwingEffect();
        }

        private void PlaySwingEffect()
        {
            if (_lightRenderer == null) return;

            _swingTween?.Kill();
            _lightRenderer.transform.localRotation = Quaternion.identity;

            _swingTween = _pivot.transform
                .DOPunchRotation(new Vector3(0, 0, _swingAngle), _swingDuration, _vibrato, _elasticity)
                .SetEase(Ease.OutQuad);
        }

        #region  Helpers
        private SpriteRenderer GetSprite(KeyShape shape)
        {
            foreach(var s in _shapes)
            {
                if (s.Shape == shape) return s.SpriteRdr;
            }
            return default;
        }

        private Color GetColor(KeyColor key)
        {
            foreach(var c in _colors)
            {
                if (c.Key == key)
                {
                    return c.Color;
                }
            }
            return default;
        }
        #endregion
    }  

    [Serializable]
    public class ShapeSpriteEntry
    {
        public KeyShape Shape;
        public SpriteRenderer SpriteRdr;
    }  

    [Serializable]
    public class ShapeColorEntry
    {
        public KeyColor Key;
        public Color Color;
    }
}
