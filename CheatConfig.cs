namespace Josiwe.ATS.Cheats
{
    public class CheatConfig
    {
        public CheatConfig() { }

        #region World Map
        public float ZoomMultiplier { get; set; } = 7.0f;
        public bool AllRacesInWorldMap { get; set; } = false;
        public int BonusPreparationPoints { get; set; } = 0;
        #endregion


        #region Game Map
        public bool WildcardBlueprints { get; set; } = false;
        public bool InfiniteCornerstoneRerolls { get; set; } = false;
        public float ResolveMultiplier { get; set; } = 1.0f;
        public float ReputationMutiplier { get; set; } = 1.0f;
        public float ImpatienceMultiplier { get; set; } = 1.0f;
        public int ReputationStopgap { get; set; } = 0;
        public int ImpatienceStopgap { get; set; } = 0;
        public int MoarMaxReputation { get; set; } = 0;
        public int MoarMaxImpatience { get; set; } = 0;
        public int BlueprintsMultiplier { get; set; } = 1;
        public int CashRewardMultiplier { get; set; } = 1;
        public int CornerstonePicksPerSeason { get; set; } = 1;
        public bool EnableAllBuildingsMoving { get; set; } = false;
        #endregion

        #region Season Rewards
        public bool MoarSeasonRewards { get; set; } = false;
        public float StormLengthMultiplier { get; set; } = 1.0f;
        public float DrizzleLengthMultiplier { get; set; } = 1.0f;
        public float ClearanceLengthMultiplier { get; set; } = 1.0f;
        #endregion

        // Each description contains hints (plucked from the game's code) about the defaul values.
        #region Prestige Difficulties
        // Longer Storm - One of the seals is loosening its grip, leaking darkness upon this land. Storm season lasts 100% longer.
        public float Prestige_2_Amount { get; set; } = 1.0f;
        // Higher Blueprints Reroll Cost - The Archivist assigned to your settlement is fiercely loyal to the Royal Court, so bribing him will be more expensive. Blueprint rerolls cost 10 Amber more.
        public int Prestige_4_Amount { get; set; } = 10;
        // Faster Leaving - Villagers are less understanding than they used to be. They’re probably getting a bit spoiled by now. Villagers are 100% faster to leave if they have low Resolve.
        public float Prestige_5_Amount { get; set; } = 1.0f;
        // Wet Soil - It's particularly hard to build anything in this region. Buildings require 50% more materials.
        public float Prestige_6_Amount { get; set; } = 0.5f;
        // Parasites - One of the villagers was sick, and infected the rest of the settlement with a parasite. All villagers have a 50% chance of eating twice as much during their break.
        public float Prestige_7_Amount { get; set; } = 0.5f;
        // Higher Needs Consumption Rate - Villagers have forgotten what a modest life looks like. They want to enjoy life to the fullest. Villagers have a 50% chance to consume double the amount of luxury goods.
        public float Prestige_8_Amount { get; set; } = 0.5f;
        // Longer Relics Working Time - Villagers are reluctant to venture into Dangerous Glades. Scouts work 33% slower on Glade Events.
        public float Prestige_9_Amount { get; set; } = -0.33f;
        // Higher Traders Prices - Traders gossip about you doing pretty well lately. All your goods are worth 50% less to traders.
        public float Prestige_10_Amount { get; set; } = -0.5f;
        // Fewer Blueprints Options - The greedy Royal Archivist sold most of the blueprints to traders and fled the Citadel. You have 2 fewer blueprint choices.
        public int Prestige_12_Amount { get; set; } = -2;
        // Fewer Cornerstones Options - The Royal Envoy comes to you with bad news. The Queen has restricted your cornerstone choices by 2.
        public int Prestige_13_Amount { get; set; } = 2;
        // Lower Impatience Reduction - The Queen expects a lot from a viceroy of your rank. Impatience falls by 0.5 points less for every Reputation point you gain.
        public float Prestige_14_Amount { get; set; } = 0.5f;
        // Global Reputation Treshold Increase - You took a very peculiar group of settlers with you. They seem perpetually dissatisfied. The Resolve threshold at which each species starts producing Reputation increases by 1 more point for every Reputation point they generate.
        public int Prestige_15_Amount { get; set; } = 1;
        // Hunger Multiplier Effects - Famine outbreaks in your previous settlements have made the villagers particularly sensitive to food shortages. Every time villagers have nothing to eat during a break, they will gain two stacks of the Hunger effect instead of one.        
        public int Prestige_17_Amount { get; set; } = 1;
        // Faster Fuel Sacrifice - The Ancient Hearth seems to have a defect. No matter how hard the firekeeper tries, sacrificed resources are burning 35% quicker.        
        public float Prestige_18_Amount { get; set; } = -0.35f;
        #endregion
    }
}
