namespace Josiwe.ATS.Cheats
{
    public class CheatConfig
    {
        public CheatConfig() { }

        public bool MoarSeasonRewards { get; set; } = false;
        public bool AllRacesInWorldMap { get; set; } = false;
        public bool EnableWildcardBlueprints { get; set; } = false;
        public bool EnableInfiniteCornerstoneRerolls { get; set; } = false;

        public float ZoomMultiplier { get; set; } = 7.0f;
        public float ResolveMultiplier { get; set; } = 1.0f;
        public float ReputationMutiplier { get; set; } = 1.0f;
        public float ImpatienceMultiplier { get; set; } = 1.0f;
        public float StormLengthMultiplier { get; set; } = 1.0f;
        public float DrizzleLengthMultiplier { get; set; } = 1.0f;
        public float ClearanceLengthMultiplier { get; set; } = 1.0f;

        public int ReputationStopgap { get; set; } = 0;
        public int ImpatienceStopgap { get; set; } = 0;

        public int BlueprintsMultiplier { get; set; } = 1;
        public int CashRewardMultiplier { get; set; } = 1;
        public int CornerstonePicksPerSeason { get; set; } = 1;

        #region Difficulty Modding
        public int P4Amount { get; set; } = 10;
        public int P12Amount { get; set; } = 1;
        public int P13Amount { get; set; } = 2;
        public int P15Amount { get; set; } = 1;
        public int P17Amount { get; set; } = 1;
        
        public float P2Amount { get; set; } = 1.0f;
        public float P5Amount { get; set; } = 1.0f;
        public float P6Amount { get; set; } = 0.5f;
        public float P7Amount { get; set; } = 0.5f;
        public float P8Amount { get; set; } = 0.5f;
        public float P9Amount { get; set; } = -0.33f;
        public float P10Amount { get; set; } = -0.5f;
        public float P14Amount { get; set; } = 0.5f;
        public float P18Amount { get; set; } = -0.35f;
        #endregion
    }
}
