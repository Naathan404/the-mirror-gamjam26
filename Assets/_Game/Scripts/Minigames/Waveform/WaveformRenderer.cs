using DG.Tweening;
using UnityEngine;

namespace Game.Minigames.Waveform
{
    [RequireComponent(typeof(LineRenderer))]
    public class WaveformRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private LineRenderer _ghostLineRenderer; // gán 1 LineRenderer con riêng, material hỗ trợ vertex alpha
        [SerializeField] private int _resolution = 100;
        [SerializeField] private float _surfaceOffset = 0.01f;
        [SerializeField] private float _ghostMaxAlpha = 0.4f;
        [SerializeField] private float _ghostFadeDuration = 0.15f;

        private float _ghostAlpha;
        private Gradient _ghostBaseGradient;
        private Tween _ghostFadeTween;

        private void Awake()
        {
            if (_ghostLineRenderer != null)
            {
                _ghostBaseGradient = _ghostLineRenderer.colorGradient;
                _ghostLineRenderer.gameObject.SetActive(false);
            }
        }

        public void Draw(SineComponent[] components, Transform surface, float xMin, float xMax, float domainHalfWidth,
            float rowCenterY, float rowHalfHeight, float? fixedMaxAmplitude = null)
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = _resolution;
            _lineRenderer.SetPositions(ComputePositions(components, surface, xMin, xMax, domainHalfWidth, rowCenterY, rowHalfHeight, fixedMaxAmplitude));
        }

        public void ShowGhost(SineComponent component, Transform surface, float xMin, float xMax, float domainHalfWidth,
            float rowCenterY, float rowHalfHeight, float fixedMaxAmplitude)
        {
            if (_ghostLineRenderer == null) return;

            _ghostLineRenderer.useWorldSpace = true;
            _ghostLineRenderer.positionCount = _resolution;
            _ghostLineRenderer.SetPositions(ComputePositions(new[] { component }, surface, xMin, xMax, domainHalfWidth, rowCenterY, rowHalfHeight, fixedMaxAmplitude));
            _ghostLineRenderer.gameObject.SetActive(true);
            FadeGhostTo(_ghostMaxAlpha);
        }

        // Gọi khi player đang chỉnh dial trong lúc ghost đang hiện, để ghost bám theo giá trị mới
        public void UpdateGhostPositions(SineComponent component, Transform surface, float xMin, float xMax, float domainHalfWidth,
            float rowCenterY, float rowHalfHeight, float fixedMaxAmplitude)
        {
            if (_ghostLineRenderer == null || !_ghostLineRenderer.gameObject.activeSelf) return;
            _ghostLineRenderer.SetPositions(ComputePositions(new[] { component }, surface, xMin, xMax, domainHalfWidth, rowCenterY, rowHalfHeight, fixedMaxAmplitude));
        }

        public void HideGhost()
        {
            if (_ghostLineRenderer == null) return;
            FadeGhostTo(0f, () => _ghostLineRenderer.gameObject.SetActive(false));
        }

        private void FadeGhostTo(float targetAlpha, System.Action onComplete = null)
        {
            _ghostFadeTween?.Kill();
            _ghostFadeTween = DOTween.To(() => _ghostAlpha, v => { _ghostAlpha = v; ApplyGhostAlpha(v); }, targetAlpha, _ghostFadeDuration)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void ApplyGhostAlpha(float alpha)
        {
            var alphaKeys = new GradientAlphaKey[_ghostBaseGradient.alphaKeys.Length];
            for (int i = 0; i < alphaKeys.Length; i++)
                alphaKeys[i] = new GradientAlphaKey(_ghostBaseGradient.alphaKeys[i].alpha * alpha, _ghostBaseGradient.alphaKeys[i].time);

            var g = new Gradient();
            g.SetKeys(_ghostBaseGradient.colorKeys, alphaKeys);
            _ghostLineRenderer.colorGradient = g;
        }

        private Vector3[] ComputePositions(SineComponent[] components, Transform surface, float xMin, float xMax,
            float domainHalfWidth, float rowCenterY, float rowHalfHeight, float? fixedMaxAmplitude)
        {
            float scaleFactor;
            if (fixedMaxAmplitude.HasValue)
            {
                scaleFactor = rowHalfHeight / fixedMaxAmplitude.Value;
            }
            else
            {
                float maxPossibleAmp = 0f;
                foreach (var c in components) maxPossibleAmp += Mathf.Abs(c.Amplitude);
                scaleFactor = maxPossibleAmp > rowHalfHeight ? rowHalfHeight / maxPossibleAmp : 1f;
            }

            var positions = new Vector3[_resolution];
            for (int i = 0; i < _resolution; i++)
            {
                float t = i / (float)(_resolution - 1);
                float physicalX = Mathf.Lerp(xMin, xMax, t);
                float logicalX = Mathf.Lerp(-domainHalfWidth, domainHalfWidth, t);
                float localY = rowCenterY + WaveformMath.EvaluaSum(components, logicalX) * scaleFactor;
                Vector3 localPoint = new Vector3(physicalX, localY, -_surfaceOffset);
                positions[i] = surface.TransformPoint(localPoint);
            }
            return positions;
        }
    }
}