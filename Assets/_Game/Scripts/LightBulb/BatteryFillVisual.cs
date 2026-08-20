using DG.Tweening;
using Game.Core;
using UnityEngine;

namespace Game.LightFlash
{
    public class BatteryFillVisual : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LightBulbController _lightBulbController;
        [SerializeField] private Transform _fillTransform;
        [SerializeField] private Renderer _fillRenderer;

        [Header("Fill Settng")]
        [SerializeField] private Vector3 _fillAxis = Vector3.up;
        [SerializeField] private float _maxScaleOnAxis = 1f;
        [SerializeField] private bool _pivotIsCenter = true;

        [Header("Full Charge Effect")]
        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField] private float _emissionFadeDuration = 0.4f;


        private Vector3 _baseScale;
        private Vector3 _baseLocalPos;
        private float _extentOnAxis;

        private MaterialPropertyBlock _mpb;
        private Tween _emissionTween;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private Color _originalColor;
        private bool _colorCached = false;

        private void Awake()
        {
            if (_fillTransform == null)
            {
                Debug.LogWarning("[BatteryFillVisual] Chưa gán _fillTransform.");
                enabled = false;
                return;
            }

            _baseScale = _fillTransform.localScale;
            _baseLocalPos = _fillTransform.localPosition;

            var renderer = _fillTransform.GetComponent<Renderer>();
            if (renderer != null)
            {
                MeshFilter mf = _fillTransform.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Vector3 localExtents = Vector3.Scale(mf.sharedMesh.bounds.extents, _baseScale);
                    _extentOnAxis = Vector3.Dot(localExtents, _fillAxis.normalized);
                }
            }

            if (_fillRenderer != null)
                _mpb = new MaterialPropertyBlock();
        }

        private void Start()
        {
            if (_lightBulbController != null)
                _lightBulbController.OnBatteryProgressChanged += UpdateFill;

            GameEvents.OnBatteryChargeCompleted += PlayFullChargeEffect;
        }

        private void OnDestroy()
        {
            if (_lightBulbController != null)
                _lightBulbController.OnBatteryProgressChanged -= UpdateFill;

            GameEvents.OnBatteryChargeCompleted -= PlayFullChargeEffect;

            _emissionTween?.Kill();
        }

        private void UpdateFill(float progress01)
        {
            progress01 = Mathf.Clamp01(progress01);

            Vector3 axisNorm = _fillAxis.normalized;
            float targetScaleOnAxis = progress01 * _maxScaleOnAxis;

            Vector3 newScale = _baseScale;
            if (axisNorm.x != 0) newScale.x = targetScaleOnAxis;
            if (axisNorm.y != 0) newScale.y = targetScaleOnAxis;
            if (axisNorm.z != 0) newScale.z = targetScaleOnAxis;
            _fillTransform.localScale = newScale;

            if (_pivotIsCenter)
            {
                float scaleRatio = _maxScaleOnAxis > 0 ? targetScaleOnAxis / _maxScaleOnAxis : 0f;
                float offset = _extentOnAxis * (1f - scaleRatio);
                _fillTransform.localPosition = _baseLocalPos - axisNorm * offset;
            }
        }

        private void PlayFullChargeEffect()
        {
            _fillTransform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 8, 0.8f);

            if (_fillRenderer == null || _mpb == null)
                return;

            if (!_colorCached)
            {
                _originalColor = _fillRenderer.sharedMaterial.GetColor(BaseColor);
                _colorCached = true;
            }

            _emissionTween?.Kill();

            SetBaseColor(_flashColor);
            _emissionTween = DOTween.To(
                () => 0f,
                t => SetBaseColor(Color.Lerp(_flashColor, _originalColor, t)),
                1f,
                _emissionFadeDuration
            );
        }

        private void SetBaseColor(Color c)
        {
            _fillRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColor, c);
            _fillRenderer.SetPropertyBlock(_mpb);
        }
    }
}