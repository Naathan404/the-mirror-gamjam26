namespace Game.Core
{
    public static class GameConstants
    {
        public static int ENTITY_MAX_STATE = 6;
        public static int ENTITY_START_STATE = 6;
        public static View START_VIEW = View.Mirror;

        public static int NUMBER_OF_MINIGAMES = 4;
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
        WordSearch
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
        Black,
        White,
        Gray,
        Teal,
        Cyan,
        Navy,
        Maroon,
        Olive,
        Gold,
        Coral
    }
}