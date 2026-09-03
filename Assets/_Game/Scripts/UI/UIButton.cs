using DG.Tweening;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Thêm thư viện này để dùng Graphic (Image/Text)

[RequireComponent(typeof(CanvasGroup))]
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float _punchAmount = 1.2f;
    [SerializeField] private float _punchDuration = 0.2f;
    [SerializeField] private bool _isViewButton = false;

    [Header("Locked State Settings (Tùy chỉnh lúc bị vô hiệu hóa)")]
    [Tooltip("Kéo component Image hoặc Text của nút vào đây để đổi màu (Nếu cần)")]
    [SerializeField] private Graphic _targetGraphic;

    [Space(5)]
    [SerializeField] private float _normalScale = 1f;
    [SerializeField] private float _lockedScale = 0.9f;

    [Space(5)]
    [SerializeField] private float _normalAlpha = 1f;
    [SerializeField] private float _lockedAlpha = 0.5f;

    [Space(5)]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _lockedColor = Color.gray;

    private CanvasGroup _canvasGroup;
    private bool _isLocked = false;
    private bool _isHighLight = false;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        GameEvents.OnViewChangeStarted += HandleViewChanged;
    }

    private void OnDestroy()
    {
        GameEvents.OnViewChangeStarted -= HandleViewChanged;
    }

    private void OnDisable()
    {
        this.transform.DOKill();
        if (_canvasGroup != null) _canvasGroup.DOKill();
        if (_targetGraphic != null) _targetGraphic.DOKill();

        _isHighLight = false;

        float targetScale = _isLocked ? _lockedScale : _normalScale;
        float targetAlpha = _isLocked ? _lockedAlpha : _normalAlpha;
        Color targetColor = _isLocked ? _lockedColor : _normalColor;

        transform.localScale = targetScale * Vector2.one;

        if (_canvasGroup != null)
            _canvasGroup.alpha = targetAlpha;

        if (_targetGraphic != null)
            _targetGraphic.color = targetColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isLocked || _isHighLight) return;
        _isHighLight = true;
        this.transform.DOKill();
        transform.localScale = _normalScale * Vector2.one;
        transform.DOScale(_punchAmount * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isLocked) return;
        transform.DOScale(_normalScale * Vector2.one, _punchDuration).SetEase(Ease.OutExpo);
        _isHighLight = false;
    }

    private void HandleViewChanged(View _)
    {
        if (!_isViewButton || _isLocked) return;
        this.transform.DOKill();
        transform.localScale = _normalScale * Vector2.one;
        _isHighLight = false;
    }

    public void SetLockedState(bool isLocked)
    {
        _isLocked = isLocked;
        _isHighLight = false;
        this.transform.DOKill();

        if (_isLocked)
        {
            transform.DOScale(_lockedScale * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
            if (_canvasGroup != null) _canvasGroup.DOFade(_lockedAlpha, _punchDuration);
            if (_targetGraphic != null) _targetGraphic.DOColor(_lockedColor, _punchDuration);
        }
        else
        {
            transform.DOScale(_normalScale * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
            if (_canvasGroup != null) _canvasGroup.DOFade(_normalAlpha, _punchDuration);
            if (_targetGraphic != null) _targetGraphic.DOColor(_normalColor, _punchDuration);
        }
    }
}