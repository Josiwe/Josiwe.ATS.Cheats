namespace Josiwe.ATS.Cheats
{
    internal class CheatConfig
    {
        public CheatConfig() { }

        public bool MoarSeasonRewards { get; set; } = false;
        public bool AllRacesInWorldMap { get; set; } = false;
        public bool EnableWildcardBlueprints { get; set; } = false;
        public bool EnableInfiniteCornerstoneRerolls { get; set; } = false;

        public float ResolveMultiplier { get; set; } = 1.0f;
        public float ReputationMutiplier { get; set; } = 1.0f;
        public float ImpatienceMultiplier { get; set; } = 1.0f;
        public float StormLengthMultiplier { get; set; } = 1.0f;
        public float DrizzleLengthMultiplier { get; set; } = 1.0f;
        public float ClearanceLengthMultiplier { get; set; } = 1.0f;

        public int ReputationStopgap { get; set; } = 0;
        public int ImpatienceStopgap { get; set; } = 0;

        public int ZoomLimitMultiplier { get; set; } = 7;
        public int BlueprintsMultiplier { get; set; } = 1;
        public int CashRewardMultiplier { get; set; } = 1;
        public int CornerstonePicksPerSeason { get; set; } = 1;
    }
}
