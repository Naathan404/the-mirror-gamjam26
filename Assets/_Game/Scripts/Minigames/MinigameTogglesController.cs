using Game.Core;
using Game.Interactables; // Thêm thư viện này để truy cập MinigameTrigger
using Game.Systems.Lock;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using KeyCode = Game.Core.KeyCode;

public class MinigameTogglesController : MonoBehaviour
{
    [Header("Tham chiếu")]
    [SerializeField] private MinigameTrigger[] _toggles;
    [SerializeField] private Transform[] _spawnPos;

    private List<MinigameType> _completedGames = new List<MinigameType>();

    private List<MinigameTrigger> _toggleActives = new();

    private void OnEnable()
    {
        GameEvents.OnMinigameOpened += HideAllToggles;
        GameEvents.OnMinigameClosed += ShowAllToggles;
        GameEvents.OnMinigameCompleted += HandleMinigameCompleted;

        //GameEvents.OnPasscodeGenerated += HandlePasscodeGenerated;
    }

    private void OnDisable()
    {
        GameEvents.OnMinigameOpened -= HideAllToggles;
        GameEvents.OnMinigameClosed -= ShowAllToggles;
        GameEvents.OnMinigameCompleted -= HandleMinigameCompleted;

        //GameEvents.OnPasscodeGenerated -= HandlePasscodeGenerated;
    }

    private void Start()
    {
        Invoke(nameof(FetchPasscodeAndSetup), 0.15f);
    }

    private void HandleMinigameCompleted(MinigameType type, KeyCode code)
    {
        if (!_completedGames.Contains(type))
        {
            _completedGames.Add(type);
        }
    }

    private void FetchPasscodeAndSetup()
    {
        if (PasscodeController.Instance == null) return;

        Dictionary<MinigameType, KeyCode> dic = PasscodeController.Instance.GetCurrentPasscodeMap();
        if (dic == null) return;

        HideAllToggles(MinigameType.Maze);
        int i = 0;
        foreach(var kvp in dic)
        {
            var type = kvp.Key;
            var spawnPos = _spawnPos[i].position;

            foreach(var t in _toggles)
            {
                if (t.targetMinigame == type)
                {
                    t.gameObject.SetActive(true);
                    t.transform.position = spawnPos;
                    _toggleActives.Add(t);
                }
            }

            i++;
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
        foreach (var t in _toggleActives)
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