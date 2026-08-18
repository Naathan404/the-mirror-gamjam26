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
    }

    private void OnDestroy()
    {
        GameEvents.OnEntityStateChanged -= HandleChangeBackground;
    }
    #endregion


    private void HandleChangeBackground(int state)
    {
        if (state > GameConstants.ENTITY_MAX_STATE) return;
        if (state == 0)
        {
            Debug.Log("Jumpscare");
            return;
        }

        _background.sprite = _viewMirrorBackgrounds[state - 1];
    }
}
