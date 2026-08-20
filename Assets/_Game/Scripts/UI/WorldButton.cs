using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class WorldButton : MonoBehaviour
{
    [SerializeField] private float _punchAmount = 1.2f;
    [SerializeField] private float _punchDuration = 0.2f;
    [SerializeField] private bool _isHighLight = false;

    private Vector3 _originalScale;

    private void Start() => _originalScale = transform.localScale;

    public void OnMouseEnter()
    {
        if (_isHighLight) return;
        _isHighLight = true;
        this.transform.DOKill();
        transform.localScale = _originalScale;
        transform.DOScale(_punchAmount * _originalScale, _punchDuration).SetEase(Ease.OutQuint);
    }

    private void OnMouseExit()
    {
        transform.DOScale(_originalScale, _punchDuration).SetEase(Ease.OutExpo);
        _isHighLight = false;
    }
}
