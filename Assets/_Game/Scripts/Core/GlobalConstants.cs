namespace Game.Core
{
    public static class GameConstants
    {
        public static int ENTITY_MAX_STATE = 6;
        public static int ENTITY_START_STATE = 6;
        public static View START_VIEW = View.Mirror;
    }

    public enum View
    {
        Mirror, 
        Desk, 
        Behind
    }    
}