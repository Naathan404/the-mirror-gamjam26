using UnityEngine;

namespace Game.Minigames.Laser
{
    public enum LaserDirection
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    // '/' = Slash, '\' = Backslash
    public enum MirrorOrientation
    {
        Slash,
        Backslash
    }

    public enum LaserCellType
    {
        Empty,
        Gun,
        Bulb,
        Stone,
        Mirror
    }

    public static class LaserDirectionUtil
    {
        public static Vector2Int ToVector(LaserDirection dir)
        {
            switch (dir)
            {
                case LaserDirection.Up: return new Vector2Int(0, -1);
                case LaserDirection.Down: return new Vector2Int(0, 1);
                case LaserDirection.Left: return new Vector2Int(-1, 0);
                case LaserDirection.Right: return new Vector2Int(1, 0);
            }
            return Vector2Int.zero;
        }

        public static LaserDirection RotateClockwise(LaserDirection dir)
        {
            return (LaserDirection)(((int)dir + 1) % 4);
        }

        public static LaserDirection Opposite(LaserDirection dir)
        {
            return (LaserDirection)(((int)dir + 2) % 4);
        }

        public static LaserDirection Reflect(MirrorOrientation orientation, LaserDirection incoming)
        {
            if (orientation == MirrorOrientation.Slash) // '/'
            {
                switch (incoming)
                {
                    case LaserDirection.Right: return LaserDirection.Up;
                    case LaserDirection.Left: return LaserDirection.Down;
                    case LaserDirection.Up: return LaserDirection.Right;
                    case LaserDirection.Down: return LaserDirection.Left;
                }
            }
            else // '\'
            {
                switch (incoming)
                {
                    case LaserDirection.Right: return LaserDirection.Down;
                    case LaserDirection.Left: return LaserDirection.Up;
                    case LaserDirection.Up: return LaserDirection.Left;
                    case LaserDirection.Down: return LaserDirection.Right;
                }
            }
            return incoming;
        }

        public static MirrorOrientation? FindOrientationFor(LaserDirection incoming, LaserDirection outgoing)
        {
            if (Reflect(MirrorOrientation.Slash, incoming) == outgoing) return MirrorOrientation.Slash;
            if (Reflect(MirrorOrientation.Backslash, incoming) == outgoing) return MirrorOrientation.Backslash;
            return null;
        }
    }
}