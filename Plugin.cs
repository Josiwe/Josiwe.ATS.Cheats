using BepInEx;
using Eremite;
using Eremite.Controller;
using Eremite.Model;
using Eremite.Model.Effects;
using Eremite.Model.State;
using Eremite.Services;
using Eremite.Services.Meta;
using Eremite.Services.World;
using Eremite.View.Cameras;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Josiwe.ATS.Cheats
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        public static Plugin Instance;
        private static readonly CheatConfig _configuration = GetCheatConfig();

        #region Camera Controller Mods
        // Increases the zoom limit
        [HarmonyPatch(typeof(CameraController), nameof(CameraController.SetUp))]
        [HarmonyPostfix]
        public static void Setup_PostPatch(CameraController __instance)
        {
            // original code:
            // private Vector2 zoomLimit = new Vector2(-20f, -8f);
            var zoomMultiplier = 1.0f;
            if (_configuration != null)
                zoomMultiplier = _configuration.ZoomMultiplier;

            // zoomLimit is used as to define the min/max range for camera z
            // we want to increase x (the outer zoom limit) but leave y untouched
            float x = -20f * zoomMultiplier;
            float y = -8f;
            __instance.ZoomLimit = new Vector2(x, y);

            // with more zoom space to cross, we need a speedier zoom
            if (zoomMultiplier != 0)
                __instance.zoomSmoothTime /= zoomMultiplier;
        }
        #endregion

        #region World Embark Service Mods
        // Adds bonus prep points
        [HarmonyPatch(typeof(WorldEmbarkService), nameof(WorldEmbarkService.GetBonusPreparationPoints))]
        [HarmonyPrefix]
        public static bool GetBonusPreparationPoints(WorldEmbarkService __instance, ref int __result)
        {
            if (_configuration == null
                || _configuration.BonusPreparationPoints <= 0)
                return true; // run the original game method

            WriteLog($"Default embark points: {__instance.Effects.bonusPreparationPoints} - we added {_configuration.BonusPreparationPoints} more");
            __result = __instance.Effects.bonusPreparationPoints + _configuration.BonusPreparationPoints;

            return false; // do not run the original game method
        }
        #endregion

        #region Effects Service Mods
        // Modifies the length of each season
        [HarmonyPatch(typeof(EffectsService), nameof(EffectsService.GetSeasonLenghtRate))]
        [HarmonyPrefix]
        private static bool GetSeasonLenghtRate_PrePatch(EffectsService __instance, Season season, ref float __result)
        {
            if (_configuration == null)
                return true; // run the original game method

            var stormMultiplier = _configuration.StormLengthMultiplier;
            var drizzleMultiplier = _configuration.DrizzleLengthMultiplier;
            var clearanceMultiplier = _configuration.ClearanceLengthMultiplier;
            __result = season switch
            {
                Season.Drizzle => drizzleMultiplier > 0.0f
                                        ? __instance.Effects.drizzleLength * drizzleMultiplier
                                        : __instance.Effects.drizzleLength,
                Season.Clearance => clearanceMultiplier > 0.0f
                                        ? __instance.Effects.clearanceleLength * clearanceMultiplier
                                        : __instance.Effects.clearanceleLength,
                Season.Storm => stormMultiplier > 0.0f
                                       ? __instance.Effects.stormLength * stormMultiplier
                                       : __instance.Effects.stormLength,
                _ => throw new NotImplementedException(season.ToString()),
            };

            return false; // do not run the original game method
        }

        // Replaces a few of the effects upon embarking into a map cell
        [HarmonyPatch(typeof(EffectsService), nameof(EffectsService.AddDifficultyEarlyModifiers))]
        [HarmonyPrefix]
        private static bool AddDifficultyEarlyModifiers_PrePatch()
        {
            if (_configuration == null || Serviceable.ConditionsService.IsCustomGame())
                return true; // run the original game method

            foreach (AscensionModifierModel modifier in Serviceable.BiomeService.Difficulty.modifiers)
                if (modifier.isEarlyEffect)
                {
                    // listing all early effects (only 3 total)
                    // ascension altar (Pioneer), fewer blueprint options (P12) and fewer initial blueprints (P16)
                    WriteLog("--------------------");
                    WriteLog($"Early Effect: {modifier.effect.name} - {modifier.effect.GetType()}");
                    WriteLog($"{modifier.Name} - {modifier.effect.Description}");
                    WriteLog($" - Display Name Key {modifier.effect.DisplayNameKey}");
                    WriteLog($" - Amount Text {modifier.effect.GetAmountText()}");
                    WriteLog($" - Raw Amount Text {modifier.effect.GetRawAmountText()}");
                    WriteLog($" - Float Amount {modifier.effect.GetFloatAmount()}");
                    WriteLog($" - Int Amount {modifier.effect.GetIntAmount()}");
                    WriteLog("--------------------");

                    // modify the models for which we have config options
                    // always look at the log output to cast to the correct model
                    switch (modifier.effect.DisplayNameKey)
                    {
                        case "Effect_FewerBlueprintsOptions_Name":
                            if (_configuration.Prestige_12_Amount > 0.0f)
                            {
                                modifier.effect.ConsumeAsNonPerk();
                                break;
                            }
                            var p12Model = (ReputationRewardsBonusOptionsEffectModel)modifier.effect;
                            p12Model.amount = _configuration.Prestige_12_Amount; // change the amount
                            p12Model.ConsumeAsNonPerk();
                            break;
                        // nothing to change on these... yet...
                        case "Building_Altar_Name":
                        //var pioneerModel = (CompositeEffectModel)modifier.effect;
                        case "Effect_FewerInitialBlueprints_Name":
                        //var p16Model = (ExtraReputationPickEffectModel)modifier.effect;
                        default:
                            modifier.effect.ConsumeAsNonPerk();
                            break;
                    }
                }

            return false; // do not run the original game method
        }

        // Replaces most of the effects upon embarking into a map cell
        [HarmonyPatch(typeof(EffectsService), nameof(EffectsService.AddLateInitialEffects))]
        [HarmonyPrefix]
        private static bool AddLateInitialEffects_PrePatch()
        {
            if (_configuration == null)
                return true; // run the original game method

            foreach (EffectModel effect in Serviceable.Biome.effects)
                effect.Apply();
            foreach (string activeEventsEffect in Serviceable.WorldStateService.Cycle.activeEventsEffects)
                Serviceable.Settings.GetEffect(activeEventsEffect).Apply();
            foreach (string embarkEffect in Serviceable.MetaStateService.GameConditions.embarkEffects)
                Serviceable.Settings.GetEffect(embarkEffect).Apply();
            foreach (string lateEffect in Serviceable.MetaStateService.GameConditions.lateEffects)
                Serviceable.Settings.GetEffect(lateEffect).Apply();
            if (!Serviceable.ConditionsService.IsCustomGame())
                foreach (AscensionModifierModel modifier in Serviceable.BiomeService.Difficulty.modifiers)
                    if (!modifier.isEarlyEffect)
                    {
                        // listing all late effects
                        WriteLog("--------------------");
                        WriteLog($"Late Effect:  {modifier.effect.name} - {modifier.effect.GetType()}");
                        WriteLog($"{modifier.Name} - {modifier.effect.Description}");
                        WriteLog($" - Display Name Key {modifier.effect.DisplayNameKey}");
                        WriteLog($" - Amount Text {modifier.effect.GetAmountText()}");
                        WriteLog($" - Raw Amount Text {modifier.effect.GetRawAmountText()}");
                        WriteLog($" - Float Amount {modifier.effect.GetFloatAmount()}");
                        WriteLog($" - Int Amount {modifier.effect.GetIntAmount()}");
                        WriteLog("--------------------");

                        // modify the models for which we have config options
                        // always look at the log output to cast to the correct model
                        switch (modifier.effect.DisplayNameKey)
                        {
                            case "Effect_CrumblingSeal_Name":
                                if (_configuration.Prestige_2_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p2Model = (SeasonLengthEffectModel)modifier.effect;
                                p2Model.amount = _configuration.Prestige_2_Amount;
                                p2Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_HigherBlueprintsRerollCost_Name":
                                if (_configuration.Prestige_4_Amount < 0)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p4Model = (ReputationRewardsRerollCostEffectModel)modifier.effect;
                                p4Model.amount = _configuration.Prestige_4_Amount;
                                p4Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_FasterLeaving_Name":
                                if (_configuration.Prestige_5_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p5Model = (LeavingRateEffectModel)modifier.effect;
                                p5Model.amount = _configuration.Prestige_5_Amount;
                                p5Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_WetSoil_Name":
                                if (_configuration.Prestige_6_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p6Model = (ConstructionCostEffectModel)modifier.effect;
                                p6Model.amount = _configuration.Prestige_6_Amount;
                                p6Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_Parasites_Name":
                                if (_configuration.Prestige_7_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p7Model = (ChanceForExtraConsumptionEffectModel)modifier.effect;
                                p7Model.amount = _configuration.Prestige_7_Amount;
                                p7Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_HigherNeedsConsumptionRate_Name":
                                if (_configuration.Prestige_8_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p8Model = (ChanceForExtraConsumptionEffectModel)modifier.effect;
                                p8Model.amount = _configuration.Prestige_8_Amount;
                                p8Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_LongerRelicsWorkingTime_Name":
                                if (_configuration.Prestige_9_Amount > 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p9Model = (RelicsWorkingTimeRateEffectModel)modifier.effect;
                                p9Model.amount = _configuration.Prestige_9_Amount;
                                p9Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_HigherTradersPrices_Name":
                                if (_configuration.Prestige_10_Amount > 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p10Model = (TraderGlobalSellPriceEffectModel)modifier.effect;
                                p10Model.amount = _configuration.Prestige_10_Amount;
                                p10Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_FewerCornerstonesOptions_Name":
                                if (_configuration.Prestige_13_Amount < 0)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p13Model = (SeasonalRewardsBonusOptionsEffectModel)modifier.effect;
                                p13Model.amount = _configuration.Prestige_13_Amount;
                                p13Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_LowerImpatienceReduction_Name":
                                if (_configuration.Prestige_14_Amount < 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p14Model = (BonusReputationPenaltyPerReputationEffectModel)modifier.effect;
                                p14Model.amount = _configuration.Prestige_14_Amount;
                                p14Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_GlobalReputationTresholdIncrease_Name":
                                if (_configuration.Prestige_15_Amount < 0)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p15Model = (BonusGlobalReputationTresholdIncreaseEffectModel)modifier.effect;
                                p15Model.amount = _configuration.Prestige_15_Amount;
                                p15Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_AscensionHungerMultiplier_Name":
                                if (_configuration.Prestige_17_Amount < 0)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p17Model = (HungerMultiplierEffectModel)modifier.effect;
                                p17Model.amount = _configuration.Prestige_17_Amount;
                                p17Model.ConsumeAsNonPerk();
                                break;
                            case "Effect_FasterFuelSacrifice_Name":
                                if (_configuration.Prestige_18_Amount > 0.0f)
                                {
                                    modifier.effect.ConsumeAsNonPerk();
                                    break;
                                }
                                var p18Model = (HearthSacraficeTimeEffectModel)modifier.effect;
                                p18Model.amount = _configuration.Prestige_18_Amount;
                                p18Model.ConsumeAsNonPerk();
                                break;
                            // nothing to change on these... yet...
                            case "Wiki_Blightrot_Title":
                            //var viceroyModel = (CompositeEffectModel)modifier.effect;
                            case "Effect_ReputationChanges_Name":
                            //var p1_1Model = (CompositeEffectModel)modifier.effect;
                            case "Effect_HardOrdersOnly_Name":
                            //var p1_2Model = (CompositeEffectModel)modifier.effect;
                            case "Effect_CystsSpawn_Name":
                            //var p3Model = (HookedEffectModel)modifier.effect;
                            case "Effect_BlightrotMedium_Name":
                            //var p11Model = (CompositeEffectModel)modifier.effect;
                            case "Effect_ExplorationTax_Name":
                            //var p19Model = (CompositeEffectModel)modifier.effect;
                            case "Effect_AdditionalImpatienceForDeath_Name":
                            //var p20Model = (AdditionalDeathEffectModel)modifier.effect;
                            default:
                                modifier.effect.ConsumeAsNonPerk();
                                break;
                        }
                    }

            if (Serviceable.ConditionsService.IsBlightActive() && !Serviceable.TutorialService.IsAnyTutorial(Serviceable.Biome))
                foreach (EffectModel initialEffect in Serviceable.Settings.blightConfig.initialEffects)
                    initialEffect.Apply();

            return false; // do not run the original game method
        }
        #endregion

        #region Resolve Reputation Calculator Mods
        // Replaces the normal resolve logic
        [HarmonyPatch(typeof(ResolveReputationCalculator), nameof(ResolveReputationCalculator.AddReputation))]
        [HarmonyPrefix]
        private static bool AddReputation_PrePatch(ResolveReputationCalculator __instance, string race, float change)
        {
            // TODO: still can't find rep changes when a race is super hyped (i.e. blue)
            if (!Serviceable.ReputationService.IsValidReputationGain(change)
                || _configuration == null
                || _configuration.ResolveMultiplier < 0)
                return true; // run the original game method

            // if the change isn't from an order reward use the multiplier
            var newChange = change * _configuration.ResolveMultiplier;
            Serviceable.ReputationService.AddReputationPoints(newChange, ReputationChangeSource.Resolve);
            __instance.ReputationGains[race] += newChange;

            return false; // do not run the original game method
        }
        #endregion

        #region Reputation Service Mods
        // Replaces the normal reputation cap
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.GetReputationToWin))]
        [HarmonyPrefix]
        public static bool GetReputationToWin_PrePatch(ReputationService __instance, ref int __result)
        {
            if (_configuration == null
                || _configuration.MoarMaxReputation == 0)
                return true; // run the original game method

            // for debugging only (they spam the log)
            //WriteLog($"Default reputation bar: {__instance.Conditions.reputationToWin} - we added {_configuration.MoarMaxReputation} more");
            __result = __instance.Conditions.reputationToWin + _configuration.MoarMaxReputation;

            return false; // do not run the original game method
        }

        // Replaces the normal impatience cap
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.GetReputationPenaltyToLoose))]
        [HarmonyPrefix]
        public static bool GetReputationPenaltyToLoose_PrePatch(ReputationService __instance, ref int __result)
        {
            if (_configuration == null
                || _configuration.MoarMaxImpatience == 0)
                return true; // run the original game method

            // for debugging only (they spam the log)
            //WriteLog($"Default impatience bar: {__instance.Conditions.reputationPenaltyToLoose} - we added {_configuration.MoarMaxImpatience} more");
            __result = __instance.Conditions.reputationPenaltyToLoose + _configuration.MoarMaxImpatience;

            return false; // do not run the original game method
        }

        // Replaces normal reputation logic 
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.AddReputationPoints))]
        [HarmonyPrefix]
        public static bool AddReputationPoints_PrePatch(ReputationService __instance, float amount, ReputationChangeSource type, string reason = null)
        {
            if (!__instance.IsValidReputationGain(amount)
                || _configuration == null
                || (_configuration.ReputationMutiplier < 0.0f && _configuration.ReputationStopgap <= 0))
                return true; // run the original game method

            // it'd be nice if we could figure out when an archaeology dig site is available based off the buildings list
            // comparing map names doesn't always work though... dunno why...
            //var maxReputation = Serviceable.BuildingsService.Relics.Count == 0
            //    ? (float)__instance.GetReputationToWin() - _configuration.ReputationStopgap
            //    : (float)__instance.GetReputationToWin() - (_configuration.ReputationStopgap + 1);
            //WriteLog($"Current biome is: {Serviceable.BiomeService.CurrentBiome.Name}");

            var newAmount = amount * _configuration.ReputationMutiplier;
            // cap max reputation only for resolve gains - we do want players to win via map events and orders
            var maxReputation = type != ReputationChangeSource.Resolve
                                    ? (float)__instance.GetReputationToWin()
                                    : (float)__instance.GetReputationToWin() - _configuration.ReputationStopgap;
            // for debugging only (they spam the log)
            //WriteLog($"Reputation to win: {__instance.GetReputationToWin()} - Type: {type} - Stopgap: {maxReputation}");
            //WriteLog($"Reputation added is: {newAmount}. Vanilla would've been: {amount}");
            __instance.State.reputationSources[(int)type] += newAmount;
            __instance.State.reputation = Mathf.Clamp(__instance.State.reputation + newAmount, 0.0f, maxReputation);
            __instance.Reputation.Value = __instance.State.reputation;
            __instance.reputationChangedSubject.OnNext(new ReputationChange(newAmount, reason, type));
            __instance.CheckForWin();

            return false; // do not run the original game method
        }

        // Replaces normal impatience logic
        [HarmonyPatch(typeof(ReputationService), nameof(ReputationService.AddReputationPenalty))]
        [HarmonyPrefix]
        public static bool AddReputationPenalty_PrePatch(ReputationService __instance, float amount, ReputationChangeSource type, bool force, string reason = null)
        {
            if (_configuration == null
                || (_configuration.ImpatienceMultiplier < 0.0f && _configuration.ImpatienceStopgap <= 0))
                return true; // run the original game method

            var newAmount = amount * _configuration.ImpatienceMultiplier;
            var maxImpatience = __instance.GetReputationPenaltyToLoose() - _configuration.ImpatienceStopgap;
            // for debugging only (they spam the log)
            //WriteLog($"Impatience to lose: {__instance.GetReputationPenaltyToLoose()} - Type: {type} - Forced? {force} - Stopgap: {maxImpatience}");
            //WriteLog($"Impatience added is: {newAmount}. Vanilla would've been: {amount}");
            __instance.State.reputationPenalty = Mathf.Clamp(__instance.State.reputationPenalty + newAmount, 0.0f, maxImpatience);
            __instance.ReputationPenalty.Value = __instance.State.reputationPenalty;
            __instance.reputationPenaltyChangedSubject.OnNext(new ReputationChange(newAmount, reason, type));
            __instance.CheckForLoose();

            return false; // do not run the original game method
        }
        #endregion

            #region Cornerstone Service Mods
        // Grants infinite cornerstone rerolls 
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GetRerollsLeft))]
        [HarmonyPrefix]
        public static bool GetRerollsLeft_PrePatch()
        {
            if (_configuration == null
                || !_configuration.InfiniteCornerstoneRerolls)
                return true; // run the original game method

            Serviceable.StateService.Gameplay.cornerstonesRerollsLeft = 99;

            return true; // now run the original method
        }

        // Generates extra options for each cornerstone pick
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewards))]
        [HarmonyPrefix]
        private static bool GenerateRewards_PrePatch(CornerstonesService __instance, ref SeasonRewardModel model, int seed, List<EffectModel> toExclude)
        {
            if (_configuration == null
                || !_configuration.MoarSeasonRewards)
            {
                // DO NOT reset the bonus season rewards back to default LOL
                // they get reset on new maps. this was just adding another bug...
                //Serviceable.MetaStateService.Perks.bonusSeasonRewardsAmount = 0;
                return true; // run the original method
            }

            // when the model is null it's replaced with random model in the cornerstone service, this is my hacky patch
            if (model == null)
                model = Serviceable.Biome.seasons.SeasonRewards.Find(srm => srm.year == __instance.GetCurrentPick().date.year);

            var currentRewardsAmount = model.effectsTable.amounts.Random()
                + Serviceable.StateService.Effects.bonusSeasonalRewardsOptions
                + Serviceable.MetaStateService.Perks.bonusSeasonRewardsAmount;
            // 7 is the max the UI can display, so let's find the right number to add
            if (currentRewardsAmount < 7)
            {
                WriteLog($"Generating {7 - currentRewardsAmount} extra pick options for the cornerstones UI");
                Serviceable.MetaStateService.Perks.bonusSeasonRewardsAmount += 7 - currentRewardsAmount;
            }

            return true; // now run the original method
        }

        // Generates extra cornerstone picks per season
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GenerateRewardsFor))]
        [HarmonyPrefix]
        public static bool GenerateRewardsFor_PrePatch(CornerstonesService __instance, SeasonRewardModel model, string viewConfiguration, bool isExtra)
        {
            if (_configuration == null
                || _configuration.CornerstonePicksPerSeason <= 1)
                return true; // run the original method

            // debugging
            WriteLog($"Generating extra cornerstone picks for season change: {_configuration.CornerstonePicksPerSeason}");
            //WriteLog($"viewConfiguration: {viewConfiguration}");
            //WriteLog($"isExtra: {isExtra}");

            for (int i = 1; i < _configuration.CornerstonePicksPerSeason; i++)
                __instance.Picks.Add(__instance.CreatePick(model, new List<EffectModel>(), viewConfiguration, isExtra));

            return true; // now run the original method
        }

        // Replaces the list of exclusions when moar seasons rewards are wanted
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.GetAllCurrentOptions))]
        [HarmonyPrefix]
        public static bool GetAllCurrentOptions_PrePatch(ref List<EffectModel> __result)
        {
            if (_configuration == null
                || !_configuration.MoarSeasonRewards)
                return true; // run the original method

            __result = new List<EffectModel>();

            return false; // do not run the original game method
        }

        // Replaces the normal reward for declining a cornerstone
        [HarmonyPatch(typeof(CornerstonesService), nameof(CornerstonesService.RewardForDecline))]
        [HarmonyPrefix]
        private static bool RewardForDecline_PrePatch()
        {
            if (_configuration == null
                || _configuration.CashRewardMultiplier < 0)
                return true; // run the original game method

            var goods = Serviceable.Biome.seasons.seasonRewardsDeclineGood.ToGood();
            goods.amount *= _configuration.CashRewardMultiplier;
            Serviceable.StorageService.Main.Store(goods);

            return false; // do not run the original game method
        }
        #endregion

        #region Reputation Rewards Service Mods
        // Replace initial 3 building picks at the start of a game with configurable wildcard picks
        [HarmonyPatch(typeof(ReputationRewardsService), nameof(ReputationRewardsService.PrepareInitialPoints))]
        [HarmonyPrefix]
        private static bool PrepareInitialPoints_PrePatch(ReputationRewardsService __instance)
        {
            if (__instance.State.initialReputationPicksGranted
                || _configuration == null
                || !_configuration.WildcardBlueprints
                || _configuration.BlueprintsMultiplier < 0
                || MB.TutorialService.IsAnyTutorial(GameMB.Biome))
                return true; // run the original game method

            WriteLog("WildcardBlueprints: " + _configuration.WildcardBlueprints.ToString());
            WriteLog("Initial Reputation Picks Granted: " + __instance.State.initialReputationPicksGranted.ToString());

            __instance.State.initialReputationPicksGranted = true;
            WriteLog($"Generating {_configuration.BlueprintsMultiplier} wildcard blueprint picks");
            Serviceable.EffectsService.GrantWildcardPick(_configuration.BlueprintsMultiplier);
            __instance.State.lastGrantedReputationReward -= __instance.ReputationConfig.initialReputationRewards;

            return false; // do not run the original game method
        }

        // Replace normal reputation rewards (blueprint picks) with wildcard blueprint picks
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
                __instance.State.lastGrantedReputationReward = points;

            if (_configuration == null
                || !_configuration.WildcardBlueprints
                || MB.TutorialService.IsAnyTutorial(GameMB.Biome))
                return true; // run the original game method

            // copied from original function
            if (__instance.State.lastGrantedReputationReward >= points)
                return false; // do not run the original game method

            // for each regular reward we would normally collect, increment the granted tracker            
            int regularRewardsToCollect = __instance.CountRegularRewardsToCollect();

            //WriteLog("--------------------");
            //WriteLog($"Points: {points}");
            //WriteLog($"Reputation.Value: {Serviceable.ReputationService.Reputation.Value}");
            //WriteLog($"regularRewardsToCollect: {regularRewardsToCollect}");
            //WriteLog($"lastGrantedReputationReward: {__instance.State.lastGrantedReputationReward}");
            //WriteLog("--------------------");

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

        #region Caravan Generator Mods
        // Replaces the normal caravan generation logic (world map only)
        [HarmonyPatch(typeof(CaravanGenerator), nameof(CaravanGenerator.GetUniqueRevealedRaces))]
        [HarmonyPrefix]
        protected static bool GetUniqueRevealedRaces_PrePatch(CaravanGenerator __instance, List<EmbarkCaravanState> current, ref int __result)
        {
            if (_configuration == null
                || !_configuration.AllRacesInWorldMap)
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

        #region Work in progress items and other ideas
        // TODO: sooo many options in the following services:
        // - effectsService

        // TODO: Impatience gain when failing timed missions
        // Serviceable.ReputationService.AddReputationPoints(-0.99f, ReputationChangeSource.Other);

        // cannot patch GetTraderSellPriceFor cause there's 2 of them with the same name and bepinx can't distinguish between them... =(
        //public static float GetTraderSellPriceFor(string goodName, float amount)
        //public static float GetTraderSellPriceFor(GoodModel good)

        // TODO: modify order rewards
        //[HarmonyPatch(typeof(OrdersService), nameof(OrdersService.AddRewards))]
        //[HarmonyPrefix]
        //public static void AddRewards_PrePatch(OrdersService __instance, OrderState order)
        //{
        //    foreach (EffectModel reward in __instance.GetOrderModel(order).GetRewards(order))
        //    {
        //        // TODO: play around with rewards and orders lol
        //        reward.Apply();
        //    }
        //    __instance.GetOrderModel(order).reputationReward.Apply();
        //} 

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

        #region Controller Hooks -  Needed for injection into the main game
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

        [HarmonyPatch(typeof(GameController), nameof(GameController.StartGame))]
        [HarmonyPostfix]
        private static void HookEveryGameStart()
        {
            // Too difficult to predict when GameController will exist and I can hook observers to it
            // So just use Harmony and save us all some time. This method will run after every game start
            var isNewGame = MB.GameSaveService.IsNewGame();
            WriteLog($"Entered a game. Is this a new game? {isNewGame}.");

            // if config is enabled, we're not in a tutorial, and the value is not already set
            if (_configuration != null 
                && !MB.TutorialService.IsAnyTutorial(GameMB.Biome) 
                && _configuration.EnableAllBuildingsMoving
                && !Serviceable.EffectsService.IsMovingAllBuildingsEnabled())
            {
                // Enable moving all buildings
                Serviceable.EffectsService.EnableAllBuildingsMoving("josiwe");
            }   
        }
        #endregion

        #region Utilities
        private void Awake()
        {
            Instance = this;
            harmony = Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static CheatConfig GetCheatConfig()
        {
            string basePath = Directory.GetCurrentDirectory() + "\\BepInEx\\plugins\\Josiwe.ATS.Cheats.Config.json";
            // Tries to load the cheat config from json
            if (File.Exists(basePath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<CheatConfig>(File.ReadAllText(basePath));
                }
                catch (Exception ex)
                {
                    WriteLog($"Failed loading cheat config =(");
                    WriteLog(ex.Message);
                }
            }

            return null;
        }

        private static void WriteLog(string message)
        {
            Instance.Logger.LogInfo(message);
        }
        #endregion
    }
}
