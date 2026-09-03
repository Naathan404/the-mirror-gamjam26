using System;
using UnityEngine;

namespace Game.Core
{
    public static class GameConstants
    {
        public static int ENTITY_MAX_STATE = 6;
        public static int ENTITY_START_STATE = 6;
        public static View START_VIEW = View.Mirror;

        public static string ENTITY_AI_LEVEL = "EntityAILevel";
        public static string DIFFICULT_LEVEL_PASSED = "DifficultyLevelPassed";

        public static int NUMBER_OF_MINIGAMES = 4;
    }
    
    [Serializable]
    public class KeyCode
    {
        public int Digit;
        public KeyShape Shape;
        public KeyColor KColor;

        public KeyCode(int digit, KeyShape shape, KeyColor color)
        {
            Digit = digit;
            Shape = shape;
            KColor = color;
        }

        public Color GetColor()
        {
            return KColor switch
            {
                KeyColor.Red => new Color(1f, 0, 0),
                KeyColor.Green => new Color(0f, 1f, 0f),
                KeyColor.Blue => new Color(0f, 0f, 1f),
                KeyColor.Yellow => new Color(1.0f, 1.0f, 0.0f),
                _ => new Color(1f, 1f, 1f)
            };
        }
    }

    public enum KeyShape
    {
        Square,
        Cross,
        Triangle,
        Circle
    }

    public enum KeyColor
    {
        Red,
        Blue,
        Yellow,
        Green
    }

    public enum View
    {
        Mirror, 
        Desk, 
        Behind
    }
    
    public enum MinigameType
    {
        Maze,
        CardMatch,
        Wires,
        WordSearch,
        Lazors,
        Waveform,
        Mastermind,
        Switch
    }

    public enum ColorId
    {
        Red, 
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        Pink,
        Brown,
        DarkGreen,
        White,
        Gray,
        Teal,
        Cyan,
        Navy,
        Maroon,
        Olive,
        Gold,
        Coral,
        LightPurple,
        LightPink,
        BrownWood,
        BrownPepper
    }
}