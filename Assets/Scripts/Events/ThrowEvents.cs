namespace GWBGameJam
{
    public readonly struct OnThrowStarted
    {
        public readonly int LaneIndex;
        public OnThrowStarted(int laneIndex) { LaneIndex = laneIndex; }
    }

    public readonly struct OnThrowCompleted
    {
        public readonly int LaneIndex;
        public readonly ThrowResult Result;
        public readonly int DefeatedCount;
        public OnThrowCompleted(int laneIndex, ThrowResult result, int defeatedCount = 0)
        {
            LaneIndex = laneIndex;
            Result = result;
            DefeatedCount = defeatedCount;
        }
    }
}
