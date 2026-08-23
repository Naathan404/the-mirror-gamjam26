using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    [RequireComponent(typeof(Toggle))]
    public class ToggleSwitchUI : MonoBehaviour
    {
        [Header("Thành phần UI")]
        [Tooltip("Kéo cục Handle (cục gạt) vào đây")]
        [SerializeField] private RectTransform _handle;
        [Tooltip("Hình nền của công tắc (để đổi màu khi Bật/Tắt)")]
        [SerializeField] private Image _backgroundImage;

        [Header("Cài đặt vị trí trượt")]
        [SerializeField] private float _offPositionX = 0f;
        [SerializeField] private float _onPositionX = 25f; 
        [SerializeField] private float _slideSpeed = 15f;    

        [Header("Cài đặt màu sắc")]
        [SerializeField] private Color _offColor = Color.gray;
        [SerializeField] private Color _onColor = Color.green;

        private Toggle _toggle;
        private Vector2 _targetPosition;
        private Color _targetColor;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnToggleValueChanged);
            SetStateImmediate(_toggle.isOn);
        }

        private void OnToggleValueChanged(bool isOn)
        {
            _targetPosition = new Vector2(isOn ? _onPositionX : _offPositionX, _handle.anchoredPosition.y);
            _targetColor = isOn ? _onColor : _offColor;
        }

        private void SetStateImmediate(bool isOn)
        {
            _targetPosition = new Vector2(isOn ? _onPositionX : _offPositionX, _handle.anchoredPosition.y);
            _targetColor = isOn ? _onColor : _offColor;

            _handle.anchoredPosition = _targetPosition;
            if (_backgroundImage != null) _backgroundImage.color = _targetColor;
        }

        private void Update()
        {
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.Lerp(_handle.anchoredPosition, _targetPosition, Time.deltaTime * _slideSpeed);
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = Color.Lerp(_backgroundImage.color, _targetColor, Time.deltaTime * _slideSpeed);
            }
        }
    }
}