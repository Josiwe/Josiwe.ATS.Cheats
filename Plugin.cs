using BepInEx;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Services;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using Eremite.View.Cameras;
using UnityEngine;
using System;
using Eremite.Model.State;
using Eremite.Services.Meta;

namespace Josiwe.ATS.Cheats
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        public static Plugin Instance;

        #region Awake
        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        } 
        #endregion

        #region Setup_PostPatch
        /// <summary>
        /// Increase zoom limit
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPatch(typeof(CameraController), nameof(CameraController.SetUp))]
        [HarmonyPostfix]
        public static void Setup_PostPatch(CameraController __instance)
        {
            // original code:
            // private Vector2 zoomLimit = new Vector2(-20f, -8f);
            var config = GetCheatConfig();

            int zoomMultiplier = 1;
            if (config != null)
            {
                zoomMultiplier = config.ZoomLimitMultiplier;
            }

            // zoomLimit is used as to define the min/max range for camera z
            // we want to increase x (the outer zoom limit) but leave y untouched
            float x = -20f * zoomMultiplier;
            float y = -8f;
            __instance.ZoomLimit = new Vector2(x, y);

            // with more zoom space to cross, we need a speedier zoom
            if (zoomMultiplier != 0)
            {
                __instance.zoomSmoothTime = __instance.zoomSmoothTime / zoomMultiplier;
            }
        }
        #endregion

        #region AddRewards_PrePatch
        ///// <summary>
        ///// TODO: modify order rewards
        ///// </summary>
        ///// <param name="__instance"></param>
        ///// <param name="order"></param>
        //[HarmonyPatch(typeof(OrdersService), nameof(OrdersService.AddRewards))]
        //[HarmonyPrefix]
        //public static void AddRewards_PrePatch(OrdersService __instance, OrderState order)
        //{
        //    //CheatConfig cheatConfig = GetCheatConfig();
        //    foreach (EffectModel reward in __instance.GetOrderModel(order).GetRewards(order))
        //    {
        //        // TODO: play around with rewards and orders lol
        //        reward.Apply();
        //    }
        //    __instance.GetOrderModel(order).reputationReward.Apply();
        //} 
        #endregion

        #region AddReputation_PrePatch
        /// <summary>
        /// Replace the normal resolve logic
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="race"></param>
        /// <param name="change"></param>
        [HarmonyPatch(typeof(ResolveReputationCalculator), nameof(ResolveReputationCalculator.AddReputation))]
        [HarmonyPrefix]
        private static void AddReputation_PrePatch(ResolveReputationCalculator __instance, string race, float change)
        {
            if (!Serviceable.ReputationService.IsValidReputationGain(change))
                return;

            CheatConfig cheatConfig = GetCheatConfig();
            // if the change isn't from an order reward use the multiplier
            // TODO: still can't find rep changes when a race is super hyped (i.e. blue)
            var newChange = change != 1 ? change * cheatConfig.ResolveMultiplier : change;

            Serviceable.ReputationService.AddReputationPoints(newChange, ReputationChangeSource.Resolve);
            __instance.ReputationGains[race] += newChange;
        }
        #endregion

        #region AddReputationPoints_PrePatch
        /// <summary>
        /// Replace normal reputation logic 
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="amount"></param>
        /// <param name="type"></param>
        /// <param name="reason"></param>
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.AddReputationPoints))]
        [HarmonyPrefix]
        public static void AddReputationPoints_PrePatch(ReputationService __instance, float amount, ReputationChangeSource type, string reason = null)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (!__instance.IsValidReputationGain(amount) || cheatConfig == null || Serviceable.BuildingsService.Seals.Count > 0)
                return;

            var biomeName = Serviceable.BiomeService.CurrentBiome.Name;
            WriteLog(biomeName);
            WriteLog(biomeName);
            WriteLog(biomeName);
            var newAmount = amount * cheatConfig.ReputationMutiplier;
            // it'd be nice if we could figure out when an archaeology dig site is available based off the buildings list
            //var maxReputation = Serviceable.BuildingsService.Relics.Count == 0
            //    ? (float)__instance.GetReputationToWin() - cheatConfig.ReputationStopgap
            //    : (float)__instance.GetReputationToWin() - cheatConfig.ReputationStopgap - 1;
            // rep stopgaps should change a bit based on events in the map, such as archaeologist ruins
            // no I don't like hardcoded strings, but it'll have to do for now...
            var maxReputation = Serviceable.BiomeService.CurrentBiome.Name != "Scarlet Orchard"
                ? (float)__instance.GetReputationToWin() - cheatConfig.ReputationStopgap
                : (float)__instance.GetReputationToWin() - cheatConfig.ReputationStopgap - 1;

            __instance.State.reputationSources[(int)type] += newAmount;
            __instance.State.reputation = Mathf.Clamp(__instance.State.reputation + newAmount, 0.0f, maxReputation);
            __instance.Reputation.Value = __instance.State.reputation;
            __instance.reputationChangedSubject.OnNext(new ReputationChange(newAmount, reason, type));
            __instance.CheckForWin();
        }
        #endregion

        #region AddReputationPenalty_PrePatch
        /// <summary>
        /// Replace normal impatience logic
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="amount"></param>
        /// <param name="type"></param>
        /// <param name="force"></param>
        /// <param name="reason"></param>
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.AddReputationPenalty))]
        [HarmonyPrefix]
        public static void AddReputationPenalty_PrePatch(ReputationService __instance, float amount, ReputationChangeSource type, bool force, string reason = null)
        {
            if (Mathf.Approximately(amount, 0.0f) || !force && __instance.IsGameFinished())
                return;

            CheatConfig cheatConfig = GetCheatConfig();

            var newAmount = amount * cheatConfig.ImpatienceMultiplier;
            var maxImpatience = (float)__instance.GetReputationPenaltyToLoose() - cheatConfig.ImpatienceStopgap;
            __instance.State.reputationPenalty = Mathf.Clamp(__instance.State.reputationPenalty + newAmount, 0.0f, maxImpatience);
            __instance.ReputationPenalty.Value = __instance.State.reputationPenalty;
            __instance.reputationPenaltyChangedSubject.OnNext(new ReputationChange(newAmount, reason, type));
            __instance.CheckForLoose();
        }
        #endregion

        #region GetRerollsLeft_PrePatch
        /// <summary>
        /// Enable infinite cornerstone rerolls 
        /// </summary>
        /// <returns></returns>
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GetRerollsLeft))]
        [HarmonyPrefix]
        public static bool GetRerollsLeft_PrePatch()
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || !cheatConfig.EnableInfiniteCornerstoneRerolls)
                return true; // run the original game method

            var biomeName = ;
            WriteLog(biomeName);
            WriteLog(biomeName);
            WriteLog(biomeName);


            Serviceable.StateService.Gameplay.cornerstonesRerollsLeft = 99;

            return true; // now run the original method
        } 
        #endregion

        #region GenerateRewards_PrePatch
        /// <summary>
        /// Generate extra options for each cornerstone pick
        /// </summary>
        /// <param name="model"></param>
        /// <param name="seed"></param>
        /// <param name="toExclude"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewards))]
        [HarmonyPrefix]
        private static bool GenerateRewards_PrePatch(CornerstonesService __instance, ref SeasonRewardModel model, int seed, List<EffectModel> toExclude)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null)
                return true; // run the original method

            // when the model is null it's replaced with random model in the cornerstone service, this is my hacky patch
            if (model == null)
                model = Serviceable.Biome.seasons.SeasonRewards.Find(srm => srm.year == __instance.GetCurrentPick().date.year);

            var currentRewardsAmount = model.effectsTable.amounts.Random() + Serviceable.StateService.Effects.bonusSeasonalRewardsOptions + Serviceable.MetaStateService.Perks.bonusSeasonRewardsAmount;
            // 7 is the max the UI can display, so let's find the right number to add
            if (cheatConfig.MoarSeasonRewards && currentRewardsAmount < 7)
            {
                //WriteLog($"Generating extra pick options for the cornerstone UI: {7 - currentRewardsAmount}");
                Serviceable.MetaStateService.Perks.bonusSeasonRewardsAmount += 7 - currentRewardsAmount;
            }

            return true; // now run the original method
        } 
        #endregion

        #region GenerateRewardsFor_PrePatch
        /// <summary>
        /// Generate extra cornerstone picks per season
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="model"></param>
        /// <param name="viewConfiguration"></param>
        /// <param name="isExtra"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewardsFor))]
        [HarmonyPrefix]
        public static bool GenerateRewardsFor_PrePatch(CornerstonesService __instance, SeasonRewardModel model, string viewConfiguration, bool isExtra)
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || cheatConfig.CornerstonePicksPerSeason <= 1)
                return true; // run the original method

            WriteLog($"Generating extra cornerstone picks for season change: {cheatConfig.CornerstonePicksPerSeason}");
            for (int i = 1; i < cheatConfig.CornerstonePicksPerSeason; i++)
                __instance.Picks.Add(__instance.CreatePick(model, new List<EffectModel>(), viewConfiguration, isExtra));

            return true; // now run the original method
        } 
        #endregion

        #region GetAllCurrentOptions_PrePatch
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GetAllCurrentOptions))]
        [HarmonyPrefix]
        public static bool GetAllCurrentOptions_PrePatch(ref List<EffectModel> __result)
        {
            __result = new List<EffectModel>();

            return false; // do not run the original game method
        }
        #endregion

        #region RewardForDecline_PrePatch
        /// <summary>
        /// Replace the normal reward for declining a cornerstone
        /// </summary>
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.RewardForDecline))]
        [HarmonyPrefix]
        private static void RewardForDecline_PrePatch()
        {
            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || cheatConfig.CashRewardMultiplier <= 1)
                return;

            var goods = Serviceable.Biome.seasons.seasonRewardsDeclineGood.ToGood();
            goods.amount *= cheatConfig.CashRewardMultiplier;
            Serviceable.StorageService.Main.Store(goods);
        }
        #endregion

        #region PrepareInitialPoints_PrePatch
        /// <summary>
        /// Replace initial 3 building picks at the start of a game with configurable wildcard picks
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(ReputationRewardsService), nameof(ReputationRewardsService.PrepareInitialPoints))]
        [HarmonyPrefix]
        private static bool PrepareInitialPoints_PrePatch(ReputationRewardsService __instance)
        {
            CheatConfig cheatConfig = GetCheatConfig();

            if (cheatConfig != null)
                WriteLog("EnableWildcardBlueprints: " + cheatConfig.EnableWildcardBlueprints.ToString());

            if (cheatConfig == null || MB.TutorialService.IsAnyTutorial(GameMB.Biome) || !cheatConfig.EnableWildcardBlueprints)
                return true; // run the original game method

            WriteLog("initialReputationPicksGranted: " + __instance.State.initialReputationPicksGranted.ToString());

            if (__instance.State.initialReputationPicksGranted == false)
            {
                __instance.State.initialReputationPicksGranted = true;
                WriteLog($"Generating {cheatConfig.BlueprintsMultiplier} wildcard blueprint picks");
                Serviceable.EffectsService.GrantWildcardPick(cheatConfig.BlueprintsMultiplier);
            }

            // test code
            // Serviceable.ReputationService.AddReputationPoints(0.99f, ReputationChangeSource.Other);
            return false; // do not run the original game method
        } 
        #endregion

        #region UpdateRegularReputationReward_PrePatch
        /// <summary>
        /// Replace normal reputation rewards (blueprint picks) with wildcard blueprint picks
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(ReputationRewardsService), nameof(ReputationRewardsService.UpdateRegularReputationReward))]
        [HarmonyPrefix]
        private static bool UpdateRegularReputationReward_PrePatch(ReputationRewardsService __instance, int points)
        {
            // lastGrantedReputationReward is getting incremented elsewhere in the code,
            // but inconsistently; I can't figure out where so for now I'm putting some
            // logic here to fix the impossible situation where a reward has been 'granted'
            // before reaching its reputation threshold. Without this, rewards would be
            // skipped as the tracking variable would (sometimes) be incremented twice.

            if (__instance.State.lastGrantedReputationReward > points)
            {
                __instance.State.lastGrantedReputationReward = points;
            }

            CheatConfig cheatConfig = GetCheatConfig();
            if (cheatConfig == null || MB.TutorialService.IsAnyTutorial(GameMB.Biome) || !cheatConfig.EnableWildcardBlueprints)
                return true; // run the original game method

            // copied from original function
            if (__instance.State.lastGrantedReputationReward >= points)
                return false; // do not run the original game method

            // for each regular reward we would normally collect, increment the granted tracker            
            int regularRewardsToCollect = __instance.CountRegularRewardsToCollect();

            //WriteLog("----------");
            //WriteLog($"Points: {points}");
            //WriteLog($"Reputation.Value: {Serviceable.ReputationService.Reputation.Value}");
            //WriteLog($"regularRewardsToCollect: {regularRewardsToCollect}");
            //WriteLog($"lastGrantedReputationReward: {__instance.State.lastGrantedReputationReward}");
            //WriteLog("----------");

            __instance.State.lastGrantedReputationReward = points;

            // now grant a wildcard pick instead
            WriteLog($"Generating new wildcard blueprint pick for reputation threshold {points}");
            for (int i = 0; i < regularRewardsToCollect; i++)
                Serviceable.EffectsService.GrantWildcardPick(1);

            // complete the rest of the original function
            __instance.UpdateWaitingRewards();
            if (__instance.State.currentPick == null)
                __instance.GenerateNewPick();

            return false; // do not run the original game method
        }
        #endregion

        #region HasRace
        //// TODO: newcomers logic rewiring
        //[HarmonyPatch(typeof(NewcomersService), nameof(NewcomersService.HasRace))]
        //[HarmonyPrefix]
        //public static bool HasRace(RaceChance chance, ref bool __result)
        //{
        //    WriteLog("Races Available:");
        //    foreach (RaceModel race in Serviceable.RacesService.Races)
        //        WriteLog($" - {race.name}");

        //    __result = true;

        //    //TODO: figure out how to enable all races on a map!
        //    WriteLog($"GameController - gameplay races count: {MB.Settings.gameplayRaces}.");
        //    if (MB.Settings.gameplayRaces < 5)
        //        MB.Settings.gameplayRaces = 5;
        //    WriteLog($"GameController - new gameplay races count: {MB.Settings.gameplayRaces}.");

        //    return false; // do not run the original game method
        //} 
        #endregion

        #region GetUniqueRevealedRaces_PrePatch
        /// <summary>
        /// Replace the normal caravan generation logic (world map only)
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="current"></param>
        /// <param name="__result"></param>
        /// <returns></returns>
        [HarmonyPatch(typeof(CaravanGenerator), nameof(CaravanGenerator.GetUniqueRevealedRaces))]
        [HarmonyPrefix]
        private static bool GetUniqueRevealedRaces_PrePatch(CaravanGenerator __instance, List<EmbarkCaravanState> current, ref int __result)
        {
            var config = GetCheatConfig();
            if (config == null || !config.AllRacesInWorldMap)
                return true; // run the original game method

            for (int index = 0; index < 10; ++index)
            {
                int revealedRaces = __instance.rng.Next(1, 5);
                if (__instance.IsUnique(revealedRaces, current))
                {
                    __result = revealedRaces;

                    return false; // do not run the original game method
                }

            }
            __result = __instance.rng.Next(1, 5);

            return false; // do not run the original game method
        } 
        #endregion

        #region HookMainControllerSetup
        /// <summary>
        /// Needed for injection into the main game
        /// </summary>
        [HarmonyPatch(typeof(MainController), nameof(MainController.OnServicesReady))]
        [HarmonyPostfix]
        private static void HookMainControllerSetup()
        {
            // This method will run after game load (Roughly on entering the main menu)
            // At this point a lot of the game's data will be available.
            // Your main entry point to access this data will be `Serviceable.Settings` or `MainController.Instance.Settings`
            Instance.Logger.LogInfo($"Performing game initialization on behalf of {PluginInfo.PLUGIN_GUID}.");
            Instance.Logger.LogInfo($"The game has loaded {MainController.Instance.Settings.effects.Length} effects.");
        } 
        #endregion

        #region HookEveryGameStart
        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart()
        {
            // Too difficult to predict when GameController will exist and I can hook observers to it
            // So just use Harmony and save us all some time. This method will run after every game start
            var isNewGame = MB.GameSaveService.IsNewGame();
            WriteLog($"Entered a game. Is this a new game? {isNewGame}.");
        } 
        #endregion

        #region GetCheatConfig
        private static CheatConfig GetCheatConfig()
        {
            CheatConfig cheatConfig = null;

            string basePath = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\Josiwe.ATS.Cheats.Config.json";
            //WriteLog($"Loading cheat config using basePath {basePath}");

            // Tries to load the cheat config from json
            if (File.Exists(basePath))
            {
                try
                {
                    string json = File.ReadAllText(basePath);
                    cheatConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<CheatConfig>(File.ReadAllText(basePath));
                }
                catch (Exception ex)
                {
                    WriteLog($"Failed loading cheat config =(");
                    WriteLog(ex.Message);
                }
            }
            else
            {
                //WriteLog("Cheat config file not found =(");
            }

            return cheatConfig;
        }
        #endregion

        #region WriteLog
        private static void WriteLog(string message)
        {
            Instance.Logger.LogInfo("Josiwe.ATS.Cheats:: " + message);
        } 
        #endregion
    }
}
