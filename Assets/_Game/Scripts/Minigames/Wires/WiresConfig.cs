using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Game.Minigames.Maze
{
    [CreateAssetMenu(fileName = "WiresConfig", menuName = "Game/Minigames/Wires Config")]
    public class WiresConfig : ScriptableObject
    {
        [Header("Wire Settings")]
        [Range(4, 25)]
        [SerializeField] private int _wireCount;
        [SerializeField] private int _hiddenCount;

        [Header("Wire Color Settings")]
        [SerializeField] private List<WireColor> _wireColors;

        [Header("Visual Config")]
        [SerializeField] private int _scaleThreshold = 14;
        [Range(-0.5f, 1.5f)]
        [SerializeField] private float _scaleFactor = 0.85f;

        public Vector3 Scale => _wireCount >= _scaleThreshold ? Vector3.one * _scaleFactor : Vector3.one;

        public int WireCount => _wireCount;
        public int HiddenCount => _hiddenCount;
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