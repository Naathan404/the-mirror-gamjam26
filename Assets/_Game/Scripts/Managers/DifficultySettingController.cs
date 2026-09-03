using System.Collections.Generic;
using System.Security.Cryptography;
using Game.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DifficultySettingController : MonoBehaviour
{
    [SerializeField] private List<Button> _levelButtons;
    [SerializeField] private List<GameObject> _glowStars;
    
    [Header("Visual")]
    [SerializeField] private Color _unchoicedColor = Color.white;
    [SerializeField] private Color _choicedColor = Color.red;

    public int debugLevel = 99;

    private void Start()
    {
        UpdateGlowStars();
        SetDifficultyLevel(PlayerPrefs.GetInt(GameConstants.ENTITY_AI_LEVEL, 0));
        debugLevel = PlayerPrefs.GetInt(GameConstants.ENTITY_AI_LEVEL, 0);
    }

    
    public void SetDifficultyLevel(int level)
    {
        if (level >= _levelButtons.Count)
        {
            Debug.Log("lvl ko hop le");
            return;
        }

        PlayerPrefs.SetInt(GameConstants.ENTITY_AI_LEVEL, level);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        int level = PlayerPrefs.GetInt(GameConstants.ENTITY_AI_LEVEL, 0);
        debugLevel = level;
        
        for (int i = 0; i < _levelButtons.Count; i++)
        {
            if (i <= level)
            {
                _levelButtons[i].image.color = _choicedColor;
            }
            else
            {
                _levelButtons[i].image.color = _unchoicedColor;
            }
        }
    }

    private void UpdateGlowStars()
    {
        foreach(var obj in _glowStars) obj.SetActive(false);
        int passed = PlayerPrefs.GetInt(GameConstants.DIFFICULT_LEVEL_PASSED, -1);
        if (passed < 0)
        {
            return;
        }
        else
        {
            for(int i = 0; i <= passed; i++)
            {
                _glowStars[i].SetActive(true);
            }
        }
    }
}
