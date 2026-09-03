using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

[CreateAssetMenu(fileName = "Entity Difficulty Config", menuName = "Game/Entity Difficult Config")]
public class EntityDifficultyConfigSO : ScriptableObject
{
    public EntityAILevel CurrentEntityAILevel=> GetAILevel(PlayerPrefs.GetInt(GameConstants.ENTITY_AI_LEVEL, 0));

    [SerializeField] private List<EntityAILevel> _aiLevels;

    private EntityAILevel GetAILevel(int level)
    {
        if (level < 0 || level >= _aiLevels.Count)
        {
            return null;
        }

        return _aiLevels[level];
    }
}

[Serializable]
public class EntityAILevel
{
    [Header("Direct")]
    public float BaseMoveInterval = 10f;
    public float BaseMoveChange = 0.15f;
    public float MoveChancePerMinigamge = 0.075f;

    [Header("Indirect")]
    public float MinMoveInterval = 3f;
    public float MoveChangeCap = 0.5f;
    public float BaseInsuranceTime = 90f;
    public float InsuranceTimeStep = 8f;
}
