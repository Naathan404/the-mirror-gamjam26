using Game.Core;
using UnityEngine;

public class ViewMirrorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _background;

    [Header("Backgrounds")]
    [SerializeField] private Sprite[] _viewMirrorBackgrounds;


    #region Base
    private void Start()
    {
        GameEvents.OnEntityStateChanged += HandleChangeBackground;
        GameEvents.OnGameLost += ResetBackground;
    }

    private void OnDestroy()
    {
        GameEvents.OnEntityStateChanged -= HandleChangeBackground;
        GameEvents.OnGameLost -= ResetBackground;
    }
    #endregion


    private void HandleChangeBackground(int state)
    {
        if (_background == null || _viewMirrorBackgrounds == null || _viewMirrorBackgrounds.Length == 0)
        {
            return;
        }

        if (state > GameConstants.ENTITY_MAX_STATE || state <= 0)
        {
            return;
        }

        int backgroundIndex = state - 1;
        if (backgroundIndex >= _viewMirrorBackgrounds.Length)
        {
            return;
        }

        _background.sprite = _viewMirrorBackgrounds[backgroundIndex];
    }

    private void ResetBackground()
    {
        if (_background == null || _viewMirrorBackgrounds == null || _viewMirrorBackgrounds.Length == 0)
        {
            return;
        }

        _background.sprite = _viewMirrorBackgrounds[_viewMirrorBackgrounds.Length - 1];
    }
}
