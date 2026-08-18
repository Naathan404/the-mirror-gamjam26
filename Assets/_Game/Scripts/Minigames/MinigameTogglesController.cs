using Game.Core;
using UnityEngine;

public class MinigameTogglesController : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private GameObject[] _toggles;
    void Start()
    {
        GameEvents.OnMinigameOpened += HideAllToggles;
    }

    private void OnDestroy()
    {
        GameEvents.OnMinigameOpened -= HideAllToggles;
    }

    private void HideAllToggles(MinigameType _)
    {
        foreach(var t in _toggles)
        {
            t.SetActive(false);
        }
    }
}
