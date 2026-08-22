using DG.Tweening;
using Game.Core;
using UnityEngine;

public class ChargeButton : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private LightBulbController _lightController; 

    [Header("Visual")]
    [SerializeField] private Material _redMAT;
    [SerializeField] private Material _greenMAT;
    [SerializeField] private Material _yellowMAT;
    [SerializeField] private Renderer _renderer;

    [Header("Button Animation")]
    [SerializeField] private Vector3 _moveOffset = Vector3.zero;
    [SerializeField] private float _moveduration = 1f;

    private bool _isMoving = false;
    private bool _isBatteryFull = false;

    private void Start()
    {
        GameEvents.OnLightFlashed += HandleLightFlashed;
        GameEvents.OnBatteryChargeCompleted += HandleBatteryChargeCompleted;

        _renderer.material = _greenMAT;
    }

    private void OnDestroy()
    {
        GameEvents.OnLightFlashed -= HandleLightFlashed;
        GameEvents.OnBatteryChargeCompleted -= HandleBatteryChargeCompleted;
    }

    private void OnMouseDown()
    {
        if (_isMoving) return;
        _isMoving = true;

        Vector3 pos = transform.position;
        AudioController.Instance.PlaySFX(SoundName.Button3DClick);

        if ( _lightController.IsBatteryFull)
        {
            _renderer.material = _yellowMAT;
            transform.DOKill();
            transform.DOPunchPosition(new Vector3(0f, 0.5f, 0f), 0.4f).OnComplete(() =>
            {
               if (_renderer != null)
                    _renderer.material = _greenMAT; 
                _isMoving = false;
            });
            return;
        }


        transform.DOKill();
        transform.DOMove(pos + _moveOffset, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            if (_renderer != null)
                _renderer.material = _redMAT;
            transform.DOMove(pos, _moveduration / 2f).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                GameEvents.RaiseBatteryChargeStarted();
                _isMoving = false;  
            });

        });
            
    }

    private void HandleLightFlashed()
    {
        if (_renderer != null)
            _renderer.material = _redMAT;
    }

    private void HandleBatteryChargeCompleted()
    {
        if (_renderer != null)
            _renderer.material = _greenMAT;
    }
}
