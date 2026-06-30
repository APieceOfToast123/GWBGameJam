namespace GWBGameJam
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        LevelTransition,
        Death,
        Victory
    }

    public enum DoughState
    {
        None,
        TooSoft,
        Softest,
        Medium,
        Hardest,
        TooHard
    }

    public enum IngredientType
    {
        Flour,
        Water
    }

    public enum BakingState
    {
        Idle,
        Undercooked,
        Cooked,
        Burnt
    }

    public enum ThrowResult
    {
        EmptyLane,
        Hit,
        PartialHit,
        WrongRatio,
        WrongBake
    }
}
