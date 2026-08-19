using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Minigames.Maze
{
    [CreateAssetMenu(fileName = "WiresConfig", menuName = "Game/Minigames/Wires Config")]
    public class WiresConfig : ScriptableObject
    {
        [Header("Wire Settings")]
        [Range(4, 18)]
        [SerializeField] private int _wireCount;

        [Header("Wire Color Settings")]
        [SerializeField] private List<WireColor> _wireColors;

        public int WireCount => _wireCount;
        public int ColorCount => _wireColors.Count;

        #region Helpers
        public Color GetColorById(ColorId colorId)
        {
            foreach(var c in _wireColors)
            {
                if (c.ColorId == colorId)
                    return c.Color;
            }
            return default;
        }
        #endregion
    }

    [Serializable]
    public class WireColor
    {
        public ColorId ColorId;
        public Color Color;
    }
}