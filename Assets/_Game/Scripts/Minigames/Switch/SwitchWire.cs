using System;
using UnityEngine;
using DG.Tweening;

namespace Game.Minigames
{
    [RequireComponent(typeof(LineRenderer))]
    public class SwitchWire : MonoBehaviour
    {
        public SwitchNode NodeA { get; private set; }
        public SwitchNode NodeB { get; private set; }

        private LineRenderer _line;

        private Color _idleColor;
        private Color _hoverColor;
        private Color _pulseColor;

        private Color _currentBaseColor;
        private bool _isHovered = false;

        public void Init(SwitchNode a, SwitchNode b, Material mat, Color idle, Color hover, Color pulse)
        {
            NodeA = a;
            NodeB = b;
            _idleColor = idle;
            _hoverColor = hover;
            _pulseColor = pulse;
            _currentBaseColor = _idleColor;

            _line = GetComponent<LineRenderer>();

            _line.useWorldSpace = true;
            _line.material = mat;
            _line.startWidth = 0.08f;
            _line.endWidth = 0.08f;
            _line.positionCount = 20;

            _line.widthMultiplier = 0f;

            _line.sortingLayerName = "Default";
            _line.sortingOrder = 5;

            GenerateCurvedPath();
            ApplySolidColor(_currentBaseColor);
        }

        private void GenerateCurvedPath()
        {
            Vector3 start = NodeA.transform.position;
            Vector3 end = NodeB.transform.position;

            Vector3 direction = (end - start).normalized;
            Vector3 cameraUp = Camera.main != null ? -Camera.main.transform.forward : Vector3.up;

            Vector3 perpendicular = Vector3.Cross(direction, cameraUp).normalized;

            float amplitude = UnityEngine.Random.Range(0.15f, 0.3f) * (UnityEngine.Random.value > 0.5f ? 1 : -1);
            float frequency = UnityEngine.Random.Range(1f, 2.5f); 
            float phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f); 

            Vector3 zOffset = cameraUp * 0.015f;

            Vector3[] pathPoints = new Vector3[20];
            for (int i = 0; i < 20; i++)
            {
                float t = i / 19f;
                Vector3 basePos = Vector3.Lerp(start, end, t); 

                float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f + phaseOffset);

                float envelope = Mathf.Sin(t * Mathf.PI);

                Vector3 curveOffset = perpendicular * (wave * envelope * amplitude);

                pathPoints[i] = basePos + curveOffset + zOffset;
            }
            _line.SetPositions(pathPoints);
        }

        public void SetHoverState(bool isHovered)
        {
            _isHovered = isHovered;
            _currentBaseColor = _isHovered ? _hoverColor : _idleColor;
            ApplySolidColor(_currentBaseColor);
        }

        private void ApplySolidColor(Color c)
        {
            _line.startColor = c;
            _line.endColor = c;

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a, 1f) }
            );
            _line.colorGradient = g;
        }

        public void ShootPulse(SwitchNode fromNode, Action onArrived)
        {
            bool isFromA = (fromNode == NodeA);

            DOVirtual.Float(-0.25f, 1.25f, 0.35f, (v) =>
            {
                float actualT = isFromA ? v : (1f - v);
                UpdateWaveGradient(actualT);
            })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                ApplySolidColor(_currentBaseColor);
                onArrived?.Invoke();
            });
        }

        private void UpdateWaveGradient(float waveCenterTime)
        {
            float waveWidth = 0.3f; 

            Gradient gradient = new Gradient();

            GradientColorKey[] colorKeys = new GradientColorKey[8];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[8];

            for (int i = 0; i < 8; i++)
            {
                float pointTime = i / 7f;
                float dist = Mathf.Abs(pointTime - waveCenterTime);

                float intensity = Mathf.Clamp01(1f - (dist / waveWidth));

                intensity = Mathf.SmoothStep(0f, 1f, intensity);

                Color mixedColor = Color.Lerp(_currentBaseColor, _pulseColor, intensity);

                colorKeys[i] = new GradientColorKey(mixedColor, pointTime);
                alphaKeys[i] = new GradientAlphaKey(mixedColor.a, pointTime);
            }

            gradient.SetKeys(colorKeys, alphaKeys);
            _line.colorGradient = gradient;
        }

        public void PlaySpawnEffect(float delay)
        {
            _line.widthMultiplier = 0f;

            DOVirtual.Float(0f, 1f, 0.3f, v =>
            {
                if (_line != null) _line.widthMultiplier = v;
            })
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
        }
    }
}