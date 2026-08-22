using DG.Tweening;
using Game.Core;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float _punchAmount = 1.2f;
    [SerializeField] private float _punchDuration = 0.2f;
    [SerializeField] private bool _isHighLight = false;

    [SerializeField] private bool _isViewButton = false;

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
        if (_isHighLight) return;
        _isHighLight = true;
        this.transform.DOKill();
        transform.localScale = Vector2.one;
        transform.DOScale(_punchAmount * Vector2.one, _punchDuration).SetEase(Ease.OutQuint);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.DOScale(Vector2.one, _punchDuration).SetEase(Ease.OutExpo);
        _isHighLight = false;
    }

    private void HandleViewChanged(View _)
    {
        if (!_isViewButton) return;
        this.transform.DOKill();
        transform.localScale = Vector2.one;
        _isHighLight = false;
    }
}
