using Game.Core;
using UnityEngine;

public class MinigameTogglesController : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private GameObject[] _toggles;
    void Start()
    {
        GameEvents.OnMinigameOpened += HideAllToggles;
        GameEvents.OnMinigameClosed += ShowAllToggles;
    }

    private void OnDestroy()
    {
        GameEvents.OnMinigameOpened -= HideAllToggles;
        GameEvents.OnMinigameClosed -= ShowAllToggles;
    }

    private void HideAllToggles(MinigameType _)
    {
        foreach(var t in _toggles)
        {
            t.SetActive(false);
        }
    }

    private void ShowAllToggles(MinigameType _) //TODO: show toggles when minigame unlocked
    {
        foreach (var t in _toggles)
        {
            t.SetActive(true);
        }
    }
}
