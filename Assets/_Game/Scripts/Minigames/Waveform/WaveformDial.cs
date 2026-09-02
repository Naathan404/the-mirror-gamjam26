using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Minigames.Waveform
{
    public class WaveformDial : MonoBehaviour
    {
        public enum ParamType { Amplitude, Frequency }

        [SerializeField] private int _waveIndex;
        [SerializeField] private ParamType _paramType;
        [SerializeField] Transform _knobVisual;

        private float _currentValue;
        private WaveformConfigSO _config;

        public event System.Action<WaveformDial> OnValueChanged;
        public event System.Action<WaveformDial> OnFocusEnter;
        public event System.Action<WaveformDial> OnFocusExit;

        public float CurrentValue => _currentValue;
        public int WaveIndex => _waveIndex;
        public ParamType Type => _paramType;

        [Header("Hover Feedback")]
        [SerializeField] private float _hoverScale = 1.15f;
        [SerializeField] private float _hoverScaleDuration = 0.15f;



        public void Init(WaveformConfigSO config, float initValue)
        {
            _config = config;
            _currentValue = initValue;
            UpdateKnobVisual();
        }

        public void SetIndex(int waveIndex, ParamType type)
        {
            _waveIndex = waveIndex;
            _paramType = type;
        }

        private void OnMouseOver()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Adjust(scroll > 0 ? 1 : -1);
            }
        }

        private void OnMouseDrag()
        {
            float delta = Input.GetAxis("Mouse Y");
            Adjust(delta > 0 ? 1 : (delta < 0 ? -1 : 0));
        }

        private void OnMouseEnter()
        {
            transform.DOScale(_hoverScale, _hoverScaleDuration).SetEase(Ease.OutBack);
            OnFocusEnter?.Invoke(this);
        }

        private void OnMouseExit()
        {
            transform.DOScale(1f, _hoverScaleDuration).SetEase(Ease.OutQuad);
            OnFocusExit?.Invoke(this);
        }
        
        public void Adjust(float direction)
        {
            float step = _paramType == ParamType.Amplitude ? _config.AmplitudeStep : _config.FrequencyStep;
            Vector2 range = _paramType == ParamType.Amplitude ? _config.AmplitudeRange : _config.FrequencyRange;

            float newValue = Mathf.Clamp(_currentValue + direction * step, range.x, range.y);
            if (Mathf.Approximately(newValue, _currentValue)) return; // đã ở biên, không đổi gì thì không làm gì cả

            _currentValue = newValue;
            AudioController.Instance.PlaySFX(SoundName.Waveform_Rotate);
            UpdateKnobVisual();
            OnValueChanged?.Invoke(this);
        }
        
        private void UpdateKnobVisual()
        {
            Vector2 range = _paramType == ParamType.Amplitude ? _config.AmplitudeRange : _config.FrequencyRange;
            float t = Mathf.InverseLerp(range.x, range.y, _currentValue);
            float angle = Mathf.Lerp(-135f, 135f, t);
            _knobVisual.DOLocalRotate(new Vector3(0, 0, -angle), 0.15f);

            bool atLimit = Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f);
            if (atLimit)
                _knobVisual.DOPunchScale(0.2f * Vector3.one, 0.1f); // rung nhẹ báo hiệu đã chạm biên
        }
    }
}