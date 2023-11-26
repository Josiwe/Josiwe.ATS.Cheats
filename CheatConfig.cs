namespace Josiwe.ATS.Cheats
{
    internal class CheatConfig
    {
        public CheatConfig() { }

        public bool MoarSeasonRewards { get; set; } = true;
        public bool AllRacesInWorldMap { get; set; } = true;
        public bool EnableWildcardBlueprints { get; set; } = true;
        public bool EnableInfiniteCornerstoneRerolls { get; set; } = true;

        public float ResolveMultiplier { get; set; } = 0.0f;
        public float ReputationMutiplier { get; set; } = 0.0f;
        public float ImpatienceMultiplier { get; set; } = 1.0f;
        public float StormLengthMultiplier { get; set; } = 1.0f;
        public float DrizzleLengthMultiplier { get; set; } = 2.0f;
        public float ClearanceLengthMultiplier { get; set; } = 2.0f;

        public int ReputationStopgap { get; set; } = 1;
        public int ImpatienceStopgap { get; set; } = 1;
        public int ZoomLimitMultiplier { get; set; } = 7;
        public int BlueprintsMultiplier { get; set; } = 3;
        public int CashRewardMultiplier { get; set; } = 1000;
        public int CornerstonePicksPerSeason { get; set; } = 3;
    }
}
