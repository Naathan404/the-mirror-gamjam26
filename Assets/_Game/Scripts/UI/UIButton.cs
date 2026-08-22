using DG.Tweening;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float _punchAmount = 1.2f;
    [SerializeField] private float _punchDuration = 0.2f;
    [SerializeField] private bool _isHighLight = false;

    [SerializeField] private bool _isViewButton = false;

    private CanvasGroup _canvasGroup;
    private bool _isLocked = false;

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isLocked || _isHighLight) return; // Bị khóa thì không cho phóng to
        _isHighLight = true;
        this.transform.DOKill();
        transform.localScale = Vector2.one;
        transform.DOScale(_punchAmount * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isLocked) return;
        transform.DOScale(Vector2.one, _punchDuration).SetEase(Ease.OutExpo);
        _isHighLight = false;
    }

    private void HandleViewChanged(View _)
    {
        if (!_isViewButton || _isLocked) return;
        this.transform.DOKill();
        transform.localScale = Vector2.one;
        _isHighLight = false;
    }
    public void SetLockedState(bool isLocked)
    {
        _isLocked = isLocked;
        _isHighLight = false;
        this.transform.DOKill();

        if (_isLocked)
        {
            // Khi đang ở ngôn ngữ này: Nút thu nhỏ lại 0.9, màu nhạt đi 50%
            transform.DOScale(0.9f * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
            if (_canvasGroup != null) _canvasGroup.DOFade(0.5f, _punchDuration);
        }
        else
        {
            // Trả về bình thường: Kích thước 1.0, rõ 100%
            transform.DOScale(Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
            if (_canvasGroup != null) _canvasGroup.DOFade(1f, _punchDuration);
        }
    }
}