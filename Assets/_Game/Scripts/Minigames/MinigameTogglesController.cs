using System.Collections.Generic;
using Game.Core;
using Game.Interactables; // Thêm thư viện này để truy cập MinigameTrigger
using UnityEngine;

public class MinigameTogglesController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private MinigameTrigger[] _toggles;

    private List<MinigameType> _completedGames = new List<MinigameType>();

    private void Start()
    {
        GameEvents.OnMinigameOpened += HideAllToggles;
        GameEvents.OnMinigameClosed += ShowAllToggles;
        GameEvents.OnMinigameCompleted += HandleMinigameCompleted;
    }

    private void OnDestroy()
    {
        GameEvents.OnMinigameOpened -= HideAllToggles;
        GameEvents.OnMinigameClosed -= ShowAllToggles;
        GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;
    }

    private void HandleMinigameCompleted(MinigameType type, int digit)
    {
        if (!_completedGames.Contains(type))
        {
            _completedGames.Add(type);
        }
    }

    private void HideAllToggles(MinigameType _)
    {
        foreach (var t in _toggles)
        {
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private void ShowAllToggles(MinigameType _)
    {
        foreach (var t in _toggles)
        {
            if (t != null)
            {
                if (!_completedGames.Contains(t.targetMinigame))
                {
                    t.gameObject.SetActive(true);
                }
            }
        }
    }
}