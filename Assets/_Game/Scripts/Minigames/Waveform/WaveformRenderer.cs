using UnityEngine;

namespace Game.Minigames.Waveform
{
    [RequireComponent(typeof(LineRenderer))]
    public class WaveformRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private int _resolution = 100;
        [SerializeField] private float _surfaceOffset = 0.01f; // lùi theo local Ztrục pháp tuyến của background

        public void Draw(SineComponent[] components, Transform surface, float xMin, float xMax, float domainHalfWidth, float rowCenterY, float rowHalfHeight)
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = _resolution;

            float maxPossibleAmp = 0f;
            foreach (var c in components) maxPossibleAmp += Mathf.Abs(c.Amplitude);
            float scaleFactor = maxPossibleAmp > rowHalfHeight ? rowHalfHeight / maxPossibleAmp : 1f;

            for (int i = 0; i < _resolution; i++)
            {
                float t = i / (float)(_resolution - 1);

                float physicalX = Mathf.Lerp(xMin, xMax, t);              // vị trí vẽ thật trên bàn
                float logicalX = Mathf.Lerp(-domainHalfWidth, domainHalfWidth, t); // input cho sin nhiều chu kỳ

                float localY = rowCenterY + WaveformMath.EvaluaSum(components, logicalX) * scaleFactor;

                Vector3 localPoint = new Vector3(physicalX, localY, -_surfaceOffset);
                _lineRenderer.SetPosition(i, surface.TransformPoint(localPoint));
            }
        }
    }
}